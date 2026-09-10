using System.ComponentModel.DataAnnotations;

namespace JobShodho.Models;

public class CandidateProfile
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public int YearsOfExperience { get; set; }

    [MaxLength(2000)]
    public string? ProfessionalSummary { get; set; }

    [MaxLength(2000)]
    public string? Skills { get; set; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; set; }

    [MaxLength(500)]
    public string? GitHubUrl { get; set; }

    [MaxLength(500)]
    public string? PortfolioUrl { get; set; }

    [MaxLength(2000)]
    public string? EmailSignature { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
