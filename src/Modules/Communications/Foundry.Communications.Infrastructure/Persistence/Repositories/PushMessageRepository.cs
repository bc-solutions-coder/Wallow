using Foundry.Communications.Application.Channels.Push.Interfaces;
using Foundry.Communications.Domain.Channels.Push.Entities;
using Foundry.Communications.Domain.Channels.Push.Identity;
using Microsoft.EntityFrameworkCore;

namespace Foundry.Communications.Infrastructure.Persistence.Repositories;

public sealed class PushMessageRepository(CommunicationsDbContext context) : IPushMessageRepository
{
    public Task<PushMessage?> GetByIdAsync(PushMessageId id, CancellationToken cancellationToken = default)
    {
        return context.PushMessages
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public void Add(PushMessage message)
    {
        context.PushMessages.Add(message);
    }

    public void Update(PushMessage message)
    {
        context.PushMessages.Update(message);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
