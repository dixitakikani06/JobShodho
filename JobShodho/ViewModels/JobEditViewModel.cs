using System.ComponentModel.DataAnnotations;

namespace JobShodho.ViewModels;

public class JobEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string RecipientEmail { get; set; } = string.Empty;

    [Required]
    public string JobDescription { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? JobUrl { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }
}
