using JobShodho.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobShodho.Data.Configurations;

public class InterviewContentConfiguration : IEntityTypeConfiguration<InterviewContent>
{
    public void Configure(EntityTypeBuilder<InterviewContent> builder)
    {
        builder.HasIndex(i => i.JobApplicationId).IsUnique();
    }
}
