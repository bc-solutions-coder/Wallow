using Foundry.Communications.Application.Channels.Push.Interfaces;
using Foundry.Communications.Domain.Channels.Push;
using Foundry.Communications.Domain.Channels.Push.Identity;
using Foundry.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Foundry.Communications.Infrastructure.Persistence.Repositories;

public sealed class DeviceRegistrationRepository(CommunicationsDbContext context) : IDeviceRegistrationRepository
{
    public Task<DeviceRegistration?> GetByIdAsync(DeviceRegistrationId id, CancellationToken cancellationToken = default)
    {
        return context.DeviceRegistrations
            .AsTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceRegistration>> GetActiveByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await context.DeviceRegistrations
            .AsTracking()
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync(cancellationToken);
    }

    public void Add(DeviceRegistration registration)
    {
        context.DeviceRegistrations.Add(registration);
    }

    public void Update(DeviceRegistration registration)
    {
        context.DeviceRegistrations.Update(registration);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
