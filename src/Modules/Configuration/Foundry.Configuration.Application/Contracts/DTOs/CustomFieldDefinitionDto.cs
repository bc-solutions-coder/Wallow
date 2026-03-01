using Foundry.Shared.Kernel.CustomFields;

namespace Foundry.Configuration.Application.Contracts.DTOs;

public sealed record CustomFieldDefinitionDto
{
    public required Guid Id { get; init; }
    public required string EntityType { get; init; }
    public required string FieldKey { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required CustomFieldType FieldType { get; init; }
    public required int DisplayOrder { get; init; }
    public required bool IsRequired { get; init; }
    public required bool IsActive { get; init; }
    public FieldValidationRules? ValidationRules { get; init; }
    public IReadOnlyList<CustomFieldOption>? Options { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? UpdatedAt { get; init; }
}

public sealed record EntityTypeDto(string EntityType, string Module, string Description);
