namespace JobShodho.Models;

public class InterviewContent
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }

    public string? JobTitle { get; set; }

    public string? Company { get; set; }

    /// <summary>JSON: {"questions":[{"question":"...","answer":"..."}]}</summary>
    public string ContentJson { get; set; } = string.Empty;

    public string? PdfPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
