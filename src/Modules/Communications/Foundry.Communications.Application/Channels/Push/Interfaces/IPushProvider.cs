using Foundry.Communications.Domain.Channels.Push.Entities;

namespace Foundry.Communications.Application.Channels.Push.Interfaces;

public readonly record struct PushDeliveryResult(bool Success, string? ErrorMessage);

public interface IPushProvider
{
    Task<PushDeliveryResult> SendAsync(PushMessage message, string deviceToken, CancellationToken cancellationToken = default);
}
