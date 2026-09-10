using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Models.Enums;
using JobShodho.Services.Interfaces;
using JobShodho.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace JobShodho.Services;

public class JobApplicationService : IJobApplicationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<JobApplicationService> _logger;

    public JobApplicationService(ApplicationDbContext db, ILogger<JobApplicationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var vm = new DashboardViewModel
        {
            TotalJobs = await _db.JobApplications.CountAsync(cancellationToken),
            Pending = await _db.JobApplications.CountAsync(j => j.EmailStatus == EmailStatus.Pending, cancellationToken),
            ReadyForReview = await _db.JobApplications.CountAsync(j => j.EmailStatus == EmailStatus.ReadyForReview, cancellationToken),
            Sent = await _db.JobApplications.CountAsync(j => j.EmailStatus == EmailStatus.Sent, cancellationToken),
            Failed = await _db.JobApplications.CountAsync(j => j.EmailStatus == EmailStatus.Failed, cancellationToken),
            SentToday = await _db.JobApplications.CountAsync(j => j.EmailStatus == EmailStatus.Sent && j.SentAt != null && j.SentAt.Value.Date == today, cancellationToken),
            FailedToday = await _db.JobApplications.CountAsync(j => j.EmailStatus == EmailStatus.Failed && j.LastEmailAttemptAt != null && j.LastEmailAttemptAt.Value.Date == today, cancellationToken),
        };

        vm.RemainingToSend = vm.ReadyForReview;
        vm.RecentApplications = await GetRecentApplicationsAsync(10, cancellationToken);

        return vm;
    }

    public async Task<List<JobApplication>> GetRecentApplicationsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _db.JobApplications
            .Include(j => j.SelectedResume)
            .OrderByDescending(j => j.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<JobApplication>> GetPagedAsync(JobListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.JobApplications.Include(j => j.SelectedResume).AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(j => j.EmailStatus == filter.Status.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Company))
        {
            query = query.Where(j => j.CompanyName.Contains(filter.Company));
        }
        if (!string.IsNullOrWhiteSpace(filter.JobTitle))
        {
            query = query.Where(j => j.JobTitle.Contains(filter.JobTitle));
        }
        if (filter.FromDate.HasValue)
        {
            query = query.Where(j => j.CreatedAt >= filter.FromDate.Value.Date);
        }
        if (filter.ToDate.HasValue)
        {
            var to = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(j => j.CreatedAt < to);
        }
        if (filter.IsEmailSent.HasValue)
        {
            query = query.Where(j => j.IsEmailSent == filter.IsEmailSent.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobApplication>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.JobApplications
            .Include(j => j.SelectedResume)
            .Include(j => j.InterviewContent)
            .Include(j => j.EmailSendLogs.OrderByDescending(l => l.CreatedAt))
            .Include(j => j.AiGenerationLogs.OrderByDescending(l => l.CreatedAt))
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<JobApplication> CreateAsync(JobApplication jobApplication, CancellationToken cancellationToken = default)
    {
        jobApplication.CreatedAt = DateTime.UtcNow;
        jobApplication.UpdatedAt = DateTime.UtcNow;
        jobApplication.ImportedAt = DateTime.UtcNow;
        _db.JobApplications.Add(jobApplication);
        await _db.SaveChangesAsync(cancellationToken);
        return jobApplication;
    }

    public async Task UpdateAsync(JobApplication jobApplication, CancellationToken cancellationToken = default)
    {
        jobApplication.UpdatedAt = DateTime.UtcNow;
        _db.JobApplications.Update(jobApplication);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.JobApplications.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return;
        }
        _db.JobApplications.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetReviewedAsync(int id, bool isReviewed, CancellationToken cancellationToken = default)
    {
        var entity = await _db.JobApplications.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return;
        }
        entity.IsReviewed = isReviewed;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
