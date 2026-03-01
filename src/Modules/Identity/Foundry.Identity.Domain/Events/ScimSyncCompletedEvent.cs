using Foundry.Shared.Kernel.Domain;

namespace Foundry.Identity.Domain.Events;

public sealed record ScimSyncCompletedEvent(
    Guid TenantId,
    string Operation,
    string ResourceType,
    bool Success,
    string? ErrorMessage) : DomainEvent;
