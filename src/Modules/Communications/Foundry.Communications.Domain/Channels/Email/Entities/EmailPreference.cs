using Foundry.Communications.Domain.Channels.Email.Enums;
using Foundry.Communications.Domain.Channels.Email.Identity;
using Foundry.Shared.Kernel.Domain;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;

namespace Foundry.Communications.Domain.Channels.Email.Entities;

public sealed class EmailPreference : AggregateRoot<EmailPreferenceId>, ITenantScoped
{
    public TenantId TenantId { get; set; }
    public Guid UserId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public bool IsEnabled { get; private set; }

    private EmailPreference() { }

    private EmailPreference(
        Guid userId,
        NotificationType notificationType,
        bool isEnabled)
        : base(EmailPreferenceId.New())
    {
        UserId = userId;
        NotificationType = notificationType;
        IsEnabled = isEnabled;
        SetCreated();
    }

    public static EmailPreference Create(
        Guid userId,
        NotificationType notificationType,
        bool isEnabled = true)
    {
        return new EmailPreference(userId, notificationType, isEnabled);
    }

    public void Enable()
    {
        IsEnabled = true;
        SetUpdated();
    }

    public void Disable()
    {
        IsEnabled = false;
        SetUpdated();
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        SetUpdated();
    }
}
