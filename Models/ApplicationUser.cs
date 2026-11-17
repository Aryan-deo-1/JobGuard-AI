using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace _NET_PROJECT.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobAnalysis> JobAnalyses { get; set; } = new List<JobAnalysis>();
}

