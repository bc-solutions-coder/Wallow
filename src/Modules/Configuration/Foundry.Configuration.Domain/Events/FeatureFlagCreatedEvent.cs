using Foundry.Configuration.Domain.Enums;
using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.Events;

public sealed record FeatureFlagCreatedEvent(
    Guid FlagId,
    string Key,
    FlagType FlagType) : DomainEvent;
