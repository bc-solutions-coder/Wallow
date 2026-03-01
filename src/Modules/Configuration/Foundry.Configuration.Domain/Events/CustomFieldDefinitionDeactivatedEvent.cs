using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.Events;

public sealed record CustomFieldDefinitionDeactivatedEvent(
    Guid DefinitionId,
    Guid TenantId,
    string EntityType,
    string FieldKey) : DomainEvent;
