using System.ComponentModel.DataAnnotations;

namespace JobShodho.Models;

public class Resume
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string? ExtractedText { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
}
