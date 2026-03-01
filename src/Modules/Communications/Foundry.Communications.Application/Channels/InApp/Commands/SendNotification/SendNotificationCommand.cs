using Foundry.Communications.Domain.Channels.InApp.Enums;

namespace Foundry.Communications.Application.Channels.InApp.Commands.SendNotification;

public sealed record SendNotificationCommand(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message);
