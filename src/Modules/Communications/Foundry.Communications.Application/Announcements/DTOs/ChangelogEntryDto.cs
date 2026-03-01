using Foundry.Communications.Domain.Announcements.Enums;

namespace Foundry.Communications.Application.Announcements.DTOs;

public sealed record ChangelogEntryDto(
    Guid Id,
    string Version,
    string Title,
    string Content,
    DateTime ReleasedAt,
    bool IsPublished,
    IReadOnlyList<ChangelogItemDto> Items,
    DateTime CreatedAt);

public sealed record ChangelogItemDto(
    Guid Id,
    string Description,
    ChangeType Type);
