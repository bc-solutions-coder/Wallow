namespace Foundry.Shared.Contracts.Realtime;

public sealed record UserPresence(
    string UserId,
    string? DisplayName,
    IReadOnlyList<string> ConnectionIds,
    IReadOnlyList<string> CurrentPages);
