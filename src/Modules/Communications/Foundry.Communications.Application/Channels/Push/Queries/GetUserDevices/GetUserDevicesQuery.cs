namespace Foundry.Communications.Application.Channels.Push.Queries.GetUserDevices;

public sealed record GetUserDevicesQuery(Guid UserId, Guid TenantId);
