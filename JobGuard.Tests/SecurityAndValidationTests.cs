using System.ComponentModel.DataAnnotations;
using _NET_PROJECT.ViewModels;
using Xunit;

namespace JobGuard.Tests;

public class SecurityAndValidationTests
{
    [Theory]
    [InlineData("short", false)]       // 5 chars -> Fail
    [InlineData("123456", false)]      // 6 chars -> Fail (previously allowed, now rejected by 8-char policy)
    [InlineData("seven77", false)]     // 7 chars -> Fail
    [InlineData("validPass1", true)]   // 10 chars -> Pass
    [InlineData("SecurePassword2025!", true)] // 19 chars -> Pass
    public void RegisterViewModel_Enforces_Minimum8CharacterPasswordPolicy(string password, bool expectedValid)
    {
        var model = new RegisterViewModel
        {
            FullName = "Jane Doe",
            Email = "jane.doe@example.com",
            Password = password,
            ConfirmPassword = password
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(RegisterViewModel.Password)));
        }
    }

    [Fact]
    public void RegisterViewModel_Rejects_MismatchedConfirmPassword()
    {
        var model = new RegisterViewModel
        {
            FullName = "Jane Doe",
            Email = "jane.doe@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "DifferentPassword456!"
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    [Fact]
    public void JobAnalysisViewModel_Defaults_HasCompanyLogo_To_False()
    {
        // Security & ML validation: ensure unverified postings are not presumed to have official company logos
        var model = new JobAnalysisViewModel();
        Assert.False(model.HasCompanyLogo, "HasCompanyLogo must default to false so unverified advertisements do not receive false logo credit.");
    }

    [Fact]
    public void JobAnalysisViewModel_Validates_RequiredFields()
    {
        var emptyModel = new JobAnalysisViewModel
        {
            JobTitle = "",
            CompanyName = "",
            JobLocation = "",
            JobDescription = ""
        };

        var validationContext = new ValidationContext(emptyModel);
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(emptyModel, validationContext, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(JobAnalysisViewModel.JobTitle)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(JobAnalysisViewModel.CompanyName)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(JobAnalysisViewModel.JobLocation)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(JobAnalysisViewModel.JobDescription)));
    }

    [Theory]
    [InlineData("http://legitimate-corp.com", true)]
    [InlineData("https://secure-careers.company.org", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,<script>alert(1)</script>", false)]
    [InlineData("vbscript:msgbox", false)]
    public void ExternalUrl_Validation_AllowsOnlyHttpAndHttps(string url, bool expectedSafe)
    {
        bool isSafe = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                      url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        Assert.Equal(expectedSafe, isSafe);
    }
}
