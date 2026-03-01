using FluentValidation;

namespace Foundry.Configuration.Application.FeatureFlags.Commands.UpdateFeatureFlag;

public sealed class UpdateFeatureFlagValidator : AbstractValidator<UpdateFeatureFlagCommand>
{
    public UpdateFeatureFlagValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Feature flag ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Feature flag name is required")
            .MaximumLength(200).WithMessage("Feature flag name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.RolloutPercentage)
            .InclusiveBetween(0, 100).WithMessage("Rollout percentage must be between 0 and 100")
            .When(x => x.RolloutPercentage is not null);
    }
}
