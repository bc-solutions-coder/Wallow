using Foundry.Communications.Domain.Preferences;
using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Application.Preferences.Interfaces;

public interface INotificationPreferenceChecker
{
    Task<bool> IsChannelEnabledAsync(UserId userId, ChannelType channelType, string notificationType, CancellationToken cancellationToken = default);
}
