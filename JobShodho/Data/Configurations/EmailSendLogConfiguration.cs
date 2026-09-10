using JobShodho.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobShodho.Data.Configurations;

public class EmailSendLogConfiguration : IEntityTypeConfiguration<EmailSendLog>
{
    public void Configure(EntityTypeBuilder<EmailSendLog> builder)
    {
        builder.Property(l => l.Status).HasConversion<int>();

        builder.HasOne(l => l.JobApplication)
            .WithMany(j => j.EmailSendLogs)
            .HasForeignKey(l => l.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.JobApplicationId);
    }
}
