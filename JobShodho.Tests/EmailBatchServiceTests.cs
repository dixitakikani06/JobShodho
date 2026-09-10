using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Models.Enums;
using JobShodho.Services;
using JobShodho.Services.Interfaces;
using JobShodho.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobShodho.Tests;

/// <summary>
/// EmailBatchService's claim logic runs raw "UPDATE ... OUTPUT" SQL, which the EF InMemory provider does not
/// support, so these tests run against a real, disposable LocalDB database (mirroring production SQL Server).
/// They exist specifically to lock down the two acceptance-critical behaviors from the spec: "Send Next 10"
/// never claims more than the requested batch size, and an already-sent job can never be claimed again.
/// </summary>
public class EmailBatchServiceTests : IAsyncLifetime
{
    private readonly string _databaseName = $"JobShodho_Test_{Guid.NewGuid():N}";
    private ApplicationDbContext _db = null!;

    public async Task InitializeAsync()
    {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        _db = new ApplicationDbContext(options);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    private static JobApplication MakeJob(string company, EmailStatus status, bool isSent = false) => new()
    {
        CompanyName = company,
        JobTitle = "Developer",
        RecipientEmail = $"hr@{company.ToLowerInvariant()}.com",
        JobDescription = "Test job",
        EmailSubject = "Subject",
        EmailBody = "Body",
        EmailStatus = status,
        IsEmailSent = isSent
    };

    [Fact]
    public async Task ClaimNextBatchAsync_NeverClaimsMoreThanRequestedMax()
    {
        for (var i = 0; i < 15; i++)
        {
            _db.JobApplications.Add(MakeJob($"Company{i}", EmailStatus.ReadyForReview));
        }
        await _db.SaveChangesAsync();

        var service = new EmailBatchService(_db, new FakeEmailService(), new FakeWebHostEnvironment(), NullLogger<EmailBatchService>.Instance);

        var claimed = await service.ClaimNextBatchAsync(10);

        Assert.Equal(10, claimed.Count);

        var stillReady = await _db.JobApplications.CountAsync(j => j.EmailStatus == EmailStatus.ReadyForReview);
        Assert.Equal(5, stillReady);
    }

    [Fact]
    public async Task ClaimNextBatchAsync_NeverClaimsAlreadySentOrFailedJobs()
    {
        _db.JobApplications.Add(MakeJob("SentCo", EmailStatus.Sent, isSent: true));
        _db.JobApplications.Add(MakeJob("FailedCo", EmailStatus.Failed));
        _db.JobApplications.Add(MakeJob("ReadyCo", EmailStatus.ReadyForReview));
        await _db.SaveChangesAsync();

        var service = new EmailBatchService(_db, new FakeEmailService(), new FakeWebHostEnvironment(), NullLogger<EmailBatchService>.Instance);

        var claimed = await service.ClaimNextBatchAsync(10);

        Assert.Single(claimed);
        var claimedJob = await _db.JobApplications.FindAsync(claimed[0]);
        Assert.Equal("ReadyCo", claimedJob!.CompanyName);
    }

    [Fact]
    public async Task ClaimNextBatchAsync_TwoSequentialCallsNeverClaimTheSameRow()
    {
        for (var i = 0; i < 3; i++)
        {
            _db.JobApplications.Add(MakeJob($"Company{i}", EmailStatus.ReadyForReview));
        }
        await _db.SaveChangesAsync();

        var service = new EmailBatchService(_db, new FakeEmailService(), new FakeWebHostEnvironment(), NullLogger<EmailBatchService>.Instance);

        var firstBatch = await service.ClaimNextBatchAsync(10);
        var secondBatch = await service.ClaimNextBatchAsync(10);

        Assert.Equal(3, firstBatch.Count);
        Assert.Empty(secondBatch);
    }

    [Fact]
    public async Task ProcessClaimedAsync_MarksSuccessAndFailureIndependently_OneFailureDoesNotAbortTheBatch()
    {
        var okJob = MakeJob("OkCo", EmailStatus.ReadyForReview);
        var badJob = MakeJob("BadCo", EmailStatus.ReadyForReview);
        _db.JobApplications.AddRange(okJob, badJob);
        await _db.SaveChangesAsync();

        var fakeEmail = new FakeEmailService
        {
            ResultFor = recipient => recipient.Contains("badco")
                ? new EmailSendResult(false, null, "Simulated SMTP failure.")
                : new EmailSendResult(true, "msg-1", null)
        };

        var service = new EmailBatchService(_db, fakeEmail, new FakeWebHostEnvironment(), NullLogger<EmailBatchService>.Instance);
        var claimed = await service.ClaimNextBatchAsync(10);

        var result = await service.ProcessClaimedAsync(claimed);

        Assert.Equal(2, result.Claimed);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);

        var refreshedOk = await _db.JobApplications.FindAsync(okJob.Id);
        var refreshedBad = await _db.JobApplications.FindAsync(badJob.Id);
        Assert.Equal(EmailStatus.Sent, refreshedOk!.EmailStatus);
        Assert.True(refreshedOk.IsEmailSent);
        Assert.Equal(EmailStatus.Failed, refreshedBad!.EmailStatus);
        Assert.False(refreshedBad.IsEmailSent);

        Assert.Equal(2, await _db.EmailSendLogs.CountAsync());
    }

    [Fact]
    public async Task SendSingleAsync_OnAlreadySentJob_ClaimsNothing()
    {
        var sentJob = MakeJob("AlreadySent", EmailStatus.Sent, isSent: true);
        _db.JobApplications.Add(sentJob);
        await _db.SaveChangesAsync();

        var service = new EmailBatchService(_db, new FakeEmailService(), new FakeWebHostEnvironment(), NullLogger<EmailBatchService>.Instance);

        var result = await service.SendSingleAsync(sentJob.Id);

        Assert.Equal(0, result.Claimed);
        Assert.Equal(0, await _db.EmailSendLogs.CountAsync());
    }

    [Fact]
    public async Task RetryAsync_OnlyClaimsFailedJobs_NotReadyOrSentOnes()
    {
        var readyJob = MakeJob("ReadyCo", EmailStatus.ReadyForReview);
        _db.JobApplications.Add(readyJob);
        await _db.SaveChangesAsync();

        var service = new EmailBatchService(_db, new FakeEmailService(), new FakeWebHostEnvironment(), NullLogger<EmailBatchService>.Instance);

        var result = await service.RetryAsync(readyJob.Id);

        Assert.Equal(0, result.Claimed);
        var refreshed = await _db.JobApplications.FindAsync(readyJob.Id);
        Assert.Equal(EmailStatus.ReadyForReview, refreshed!.EmailStatus);
    }
}
