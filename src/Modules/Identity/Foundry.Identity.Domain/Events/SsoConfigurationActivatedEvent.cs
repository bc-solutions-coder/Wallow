using Foundry.Shared.Kernel.Domain;

namespace Foundry.Identity.Domain.Events;

public sealed record SsoConfigurationActivatedEvent(
    Guid SsoConfigurationId,
    Guid TenantId,
    string DisplayName,
    string Protocol) : DomainEvent;
