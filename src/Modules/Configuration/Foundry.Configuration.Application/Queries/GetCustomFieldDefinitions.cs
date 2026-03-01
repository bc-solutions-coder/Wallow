using Foundry.Configuration.Application.Contracts;
using Foundry.Configuration.Application.Contracts.DTOs;
using Foundry.Configuration.Domain.Entities;

namespace Foundry.Configuration.Application.Queries;

public sealed record GetCustomFieldDefinitions(string EntityType, bool IncludeInactive = false);

public sealed class GetCustomFieldDefinitionsHandler
{
    private readonly ICustomFieldDefinitionRepository _repository;

    public GetCustomFieldDefinitionsHandler(ICustomFieldDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> Handle(
        GetCustomFieldDefinitions query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomFieldDefinition> definitions = await _repository.GetByEntityTypeAsync(
            query.EntityType,
            query.IncludeInactive,
            cancellationToken);

        return definitions.ToDtoList();
    }
}
