using JobShodho.Models;
using JobShodho.Models.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobShodho.Data;

/// <summary>Development-only seed data so the dashboard/jobs/resumes pages have something to show on first run.
/// Uses example.com/.example addresses only - never real recipient emails - and is a no-op once any row exists.</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IWebHostEnvironment env, ILogger logger)
    {
        if (!await db.CandidateProfiles.AnyAsync())
        {
            db.CandidateProfiles.Add(new CandidateProfile
            {
                FullName = "Jordan Candidate",
                Email = "jordan.candidate@example.com",
                Phone = "+1 555 0100",
                Location = "Remote",
                YearsOfExperience = 4,
                ProfessionalSummary = "Backend-focused .NET developer with 4 years building REST APIs, EF Core data access, and SQL Server-backed web applications.",
                Skills = "ASP.NET Core, C#, .NET, MVC, Entity Framework, SQL Server, REST APIs, JavaScript, HTML, CSS, Git",
                LinkedInUrl = "https://linkedin.com/in/example",
                GitHubUrl = "https://github.com/example",
                EmailSignature = "Best regards,\nJordan Candidate"
            });
            logger.LogInformation("Seeded default candidate profile.");
        }

        if (!await db.JobApplications.AnyAsync())
        {
            db.JobApplications.AddRange(
                new JobApplication
                {
                    CompanyName = "Acme Software",
                    JobTitle = ".NET Developer",
                    RecipientEmail = "careers@acmesoftware.example",
                    JobDescription = "We are looking for a .NET developer with experience in ASP.NET Core, C#, and SQL Server to join our backend team building internal tools.",
                    JobUrl = "https://acmesoftware.example/careers/dotnet-developer",
                    Location = "Remote",
                    Source = "Seed Data",
                    EmailStatus = EmailStatus.Pending
                },
                new JobApplication
                {
                    CompanyName = "Bright Labs",
                    JobTitle = "Backend Developer",
                    RecipientEmail = "jobs@brightlabs.example",
                    JobDescription = "Bright Labs is hiring a backend developer to design and maintain REST APIs using ASP.NET Core and Entity Framework.",
                    JobUrl = "https://brightlabs.example/jobs/backend-developer",
                    Location = "Ahmedabad, India",
                    Source = "Seed Data",
                    EmailStatus = EmailStatus.Pending
                },
                new JobApplication
                {
                    CompanyName = "Vertex Systems",
                    JobTitle = "Software Engineer - .NET",
                    RecipientEmail = "hr@vertexsystems.example",
                    JobDescription = "Vertex Systems needs a software engineer comfortable with C#, MVC, SQL Server and building maintainable web applications.",
                    JobUrl = "https://vertexsystems.example/jobs/dotnet-engineer",
                    Location = "Hybrid",
                    Source = "Seed Data",
                    EmailStatus = EmailStatus.Pending
                });
            logger.LogInformation("Seeded 3 sample job applications.");
        }

        if (!await db.Resumes.AnyAsync())
        {
            await SeedResumeAsync(db, env, "General .NET Developer Resume",
                "Jordan Candidate - .NET Developer\n4 years of experience with ASP.NET Core, C#, SQL Server, and REST APIs.\nBuilt and maintained internal business web applications using MVC and Entity Framework.");
            await SeedResumeAsync(db, env, "Backend / API Focused Resume",
                "Jordan Candidate - Backend Developer\n4 years building REST APIs with ASP.NET Core and Entity Framework, backed by SQL Server.\nComfortable with Git-based workflows and writing maintainable, testable backend code.");
            logger.LogInformation("Seeded 2 sample resumes.");
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedResumeAsync(ApplicationDbContext db, IWebHostEnvironment env, string name, string text)
    {
        var storedFileName = $"{Guid.NewGuid():N}.pdf";
        var folderPath = Path.Combine(env.WebRootPath, "uploads", "resumes");
        Directory.CreateDirectory(folderPath);
        var fullPath = Path.Combine(folderPath, storedFileName);

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Content().Column(col =>
                {
                    foreach (var line in text.Split('\n'))
                    {
                        col.Item().Text(line);
                    }
                });
            });
        }).GeneratePdf();

        await File.WriteAllBytesAsync(fullPath, pdfBytes);

        db.Resumes.Add(new Resume
        {
            Name = name,
            OriginalFileName = $"{name}.pdf",
            StoredFileName = storedFileName,
            FilePath = $"/uploads/resumes/{storedFileName}",
            ContentType = "application/pdf",
            FileSizeBytes = pdfBytes.Length,
            ExtractedText = text,
            IsActive = true
        });
    }
}
