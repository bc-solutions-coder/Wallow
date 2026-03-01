namespace Foundry.Communications.Application.Channels.InApp.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid UserId);
