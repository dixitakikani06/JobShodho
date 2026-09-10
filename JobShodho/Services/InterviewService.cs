using System.Text.Json;
using System.Text.RegularExpressions;
using JobShodho.Data;
using JobShodho.Services.Ai;
using JobShodho.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobShodho.Services;

public partial class InterviewService : IInterviewService
{
    private const string RelativeFolder = "uploads/interview";

    private readonly ApplicationDbContext _db;
    private readonly IPdfService _pdfService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<InterviewService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public InterviewService(ApplicationDbContext db, IPdfService pdfService, IWebHostEnvironment env, ILogger<InterviewService> logger)
    {
        _db = db;
        _pdfService = pdfService;
        _env = env;
        _logger = logger;
    }

    public async Task<InterviewGenerationResponse?> GetParsedContentAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var job = await _db.JobApplications.Include(j => j.InterviewContent).FirstOrDefaultAsync(j => j.Id == jobApplicationId, cancellationToken);
        if (job?.InterviewContent is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<InterviewGenerationResponse>(job.InterviewContent.ContentJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse interview content for job {JobId}", jobApplicationId);
            return null;
        }
    }

    public async Task<(bool Success, string? PdfPath, string? ErrorMessage)> GeneratePdfAsync(int jobApplicationId, CancellationToken cancellationToken = default)
    {
        var job = await _db.JobApplications.Include(j => j.InterviewContent).FirstOrDefaultAsync(j => j.Id == jobApplicationId, cancellationToken);
        if (job is null)
        {
            return (false, null, "Job application not found.");
        }

        var content = await GetParsedContentAsync(jobApplicationId, cancellationToken);
        if (content is null || content.Questions.Count == 0)
        {
            return (false, null, "No interview questions have been generated for this job yet.");
        }

        try
        {
            var pdfBytes = _pdfService.GenerateInterviewPdf(job, content);

            var folderPath = Path.Combine(_env.WebRootPath, RelativeFolder);
            Directory.CreateDirectory(folderPath);

            var fileName = $"{job.Id}-{Slugify(job.CompanyName)}-{Slugify(job.JobTitle)}.pdf";
            var fullPath = Path.Combine(folderPath, fileName);
            await File.WriteAllBytesAsync(fullPath, pdfBytes, cancellationToken);

            var relativePath = $"/{RelativeFolder}/{fileName}";
            job.InterviewContentPath = relativePath;
            if (job.InterviewContent is not null)
            {
                job.InterviewContent.PdfPath = relativePath;
                job.InterviewContent.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Interview PDF generated for job {JobId} at {Path}", jobApplicationId, relativePath);
            return (true, relativePath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate interview PDF for job {JobId}", jobApplicationId);
            return (false, null, "Unable to generate the PDF. Please try again.");
        }
    }

    private static string Slugify(string value)
    {
        var slug = SlugInvalidCharsRegex().Replace(value.ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "job" : slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugInvalidCharsRegex();
}
