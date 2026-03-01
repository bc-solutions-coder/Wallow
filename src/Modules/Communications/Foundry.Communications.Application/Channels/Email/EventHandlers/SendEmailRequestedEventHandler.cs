using Foundry.Shared.Contracts.Communications.Email;
using Foundry.Shared.Contracts.Communications.Email.Events;
using Microsoft.Extensions.Logging;

namespace Foundry.Communications.Application.Channels.Email.EventHandlers;

public static partial class SendEmailRequestedEventHandler
{
    public static async Task HandleAsync(
        SendEmailRequestedEvent @event,
        IEmailService emailService,
        ILogger<SendEmailRequestedEvent> logger,
        CancellationToken cancellationToken = default)
    {
        LogProcessingSendEmail(logger, @event.SourceModule ?? "Unknown", @event.To);

        try
        {
            await emailService.SendAsync(
                @event.To,
                @event.From,
                @event.Subject,
                @event.Body,
                cancellationToken);

            LogEmailSent(logger, @event.To);
        }
        catch (Exception ex)
        {
            LogEmailFailed(logger, ex, @event.To, @event.EventId);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing SendEmailRequestedEvent from {SourceModule} to {To}")]
    private static partial void LogProcessingSendEmail(ILogger logger, string sourceModule, string to);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent successfully to {To}")]
    private static partial void LogEmailSent(ILogger logger, string to);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email to {To} for event {EventId}")]
    private static partial void LogEmailFailed(ILogger logger, Exception ex, string to, Guid eventId);
}
