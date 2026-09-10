using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Models.Enums;
using JobShodho.Services.Interfaces;
using JobShodho.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace JobShodho.Services;

public class EmailBatchService : IEmailBatchService
{
    private const int ReadyForReview = (int)EmailStatus.ReadyForReview;
    private const int Sending = (int)EmailStatus.Sending;
    private const int Failed = (int)EmailStatus.Failed;

    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailBatchService> _logger;

    public EmailBatchService(
        ApplicationDbContext db,
        IEmailService emailService,
        IWebHostEnvironment env,
        ILogger<EmailBatchService> logger)
    {
        _db = db;
        _emailService = emailService;
        _env = env;
        _logger = logger;
    }

    public async Task<List<int>> ClaimNextBatchAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var claimed = await _db.Database.SqlQuery<int>($"""
            UPDATE TOP ({maxCount}) JobApplications
            SET EmailStatus = {Sending}, UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.Id
            WHERE EmailStatus = {ReadyForReview} AND IsEmailSent = 0
            """).ToListAsync(cancellationToken);

        if (claimed.Count > 0)
        {
            _logger.LogInformation("Claimed {Count} job(s) for sending: {Ids}", claimed.Count, string.Join(",", claimed));
        }

        return claimed;
    }

    public async Task<BatchSendResultViewModel> SendNextBatchAsync(int maxCount = 10, CancellationToken cancellationToken = default)
    {
        var claimed = await ClaimNextBatchAsync(maxCount, cancellationToken);
        return await ProcessClaimedAsync(claimed, cancellationToken);
    }

    public async Task<BatchSendResultViewModel> SendSingleAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var claimed = await ClaimSpecificAsync(jobApplicationId, ReadyForReview, cancellationToken);
        return await ProcessClaimedAsync(claimed, cancellationToken);
    }

    public async Task<BatchSendResultViewModel> RetryAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var claimed = await ClaimSpecificAsync(jobApplicationId, Failed, cancellationToken);
        return await ProcessClaimedAsync(claimed, cancellationToken);
    }

    public async Task<BatchSendResultViewModel> ProcessClaimedAsync(List<int> jobApplicationIds, CancellationToken cancellationToken = default)
    {
        var result = new BatchSendResultViewModel { Claimed = jobApplicationIds.Count };

        foreach (var id in jobApplicationIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var success = await ProcessOneAsync(id, cancellationToken);
                if (success)
                {
                    result.Succeeded++;
                }
                else
                {
                    result.Failed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending job application {JobId}", id);
                result.Failed++;
                result.Messages.Add($"Job {id}: unexpected error while sending.");
                await MarkFailedAsync(id, "Unexpected error while sending.", cancellationToken);
            }
        }

        return result;
    }

    private async Task<List<int>> ClaimSpecificAsync(int id, int fromStatus, CancellationToken cancellationToken)
    {
        return await _db.Database.SqlQuery<int>($"""
            UPDATE JobApplications
            SET EmailStatus = {Sending}, UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.Id
            WHERE Id = {id} AND EmailStatus = {fromStatus} AND IsEmailSent = 0
            """).ToListAsync(cancellationToken);
    }

    private async Task<bool> ProcessOneAsync(int id, CancellationToken cancellationToken)
    {
        var job = await _db.JobApplications.Include(j => j.SelectedResume).FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found while processing send batch", id);
            return false;
        }

        _logger.LogInformation("Email sending started for job {JobId} ({Recipient})", id, job.RecipientEmail);

        string? attachmentFullPath = null;
        if (job.SelectedResume is not null)
        {
            attachmentFullPath = Path.Combine(_env.WebRootPath, "uploads", "resumes", job.SelectedResume.StoredFileName);
        }

        EmailSendResult result;
        if (string.IsNullOrWhiteSpace(job.EmailSubject) || string.IsNullOrWhiteSpace(job.EmailBody))
        {
            result = new EmailSendResult(false, null, "Missing email subject or body.");
        }
        else
        {
            result = await _emailService.SendAsync(job.RecipientEmail, job.EmailSubject, job.EmailBody, attachmentFullPath, cancellationToken);
        }

        job.EmailAttemptCount++;
        job.LastEmailAttemptAt = DateTime.UtcNow;

        if (result.Success)
        {
            job.IsEmailSent = true;
            job.EmailStatus = EmailStatus.Sent;
            job.SentAt = DateTime.UtcNow;
            job.LastErrorMessage = null;
            _logger.LogInformation("Email sent for job {JobId}", id);
        }
        else
        {
            job.IsEmailSent = false;
            job.EmailStatus = EmailStatus.Failed;
            job.LastErrorMessage = result.ErrorMessage;
            _logger.LogWarning("Email failed for job {JobId}: {Error}", id, result.ErrorMessage);
        }

        _db.EmailSendLogs.Add(new EmailSendLog
        {
            JobApplicationId = job.Id,
            RecipientEmail = job.RecipientEmail,
            Subject = job.EmailSubject,
            Status = result.Success ? EmailLogStatus.Sent : EmailLogStatus.Failed,
            AttemptNumber = job.EmailAttemptCount,
            SentAt = result.Success ? DateTime.UtcNow : null,
            ErrorMessage = result.ErrorMessage,
            ProviderMessageId = result.ProviderMessageId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return result.Success;
    }

    private async Task MarkFailedAsync(int id, string errorMessage, CancellationToken cancellationToken)
    {
        var job = await _db.JobApplications.FindAsync([id], cancellationToken);
        if (job is null)
        {
            return;
        }
        job.IsEmailSent = false;
        job.EmailStatus = EmailStatus.Failed;
        job.LastErrorMessage = errorMessage;
        job.EmailAttemptCount++;
        job.LastEmailAttemptAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
