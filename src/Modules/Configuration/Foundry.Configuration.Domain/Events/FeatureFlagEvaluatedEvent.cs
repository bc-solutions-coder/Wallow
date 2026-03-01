using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.Events;

public sealed record FeatureFlagEvaluatedEvent(
    string FlagKey,
    Guid TenantId,
    Guid? UserId,
    string Result,
    string Reason,
    DateTimeOffset Timestamp) : DomainEvent;
