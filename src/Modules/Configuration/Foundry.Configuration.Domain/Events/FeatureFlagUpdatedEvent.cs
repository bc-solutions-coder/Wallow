using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.Events;

public sealed record FeatureFlagUpdatedEvent(
    Guid FlagId,
    string Key,
    string ChangedProperties) : DomainEvent;
