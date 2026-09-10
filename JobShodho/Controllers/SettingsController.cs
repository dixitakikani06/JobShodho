using JobShodho.Models;
using JobShodho.Options;
using JobShodho.Services.Interfaces;
using JobShodho.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JobShodho.Controllers;

public class SettingsController : Controller
{
    private readonly ICandidateProfileService _candidateProfileService;
    private readonly OpenAiOptions _openAiOptions;
    private readonly EmailOptions _emailOptions;

    public SettingsController(
        ICandidateProfileService candidateProfileService,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<EmailOptions> emailOptions)
    {
        _candidateProfileService = candidateProfileService;
        _openAiOptions = openAiOptions.Value;
        _emailOptions = emailOptions.Value;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await _candidateProfileService.GetProfileAsync(cancellationToken);

        ViewData["OpenAiConfigured"] = _openAiOptions.IsConfigured;
        ViewData["OpenAiModel"] = _openAiOptions.Model;
        ViewData["EmailConfigured"] = _emailOptions.IsConfigured;
        ViewData["EmailHost"] = _emailOptions.Host;

        var vm = new CandidateProfileViewModel
        {
            FullName = profile.FullName,
            Email = profile.Email,
            Phone = profile.Phone,
            Location = profile.Location,
            YearsOfExperience = profile.YearsOfExperience,
            ProfessionalSummary = profile.ProfessionalSummary,
            Skills = profile.Skills,
            LinkedInUrl = profile.LinkedInUrl,
            GitHubUrl = profile.GitHubUrl,
            PortfolioUrl = profile.PortfolioUrl,
            EmailSignature = profile.EmailSignature
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CandidateProfileViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewData["OpenAiConfigured"] = _openAiOptions.IsConfigured;
            ViewData["OpenAiModel"] = _openAiOptions.Model;
            ViewData["EmailConfigured"] = _emailOptions.IsConfigured;
            ViewData["EmailHost"] = _emailOptions.Host;
            return View(vm);
        }

        var profile = new CandidateProfile
        {
            FullName = vm.FullName.Trim(),
            Email = vm.Email.Trim(),
            Phone = vm.Phone?.Trim(),
            Location = vm.Location?.Trim(),
            YearsOfExperience = vm.YearsOfExperience,
            ProfessionalSummary = vm.ProfessionalSummary?.Trim(),
            Skills = vm.Skills?.Trim(),
            LinkedInUrl = vm.LinkedInUrl?.Trim(),
            GitHubUrl = vm.GitHubUrl?.Trim(),
            PortfolioUrl = vm.PortfolioUrl?.Trim(),
            EmailSignature = vm.EmailSignature?.Trim()
        };

        await _candidateProfileService.UpdateProfileAsync(profile, cancellationToken);

        TempData["SuccessMessage"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
