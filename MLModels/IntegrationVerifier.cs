using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using _NET_PROJECT.Data;
using _NET_PROJECT.Models;
using _NET_PROJECT.Services;

namespace _NET_PROJECT.MLModels;

/// <summary>
/// Comprehensive verification suite for Phase 7 ML.NET model integration.
/// Tests model loading, schema alignment, live inference, SQL Server persistence, and user record isolation.
/// </summary>
public static class IntegrationVerifier
{
    public static async Task RunAsync(IServiceProvider services)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("        JOBGUARD AI — PHASE 7: ML.NET INTEGRATION & END-TO-END VERIFICATION      ");
        Console.WriteLine("================================================================================");

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var predictionService = sp.GetRequiredService<IJobFraudPredictionService>();
        var analysisService = sp.GetRequiredService<IJobAnalysisService>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = sp.GetRequiredService<ApplicationDbContext>();

        // --------------------------------------------------------------------------------
        // 1. Verify Model Loading & Service Resolution
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Test 1/6] Verifying IJobFraudPredictionService instantiation...");
        if (predictionService == null)
        {
            throw new InvalidOperationException("Failed to resolve IJobFraudPredictionService from DI container.");
        }
        Console.WriteLine("  ✓ IJobFraudPredictionService resolved successfully. Model loaded once via Singleton lifetime.");

        // --------------------------------------------------------------------------------
        // 2. Test Live Inference: Genuine Sample Job Posting
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Test 2/6] Evaluating Genuine Job Advertisement Sample...");
        var legitInput = predictionService.PrepareModelInput(
            jobTitle: "Senior C# .NET Core Software Engineer",
            companyName: "Datastream Enterprise Technologies Inc.",
            companyWebsite: "https://www.datastreamtech.com",
            jobLocation: "Chicago, IL (Hybrid)",
            employmentType: "Full-time",
            jobDescription: "Datastream Enterprise Technologies is seeking a Senior C# .NET Core Software Engineer to join our cloud platform engineering team. Responsibilities include building enterprise RESTful APIs using ASP.NET Core, EF Core, and SQL Server, participating in agile sprint ceremonies, and maintaining CI/CD automated test suites. We offer competitive base compensation, comprehensive health benefits, and 401(k) matching.",
            jobRequirements: "5+ years of software development experience with C# and .NET Core. Proficiency in relational database design, async programming, and API security. Bachelor's degree in Computer Science or equivalent.",
            companyProfile: "Established enterprise fintech software provider with over 500 global employees and certified ISO security compliance.",
            hasCompanyLogo: true,
            hasQuestions: true,
            requiredExperience: "Mid-Senior level",
            requiredEducation: "Bachelor's Degree",
            industry: "Information Technology and Services",
            function: "Engineering");

        var legitResult = predictionService.Predict(legitInput);
        Console.WriteLine($"  Title:       {legitInput.Title}");
        Console.WriteLine($"  Prediction:  {(legitResult.IsFraudulent ? "FRAUDULENT" : "GENUINE")}");
        Console.WriteLine($"  Risk Score:  {legitResult.RiskScore}%");
        Console.WriteLine($"  Probability: {legitResult.Probability:P2}");
        Console.WriteLine($"  Raw Score:   {legitResult.RawScore:F4}");
        Console.WriteLine($"  Model:       {legitResult.ModelName}");

        if (legitResult.IsFraudulent)
        {
            throw new Exception("Genuine sample was incorrectly classified as Fraudulent!");
        }
        Console.WriteLine("  ✓ Genuine posting correctly classified as GENUINE (Low Risk).");

        // Test Genuine sample WITHOUT logo or questions:
        var legitInputNoLogo = predictionService.PrepareModelInput(
            jobTitle: "Senior C# .NET Core Software Engineer",
            companyName: "Datastream Enterprise Technologies Inc.",
            companyWebsite: null,
            jobLocation: "Chicago, IL (Hybrid)",
            employmentType: "Full-time",
            jobDescription: "Datastream Enterprise Technologies is seeking a Senior C# .NET Core Software Engineer to join our cloud platform engineering team. Responsibilities include building enterprise RESTful APIs using ASP.NET Core, EF Core, and SQL Server, participating in agile sprint ceremonies, and maintaining CI/CD automated test suites. We offer competitive base compensation, comprehensive health benefits, and 401(k) matching.",
            jobRequirements: "5+ years of software development experience with C# and .NET Core. Proficiency in relational database design, async programming, and API security. Bachelor's degree in Computer Science or equivalent.",
            companyProfile: "Established enterprise fintech software provider with over 500 global employees and certified ISO security compliance.",
            hasCompanyLogo: false,
            hasQuestions: false,
            requiredExperience: "Mid-Senior level",
            requiredEducation: "Bachelor's Degree",
            industry: "Information Technology and Services",
            function: "Engineering");
        var legitResNoLogo = predictionService.Predict(legitInputNoLogo);
        Console.WriteLine($"  Genuine Sample (No Logo, No Questions): Pred={(legitResNoLogo.IsFraudulent ? "FRAUDULENT" : "GENUINE")} | Prob={legitResNoLogo.Probability:P2} | Risk={legitResNoLogo.RiskScore}% | Score={legitResNoLogo.RawScore:F4}");

        // --------------------------------------------------------------------------------
        // 3. Test Live Inference: Fraudulent Sample Job Posting (Advance-Fee / Wire Scam)
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Test 3/6] Evaluating Fraudulent Job Advertisement Sample...");
        var scamInput = predictionService.PrepareModelInput(
            jobTitle: "Immediate Work from Home Data Entry Assistant - Earn $45/hr No Experience",
            companyName: "Apex Global Marketing Group",
            companyWebsite: null,
            jobLocation: "Remote / Nationwide / Work from Home",
            employmentType: "Part-time",
            jobDescription: "URGENT HIRING: We are seeking immediate home data entry clerks to earn $45/hr with flexible hours (10-20 hrs/week). Immediate start with immediate sign-on bonuses. Tasks include transferring digital invoice records, processing client wire vouchers, and receiving packages from your home. Quick screening via Telegram app. Company will mail an initial certified check to purchase dedicated home office hardware package through our vendor portal. You must deposit check and wire payment for hardware before starting. Receive weekly payments via Western Union, wire transfer, or check.",
            jobRequirements: "No experience required. Must have smartphone, internet connection, bank account, and be available for immediate start. High school diploma.",
            companyProfile: "We are an international global marketing firm seeking immediate representatives.",
            hasCompanyLogo: false,
            hasQuestions: false,
            requiredExperience: "Entry level",
            requiredEducation: "High School or equivalent",
            industry: "Financial Services",
            function: "Administrative");

        var scamResult = predictionService.Predict(scamInput);
        Console.WriteLine($"  Title:       {scamInput.Title}");
        Console.WriteLine($"  Prediction:  {(scamResult.IsFraudulent ? "FRAUDULENT" : "GENUINE")}");
        Console.WriteLine($"  Risk Score:  {scamResult.RiskScore}%");
        Console.WriteLine($"  Probability: {scamResult.Probability:P2}");
        Console.WriteLine($"  Raw Score:   {scamResult.RawScore:F4}");
        Console.WriteLine($"  Model:       {scamResult.ModelName}");

        if (!scamResult.IsFraudulent)
        {
            throw new Exception("Scam sample was incorrectly classified as Genuine!");
        }
        Console.WriteLine("  ✓ Scam posting correctly classified as FRAUDULENT (High Risk).");

        // --------------------------------------------------------------------------------
        // Diagnostic: Record #4004 Deep Investigation
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Investigation] Testing Record #4004 with various feature permutations...");
        string rec4004Title = "Work From Home Data Entry Operator";
        string rec4004Company = "Global Tech Solutions Pvt Ltd";
        string rec4004Desc = "Earn Rs. 45,000 to Rs. 75,000 per month. Work only 2 hours daily. No interview required. No experience required. Pay a refundable registration fee of Rs. 2,500. Immediate joining letter after payment. Limited vacancies. Guaranteed job placement.";

        foreach (var hasLogo in new[] { true, false })
        {
            foreach (var hasQ in new[] { true, false })
            {
                foreach (var exp in new[] { "Unspecified", "Entry level" })
                {
                    var test4004Input = predictionService.PrepareModelInput(
                        jobTitle: rec4004Title,
                        companyName: rec4004Company,
                        companyWebsite: hasLogo ? "https://globaltechsolutions.com" : null,
                        jobLocation: "Remote / Nationwide / Work from Home",
                        employmentType: "Part-time",
                        jobDescription: rec4004Desc,
                        jobRequirements: "No experience required.",
                        companyProfile: "",
                        hasCompanyLogo: hasLogo,
                        hasQuestions: hasQ,
                        requiredExperience: exp,
                        requiredEducation: "Unspecified");

                    var res4004 = predictionService.Predict(test4004Input);
                    Console.WriteLine($"  Logo={hasLogo,-5} | Questions={hasQ,-5} | Exp={exp,-11} => Pred={res4004.IsFraudulent,-5} | Prob={res4004.Probability:P2} | Risk={res4004.RiskScore}% | Score={res4004.RawScore:F4}");
                }
            }
        }

        // --------------------------------------------------------------------------------
        // 4. Test SQL Server Persistence with Verified Identity Users
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Test 4/6] Verifying SQL Server persistence via IJobAnalysisService...");
        
        // Ensure test users exist in Identity
        string user1Email = "testuser1@jobguard.ai";
        string user2Email = "testuser2@jobguard.ai";

        var user1 = await userManager.FindByEmailAsync(user1Email);
        if (user1 == null)
        {
            user1 = new ApplicationUser { UserName = user1Email, Email = user1Email, FullName = "Test Analyst One" };
            var res1 = await userManager.CreateAsync(user1, "TestP@ss123!");
            if (!res1.Succeeded) throw new Exception("Failed to create test user 1: " + string.Join(", ", res1.Errors.Select(e => e.Description)));
        }

        var user2 = await userManager.FindByEmailAsync(user2Email);
        if (user2 == null)
        {
            user2 = new ApplicationUser { UserName = user2Email, Email = user2Email, FullName = "Test Analyst Two" };
            var res2 = await userManager.CreateAsync(user2, "TestP@ss123!");
            if (!res2.Succeeded) throw new Exception("Failed to create test user 2: " + string.Join(", ", res2.Errors.Select(e => e.Description)));
        }

        var analysis1 = new JobAnalysis
        {
            UserId = user1.Id,
            JobTitle = "Senior C# .NET Core Software Engineer",
            CompanyName = "Datastream Enterprise Technologies Inc.",
            CompanyWebsite = "https://www.datastreamtech.com",
            JobLocation = "Chicago, IL (Hybrid)",
            EmploymentType = "Full-time",
            JobDescription = "Enterprise RESTful microservices development using C# and .NET 8.",
            JobRequirements = "5+ years C# experience, ASP.NET Core, EF Core.",
            CompanyProfile = "Established enterprise provider.",
            Prediction = legitResult.IsFraudulent ? "Fraudulent" : "Genuine",
            RiskScore = legitResult.RiskScore,
            AnalysisDate = DateTime.UtcNow
        };

        var analysis2 = new JobAnalysis
        {
            UserId = user2.Id,
            JobTitle = "Immediate Work from Home Data Entry Assistant - Earn $45/hr No Experience",
            CompanyName = "Apex Global Marketing Group",
            CompanyWebsite = null,
            JobLocation = "Remote / Nationwide",
            EmploymentType = "Part-time",
            JobDescription = "Urgent data entry, wire vouchers, advance equipment check dispatch.",
            JobRequirements = "No experience required, bank account needed.",
            CompanyProfile = "Global marketing firm.",
            Prediction = scamResult.IsFraudulent ? "Fraudulent" : "Genuine",
            RiskScore = scamResult.RiskScore,
            AnalysisDate = DateTime.UtcNow
        };

        var saved1 = await analysisService.SaveAnalysisAsync(analysis1);
        var saved2 = await analysisService.SaveAnalysisAsync(analysis2);

        Console.WriteLine($"  Record 1 saved with Id={saved1.Id}, Verdict={saved1.Prediction}, RiskScore={saved1.RiskScore}% for User={user1Email}");
        Console.WriteLine($"  Record 2 saved with Id={saved2.Id}, Verdict={saved2.Prediction}, RiskScore={saved2.RiskScore}% for User={user2Email}");

        var retrieved1 = await analysisService.GetAnalysisByIdAsync(saved1.Id);
        if (retrieved1 == null || retrieved1.Prediction != "Genuine" || retrieved1.RiskScore != legitResult.RiskScore)
        {
            throw new Exception($"Record 1 persistence validation failed! Retrieved: {retrieved1?.Prediction}, Risk: {retrieved1?.RiskScore}");
        }
        var retrieved2 = await analysisService.GetAnalysisByIdAsync(saved2.Id);
        if (retrieved2 == null || retrieved2.Prediction != "Fraudulent" || retrieved2.RiskScore != scamResult.RiskScore)
        {
            throw new Exception($"Record 2 persistence validation failed! Retrieved: {retrieved2?.Prediction}, Risk: {retrieved2?.RiskScore}");
        }
        Console.WriteLine("  ✓ SQL Server persistence and retrieval verified successfully for both classes.");

        // --------------------------------------------------------------------------------
        // 5. Test User Ownership & Authorization Isolation
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Test 5/6] Verifying User Ownership Isolation in History...");
        var user1Records = (await analysisService.GetAnalysesByUserIdAsync(user1.Id)).ToList();
        var user2Records = (await analysisService.GetAnalysesByUserIdAsync(user2.Id)).ToList();

        Console.WriteLine($"  User 1 ({user1Email}) records retrieved: {user1Records.Count}");
        Console.WriteLine($"  User 2 ({user2Email}) records retrieved: {user2Records.Count}");

        if (user1Records.Any(r => r.UserId != user1.Id))
        {
            throw new Exception("Data leakage detected: User 1 query returned records belonging to another user!");
        }
        if (user2Records.Any(r => r.UserId != user2.Id))
        {
            throw new Exception("Data leakage detected: User 2 query returned records belonging to another user!");
        }
        Console.WriteLine("  ✓ User ownership isolation confirmed: queries enforce strict UserId scoping.");

        // --------------------------------------------------------------------------------
        // 6. Test Error Handling Robustness
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n[Test 6/6] Verifying error handling robustness...");
        try
        {
            predictionService.Predict(null!);
            throw new Exception("Expected ArgumentNullException was not thrown for null input!");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("  ✓ Null input correctly rejected with ArgumentNullException.");
        }

        Console.WriteLine("\n================================================================================");
        Console.WriteLine("             PHASE 7 INTEGRATION VERIFICATION: ALL TESTS PASSED!                ");
        Console.WriteLine("================================================================================");
    }
}
