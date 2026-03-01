using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.Events;

public sealed record FeatureFlagDeletedEvent(
    Guid FlagId,
    string Key) : DomainEvent;
