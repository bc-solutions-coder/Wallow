using Foundry.Communications.Application.Announcements.DTOs;
using Foundry.Communications.Application.Announcements.Interfaces;
using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Application.Announcements.Queries.GetChangelogEntry;

public sealed record GetChangelogByVersionQuery(string Version);
public sealed record GetLatestChangelogQuery;

public sealed class GetChangelogByVersionHandler
{
    private readonly IChangelogRepository _repository;

    public GetChangelogByVersionHandler(IChangelogRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ChangelogEntryDto>> Handle(GetChangelogByVersionQuery query, CancellationToken ct)
    {
        ChangelogEntry? entry = await _repository.GetByVersionAsync(query.Version, ct);

        if (entry is null || !entry.IsPublished)
        {
            return Result.Failure<ChangelogEntryDto>(Error.NotFound("Changelog", query.Version));
        }

        return Result.Success(MapToDto(entry));
    }

    private static ChangelogEntryDto MapToDto(ChangelogEntry entry)
    {
        return new ChangelogEntryDto(
            entry.Id.Value,
            entry.Version,
            entry.Title,
            entry.Content,
            entry.ReleasedAt,
            entry.IsPublished,
            entry.Items.Select(i => new ChangelogItemDto(i.Id.Value, i.Description, i.Type)).ToList(),
            entry.CreatedAt);
    }
}

public sealed class GetLatestChangelogHandler
{
    private readonly IChangelogRepository _repository;

    public GetLatestChangelogHandler(IChangelogRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ChangelogEntryDto>> Handle(GetLatestChangelogQuery _, CancellationToken ct)
    {
        ChangelogEntry? entry = await _repository.GetLatestPublishedAsync(ct);

        if (entry is null)
        {
            return Result.Failure<ChangelogEntryDto>(Error.NotFound("Changelog", "latest"));
        }

        return Result.Success(MapToDto(entry));
    }

    private static ChangelogEntryDto MapToDto(ChangelogEntry entry)
    {
        return new ChangelogEntryDto(
            entry.Id.Value,
            entry.Version,
            entry.Title,
            entry.Content,
            entry.ReleasedAt,
            entry.IsPublished,
            entry.Items.Select(i => new ChangelogItemDto(i.Id.Value, i.Description, i.Type)).ToList(),
            entry.CreatedAt);
    }
}
