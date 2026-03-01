using Foundry.Communications.Application.Channels.Email.DTOs;
using Foundry.Communications.Application.Channels.Email.Interfaces;
using Foundry.Communications.Application.Channels.Email.Mappings;
using Foundry.Communications.Domain.Channels.Email.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Application.Channels.Email.Queries.GetEmailPreferences;

public sealed class GetEmailPreferencesHandler(IEmailPreferenceRepository preferenceRepository)
{
    public async Task<Result<IReadOnlyList<EmailPreferenceDto>>> Handle(
        GetEmailPreferencesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<EmailPreference> preferences = await preferenceRepository.GetByUserIdAsync(
            query.UserId,
            cancellationToken);

        List<EmailPreferenceDto> dtos = preferences.Select(p => p.ToDto()).ToList();

        return Result.Success<IReadOnlyList<EmailPreferenceDto>>(dtos);
    }
}
