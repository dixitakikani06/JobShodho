using JobShodho.Services.Interfaces;

namespace JobShodho.BackgroundJobs;

/// <summary>Thin Hangfire entry point for sending an already-claimed batch of jobs in the background.</summary>
public class EmailBatchJob
{
    private readonly IEmailBatchService _emailBatchService;
    private readonly ILogger<EmailBatchJob> _logger;

    public EmailBatchJob(IEmailBatchService emailBatchService, ILogger<EmailBatchJob> logger)
    {
        _emailBatchService = emailBatchService;
        _logger = logger;
    }

    public async Task ProcessBatchAsync(List<int> jobApplicationIds)
    {
        var result = await _emailBatchService.ProcessClaimedAsync(jobApplicationIds, CancellationToken.None);
        _logger.LogInformation(
            "Email batch job finished: {Claimed} claimed, {Succeeded} succeeded, {Failed} failed",
            result.Claimed, result.Succeeded, result.Failed);
    }
}
