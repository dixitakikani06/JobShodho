using JobShodho.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobShodho.Controllers;

public class ResumeController : Controller
{
    private readonly IResumeService _resumeService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ResumeController> _logger;

    public ResumeController(IResumeService resumeService, IWebHostEnvironment env, ILogger<ResumeController> logger)
    {
        _resumeService = resumeService;
        _env = env;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var resumes = await _resumeService.GetAllAsync(activeOnly: false, cancellationToken);
        return View(resumes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, string? name, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a resume file (PDF or DOCX) to upload.";
            return RedirectToAction(nameof(Index));
        }

        var (success, errorMessage, _) = await _resumeService.UploadAsync(file, name ?? string.Empty, cancellationToken);
        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage ?? "Unable to upload resume.";
        }
        else
        {
            TempData["SuccessMessage"] = "Resume uploaded successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        await _resumeService.SetActiveAsync(id, isActive, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _resumeService.DeleteAsync(id, cancellationToken);
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
            ? "Resume deleted."
            : "This resume is assigned to one or more jobs and cannot be deleted.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var resume = await _resumeService.GetByIdAsync(id, cancellationToken);
        if (resume is null)
        {
            return NotFound();
        }

        var fullPath = Path.Combine(_env.WebRootPath, "uploads", "resumes", resume.StoredFileName);
        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("Resume file missing on disk: {FilePath}", fullPath);
            TempData["ErrorMessage"] = "Resume file could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, cancellationToken);
        return File(bytes, resume.ContentType, resume.OriginalFileName);
    }
}
