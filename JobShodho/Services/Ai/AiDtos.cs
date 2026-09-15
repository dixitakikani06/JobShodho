using System.Text.Json.Serialization;

namespace JobShodho.Services.Ai;

public class EmailGenerationResponse
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class ResumeSelectionResponse
{
    [JsonPropertyName("resumeId")]
    public int ResumeId { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class InterviewQuestionItem
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;
}

public class InterviewGenerationResponse
{
    [JsonPropertyName("jobTitle")]
    public string JobTitle { get; set; } = string.Empty;

    [JsonPropertyName("company")]
    public string Company { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public List<InterviewQuestionItem> Questions { get; set; } = new();
}

public class JobListingItem
{
    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("jobTitle")]
    public string JobTitle { get; set; } = string.Empty;

    [JsonPropertyName("recipientEmail")]
    public string RecipientEmail { get; set; } = string.Empty;

    [JsonPropertyName("jobDescription")]
    public string JobDescription { get; set; } = string.Empty;

    [JsonPropertyName("jobUrl")]
    public string? JobUrl { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

public class JobListingGenerationResponse
{
    [JsonPropertyName("jobs")]
    public List<JobListingItem> Jobs { get; set; } = new();
}

public record AiActionResult(bool Success, string? ErrorMessage);
