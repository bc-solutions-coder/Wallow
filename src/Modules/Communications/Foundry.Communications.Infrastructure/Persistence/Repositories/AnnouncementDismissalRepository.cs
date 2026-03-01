using Foundry.Communications.Application.Announcements.Interfaces;
using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Announcements.Identity;
using Foundry.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Foundry.Communications.Infrastructure.Persistence.Repositories;

public sealed class AnnouncementDismissalRepository : IAnnouncementDismissalRepository
{
    private readonly CommunicationsDbContext _context;

    public AnnouncementDismissalRepository(CommunicationsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AnnouncementDismissal>> GetByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        return await _context.AnnouncementDismissals
            .Where(d => d.UserId == userId)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(AnnouncementId announcementId, UserId userId, CancellationToken ct = default)
    {
        return _context.AnnouncementDismissals
            .AnyAsync(d => d.AnnouncementId == announcementId && d.UserId == userId, ct);
    }

    public async Task AddAsync(AnnouncementDismissal dismissal, CancellationToken ct = default)
    {
        await _context.AnnouncementDismissals.AddAsync(dismissal, ct);
        await _context.SaveChangesAsync(ct);
    }
}
