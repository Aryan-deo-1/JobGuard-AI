using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.ML;
using _NET_PROJECT.MLModels;

namespace _NET_PROJECT.Services;

/// <summary>
/// Thread-safe ML.NET prediction service that executes the trained FastTree binary classification model.
/// Reuses a pool of PredictionEngine instances to safely serve concurrent web requests.
/// </summary>
public class JobFraudPredictionService : IJobFraudPredictionService, IDisposable
{
    private readonly ILogger<JobFraudPredictionService> _logger;
    private readonly MLContext _mlContext;
    private readonly ITransformer _trainedModel;
    private readonly ConcurrentBag<PredictionEngine<JobPostingModelInput, JobPostingPrediction>> _enginePool;
    private bool _disposed;

    public const string ArchitectureName = "FastTree (Gradient Boosted Trees)";

    public JobFraudPredictionService(IWebHostEnvironment env, ILogger<JobFraudPredictionService> logger)
    {
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
        _enginePool = new ConcurrentBag<PredictionEngine<JobPostingModelInput, JobPostingPrediction>>();

        string modelPath = Path.Combine(env.ContentRootPath, "MLModels", "JobGuardModel.zip");

        if (!File.Exists(modelPath))
        {
            _logger.LogCritical("JobGuard ML.NET model file is missing at: {ModelPath}", modelPath);
            throw new FileNotFoundException(
                $"The trained machine learning model could not be found at '{modelPath}'. Please ensure model training has been completed.", modelPath);
        }

        try
        {
            _logger.LogInformation("Loading trained ML.NET model from {ModelPath}...", modelPath);
            _trainedModel = _mlContext.Model.Load(modelPath, out var inputSchema);
            _logger.LogInformation("ML.NET model successfully loaded. Schema column count: {Count}", inputSchema.Count);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to deserialize and initialize ML.NET model from {ModelPath}.", modelPath);
            throw new InvalidOperationException($"Unable to initialize ML.NET model from '{modelPath}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public JobPredictionResult Predict(JobPostingModelInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input), "Model input cannot be null.");
        }

        PredictionEngine<JobPostingModelInput, JobPostingPrediction>? engine = null;
        try
        {
            // Acquire a thread-safe PredictionEngine from the pool or create one on demand
            if (!_enginePool.TryTake(out engine))
            {
                engine = _mlContext.Model.CreatePredictionEngine<JobPostingModelInput, JobPostingPrediction>(_trainedModel);
            }

            var prediction = engine.Predict(input);

            // Probability represents the positive class (1 = Fraudulent) likelihood
            double probability = prediction.Probability;
            bool isFraud = prediction.PredictedLabel;
            float rawScore = prediction.Score;

            // Compute standard 0-100 integer risk score
            int riskScore = (int)Math.Clamp(Math.Round(probability * 100.0), 0, 100);

            _logger.LogInformation(
                "Inference complete: Title='{Title}', PredictedLabel={Label}, Probability={Prob:P2}, RiskScore={Risk}, Score={Score:F4}",
                input.Title, isFraud ? "Fraudulent" : "Genuine", probability, riskScore, rawScore);

            return new JobPredictionResult(
                IsFraudulent: isFraud,
                Probability: probability,
                RawScore: rawScore,
                RiskScore: riskScore,
                ModelName: "FastTree");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during ML.NET model inference for job posting '{Title}'.", input.Title);
            throw;
        }
        finally
        {
            // Return engine to the pool for subsequent requests
            if (engine != null && !_disposed)
            {
                _enginePool.Add(engine);
            }
        }
    }

    /// <inheritdoc />
    public JobPostingModelInput PrepareModelInput(
        string jobTitle,
        string companyName,
        string? companyWebsite,
        string jobLocation,
        string employmentType,
        string jobDescription,
        string? jobRequirements,
        string? companyProfile,
        string? benefits = null,
        bool hasCompanyLogo = false,
        bool hasQuestions = false,
        string? requiredExperience = null,
        string? requiredEducation = null,
        string? industry = null,
        string? function = null)
    {
        // 1. Text normalization: HTML stripping, entity decoding, and whitespace collapsing
        string cleanTitle = CleanText(jobTitle);
        string cleanProfile = CleanText(companyProfile);
        string cleanDescription = CleanText(jobDescription);
        string cleanRequirements = CleanText(jobRequirements);
        string cleanBenefits = CleanText(benefits);
        string cleanLocation = CleanText(jobLocation);
        string cleanEmploymentType = CleanText(employmentType);

        // Map "Remote" to Part-time or Full-time if specified as employment type
        if (cleanEmploymentType.Equals("Remote", StringComparison.OrdinalIgnoreCase))
        {
            cleanEmploymentType = "Full-time";
        }

        // 2. Synthesize combined_text: title + " " + company_profile + " " + description + " " + requirements + " " + benefits
        // This follows the identical synthesis logic used in Phase 5 preprocessing and Phase 6 model training
        string combined = $"{cleanTitle} {cleanProfile} {cleanDescription} {cleanRequirements} {cleanBenefits}".Trim();
        combined = Regex.Replace(combined, @"\s+", " ").Trim();

        // 3. Derive binary / numeric indicators based on dataset feature definitions
        bool isRemote = cleanEmploymentType.Contains("Remote", StringComparison.OrdinalIgnoreCase) ||
                        cleanLocation.Contains("Remote", StringComparison.OrdinalIgnoreCase) ||
                        cleanDescription.Contains("work from home", StringComparison.OrdinalIgnoreCase) ||
                        cleanDescription.Contains("remote", StringComparison.OrdinalIgnoreCase);

        return new JobPostingModelInput
        {
            JobId = 0f, // Excluded from training features
            Title = cleanTitle,
            CompanyProfile = string.IsNullOrWhiteSpace(cleanProfile) ? "Unspecified" : cleanProfile,
            Description = cleanDescription,
            Requirements = string.IsNullOrWhiteSpace(cleanRequirements) ? "Unspecified" : cleanRequirements,
            Benefits = string.IsNullOrWhiteSpace(cleanBenefits) ? "Unspecified" : cleanBenefits,
            CombinedText = combined,
            Location = string.IsNullOrWhiteSpace(cleanLocation) ? "Unspecified" : cleanLocation,
            Department = "Unspecified",
            SalaryRange = "Unspecified",
            EmploymentType = string.IsNullOrWhiteSpace(cleanEmploymentType) ? "Unspecified" : cleanEmploymentType,
            RequiredExperience = string.IsNullOrWhiteSpace(requiredExperience) ? "Unspecified" : requiredExperience,
            RequiredEducation = string.IsNullOrWhiteSpace(requiredEducation) ? "Unspecified" : requiredEducation,
            Industry = string.IsNullOrWhiteSpace(industry) ? "Unspecified" : industry,
            Function = string.IsNullOrWhiteSpace(function) ? "Unspecified" : function,
            Telecommuting = isRemote ? 1f : 0f,
            HasCompanyLogo = hasCompanyLogo ? 1f : 0f,
            HasQuestions = hasQuestions ? 1f : 0f,
            Fraudulent = false // Ignored during inference
        };
    }

    private static string CleanText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Strip HTML tags
        string noHtml = Regex.Replace(input, "<.*?>", " ");
        // Decode HTML entities
        string decoded = WebUtility.HtmlDecode(noHtml);
        // Collapse whitespace characters (tabs, newlines, multiple spaces)
        string collapsed = Regex.Replace(decoded, @"\s+", " ").Trim();
        return collapsed;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        while (_enginePool.TryTake(out var engine))
        {
            engine.Dispose();
        }
    }
}
