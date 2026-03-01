namespace Foundry.Communications.Api.Contracts.Email.Enums;

/// <summary>Type of email notification for API contracts.</summary>
public enum ApiNotificationType
{
    /// <summary>Task assignment notification.</summary>
    TaskAssigned = 0,

    /// <summary>Task completion notification.</summary>
    TaskCompleted = 1,

    /// <summary>Billing invoice notification.</summary>
    BillingInvoice = 2,

    /// <summary>System notification.</summary>
    SystemNotification = 3
}
