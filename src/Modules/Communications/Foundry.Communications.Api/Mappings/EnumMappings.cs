using Foundry.Communications.Api.Contracts.Email.Enums;
using Foundry.Communications.Domain.Enums;

namespace Foundry.Communications.Api.Mappings;

public static class EnumMappings
{
    public static NotificationType ToDomain(this ApiNotificationType api) => api switch
    {
        ApiNotificationType.TaskAssigned => NotificationType.TaskAssigned,
        ApiNotificationType.TaskCompleted => NotificationType.TaskCompleted,
        ApiNotificationType.TaskComment => NotificationType.TaskComment,
        ApiNotificationType.SystemAlert => NotificationType.SystemAlert,
        ApiNotificationType.BillingInvoice => NotificationType.BillingInvoice,
        ApiNotificationType.Mention => NotificationType.Mention,
        ApiNotificationType.Announcement => NotificationType.Announcement,
        ApiNotificationType.SystemNotification => NotificationType.SystemNotification,
        _ => throw new ArgumentOutOfRangeException(nameof(api), api, null)
    };

    public static ApiNotificationType ToApi(this NotificationType domain) => domain switch
    {
        NotificationType.TaskAssigned => ApiNotificationType.TaskAssigned,
        NotificationType.TaskCompleted => ApiNotificationType.TaskCompleted,
        NotificationType.TaskComment => ApiNotificationType.TaskComment,
        NotificationType.SystemAlert => ApiNotificationType.SystemAlert,
        NotificationType.BillingInvoice => ApiNotificationType.BillingInvoice,
        NotificationType.Mention => ApiNotificationType.Mention,
        NotificationType.Announcement => ApiNotificationType.Announcement,
        NotificationType.SystemNotification => ApiNotificationType.SystemNotification,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
    };
}
