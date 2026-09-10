using JobShodho.Models;
using Microsoft.AspNetCore.Http;

namespace JobShodho.Services.Interfaces;

public interface IResumeService
{
    Task<List<Resume>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    Task<Resume?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorMessage, Resume? Resume)> UploadAsync(IFormFile file, string name, CancellationToken cancellationToken = default);

    Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
