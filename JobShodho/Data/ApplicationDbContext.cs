using JobShodho.Models;
using Microsoft.EntityFrameworkCore;

namespace JobShodho.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<EmailSendLog> EmailSendLogs => Set<EmailSendLog>();
    public DbSet<InterviewContent> InterviewContents => Set<InterviewContent>();
    public DbSet<AiGenerationLog> AiGenerationLogs => Set<AiGenerationLog>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
