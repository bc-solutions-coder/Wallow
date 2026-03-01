using Foundry.Communications.Application.Channels.Email.DTOs;
using Foundry.Communications.Application.Channels.Email.Interfaces;
using Foundry.Communications.Application.Channels.Email.Mappings;
using Foundry.Communications.Domain.Channels.Email.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Application.Channels.Email.Commands.UpdateEmailPreferences;

public sealed class UpdateEmailPreferencesHandler(IEmailPreferenceRepository preferenceRepository)
{
    public async Task<Result<EmailPreferenceDto>> Handle(
        UpdateEmailPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        EmailPreference? preference = await preferenceRepository.GetByUserAndTypeAsync(
            command.UserId,
            command.NotificationType,
            cancellationToken);

        if (preference is null)
        {
            preference = EmailPreference.Create(
                command.UserId,
                command.NotificationType,
                command.IsEnabled);

            preferenceRepository.Add(preference);
        }
        else
        {
            if (command.IsEnabled)
            {
                preference.Enable();
            }
            else
            {
                preference.Disable();
            }
        }

        await preferenceRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(preference.ToDto());
    }
}
