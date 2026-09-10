using JobShodho.Options;
using JobShodho.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace JobShodho.Services;

public class EmailService : IEmailService
{
    private static readonly string[] AllowedAttachmentExtensions = { ".pdf", ".docx" };

    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(string recipient, string subject, string body, string? attachmentPath, CancellationToken cancellationToken = default)
    {
        if (!_emailOptions.IsConfigured)
        {
            return new EmailSendResult(false, null, "Email is not configured. Please set SMTP settings before sending.");
        }

        if (string.IsNullOrWhiteSpace(attachmentPath))
        {
            return new EmailSendResult(false, null, "Resume file could not be found. No resume is selected for this job.");
        }

        if (!File.Exists(attachmentPath))
        {
            _logger.LogWarning("Attachment file missing at {Path}", attachmentPath);
            return new EmailSendResult(false, null, "Resume file could not be found on disk.");
        }

        var extension = Path.GetExtension(attachmentPath).ToLowerInvariant();
        if (!AllowedAttachmentExtensions.Contains(extension))
        {
            return new EmailSendResult(false, null, $"Resume file type '{extension}' is not allowed as an attachment.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromEmail));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.MessageId = MimeUtils.GenerateMessageId();

        var builder = new BodyBuilder { TextBody = body };
        builder.Attachments.Add(attachmentPath, cancellationToken: cancellationToken);
        message.Body = builder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            var socketOptions = _emailOptions.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_emailOptions.Host, _emailOptions.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_emailOptions.Username))
            {
                await client.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {Recipient} with subject '{Subject}'", recipient, subject);
            return new EmailSendResult(true, message.MessageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return new EmailSendResult(false, null, "Unable to send this email. " + ex.Message);
        }
    }
}
