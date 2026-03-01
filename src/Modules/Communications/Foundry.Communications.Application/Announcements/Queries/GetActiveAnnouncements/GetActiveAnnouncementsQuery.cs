using Foundry.Communications.Application.Announcements.DTOs;
using Foundry.Communications.Application.Announcements.Services;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Application.Announcements.Queries.GetActiveAnnouncements;

public sealed record GetActiveAnnouncementsQuery(
    Guid UserId,
    Guid TenantId,
    string? PlanName,
    IReadOnlyList<string> Roles);

public sealed class GetActiveAnnouncementsHandler
{
    private readonly IAnnouncementTargetingService _targetingService;

    public GetActiveAnnouncementsHandler(IAnnouncementTargetingService targetingService)
    {
        _targetingService = targetingService;
    }

    public async Task<Result<IReadOnlyList<AnnouncementDto>>> Handle(
        GetActiveAnnouncementsQuery query,
        CancellationToken ct)
    {
        UserContext userContext = new UserContext(
            UserId.Create(query.UserId),
            TenantId.Create(query.TenantId),
            query.PlanName,
            query.Roles);

        IReadOnlyList<AnnouncementDto> announcements = await _targetingService.GetActiveAnnouncementsForUserAsync(userContext, ct);
        return Result.Success(announcements);
    }
}
