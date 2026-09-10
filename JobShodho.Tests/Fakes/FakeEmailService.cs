using JobShodho.Services.Interfaces;

namespace JobShodho.Tests.Fakes;

/// <summary>Stands in for the real SMTP-backed IEmailService so batch-processing tests never touch the network.</summary>
public class FakeEmailService : IEmailService
{
    public Func<string, EmailSendResult> ResultFor { get; set; } = _ => new EmailSendResult(true, "fake-message-id", null);

    public List<string> SentTo { get; } = new();

    public Task<EmailSendResult> SendAsync(string recipient, string subject, string body, string? attachmentPath, CancellationToken cancellationToken = default)
    {
        SentTo.Add(recipient);
        return Task.FromResult(ResultFor(recipient));
    }
}
