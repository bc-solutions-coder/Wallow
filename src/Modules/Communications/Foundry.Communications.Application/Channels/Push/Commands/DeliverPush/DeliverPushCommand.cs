using Foundry.Communications.Domain.Channels.Push.Enums;
using Foundry.Communications.Domain.Channels.Push.Identity;

namespace Foundry.Communications.Application.Channels.Push.Commands.DeliverPush;

public sealed record DeliverPushCommand(
    PushMessageId PushMessageId,
    DeviceRegistrationId DeviceRegistrationId,
    string Token,
    PushPlatform Platform);
