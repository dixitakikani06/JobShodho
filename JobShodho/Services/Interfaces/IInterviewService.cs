using JobShodho.Services.Ai;

namespace JobShodho.Services.Interfaces;

public interface IInterviewService
{
    Task<InterviewGenerationResponse?> GetParsedContentAsync(int jobApplicationId, CancellationToken cancellationToken = default);

    Task<(bool Success, string? PdfPath, string? ErrorMessage)> GeneratePdfAsync(int jobApplicationId, CancellationToken cancellationToken = default);
}
