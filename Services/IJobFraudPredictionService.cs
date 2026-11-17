using _NET_PROJECT.MLModels;

namespace _NET_PROJECT.Services;

/// <summary>
/// Encapsulates the output of a machine learning fraud prediction.
/// </summary>
public record JobPredictionResult(
    bool IsFraudulent,
    double Probability,
    float RawScore,
    int RiskScore,
    string ModelName);

/// <summary>
/// Service contract for running ML.NET fraud inference on job advertisements.
/// </summary>
public interface IJobFraudPredictionService
{
    /// <summary>
    /// Executes the trained FastTree ML.NET model in a thread-safe manner.
    /// </summary>
    JobPredictionResult Predict(JobPostingModelInput input);

    /// <summary>
    /// Prepares a standardized model input using the exact text synthesis, HTML stripping,
    /// whitespace normalization, and categorical imputation rules established during Phase 5/6 model training.
    /// </summary>
    JobPostingModelInput PrepareModelInput(
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
        string? function = null);
}
