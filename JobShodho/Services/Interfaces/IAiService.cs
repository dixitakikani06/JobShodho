using JobShodho.Services.Ai;

namespace JobShodho.Services.Interfaces;

public interface IAiService
{
    Task<AiActionResult> GenerateEmailAsync(int jobApplicationId, CancellationToken cancellationToken = default);

    Task<AiActionResult> SelectResumeAsync(int jobApplicationId, CancellationToken cancellationToken = default);

    Task<AiActionResult> GenerateInterviewQuestionsAsync(int jobApplicationId, CancellationToken cancellationToken = default);

    /// <summary>Runs resume selection, email, then interview generation for one job, in that order.</summary>
    Task<AiActionResult> GenerateAllAsync(int jobApplicationId, CancellationToken cancellationToken = default);
}
