using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Models.Enums;
using JobShodho.Options;
using JobShodho.Services.Ai;
using JobShodho.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobShodho.Services;

public class AiService : IAiService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICandidateProfileService _candidateProfileService;
    private readonly OpenAiOptions _openAiOptions;
    private readonly CandidateOptions _candidateOptions;
    private readonly ILogger<AiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        ICandidateProfileService candidateProfileService,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<CandidateOptions> candidateOptions,
        ILogger<AiService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _candidateProfileService = candidateProfileService;
        _openAiOptions = openAiOptions.Value;
        _candidateOptions = candidateOptions.Value;
        _logger = logger;
    }

    public async Task<AiActionResult> GenerateEmailAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var job = await _db.JobApplications.Include(j => j.SelectedResume).FirstOrDefaultAsync(j => j.Id == jobApplicationId, cancellationToken);
        if (job is null)
        {
            return new AiActionResult(false, "Job application not found.");
        }

        if (!_openAiOptions.IsConfigured)
        {
            return new AiActionResult(false, "OpenAI is not configured. Please set an API key (Settings or User Secrets) before generating content.");
        }

        var profile = await _candidateProfileService.GetProfileAsync(cancellationToken);
        var (system, user) = PromptBuilder.BuildEmailPrompt(job, profile, _candidateOptions.TechnologyBackground, job.SelectedResume);

        job.EmailStatus = EmailStatus.Generating;
        await _db.SaveChangesAsync(cancellationToken);

        var (raw, error) = await CallOpenAiAsync(system, user, cancellationToken);
        if (error is not null)
        {
            job.EmailStatus = EmailStatus.Pending;
            job.LastErrorMessage = error;
            await _db.SaveChangesAsync(cancellationToken);
            await LogAsync(jobApplicationId, GenerationType.Email, user, raw, false, error, cancellationToken);
            return new AiActionResult(false, error);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<EmailGenerationResponse>(raw!, JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Subject) || string.IsNullOrWhiteSpace(parsed.Body))
            {
                throw new JsonException("AI response did not contain subject/body.");
            }

            job.EmailSubject = parsed.Subject.Trim();
            job.EmailBody = parsed.Body.Trim();
            job.EmailStatus = EmailStatus.ReadyForReview;
            job.LastErrorMessage = null;
            await _db.SaveChangesAsync(cancellationToken);

            await LogAsync(jobApplicationId, GenerationType.Email, user, raw, true, null, cancellationToken);
            return new AiActionResult(true, null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI email response for job {JobId}", jobApplicationId);
            job.EmailStatus = EmailStatus.Pending;
            job.LastErrorMessage = "Unable to generate the email. Please try again.";
            await _db.SaveChangesAsync(cancellationToken);
            await LogAsync(jobApplicationId, GenerationType.Email, user, raw, false, ex.Message, cancellationToken);
            return new AiActionResult(false, "Unable to generate the email. Please try again.");
        }
    }

    public async Task<AiActionResult> SelectResumeAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var job = await _db.JobApplications.FirstOrDefaultAsync(j => j.Id == jobApplicationId, cancellationToken);
        if (job is null)
        {
            return new AiActionResult(false, "Job application not found.");
        }

        if (!_openAiOptions.IsConfigured)
        {
            return new AiActionResult(false, "OpenAI is not configured. Please set an API key (Settings or User Secrets) before generating content.");
        }

        var resumes = await _db.Resumes.Where(r => r.IsActive).ToListAsync(cancellationToken);
        if (resumes.Count == 0)
        {
            return new AiActionResult(false, "No active resumes available to select from. Upload a resume first.");
        }

        var (system, user) = PromptBuilder.BuildResumeSelectionPrompt(job, resumes);
        var (raw, error) = await CallOpenAiAsync(system, user, cancellationToken);
        if (error is not null)
        {
            await LogAsync(jobApplicationId, GenerationType.ResumeSelection, user, raw, false, error, cancellationToken);
            return new AiActionResult(false, error);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ResumeSelectionResponse>(raw!, JsonOptions);
            if (parsed is null || resumes.All(r => r.Id != parsed.ResumeId))
            {
                throw new JsonException("AI response did not reference a valid resume id.");
            }

            job.SelectedResumeId = parsed.ResumeId;
            job.ResumeSelectionScore = parsed.Score;
            job.ResumeSelectionReason = parsed.Reason;
            await _db.SaveChangesAsync(cancellationToken);

            await LogAsync(jobApplicationId, GenerationType.ResumeSelection, user, raw, true, null, cancellationToken);
            return new AiActionResult(true, null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI resume-selection response for job {JobId}", jobApplicationId);
            await LogAsync(jobApplicationId, GenerationType.ResumeSelection, user, raw, false, ex.Message, cancellationToken);
            return new AiActionResult(false, "Unable to select a resume. Please try again.");
        }
    }

    public async Task<AiActionResult> GenerateInterviewQuestionsAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var job = await _db.JobApplications
            .Include(j => j.SelectedResume)
            .Include(j => j.InterviewContent)
            .FirstOrDefaultAsync(j => j.Id == jobApplicationId, cancellationToken);
        if (job is null)
        {
            return new AiActionResult(false, "Job application not found.");
        }

        if (!_openAiOptions.IsConfigured)
        {
            return new AiActionResult(false, "OpenAI is not configured. Please set an API key (Settings or User Secrets) before generating content.");
        }

        var profile = await _candidateProfileService.GetProfileAsync(cancellationToken);
        var (system, user) = PromptBuilder.BuildInterviewPrompt(job, profile, _candidateOptions.TechnologyBackground, job.SelectedResume);
        var (raw, error) = await CallOpenAiAsync(system, user, cancellationToken);
        if (error is not null)
        {
            await LogAsync(jobApplicationId, GenerationType.InterviewQuestions, user, raw, false, error, cancellationToken);
            return new AiActionResult(false, error);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<InterviewGenerationResponse>(raw!, JsonOptions);
            if (parsed is null || parsed.Questions.Count == 0)
            {
                throw new JsonException("AI response did not contain any questions.");
            }

            if (job.InterviewContent is null)
            {
                var content = new InterviewContent
                {
                    JobApplicationId = job.Id,
                    JobTitle = job.JobTitle,
                    Company = job.CompanyName,
                    ContentJson = raw!,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.InterviewContents.Add(content);
                await _db.SaveChangesAsync(cancellationToken);
                job.InterviewContentId = content.Id;
            }
            else
            {
                job.InterviewContent.ContentJson = raw!;
                job.InterviewContent.JobTitle = job.JobTitle;
                job.InterviewContent.Company = job.CompanyName;
                job.InterviewContent.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);

            await LogAsync(jobApplicationId, GenerationType.InterviewQuestions, user, raw, true, null, cancellationToken);
            return new AiActionResult(true, null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI interview response for job {JobId}", jobApplicationId);
            await LogAsync(jobApplicationId, GenerationType.InterviewQuestions, user, raw, false, ex.Message, cancellationToken);
            return new AiActionResult(false, "Unable to generate interview questions. Please try again.");
        }
    }

    public async Task<AiActionResult> GenerateAllAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var resumeResult = await SelectResumeAsync(jobApplicationId, cancellationToken);
        var emailResult = await GenerateEmailAsync(jobApplicationId, cancellationToken);
        var interviewResult = await GenerateInterviewQuestionsAsync(jobApplicationId, cancellationToken);

        if (!emailResult.Success)
        {
            return emailResult;
        }
        if (!resumeResult.Success)
        {
            return new AiActionResult(false, $"Email generated, but resume selection failed: {resumeResult.ErrorMessage}");
        }
        if (!interviewResult.Success)
        {
            return new AiActionResult(false, $"Email and resume ready, but interview generation failed: {interviewResult.ErrorMessage}");
        }

        return new AiActionResult(true, null);
    }

    public async Task<(bool Success, string? ErrorMessage, List<JobListingItem>? Jobs)> GenerateJobListingsAsync(int count, string? focusArea, CancellationToken cancellationToken = default)
    {
        if (!_openAiOptions.IsConfigured)
        {
            return (false, "OpenAI is not configured. Please set an API key (Settings or User Secrets) before generating content.", null);
        }

        var clampedCount = Math.Clamp(count, 1, 25);
        var (system, user) = PromptBuilder.BuildJobListingsPrompt(clampedCount, focusArea, _candidateOptions.TechnologyBackground);
        var (raw, error) = await CallOpenAiAsync(system, user, cancellationToken);
        if (error is not null)
        {
            _logger.LogWarning("Job listing generation failed: {Error}", error);
            return (false, error, null);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<JobListingGenerationResponse>(raw!, JsonOptions);
            if (parsed is null || parsed.Jobs.Count == 0)
            {
                throw new JsonException("AI response did not contain any job listings.");
            }
            return (true, null, parsed.Jobs);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI job-listing response");
            return (false, "Unable to generate job listings. Please try again.", null);
        }
    }

    private async Task<(string? Raw, string? Error)> CallOpenAiAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_openAiOptions.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

            var requestBody = new
            {
                model = _openAiOptions.Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.6
            };

            using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("chat/completions", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API call failed with status {StatusCode}: {Body}", response.StatusCode, responseBody);
                return (responseBody, "Unable to generate content. The AI service returned an error.");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return (responseBody, "The AI service returned an empty response.");
            }

            return (messageContent, null);
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling the OpenAI API");
            return (null, "Unable to reach the AI service. Please try again.");
        }
    }

    private async Task LogAsync(int jobApplicationId, GenerationType type, string prompt, string? response, bool success, string? errorMessage, CancellationToken cancellationToken)
    {
        _db.AiGenerationLogs.Add(new AiGenerationLog
        {
            JobApplicationId = jobApplicationId,
            GenerationType = type,
            Prompt = prompt,
            Response = response,
            Status = success ? GenerationStatus.Success : GenerationStatus.Failed,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
