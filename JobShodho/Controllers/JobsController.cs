using JobShodho.Models;
using JobShodho.Services.Interfaces;
using JobShodho.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JobShodho.Controllers;

public class JobsController : Controller
{
    private readonly IJobApplicationService _jobApplicationService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobApplicationService jobApplicationService, ILogger<JobsController> logger)
    {
        _jobApplicationService = jobApplicationService;
        _logger = logger;
    }

    public async Task<IActionResult> Index([FromQuery] JobListFilter filter, CancellationToken cancellationToken)
    {
        var results = await _jobApplicationService.GetPagedAsync(filter, cancellationToken);
        var vm = new JobListViewModel { Filter = filter, Results = results };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var job = await _jobApplicationService.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }
        return View(job);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var job = await _jobApplicationService.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var vm = new JobEditViewModel
        {
            Id = job.Id,
            CompanyName = job.CompanyName,
            JobTitle = job.JobTitle,
            RecipientEmail = job.RecipientEmail,
            JobDescription = job.JobDescription,
            JobUrl = job.JobUrl,
            Location = job.Location,
            Source = job.Source
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobEditViewModel vm, CancellationToken cancellationToken)
    {
        if (id != vm.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var job = await _jobApplicationService.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        job.CompanyName = vm.CompanyName.Trim();
        job.JobTitle = vm.JobTitle.Trim();
        job.RecipientEmail = vm.RecipientEmail.Trim();
        job.JobDescription = vm.JobDescription.Trim();
        job.JobUrl = string.IsNullOrWhiteSpace(vm.JobUrl) ? null : vm.JobUrl.Trim();
        job.Location = string.IsNullOrWhiteSpace(vm.Location) ? null : vm.Location.Trim();
        job.Source = string.IsNullOrWhiteSpace(vm.Source) ? null : vm.Source.Trim();

        await _jobApplicationService.UpdateAsync(job, cancellationToken);

        TempData["SuccessMessage"] = "Job application updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public IActionResult Create()
    {
        return View(new JobEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobEditViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var job = new JobApplication
        {
            CompanyName = vm.CompanyName.Trim(),
            JobTitle = vm.JobTitle.Trim(),
            RecipientEmail = vm.RecipientEmail.Trim(),
            JobDescription = vm.JobDescription.Trim(),
            JobUrl = string.IsNullOrWhiteSpace(vm.JobUrl) ? null : vm.JobUrl.Trim(),
            Location = string.IsNullOrWhiteSpace(vm.Location) ? null : vm.Location.Trim(),
            Source = string.IsNullOrWhiteSpace(vm.Source) ? null : vm.Source.Trim()
        };

        var created = await _jobApplicationService.CreateAsync(job, cancellationToken);

        TempData["SuccessMessage"] = "Job application created successfully.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _jobApplicationService.DeleteAsync(id, cancellationToken);
        TempData["SuccessMessage"] = "Job application deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetReviewed(int id, bool isReviewed, CancellationToken cancellationToken)
    {
        await _jobApplicationService.SetReviewedAsync(id, isReviewed, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }
}
