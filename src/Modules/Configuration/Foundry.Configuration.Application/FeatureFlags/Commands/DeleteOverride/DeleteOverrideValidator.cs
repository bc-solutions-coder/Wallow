using FluentValidation;

namespace Foundry.Configuration.Application.FeatureFlags.Commands.DeleteOverride;

public sealed class DeleteOverrideValidator : AbstractValidator<DeleteOverrideCommand>
{
    public DeleteOverrideValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Override ID is required");
    }
}
