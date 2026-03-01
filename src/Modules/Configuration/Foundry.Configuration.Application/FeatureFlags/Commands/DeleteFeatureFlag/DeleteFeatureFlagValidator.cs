using FluentValidation;

namespace Foundry.Configuration.Application.FeatureFlags.Commands.DeleteFeatureFlag;

public sealed class DeleteFeatureFlagValidator : AbstractValidator<DeleteFeatureFlagCommand>
{
    public DeleteFeatureFlagValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Feature flag ID is required");
    }
}
