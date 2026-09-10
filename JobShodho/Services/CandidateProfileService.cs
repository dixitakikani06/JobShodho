using JobShodho.Data;
using JobShodho.Models;
using JobShodho.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobShodho.Services;

public class CandidateProfileService : ICandidateProfileService
{
    private readonly ApplicationDbContext _db;

    public CandidateProfileService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CandidateProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        profile = new CandidateProfile
        {
            FullName = string.Empty,
            Email = string.Empty,
            YearsOfExperience = 4,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.CandidateProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<CandidateProfile> UpdateProfileAsync(CandidateProfile profile, CancellationToken cancellationToken = default)
    {
        var existing = await GetProfileAsync(cancellationToken);

        existing.FullName = profile.FullName;
        existing.Email = profile.Email;
        existing.Phone = profile.Phone;
        existing.Location = profile.Location;
        existing.YearsOfExperience = profile.YearsOfExperience;
        existing.ProfessionalSummary = profile.ProfessionalSummary;
        existing.Skills = profile.Skills;
        existing.LinkedInUrl = profile.LinkedInUrl;
        existing.GitHubUrl = profile.GitHubUrl;
        existing.PortfolioUrl = profile.PortfolioUrl;
        existing.EmailSignature = profile.EmailSignature;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
