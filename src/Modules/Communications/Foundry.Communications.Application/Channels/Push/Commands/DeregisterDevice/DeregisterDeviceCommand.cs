using Foundry.Communications.Domain.Channels.Push.Identity;

namespace Foundry.Communications.Application.Channels.Push.Commands.DeregisterDevice;

public sealed record DeregisterDeviceCommand(DeviceRegistrationId DeviceRegistrationId);
