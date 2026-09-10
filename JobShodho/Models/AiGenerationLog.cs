using JobShodho.Models.Enums;

namespace JobShodho.Models;

public class AiGenerationLog
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }

    public GenerationType GenerationType { get; set; }

    public string? Prompt { get; set; }

    public string? Response { get; set; }

    public GenerationStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
