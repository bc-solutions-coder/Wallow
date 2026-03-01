namespace Foundry.Shared.Contracts.Realtime;

public interface IPresenceService
{
    Task TrackConnectionAsync(string userId, string connectionId, CancellationToken ct = default);
    Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default);
    Task SetPageContextAsync(string connectionId, string pageContext, CancellationToken ct = default);
    Task<IReadOnlyList<UserPresence>> GetOnlineUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserPresence>> GetUsersOnPageAsync(string pageContext, CancellationToken ct = default);
    Task<bool> IsUserOnlineAsync(string userId, CancellationToken ct = default);
    Task<string?> GetUserIdByConnectionAsync(string connectionId, CancellationToken ct = default);
}
