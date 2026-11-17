using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _NET_PROJECT.Models;

public class JobAnalysis
{
    [Key]
    public int Id { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? CompanyWebsite { get; set; }

    [Required]
    [MaxLength(200)]
    public string JobLocation { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EmploymentType { get; set; } = string.Empty;

    [Required]
    public string JobDescription { get; set; } = string.Empty;

    public string? JobRequirements { get; set; }

    public string? CompanyProfile { get; set; }

    [MaxLength(50)]
    public string? Prediction { get; set; }

    public int? RiskScore { get; set; }

    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
}
