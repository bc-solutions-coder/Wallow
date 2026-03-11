using Foundry.Communications.Domain.Channels.Push.Enums;

namespace Foundry.Communications.Application.Channels.Push.Interfaces;

public interface IPushProviderFactory
{
    IPushProvider GetProvider(PushPlatform platform);
}
