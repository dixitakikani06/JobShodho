using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Options;
using JobShodho.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace JobShodho.Services;

public class ResumeService : IResumeService
{
    private const string RelativeFolder = "uploads/resumes";

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly UploadOptions _uploadOptions;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(
        ApplicationDbContext db,
        IWebHostEnvironment env,
        IOptions<UploadOptions> uploadOptions,
        ILogger<ResumeService> logger)
    {
        _db = db;
        _env = env;
        _uploadOptions = uploadOptions.Value;
        _logger = logger;
    }

    public async Task<List<Resume>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Resumes.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Resume?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Resumes.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage, Resume? Resume)> UploadAsync(IFormFile file, string name, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return (false, "The uploaded file is empty.", null);
        }

        if (file.Length > _uploadOptions.MaxResumeSizeBytes)
        {
            return (false, $"File exceeds the maximum allowed size of {_uploadOptions.MaxResumeSizeBytes / (1024 * 1024)} MB.", null);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_uploadOptions.AllowedResumeExtensions.Contains(extension))
        {
            return (false, $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _uploadOptions.AllowedResumeExtensions)}.", null);
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var folderPath = Path.Combine(_env.WebRootPath, RelativeFolder);
        Directory.CreateDirectory(folderPath);
        var fullPath = Path.Combine(folderPath, storedFileName);

        string? extractedText = null;
        try
        {
            await using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStreamOut, cancellationToken);
            }

            extractedText = ExtractText(fullPath, extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save or extract text from resume {FileName}", file.FileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return (false, "Unable to process the uploaded resume file.", null);
        }

        var resume = new Resume
        {
            Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file.FileName) : name.Trim(),
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            FilePath = $"/{RelativeFolder}/{storedFileName}",
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            ExtractedText = extractedText,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Resumes.Add(resume);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Resume uploaded: {ResumeName} ({StoredFileName})", resume.Name, resume.StoredFileName);

        return (true, null, resume);
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FindAsync([id], cancellationToken);
        if (resume is null)
        {
            return;
        }
        resume.IsActive = isActive;
        resume.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (resume is null)
        {
            return false;
        }

        var inUse = await _db.JobApplications.AnyAsync(j => j.SelectedResumeId == id, cancellationToken);
        if (inUse)
        {
            return false;
        }

        var fullPath = Path.Combine(_env.WebRootPath, RelativeFolder, resume.StoredFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        _db.Resumes.Remove(resume);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? ExtractText(string fullPath, string extension)
    {
        return extension switch
        {
            ".pdf" => ExtractPdfText(fullPath),
            ".docx" => ExtractDocxText(fullPath),
            _ => null
        };
    }

    private static string? ExtractPdfText(string fullPath)
    {
        using var document = PdfDocument.Open(fullPath);
        var textParts = document.GetPages().Select(p => p.Text);
        return string.Join("\n\n", textParts);
    }

    private static string? ExtractDocxText(string fullPath)
    {
        using var wordDocument = WordprocessingDocument.Open(fullPath, false);
        var body = wordDocument.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return null;
        }
        var paragraphs = body.Elements<Paragraph>().Select(p => p.InnerText);
        return string.Join("\n", paragraphs);
    }
}
