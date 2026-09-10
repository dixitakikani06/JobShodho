using ClosedXML.Excel;
using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobShodho.Tests;

public class ExcelImportServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Stream BuildWorkbook(params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Jobs");
        string[] headers = { "CompanyName", "JobTitle", "RecipientEmail", "JobDescription", "JobUrl", "Location", "Source" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                ws.Cell(r + 2, c + 1).Value = rows[r][c];
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ImportAsync_ImportsValidRows_SkipsEmptyDuplicateAndInvalidRows()
    {
        await using var db = CreateContext();
        var service = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);

        using var stream = BuildWorkbook(
            new[] { "ABC Ltd", ".NET Developer", "hr@abc.com", "Looking for a .NET developer.", "https://abc.com/1", "Ahmedabad", "LinkedIn" },
            new[] { "XYZ Ltd", "Backend Developer", "jobs@xyz.com", "Looking for a backend developer.", "https://xyz.com/2", "Remote", "Naukri" },
            new string[] { "", "", "", "", "", "", "" },
            new[] { "BadCo", "Dev", "not-an-email", "Some description", "", "", "" },
            new[] { "ABC Ltd", ".NET Developer", "hr@abc.com", "Duplicate of row 1.", "https://abc.com/1", "Ahmedabad", "LinkedIn" });

        var result = await service.ImportAsync(stream, "sample.xlsx");

        Assert.Equal(4, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(1, result.InvalidCount);
        Assert.Equal(2, await db.JobApplications.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_SetsPendingStatusAndNotSentByDefault()
    {
        await using var db = CreateContext();
        var service = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);

        using var stream = BuildWorkbook(
            new[] { "ABC Ltd", ".NET Developer", "hr@abc.com", "Looking for a .NET developer.", "https://abc.com/1", "Ahmedabad", "LinkedIn" });

        await service.ImportAsync(stream, "sample.xlsx");

        var job = await db.JobApplications.SingleAsync();
        Assert.Equal(Models.Enums.EmailStatus.Pending, job.EmailStatus);
        Assert.False(job.IsEmailSent);
    }

    [Fact]
    public async Task ImportAsync_DetectsDuplicatesAgainstExistingDatabaseRecords()
    {
        await using var db = CreateContext();
        db.JobApplications.Add(new JobApplication
        {
            CompanyName = "ABC Ltd",
            JobTitle = ".NET Developer",
            RecipientEmail = "hr@abc.com",
            JobDescription = "Existing record."
        });
        await db.SaveChangesAsync();

        var service = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);
        using var stream = BuildWorkbook(
            new[] { "ABC Ltd", ".NET Developer", "hr@abc.com", "Same job, imported again.", "", "", "" });

        var result = await service.ImportAsync(stream, "sample.xlsx");

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.DuplicateCount);
    }

    [Fact]
    public async Task ImportAsync_RejectsFileMissingRequiredColumns()
    {
        await using var db = CreateContext();
        var service = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Jobs");
        ws.Cell(1, 1).Value = "CompanyName";
        ws.Cell(1, 2).Value = "JobTitle";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ImportAsync(stream, "sample.xlsx");

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains(result.Errors, e => e.Contains("Missing required column"));
    }

    [Fact]
    public async Task ImportAsync_RejectsNonXlsxFile()
    {
        await using var db = CreateContext();
        var service = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.ImportAsync(stream, "sample.csv");

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains(result.Errors, e => e.Contains("Invalid file format"));
    }
}
