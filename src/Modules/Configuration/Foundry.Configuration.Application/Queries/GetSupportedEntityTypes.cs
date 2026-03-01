using Foundry.Configuration.Application.Contracts.DTOs;
using Foundry.Shared.Kernel.CustomFields;

namespace Foundry.Configuration.Application.Queries;

public sealed record GetSupportedEntityTypes;

public sealed class GetSupportedEntityTypesHandler
{
    public Task<IReadOnlyList<EntityTypeDto>> Handle(
        GetSupportedEntityTypes _,
        CancellationToken __)
    {
        List<EntityTypeDto> entityTypes = CustomFieldRegistry.GetSupportedEntityTypes()
            .Select(e => new EntityTypeDto(e.EntityType, e.Module, e.Description))
            .ToList();

        return Task.FromResult<IReadOnlyList<EntityTypeDto>>(entityTypes);
    }
}
