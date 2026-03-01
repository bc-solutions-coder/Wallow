using Foundry.Storage.Domain.Entities;
using Foundry.Storage.Domain.Identity;

namespace Foundry.Storage.Application.Interfaces;

public interface IStorageBucketRepository
{
    Task<StorageBucket?> GetByIdAsync(StorageBucketId id, CancellationToken cancellationToken = default);
    Task<StorageBucket?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageBucket>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    void Add(StorageBucket bucket);
    void Update(StorageBucket bucket);
    void Remove(StorageBucket bucket);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
