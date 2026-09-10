using JobShodho.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobShodho.Data.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.Property(j => j.EmailStatus).HasConversion<int>();

        builder.HasOne(j => j.SelectedResume)
            .WithMany(r => r.JobApplications)
            .HasForeignKey(j => j.SelectedResumeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.InterviewContent)
            .WithOne(i => i.JobApplication)
            .HasForeignKey<JobApplication>(j => j.InterviewContentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(j => j.RecipientEmail);
        builder.HasIndex(j => j.EmailStatus);
        builder.HasIndex(j => j.IsEmailSent);
        builder.HasIndex(j => j.CompanyName);
        builder.HasIndex(j => j.CreatedAt);
    }
}
