using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Announcements.Identity;
using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Application.Announcements.Interfaces;

public interface IAnnouncementDismissalRepository
{
    Task<IReadOnlyList<AnnouncementDismissal>> GetByUserIdAsync(UserId userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(AnnouncementId announcementId, UserId userId, CancellationToken ct = default);
    Task AddAsync(AnnouncementDismissal dismissal, CancellationToken ct = default);
}
