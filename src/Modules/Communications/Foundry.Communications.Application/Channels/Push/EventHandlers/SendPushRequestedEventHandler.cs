using Foundry.Communications.Application.Channels.Push.Commands.SendPush;
using Foundry.Shared.Contracts.Communications.Push.Events;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Foundry.Communications.Application.Channels.Push.EventHandlers;

public static partial class SendPushRequestedEventHandler
{
    public static async Task HandleAsync(
        SendPushRequestedEvent @event,
        IMessageBus bus,
        ILogger<SendPushRequestedEvent> logger,
        CancellationToken cancellationToken = default)
    {
        LogProcessingSendPush(logger, @event.RecipientId);

        try
        {
            SendPushCommand command = new(
                new UserId(@event.RecipientId),
                new TenantId(@event.TenantId),
                @event.Title,
                @event.Body,
                @event.NotificationType);

            await bus.InvokeAsync<Result>(command, cancellationToken);

            LogPushSent(logger, @event.RecipientId);
        }
        catch (Exception ex)
        {
            LogPushFailed(logger, ex, @event.RecipientId, @event.EventId);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing SendPushRequestedEvent for recipient {RecipientId}")]
    private static partial void LogProcessingSendPush(ILogger logger, Guid recipientId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Push notification sent successfully for recipient {RecipientId}")]
    private static partial void LogPushSent(ILogger logger, Guid recipientId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send push for recipient {RecipientId} for event {EventId}")]
    private static partial void LogPushFailed(ILogger logger, Exception ex, Guid recipientId, Guid eventId);
}
