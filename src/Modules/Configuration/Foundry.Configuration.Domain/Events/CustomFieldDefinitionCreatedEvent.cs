using Foundry.Shared.Kernel.CustomFields;
using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.Events;

public sealed record CustomFieldDefinitionCreatedEvent(
    Guid DefinitionId,
    Guid TenantId,
    string EntityType,
    string FieldKey,
    string DisplayName,
    CustomFieldType FieldType) : DomainEvent;
