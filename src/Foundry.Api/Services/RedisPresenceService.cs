using Foundry.Shared.Contracts.Realtime;
using StackExchange.Redis;

namespace Foundry.Api.Services;

internal sealed partial class RedisPresenceService(
    IConnectionMultiplexer redis,
    ILogger<RedisPresenceService> logger) : IPresenceService
{
    private const string ConnectionToUserKey = "presence:conn2user";
    private const string UserConnectionsPrefix = "presence:user:";
    private const string ConnectionPagePrefix = "presence:connpage:";
    private const string PageViewersPrefix = "presence:page:";
    private static readonly TimeSpan _connectionTtl = TimeSpan.FromMinutes(30);

    private IDatabase Db => redis.GetDatabase();

    public async Task TrackConnectionAsync(string userId, string connectionId, CancellationToken ct = default)
    {
        IDatabase db = Db;
        IBatch batch = db.CreateBatch();

        // Map connectionId -> userId
        _ = batch.HashSetAsync(ConnectionToUserKey, connectionId, userId);

        // Add connectionId to user's connection set
        string userKey = UserConnectionsPrefix + userId;
        _ = batch.SetAddAsync(userKey, connectionId);
        _ = batch.KeyExpireAsync(userKey, _connectionTtl);

        batch.Execute();
        await Task.CompletedTask;

        LogTrackedConnection(connectionId, userId);
    }

    public async Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        IDatabase db = Db;

        // Look up the userId for this connection
        RedisValue userId = await db.HashGetAsync(ConnectionToUserKey, connectionId);
        if (userId.IsNullOrEmpty)
        {
            return;
        }

        string userIdStr = (string)userId!;
        IBatch batch = db.CreateBatch();

        // Remove from conn2user
        _ = batch.HashDeleteAsync(ConnectionToUserKey, connectionId);

        // Remove from user's connection set
        _ = batch.SetRemoveAsync(UserConnectionsPrefix + userIdStr, connectionId);

        // Remove page context
        RedisValue pageContext = await db.StringGetAsync(ConnectionPagePrefix + connectionId);
        _ = batch.KeyDeleteAsync(ConnectionPagePrefix + connectionId);

        if (!pageContext.IsNullOrEmpty)
        {
            _ = batch.SetRemoveAsync(PageViewersPrefix + (string)pageContext!, connectionId);
        }

        batch.Execute();

        LogRemovedConnection(connectionId, userIdStr);
    }

    public async Task SetPageContextAsync(string connectionId, string pageContext, CancellationToken ct = default)
    {
        IDatabase db = Db;

        // Remove from old page if any
        RedisValue oldPage = await db.StringGetAsync(ConnectionPagePrefix + connectionId);
        if (!oldPage.IsNullOrEmpty)
        {
            await db.SetRemoveAsync(PageViewersPrefix + (string)oldPage!, connectionId);
        }

        IBatch batch = db.CreateBatch();

        // Set new page context
        _ = batch.StringSetAsync(ConnectionPagePrefix + connectionId, pageContext, _connectionTtl);

        // Add to page viewers set
        _ = batch.SetAddAsync(PageViewersPrefix + pageContext, connectionId);
        _ = batch.KeyExpireAsync(PageViewersPrefix + pageContext, _connectionTtl);

        batch.Execute();
    }

    public async Task<IReadOnlyList<UserPresence>> GetOnlineUsersAsync(CancellationToken ct = default)
    {
        IDatabase db = Db;
        HashEntry[] allEntries = await db.HashGetAllAsync(ConnectionToUserKey);

        // Group connections by userId
        Dictionary<string, List<string>> userConnections = new Dictionary<string, List<string>>();
        foreach (HashEntry entry in allEntries)
        {
            string connId = entry.Name!;
            string userId = entry.Value!;
            if (!userConnections.TryGetValue(userId, out List<string>? list))
            {
                list = [];
                userConnections[userId] = list;
            }
            list.Add(connId);
        }

        List<UserPresence> result = new List<UserPresence>();
        foreach ((string? userId, List<string>? connectionIds) in userConnections)
        {
            List<string> pages = new List<string>();
            foreach (string connId in connectionIds)
            {
                RedisValue page = await db.StringGetAsync(ConnectionPagePrefix + connId);
                if (!page.IsNullOrEmpty)
                {
                    pages.Add((string)page!);
                }
            }

            result.Add(new UserPresence(userId, null, connectionIds, pages.Distinct().ToList()));
        }

        return result;
    }

    public async Task<IReadOnlyList<UserPresence>> GetUsersOnPageAsync(string pageContext, CancellationToken ct = default)
    {
        IDatabase db = Db;
        RedisValue[] connectionIds = await db.SetMembersAsync(PageViewersPrefix + pageContext);

        Dictionary<string, List<string>> userConnections = new Dictionary<string, List<string>>();
        foreach (RedisValue connId in connectionIds)
        {
            RedisValue userId = await db.HashGetAsync(ConnectionToUserKey, (string)connId!);
            if (userId.IsNullOrEmpty)
            {
                continue;
            }

            string uid = (string)userId!;
            if (!userConnections.TryGetValue(uid, out List<string>? list))
            {
                list = [];
                userConnections[uid] = list;
            }
            list.Add((string)connId!);
        }

        return userConnections
            .Select(kvp => new UserPresence(kvp.Key, null, kvp.Value, [pageContext]))
            .ToList();
    }

    public async Task<bool> IsUserOnlineAsync(string userId, CancellationToken ct = default)
    {
        IDatabase db = Db;
        long length = await db.SetLengthAsync(UserConnectionsPrefix + userId);
        return length > 0;
    }

    public async Task<string?> GetUserIdByConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        IDatabase db = Db;
        RedisValue userId = await db.HashGetAsync(ConnectionToUserKey, connectionId);
        return userId.IsNullOrEmpty ? null : (string)userId!;
    }
}

internal sealed partial class RedisPresenceService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Tracked connection {ConnectionId} for user {UserId}")]
    private partial void LogTrackedConnection(string connectionId, string userId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removed connection {ConnectionId} for user {UserId}")]
    private partial void LogRemovedConnection(string connectionId, string userId);
}
