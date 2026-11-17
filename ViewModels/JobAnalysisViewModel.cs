using System.ComponentModel.DataAnnotations;

namespace _NET_PROJECT.ViewModels;

public class JobAnalysisViewModel
{
    [Required(ErrorMessage = "Job Title is required.")]
    [Display(Name = "Job Title")]
    [StringLength(150, ErrorMessage = "Job Title cannot exceed 150 characters.")]
    public string JobTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company Name is required.")]
    [Display(Name = "Company Name")]
    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [Display(Name = "Company Website (Optional)")]
    [Url(ErrorMessage = "Please enter a valid URL (e.g., https://example.com).")]
    public string? CompanyWebsite { get; set; }

    [Required(ErrorMessage = "Job Location is required.")]
    [Display(Name = "Job Location")]
    [StringLength(100, ErrorMessage = "Job Location cannot exceed 100 characters.")]
    public string JobLocation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an Employment Type.")]
    [Display(Name = "Employment Type")]
    public string EmploymentType { get; set; } = "Full-time";

    [Required(ErrorMessage = "Job Description is required.")]
    [Display(Name = "Job Description")]
    [MinLength(30, ErrorMessage = "Job description should be at least 30 characters for meaningful analysis.")]
    public string JobDescription { get; set; } = string.Empty;

    [Display(Name = "Job Requirements (Optional)")]
    public string? JobRequirements { get; set; }

    [Display(Name = "Job Benefits & Perks (Optional)")]
    public string? JobBenefits { get; set; }

    [Display(Name = "Company Profile (Optional)")]
    public string? CompanyProfile { get; set; }

    [Display(Name = "Advertisement Features Official Company Logo")]
    public bool HasCompanyLogo { get; set; } = false;

    [Display(Name = "Application Involves Screening Questions")]
    public bool HasQuestions { get; set; } = false;

    [Display(Name = "Required Experience (Optional)")]
    public string? RequiredExperience { get; set; }

    [Display(Name = "Required Education (Optional)")]
    public string? RequiredEducation { get; set; }
}
