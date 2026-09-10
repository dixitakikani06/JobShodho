using JobShodho.Services.Interfaces;
using JobShodho.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JobShodho.Controllers;

public class InterviewController : Controller
{
    private readonly IJobApplicationService _jobApplicationService;
    private readonly IInterviewService _interviewService;

    public InterviewController(IJobApplicationService jobApplicationService, IInterviewService interviewService)
    {
        _jobApplicationService = jobApplicationService;
        _interviewService = interviewService;
    }

    public async Task<IActionResult> View(int jobId, CancellationToken cancellationToken)
    {
        var job = await _jobApplicationService.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var content = await _interviewService.GetParsedContentAsync(jobId, cancellationToken);
        if (content is null)
        {
            TempData["ErrorMessage"] = "No interview questions have been generated for this job yet.";
            return RedirectToAction("Review", "Email", new { id = jobId });
        }

        var vm = new ReviewViewModel { Job = job, Interview = content };
        return base.View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePdf(int jobId, CancellationToken cancellationToken)
    {
        var (success, _, errorMessage) = await _interviewService.GeneratePdfAsync(jobId, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "PDF generated successfully." : errorMessage;
        return RedirectToAction(nameof(View), new { jobId });
    }

    public async Task<IActionResult> DownloadPdf(int jobId, CancellationToken cancellationToken)
    {
        var job = await _jobApplicationService.GetByIdAsync(jobId, cancellationToken);
        if (job is null || string.IsNullOrWhiteSpace(job.InterviewContentPath))
        {
            return NotFound();
        }

        var relativePath = job.InterviewContentPath.TrimStart('/');
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
        {
            TempData["ErrorMessage"] = "The PDF could not be found. Please regenerate it.";
            return RedirectToAction(nameof(View), new { jobId });
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, cancellationToken);
        return File(bytes, "application/pdf", Path.GetFileName(fullPath));
    }
}
