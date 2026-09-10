using JobShodho.ViewModels;

namespace JobShodho.Services.Interfaces;

public interface IEmailBatchService
{
    /// <summary>Atomically claims up to <paramref name="maxCount"/> ReadyForReview, not-yet-sent jobs by
    /// flipping them to Sending. Two concurrent callers can never claim the same row.</summary>
    Task<List<int>> ClaimNextBatchAsync(int maxCount, CancellationToken cancellationToken = default);

    /// <summary>Sends every claimed job (already in the Sending state), never letting one failure abort the rest.</summary>
    Task<BatchSendResultViewModel> ProcessClaimedAsync(List<int> jobApplicationIds, CancellationToken cancellationToken = default);

    Task<BatchSendResultViewModel> SendNextBatchAsync(int maxCount = 10, CancellationToken cancellationToken = default);

    Task<BatchSendResultViewModel> SendSingleAsync(int jobApplicationId, CancellationToken cancellationToken = default);

    Task<BatchSendResultViewModel> RetryAsync(int jobApplicationId, CancellationToken cancellationToken = default);
}
