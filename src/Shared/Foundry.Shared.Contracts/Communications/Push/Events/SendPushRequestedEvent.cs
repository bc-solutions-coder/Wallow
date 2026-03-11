// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Foundry.Shared.Contracts.Communications.Push.Events;

public sealed record SendPushRequestedEvent : IntegrationEvent
{
    public required Guid RecipientId { get; init; }
    public required Guid TenantId { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string NotificationType { get; init; }
}
