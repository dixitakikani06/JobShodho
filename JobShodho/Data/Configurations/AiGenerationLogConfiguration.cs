using JobShodho.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobShodho.Data.Configurations;

public class AiGenerationLogConfiguration : IEntityTypeConfiguration<AiGenerationLog>
{
    public void Configure(EntityTypeBuilder<AiGenerationLog> builder)
    {
        builder.Property(l => l.GenerationType).HasConversion<int>();
        builder.Property(l => l.Status).HasConversion<int>();

        builder.HasOne(l => l.JobApplication)
            .WithMany(j => j.AiGenerationLogs)
            .HasForeignKey(l => l.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.JobApplicationId);
    }
}
