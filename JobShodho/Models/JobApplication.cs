using System.ComponentModel.DataAnnotations;
using JobShodho.Models.Enums;

namespace JobShodho.Models;

public class JobApplication
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

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string? EmailSubject { get; set; }

    public string? EmailBody { get; set; }

    public EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;

    public bool IsEmailSent { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? LastEmailAttemptAt { get; set; }

    public int EmailAttemptCount { get; set; }

    [MaxLength(1000)]
    public string? LastErrorMessage { get; set; }

    public int? SelectedResumeId { get; set; }
    public Resume? SelectedResume { get; set; }

    [MaxLength(1000)]
    public string? ResumeSelectionReason { get; set; }

    public int? ResumeSelectionScore { get; set; }

    public int? InterviewContentId { get; set; }
    public InterviewContent? InterviewContent { get; set; }

    [MaxLength(500)]
    public string? InterviewContentPath { get; set; }

    public bool IsReviewed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmailSendLog> EmailSendLogs { get; set; } = new List<EmailSendLog>();
    public ICollection<AiGenerationLog> AiGenerationLogs { get; set; } = new List<AiGenerationLog>();
}
