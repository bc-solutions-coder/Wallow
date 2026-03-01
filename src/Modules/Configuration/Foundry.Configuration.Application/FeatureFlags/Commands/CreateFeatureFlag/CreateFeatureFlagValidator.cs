using FluentValidation;

namespace Foundry.Configuration.Application.FeatureFlags.Commands.CreateFeatureFlag;

public sealed class CreateFeatureFlagValidator : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Feature flag key is required")
            .MaximumLength(100).WithMessage("Feature flag key must not exceed 100 characters")
            .Matches("^[a-zA-Z0-9-]+$")
            .WithMessage("Feature flag key must contain only alphanumeric characters and dashes");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Feature flag name is required")
            .MaximumLength(200).WithMessage("Feature flag name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.FlagType)
            .IsInEnum().WithMessage("Flag type must be a valid value");

        RuleFor(x => x.RolloutPercentage)
            .InclusiveBetween(0, 100).WithMessage("Rollout percentage must be between 0 and 100")
            .When(x => x.RolloutPercentage is not null);
    }
}
