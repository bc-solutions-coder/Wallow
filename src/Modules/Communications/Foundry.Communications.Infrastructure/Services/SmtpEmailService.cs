using System.Diagnostics;
using Foundry.Communications.Application.Channels.Email.Telemetry;
using Foundry.Shared.Contracts.Communications.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Foundry.Communications.Infrastructure.Services;

public sealed partial class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string? from,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        using MimeMessage message = BuildMessage(to, from, subject, body);

        await SendWithRetryAsync(message, cancellationToken);
    }

    public async Task SendWithAttachmentAsync(
        string to,
        string? from,
        string subject,
        string body,
        byte[] attachment,
        string attachmentName,
        string attachmentContentType = "application/octet-stream",
        CancellationToken cancellationToken = default)
    {
        const int maxAttachmentSizeBytes = 10 * 1024 * 1024; // 10MB

        if (attachment.Length > maxAttachmentSizeBytes)
        {
            throw new InvalidOperationException(
                $"Attachment size ({attachment.Length / 1024 / 1024}MB) exceeds maximum allowed size (10MB)");
        }

        using MimeMessage message = BuildMessageWithAttachment(to, from, subject, body, attachment, attachmentName, attachmentContentType);

        await SendWithRetryAsync(message, cancellationToken);
    }

    private MimeMessage BuildMessage(string to, string? from, string subject, string body)
    {
        MimeMessage message = new MimeMessage();

        message.From.Add(string.IsNullOrWhiteSpace(from)
            ? new MailboxAddress(_settings.DefaultFromName, _settings.DefaultFromAddress)
            : MailboxAddress.Parse(from));

        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = body
        };

        return message;
    }

    private MimeMessage BuildMessageWithAttachment(
        string to,
        string? from,
        string subject,
        string body,
        byte[] attachment,
        string attachmentName,
        string attachmentContentType)
    {
        MimeMessage message = new MimeMessage();

        message.From.Add(string.IsNullOrWhiteSpace(from)
            ? new MailboxAddress(_settings.DefaultFromName, _settings.DefaultFromAddress)
            : MailboxAddress.Parse(from));

        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        BodyBuilder builder = new BodyBuilder
        {
            HtmlBody = body
        };

        builder.Attachments.Add(attachmentName, attachment, ContentType.Parse(attachmentContentType));

        message.Body = builder.ToMessageBody();

        return message;
    }

    private async Task SendWithRetryAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using Activity? activity = EmailModuleTelemetry.ActivitySource.StartActivity("Email.Send");
        activity?.SetTag("email.to", message.To.ToString());
        activity?.SetTag("email.template", message.Subject);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _settings.MaxRetries)
        {
            attempt++;

            try
            {
                using SmtpClient client = new SmtpClient();
                client.Timeout = _settings.TimeoutSeconds * 1000;

                SecureSocketOptions secureSocketOptions = _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await client.ConnectAsync(_settings.Host, _settings.Port, secureSocketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
                {
                    await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                string recipients = message.To.ToString();
                LogEmailSent(_logger, recipients, message.Subject, attempt);

                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                LogEmailAttemptFailed(_logger, ex, message.To.ToString(), attempt, _settings.MaxRetries);

                if (attempt < _settings.MaxRetries)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 1000;
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }

        LogEmailAllAttemptsFailed(_logger, lastException, message.To.ToString(), _settings.MaxRetries);

        activity?.SetStatus(ActivityStatusCode.Error, lastException?.Message ?? "All retry attempts failed");
        if (lastException is not null)
        {
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", lastException.GetType().FullName },
                { "exception.message", lastException.Message }
            }));
        }

        throw new InvalidOperationException(
            $"Failed to send email after {_settings.MaxRetries} attempts. See inner exception for details.",
            lastException);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent successfully to {To} with subject '{Subject}' on attempt {Attempt}")]
    private static partial void LogEmailSent(ILogger logger, string to, string subject, int attempt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send email to {To} on attempt {Attempt}/{MaxRetries}")]
    private static partial void LogEmailAttemptFailed(ILogger logger, Exception ex, string to, int attempt, int maxRetries);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email to {To} after {MaxRetries} attempts")]
    private static partial void LogEmailAllAttemptsFailed(ILogger logger, Exception? ex, string to, int maxRetries);
}
