using System.Text.Json;
using Hangfire;
using JobShodho.BackgroundJobs;
using JobShodho.Data;
using JobShodho.Models.Enums;
using JobShodho.Services.Ai;
using JobShodho.Services.Interfaces;
using JobShodho.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobShodho.Controllers;

public class EmailController : Controller
{
    private const int BatchSize = 10;

    private readonly IJobApplicationService _jobApplicationService;
    private readonly IAiService _aiService;
    private readonly IResumeService _resumeService;
    private readonly IEmailBatchService _emailBatchService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        IJobApplicationService jobApplicationService,
        IAiService aiService,
        IResumeService resumeService,
        IEmailBatchService emailBatchService,
        ApplicationDbContext db,
        ILogger<EmailController> logger)
    {
        _jobApplicationService = jobApplicationService;
        _aiService = aiService;
        _resumeService = resumeService;
        _emailBatchService = emailBatchService;
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Queue(CancellationToken cancellationToken)
    {
        var jobs = await _db.JobApplications
            .Where(j => j.EmailStatus == EmailStatus.ReadyForReview)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
        return View(jobs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNextBatch(CancellationToken cancellationToken)
    {
        var claimedIds = await _emailBatchService.ClaimNextBatchAsync(BatchSize, cancellationToken);
        if (claimedIds.Count == 0)
        {
            TempData["ErrorMessage"] = "No reviewed jobs are ready to send.";
        }
        else
        {
            BackgroundJob.Enqueue<EmailBatchJob>(j => j.ProcessBatchAsync(claimedIds));
            TempData["SuccessMessage"] = $"{claimedIds.Count} email(s) queued for sending in the background. Refresh Email History shortly to see results.";
        }
        return RedirectToAction(nameof(Queue));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSingle(int id, CancellationToken cancellationToken)
    {
        var result = await _emailBatchService.SendSingleAsync(id, cancellationToken);
        if (result.Claimed == 0)
        {
            TempData["ErrorMessage"] = "This job is not ready to send (it may already be sent, or is not marked Ready for Review).";
        }
        else if (result.Succeeded > 0)
        {
            TempData["SuccessMessage"] = "Email sent successfully.";
        }
        else
        {
            var job = await _jobApplicationService.GetByIdAsync(id, cancellationToken);
            TempData["ErrorMessage"] = job?.LastErrorMessage ?? "Unable to send this email.";
        }
        return RedirectToAction(nameof(Review), new { id });
    }

    public async Task<IActionResult> Failed(CancellationToken cancellationToken)
    {
        var jobs = await _db.JobApplications
            .Where(j => j.EmailStatus == EmailStatus.Failed)
            .OrderByDescending(j => j.LastEmailAttemptAt)
            .ToListAsync(cancellationToken);
        return View(jobs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id, CancellationToken cancellationToken)
    {
        var result = await _emailBatchService.RetryAsync(id, cancellationToken);
        if (result.Claimed == 0)
        {
            TempData["ErrorMessage"] = "This job could not be retried (it may no longer be in a Failed state).";
        }
        else if (result.Succeeded > 0)
        {
            TempData["SuccessMessage"] = "Retry succeeded — email sent.";
        }
        else
        {
            TempData["ErrorMessage"] = "Retry failed again. Check the error details.";
        }
        return RedirectToAction(nameof(Failed));
    }

    public async Task<IActionResult> History(int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 25;
        var query = _db.EmailSendLogs.Include(l => l.JobApplication).OrderByDescending(l => l.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        ViewData["Page"] = page;
        ViewData["TotalPages"] = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return View(logs);
    }

    public async Task<IActionResult> Review(int id, CancellationToken cancellationToken)
    {
        var job = await _jobApplicationService.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var vm = new ReviewViewModel
        {
            Job = job,
            AvailableResumes = await _resumeService.GetAllAsync(activeOnly: true, cancellationToken)
        };

        if (job.InterviewContent is not null)
        {
            try
            {
                vm.Interview = JsonSerializer.Deserialize<InterviewGenerationResponse>(job.InterviewContent.ContentJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse stored interview content for job {JobId}", id);
            }
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAll(int id, CancellationToken cancellationToken)
    {
        var result = await _aiService.GenerateAllAsync(id, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "AI content generated successfully."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateEmail(int id, CancellationToken cancellationToken)
    {
        var result = await _aiService.GenerateEmailAsync(id, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Email generated successfully."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(int id, string emailSubject, string emailBody, CancellationToken cancellationToken)
    {
        var job = await _jobApplicationService.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(emailSubject) || string.IsNullOrWhiteSpace(emailBody))
        {
            TempData["ErrorMessage"] = "Subject and body cannot be empty.";
            return RedirectToAction(nameof(Review), new { id });
        }

        job.EmailSubject = emailSubject.Trim();
        job.EmailBody = emailBody.Trim();
        if (job.EmailStatus == Models.Enums.EmailStatus.Pending || job.EmailStatus == Models.Enums.EmailStatus.Generating)
        {
            job.EmailStatus = Models.Enums.EmailStatus.ReadyForReview;
        }

        await _jobApplicationService.UpdateAsync(job, cancellationToken);
        TempData["SuccessMessage"] = "Changes saved.";
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AiSelectResume(int id, CancellationToken cancellationToken)
    {
        var result = await _aiService.SelectResumeAsync(id, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Resume selected by AI."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeResume(int id, int resumeId, CancellationToken cancellationToken)
    {
        var job = await _jobApplicationService.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        job.SelectedResumeId = resumeId;
        job.ResumeSelectionReason = "Manually selected by user.";
        job.ResumeSelectionScore = null;
        await _jobApplicationService.UpdateAsync(job, cancellationToken);

        TempData["SuccessMessage"] = "Resume updated.";
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateInterview(int id, CancellationToken cancellationToken)
    {
        var result = await _aiService.GenerateInterviewQuestionsAsync(id, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Interview questions generated."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Review), new { id });
    }
}
