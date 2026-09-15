using JobShodho.Services.Interfaces;
using JobShodho.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JobShodho.Controllers;

public class ImportController : Controller
{
    private readonly IExcelImportService _excelImportService;
    private readonly IAiService _aiService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IExcelImportService excelImportService, IAiService aiService, ILogger<ImportController> logger)
    {
        _excelImportService = excelImportService;
        _aiService = aiService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new ImportResultViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select an Excel (.xlsx) file to upload.");
            return View("Index", new ImportResultViewModel());
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _excelImportService.ImportAsync(stream, file.FileName, cancellationToken);
            return View("Index", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while importing Excel file {FileName}", file.FileName);
            var result = new ImportResultViewModel();
            result.Errors.Add("Invalid Excel format. Unable to process the file.");
            return View("Index", result);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateWithAi(int count = 10, string? focusArea = null, CancellationToken cancellationToken = default)
    {
        var (success, errorMessage, jobs) = await _aiService.GenerateJobListingsAsync(count, focusArea, cancellationToken);
        if (!success || jobs is null || jobs.Count == 0)
        {
            TempData["ErrorMessage"] = errorMessage ?? "Unable to generate job listings.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("AI generated {Count} sample job listings (not saved to database)", jobs.Count);

        var bytes = _excelImportService.BuildJobListingsWorkbook(jobs);
        var fileName = $"ai-generated-jobs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
