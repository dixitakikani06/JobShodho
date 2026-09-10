using JobShodho.Models;

namespace JobShodho.Services.Interfaces;

public interface ICandidateProfileService
{
    Task<CandidateProfile> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<CandidateProfile> UpdateProfileAsync(CandidateProfile profile, CancellationToken cancellationToken = default);
}
