using JobShodho.ViewModels;

namespace JobShodho.Services.Interfaces;

public interface IExcelImportService
{
    Task<ImportResultViewModel> ImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
