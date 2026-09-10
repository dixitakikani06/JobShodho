using JobShodho.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobShodho.Controllers;

public class DashboardController : Controller
{
    private readonly IJobApplicationService _jobApplicationService;

    public DashboardController(IJobApplicationService jobApplicationService)
    {
        _jobApplicationService = jobApplicationService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = await _jobApplicationService.GetDashboardStatisticsAsync(cancellationToken);
        return View(vm);
    }
}
