using FluentValidation;

namespace Foundry.Communications.Application.Channels.Push.Commands.RegisterDevice;

public sealed class RegisterDeviceValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Device token is required");

        RuleFor(x => x.Platform)
            .IsInEnum().WithMessage("Invalid push platform");
    }
}
