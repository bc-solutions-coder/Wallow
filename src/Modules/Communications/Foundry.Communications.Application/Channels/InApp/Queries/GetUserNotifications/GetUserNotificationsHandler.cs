using Foundry.Communications.Application.Channels.InApp.DTOs;
using Foundry.Communications.Application.Channels.InApp.Interfaces;
using Foundry.Communications.Application.Channels.InApp.Mappings;
using Foundry.Shared.Kernel.Pagination;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Application.Channels.InApp.Queries.GetUserNotifications;

public sealed class GetUserNotificationsHandler(INotificationRepository notificationRepository)
{
    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        GetUserNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        (IReadOnlyList<Domain.Channels.InApp.Entities.Notification>? notifications, int totalCount) = await notificationRepository.GetByUserIdPagedAsync(
            query.UserId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        List<NotificationDto> dtos = notifications.Select(n => n.ToDto()).ToList();

        PagedResult<NotificationDto> pagedResult = new(
            dtos,
            totalCount,
            query.PageNumber,
            query.PageSize);

        return Result.Success(pagedResult);
    }
}
