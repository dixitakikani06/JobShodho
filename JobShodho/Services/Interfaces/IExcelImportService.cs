using JobShodho.Services.Ai;
using JobShodho.ViewModels;

namespace JobShodho.Services.Interfaces;

public interface IExcelImportService
{
    Task<ImportResultViewModel> ImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>Builds a .xlsx in the exact column format ImportAsync expects, for the "AI-generated starter
    /// sheet" download flow. Pure in-memory - never touches the database.</summary>
    byte[] BuildJobListingsWorkbook(IEnumerable<JobListingItem> jobs);
}
