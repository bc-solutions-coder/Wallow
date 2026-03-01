using Foundry.Communications.Application.Channels.InApp.DTOs;
using Foundry.Communications.Domain.Channels.InApp.Entities;

namespace Foundry.Communications.Application.Channels.InApp.Mappings;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification notification)
    {
        return new NotificationDto(
            notification.Id.Value,
            notification.UserId,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.ReadAt,
            notification.CreatedAt,
            notification.UpdatedAt);
    }
}
