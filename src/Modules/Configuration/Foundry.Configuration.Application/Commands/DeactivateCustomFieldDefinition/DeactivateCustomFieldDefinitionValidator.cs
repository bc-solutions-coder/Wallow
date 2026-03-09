using FluentValidation;

namespace Foundry.Configuration.Application.Commands.DeactivateCustomFieldDefinition;

public sealed class DeactivateCustomFieldDefinitionValidator : AbstractValidator<DeactivateCustomFieldDefinitionCommand>
{
    public DeactivateCustomFieldDefinitionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Custom field definition ID is required");
    }
}
