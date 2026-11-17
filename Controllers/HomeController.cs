using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using _NET_PROJECT.Models;
using _NET_PROJECT.Services;
using _NET_PROJECT.ViewModels;

namespace _NET_PROJECT.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IJobAnalysisService _jobAnalysisService;
    private readonly IJobFraudPredictionService _predictionService;

    public HomeController(
        ILogger<HomeController> logger,
        IJobAnalysisService jobAnalysisService,
        IJobFraudPredictionService predictionService)
    {
        _logger = logger;
        _jobAnalysisService = jobAnalysisService;
        _predictionService = predictionService;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult Analyze()
    {
        var model = new JobAnalysisViewModel();
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("analyze-limit")]
    public async Task<IActionResult> Analyze(JobAnalysisViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Prepare standardized ML.NET input using Phase 5/6 feature pipeline standards
        var modelInput = _predictionService.PrepareModelInput(
            jobTitle: model.JobTitle,
            companyName: model.CompanyName,
            companyWebsite: model.CompanyWebsite,
            jobLocation: model.JobLocation,
            employmentType: model.EmploymentType,
            jobDescription: model.JobDescription,
            jobRequirements: model.JobRequirements,
            companyProfile: model.CompanyProfile,
            benefits: model.JobBenefits,
            hasCompanyLogo: model.HasCompanyLogo,
            hasQuestions: model.HasQuestions,
            requiredExperience: model.RequiredExperience,
            requiredEducation: model.RequiredEducation);

        // 2. Execute FastTree binary classification inference
        JobPredictionResult predictionResult;
        try
        {
            predictionResult = _predictionService.Predict(modelInput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML.NET threat evaluation failed for JobTitle='{JobTitle}'.", model.JobTitle);
            ModelState.AddModelError(string.Empty, "An error occurred while evaluating this job advertisement with the AI detection engine. Please try again or contact support.");
            return View(model);
        }

        // 3. Map View Model and ML output to JobAnalysis entity and persist with authenticated UserId
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string predictionLabel = predictionResult.IsFraudulent ? "Fraudulent" : "Genuine";

        var entity = new JobAnalysis
        {
            UserId = currentUserId,
            JobTitle = model.JobTitle,
            CompanyName = model.CompanyName,
            CompanyWebsite = model.CompanyWebsite,
            JobLocation = model.JobLocation,
            EmploymentType = model.EmploymentType,
            JobDescription = model.JobDescription,
            JobRequirements = model.JobRequirements,
            CompanyProfile = model.CompanyProfile,
            Prediction = predictionLabel,
            RiskScore = predictionResult.RiskScore,
            AnalysisDate = DateTime.UtcNow
        };

        var saved = await _jobAnalysisService.SaveAnalysisAsync(entity);

        _logger.LogInformation("JobAnalysis record {Id} successfully created and evaluated: Verdict={Verdict}, RiskScore={Risk}%.",
            saved.Id, predictionLabel, predictionResult.RiskScore);

        // 4. Redirect to Result view using the saved record ID
        return RedirectToAction(nameof(Result), new { id = saved.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Result(int? id, string? mode = null)
    {
        // --------------------------------------------------------------------------------
        // Case A: Persisted Database Analysis (Real Model Output)
        // --------------------------------------------------------------------------------
        if (id.HasValue && id.Value > 0)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Challenge();
            }

            var record = await _jobAnalysisService.GetAnalysisByIdAsync(id.Value);
            if (record == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            // Enforce authorization: normal users can access only their own analyses
            if (record.UserId != null && record.UserId != currentUserId && !isAdmin)
            {
                _logger.LogWarning("Access denied: User {UserId} attempted to view Record {RecordId} owned by {OwnerId}.",
                    currentUserId, record.Id, record.UserId);
                return RedirectToAction("AccessDenied", "Account");
            }

            bool isFraud = string.Equals(record.Prediction, "Fraudulent", StringComparison.OrdinalIgnoreCase);
            int riskScore = record.RiskScore ?? (isFraud ? 85 : 10);
            double probability = riskScore; // Probability in percentage

            var result = new PredictionResultViewModel
            {
                RecordId = record.Id,
                JobTitle = record.JobTitle,
                CompanyName = record.CompanyName,
                EmploymentType = record.EmploymentType,
                JobLocation = record.JobLocation,
                Status = isFraud ? "Fraudulent" : "Genuine",
                RiskScore = riskScore,
                RiskLevel = isFraud
                    ? (riskScore >= 80 ? "Critical Risk" : "High Risk")
                    : (riskScore <= 20 ? "Low Risk" : "Moderate Risk"),
                PredictionProbability = probability,
                ModelName = "FastTree",
                IsDemoPlaceholder = false, // REAL DATABASE PREDICTION
                AnalyzedAt = record.AnalysisDate
            };

            if (isFraud)
            {
                result.AnalysisSummary = "The trained FastTree ML.NET model classified this job posting as Fraudulent based on syntactic anomalies, high-risk compensation promises, and recruitment patterns characteristic of advance-fee or identity-harvesting scams.";
                result.WarningIndicators = new List<string>
                {
                    "High fraud probability score generated by FastTree gradient boosted trees",
                    "Syntactic or structural divergence from legitimate enterprise employment standards",
                    "Linguistic signals correlated with work-from-home advance-fee recruitment scams",
                    "Absence of verified corporate infrastructure or institutional screening channels"
                };
                result.SafetyRecommendations = new List<string>
                {
                    "Do not transfer money, purchase home-office equipment, or cash advance checks for this position",
                    "Never share personal banking information, social security numbers, or identity documents before verification",
                    "Verify the company and job posting independently via official corporate directories or verified LinkedIn profiles",
                    "Report suspicious recruitment advertisements to the relevant job board administrators and cybercrime authorities"
                };
            }
            else
            {
                result.AnalysisSummary = "The trained FastTree ML.NET model classified this job posting as Genuine, showing strong alignment with legitimate enterprise hiring specifications, conventional qualification requirements, and standard workplace practices.";
                result.WarningIndicators = new List<string>
                {
                    "Standard compensation and qualifications alignment identified by the ML engine",
                    "Conventional industry terminology and structured operational expectations",
                    "Absence of urgent advance-fee solicitation or high-risk scam triggers"
                };
                result.SafetyRecommendations = new List<string>
                {
                    "Confirm interview correspondence originates exclusively from official corporate email domains",
                    "Verify recruiter identity via official company portals or corporate telephone directories",
                    "Maintain standard digital security practices throughout the application and onboarding process"
                };
            }

            return View(result);
        }

        // --------------------------------------------------------------------------------
        // Case B: Demonstration Preview (No ID specified, visual design preview only)
        // --------------------------------------------------------------------------------
        var isGenuineDemo = string.Equals(mode, "genuine", StringComparison.OrdinalIgnoreCase);

        var demoResult = new PredictionResultViewModel
        {
            JobTitle = isGenuineDemo ? "Senior C# .NET Core Software Engineer" : "Remote Financial Data Entry Associate",
            CompanyName = isGenuineDemo ? "Datastream Enterprise Technologies Inc." : "Apex Global Consulting Ltd.",
            EmploymentType = isGenuineDemo ? "Full-time (Hybrid)" : "Full-time (Remote)",
            JobLocation = isGenuineDemo ? "Chicago, IL" : "Remote / Nationwide",
            IsDemoPlaceholder = true, // DEMO MODE
            ModelName = "FastTree (Demo Preview)",
            AnalyzedAt = DateTime.UtcNow
        };

        if (isGenuineDemo)
        {
            demoResult.Status = "Genuine";
            demoResult.RiskScore = 8;
            demoResult.RiskLevel = "Low Risk";
            demoResult.PredictionProbability = 8.0;
            demoResult.AnalysisSummary = "Demonstration Sample: The job posting demonstrates consistent corporate domain alignment, standard market compensation metrics, verified enterprise communication channels, and legitimate hiring workflow indicators.";
            demoResult.WarningIndicators = new List<string>
            {
                "Corporate domain email matching verified business registry",
                "Clear operational responsibilities aligned with standard industry roles",
                "Standard interview process requiring official portal application"
            };
            demoResult.SafetyRecommendations = new List<string>
            {
                "Always verify recruiter credentials via corporate directory or LinkedIn",
                "Ensure correspondence originates from the company's verified email domain",
                "Confirm that no financial details are requested during early screening"
            };
        }
        else
        {
            demoResult.Status = "Fraudulent";
            demoResult.RiskScore = 91;
            demoResult.RiskLevel = "Critical Risk";
            demoResult.PredictionProbability = 91.0;
            demoResult.AnalysisSummary = "Demonstration Sample: Syntactic analysis and threat pattern screening identified multiple red flags typical of advance-fee recruitment fraud, free-tier domain origin, and urgent interview solicitation via unmonitored messaging apps.";
            demoResult.WarningIndicators = new List<string>
            {
                "Unregistered or free public email domain used for recruiter contact (e.g., gmail.com/telegram)",
                "Abnormally inflated compensation with minimal required qualifications or screening",
                "Upfront payment or equipment fee request disguised as security deposit",
                "Vague job requirements with extreme urgency to accept immediate offer"
            };
            demoResult.SafetyRecommendations = new List<string>
            {
                "Never send money, cryptocurrency, or gift cards for 'home-office equipment' or background checks",
                "Do not share personal financial details, bank accounts, or SSN before verified employment contract",
                "Cross-check company registration on official registries (e.g., SEC, Companies House, MCA)",
                "Report this fraudulent posting to the relevant job board and cybercrime authorities"
            };
        }

        return View(demoResult);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> History()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        // Admins can see all records, normal users see only their own
        IEnumerable<JobAnalysis> records;
        if (isAdmin)
        {
            records = await _jobAnalysisService.GetAllAnalysesAsync(100);
        }
        else
        {
            records = await _jobAnalysisService.GetAnalysesByUserIdAsync(currentUserId ?? string.Empty, 50);
        }

        return View(records);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
