using _NET_PROJECT.MLModels;
using _NET_PROJECT.Services;
using Xunit;

namespace JobGuard.Tests;

public class PredictionServiceTests
{
    private readonly IJobFraudPredictionService _predictionService;

    public PredictionServiceTests()
    {
        _predictionService = TestHelper.CreatePredictionService();
    }

    [Fact]
    public void Model_Loads_Successfully_And_Resolves()
    {
        Assert.NotNull(_predictionService);
    }

    [Fact]
    public void Predict_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _predictionService.Predict(null!));
    }

    [Fact]
    public void Predict_Produces_ConsistentOutput_AcrossRepeatedExecutions()
    {
        var input = _predictionService.PrepareModelInput(
            jobTitle: "Cloud Solutions Architect",
            companyName: "Enterprise Cloud Dynamics Ltd.",
            companyWebsite: "https://cloud-dynamics.com",
            jobLocation: "Austin, TX",
            employmentType: "Full-time",
            jobDescription: "Enterprise Cloud Dynamics is hiring a Cloud Architect to design multi-tenant AWS and Azure environments. 7+ years cloud infrastructure experience required. Full benefits, medical, dental, 401(k).",
            jobRequirements: "AWS Certified Solutions Architect, Bachelor's in CS, C# and Terraform proficiency.",
            companyProfile: "Global cloud technology consultancy founded in 2011 with 1200 employees.",
            benefits: "Comprehensive medical, 401k match, 25 days paid time off.",
            hasCompanyLogo: true,
            hasQuestions: true,
            requiredExperience: "Mid-Senior level",
            requiredEducation: "Bachelor's Degree",
            industry: "Information Technology and Services",
            function: "Engineering");

        var result1 = _predictionService.Predict(input);
        var result2 = _predictionService.Predict(input);
        var result3 = _predictionService.Predict(input);

        Assert.Equal(result1.IsFraudulent, result2.IsFraudulent);
        Assert.Equal(result1.RiskScore, result2.RiskScore);
        Assert.Equal(result1.Probability, result2.Probability, precision: 4);
        Assert.Equal(result1.RawScore, result2.RawScore, precision: 4);
        Assert.Equal(result2.IsFraudulent, result3.IsFraudulent);
        Assert.Equal(result2.RiskScore, result3.RiskScore);
    }

    [Fact]
    public void RiskScore_IsAlways_Bounded_Between_0_And_100()
    {
        var input = _predictionService.PrepareModelInput(
            jobTitle: "Junior Operations Associate",
            companyName: "Alpha Logistics",
            companyWebsite: null,
            jobLocation: "Remote",
            employmentType: "Part-time",
            jobDescription: "General administrative assistance and filing.",
            jobRequirements: null,
            companyProfile: null);

        var result = _predictionService.Predict(input);

        Assert.InRange(result.RiskScore, 0, 100);
        Assert.InRange(result.Probability, 0.0, 1.0);
    }

    [Fact]
    public void PrepareModelInput_CleansHtml_And_SynthesizesCombinedText_WithBenefits()
    {
        var input = _predictionService.PrepareModelInput(
            jobTitle: "<b>Senior Developer</b>",
            companyName: "Acme Corp &amp; Partners",
            companyWebsite: "https://acme.com",
            jobLocation: "New York, NY",
            employmentType: "Full-time",
            jobDescription: "<p>We are hiring a <strong>C# developer</strong> for our core product.</p>",
            jobRequirements: "<ul><li>5+ years experience</li><li>ASP.NET Core</li></ul>",
            companyProfile: "<div>Enterprise software vendor.</div>",
            benefits: "Competitive salary, <i>healthcare &amp; 401(k)</i>.",
            hasCompanyLogo: true,
            hasQuestions: false);

        // Verify HTML tags were stripped and entities unescaped
        Assert.DoesNotContain("<b>", input.Title);
        Assert.DoesNotContain("</b>", input.Title);
        Assert.Equal("Senior Developer", input.Title);

        Assert.DoesNotContain("<p>", input.Description);
        Assert.DoesNotContain("<strong>", input.Description);
        Assert.Contains("C# developer", input.Description);

        // Verify combined text includes benefits and is cleanly normalized
        Assert.Contains("healthcare & 401(k)", input.CombinedText);
        Assert.Contains("Senior Developer", input.CombinedText);
        Assert.Contains("Enterprise software vendor", input.CombinedText);
        Assert.DoesNotContain("  ", input.CombinedText!); // No double spaces
    }

    [Fact]
    public async Task PredictionEnginePool_Handles_ConcurrentRequests_Safely()
    {
        // Concurrency test: execute 20 concurrent predictions across separate thread pool tasks
        const int concurrentCount = 20;
        var tasks = new Task<JobPredictionResult>[concurrentCount];

        for (int i = 0; i < concurrentCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                bool isScam = index % 2 == 0;
                var input = _predictionService.PrepareModelInput(
                    jobTitle: isScam ? $"Urgent Data Clerk #{index}" : $"DevOps Engineer #{index}",
                    companyName: isScam ? "Quick Cash Ltd." : "Stripe Payments Corp.",
                    companyWebsite: isScam ? null : "https://stripe.com",
                    jobLocation: "Remote",
                    employmentType: isScam ? "Part-time" : "Full-time",
                    jobDescription: isScam
                        ? "Deposit advance cashier checks and wire money via Western Union. Urgent hiring no experience."
                        : "Maintain Kubernetes clusters, terraform IaC, and observability pipelines. 4+ years required.",
                    jobRequirements: isScam ? "No experience required." : "Kubernetes, Docker, Go, Python.",
                    companyProfile: isScam ? "" : "Global financial technology infrastructure provider.",
                    hasCompanyLogo: !isScam,
                    hasQuestions: !isScam);

                return _predictionService.Predict(input);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.Equal(concurrentCount, results.Length);
        foreach (var r in results)
        {
            Assert.NotNull(r);
            Assert.InRange(r.RiskScore, 0, 100);
            Assert.Equal("FastTree", r.ModelName);
        }
    }
}
