using JobShodho.Data;
using JobShodho.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;

namespace JobShodho.Tests;

public class DataSeederTests : IAsyncLifetime
{
    static DataSeederTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private readonly string _databaseName = $"JobShodho_Test_{Guid.NewGuid():N}";
    private ApplicationDbContext _db = null!;
    private FakeWebHostEnvironment _env = null!;

    public async Task InitializeAsync()
    {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
        _db = new ApplicationDbContext(options);
        await _db.Database.MigrateAsync();

        _env = new FakeWebHostEnvironment();
        Directory.CreateDirectory(_env.WebRootPath);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
        if (Directory.Exists(_env.WebRootPath))
        {
            Directory.Delete(_env.WebRootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SeedAsync_CreatesExpectedRowCounts_AndIsIdempotent()
    {
        await DataSeeder.SeedAsync(_db, _env, NullLogger.Instance);

        Assert.Equal(1, await _db.CandidateProfiles.CountAsync());
        Assert.Equal(3, await _db.JobApplications.CountAsync());
        Assert.Equal(2, await _db.Resumes.CountAsync());

        foreach (var job in await _db.JobApplications.ToListAsync())
        {
            Assert.Contains(".example", job.RecipientEmail);
        }

        // Running again must not duplicate rows.
        await DataSeeder.SeedAsync(_db, _env, NullLogger.Instance);
        Assert.Equal(1, await _db.CandidateProfiles.CountAsync());
        Assert.Equal(3, await _db.JobApplications.CountAsync());
        Assert.Equal(2, await _db.Resumes.CountAsync());
    }
}
