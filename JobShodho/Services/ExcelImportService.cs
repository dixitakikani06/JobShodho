using ClosedXML.Excel;
using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Models.Enums;
using JobShodho.Services.Ai;
using JobShodho.Services.Interfaces;
using JobShodho.Services.Validation;
using JobShodho.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace JobShodho.Services;

public class ExcelImportService : IExcelImportService
{
    private static readonly string[] RequiredHeaders =
    {
        "CompanyName", "JobTitle", "RecipientEmail", "JobDescription", "JobUrl", "Location", "Source"
    };

    private readonly ApplicationDbContext _db;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(ApplicationDbContext db, ILogger<ExcelImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ImportResultViewModel> ImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Excel import started for file {FileName}", fileName);
        var result = new ImportResultViewModel();

        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("Invalid file format. Please upload a .xlsx file.");
            return result;
        }

        using var workbook = OpenWorkbook(fileStream, result);
        if (workbook is null)
        {
            return result;
        }

        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null || worksheet.LastRowUsed() is null)
        {
            result.Errors.Add("The Excel file is empty.");
            return result;
        }

        var headerRow = worksheet.Row(1);
        var columnMap = BuildColumnMap(headerRow);

        var missingHeaders = RequiredHeaders.Where(h => !columnMap.ContainsKey(h)).ToList();
        if (missingHeaders.Count > 0)
        {
            result.Errors.Add($"Missing required column(s): {string.Join(", ", missingHeaders)}");
            return result;
        }

        var existingKeys = await _db.JobApplications
            .Select(j => new { j.CompanyName, j.JobTitle, j.RecipientEmail })
            .ToListAsync(cancellationToken);
        var existingKeySet = existingKeys
            .Select(k => BuildDedupeKey(k.CompanyName, k.JobTitle, k.RecipientEmail))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var batchKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toInsert = new List<JobApplication>();

        var lastRow = worksheet.LastRowUsed()!.RowNumber();
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = worksheet.Row(rowNumber);

            var companyName = GetCell(row, columnMap, "CompanyName");
            var jobTitle = GetCell(row, columnMap, "JobTitle");
            var recipientEmail = GetCell(row, columnMap, "RecipientEmail");
            var jobDescription = GetCell(row, columnMap, "JobDescription");
            var jobUrl = GetCell(row, columnMap, "JobUrl");
            var location = GetCell(row, columnMap, "Location");
            var source = GetCell(row, columnMap, "Source");

            var isCompletelyEmpty = string.IsNullOrWhiteSpace(companyName)
                && string.IsNullOrWhiteSpace(jobTitle)
                && string.IsNullOrWhiteSpace(recipientEmail)
                && string.IsNullOrWhiteSpace(jobDescription)
                && string.IsNullOrWhiteSpace(jobUrl)
                && string.IsNullOrWhiteSpace(location)
                && string.IsNullOrWhiteSpace(source);

            if (isCompletelyEmpty)
            {
                result.SkippedEmptyCount++;
                continue;
            }

            result.TotalRows++;

            var rowErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(companyName))
            {
                rowErrors.Add("CompanyName is required.");
            }
            if (string.IsNullOrWhiteSpace(jobTitle))
            {
                rowErrors.Add("JobTitle is required.");
            }
            if (string.IsNullOrWhiteSpace(jobDescription))
            {
                rowErrors.Add("JobDescription is required.");
            }
            if (!EmailValidator.IsValid(recipientEmail))
            {
                rowErrors.Add($"RecipientEmail '{recipientEmail}' is not a valid email address.");
            }

            if (rowErrors.Count > 0)
            {
                result.InvalidCount++;
                result.Errors.Add($"Row {rowNumber}: {string.Join(" ", rowErrors)}");
                continue;
            }

            var dedupeKey = BuildDedupeKey(companyName!, jobTitle!, recipientEmail!);
            if (existingKeySet.Contains(dedupeKey) || !batchKeySet.Add(dedupeKey))
            {
                result.DuplicateCount++;
                result.Errors.Add($"Row {rowNumber}: Duplicate record for '{companyName}' / '{jobTitle}' / '{recipientEmail}' skipped.");
                continue;
            }

            toInsert.Add(new JobApplication
            {
                CompanyName = companyName!.Trim(),
                JobTitle = jobTitle!.Trim(),
                RecipientEmail = recipientEmail!.Trim(),
                JobDescription = jobDescription!.Trim(),
                JobUrl = string.IsNullOrWhiteSpace(jobUrl) ? null : jobUrl.Trim(),
                Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
                Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim(),
                ImportedAt = DateTime.UtcNow,
                EmailStatus = EmailStatus.Pending,
                IsEmailSent = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (toInsert.Count > 0)
        {
            await _db.JobApplications.AddRangeAsync(toInsert, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        result.ImportedCount = toInsert.Count;
        _logger.LogInformation(
            "Excel import completed for {FileName}: {Imported} imported, {Duplicate} duplicates, {Invalid} invalid, {Empty} empty rows skipped",
            fileName, result.ImportedCount, result.DuplicateCount, result.InvalidCount, result.SkippedEmptyCount);

        return result;
    }

    public byte[] BuildJobListingsWorkbook(IEnumerable<JobListingItem> jobs)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Jobs");

        for (var i = 0; i < RequiredHeaders.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = RequiredHeaders[i];
        }

        var row = 2;
        foreach (var job in jobs)
        {
            worksheet.Cell(row, 1).Value = job.CompanyName;
            worksheet.Cell(row, 2).Value = job.JobTitle;
            worksheet.Cell(row, 3).Value = job.RecipientEmail;
            worksheet.Cell(row, 4).Value = job.JobDescription;
            worksheet.Cell(row, 5).Value = job.JobUrl ?? string.Empty;
            worksheet.Cell(row, 6).Value = job.Location ?? string.Empty;
            worksheet.Cell(row, 7).Value = job.Source ?? string.Empty;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private XLWorkbook? OpenWorkbook(Stream fileStream, ImportResultViewModel result)
    {
        try
        {
            return new XLWorkbook(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Excel file during import");
            result.Errors.Add("The file could not be read. Please ensure it is a valid .xlsx file.");
            return null;
        }
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCell = headerRow.LastCellUsed();
        if (lastCell is null)
        {
            return map;
        }

        for (var col = 1; col <= lastCell.Address.ColumnNumber; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header))
            {
                map[header] = col;
            }
        }

        return map;
    }

    private static string? GetCell(IXLRow row, Dictionary<string, int> columnMap, string headerName)
    {
        if (!columnMap.TryGetValue(headerName, out var colIndex))
        {
            return null;
        }

        return row.Cell(colIndex).GetString();
    }

    private static string BuildDedupeKey(string companyName, string jobTitle, string recipientEmail)
        => $"{companyName.Trim()}|{jobTitle.Trim()}|{recipientEmail.Trim()}".ToLowerInvariant();
}
