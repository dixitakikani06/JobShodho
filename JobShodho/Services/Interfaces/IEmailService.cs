namespace JobShodho.Services.Interfaces;

public record EmailSendResult(bool Success, string? ProviderMessageId, string? ErrorMessage);

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(string recipient, string subject, string body, string? attachmentPath, CancellationToken cancellationToken = default);
}
