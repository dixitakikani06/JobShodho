using JobShodho.Models;
using JobShodho.ViewModels;

namespace JobShodho.Services.Interfaces;

public interface IJobApplicationService
{
    Task<DashboardViewModel> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default);

    Task<List<JobApplication>> GetRecentApplicationsAsync(int count, CancellationToken cancellationToken = default);

    Task<PagedResult<JobApplication>> GetPagedAsync(JobListFilter filter, CancellationToken cancellationToken = default);

    Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<JobApplication> CreateAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);

    Task UpdateAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task SetReviewedAsync(int id, bool isReviewed, CancellationToken cancellationToken = default);
}
