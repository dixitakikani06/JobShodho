using JobShodho.Models.Enums;

namespace JobShodho.Models;

public class EmailSendLog
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }

    public string RecipientEmail { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public EmailLogStatus Status { get; set; }

    public int AttemptNumber { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ProviderMessageId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
