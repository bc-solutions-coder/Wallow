using Foundry.Communications.Domain.Announcements.Enums;
using Foundry.Communications.Domain.Announcements.Identity;
using Foundry.Shared.Kernel.Domain;

namespace Foundry.Communications.Domain.Announcements.Entities;

public sealed class Announcement : AggregateRoot<AnnouncementId>
{
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public AnnouncementType Type { get; private set; }
    public AnnouncementTarget Target { get; private set; }
    public string? TargetValue { get; private set; }
    public DateTime? PublishAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsPinned { get; private set; }
    public bool IsDismissible { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? ActionLabel { get; private set; }
    public string? ImageUrl { get; private set; }
    public AnnouncementStatus Status { get; private set; }

    private Announcement() { }

    private Announcement(
        string title,
        string content,
        AnnouncementType type,
        AnnouncementTarget target,
        string? targetValue,
        DateTime? publishAt,
        DateTime? expiresAt,
        bool isPinned,
        bool isDismissible,
        string? actionUrl,
        string? actionLabel,
        string? imageUrl)
        : base(AnnouncementId.New())
    {
        Title = title;
        Content = content;
        Type = type;
        Target = target;
        TargetValue = targetValue;
        PublishAt = publishAt;
        ExpiresAt = expiresAt;
        IsPinned = isPinned;
        IsDismissible = isDismissible;
        ActionUrl = actionUrl;
        ActionLabel = actionLabel;
        ImageUrl = imageUrl;
        Status = publishAt.HasValue ? AnnouncementStatus.Scheduled : AnnouncementStatus.Draft;
        SetCreated();
    }

    public static Announcement Create(
        string title,
        string content,
        AnnouncementType type,
        AnnouncementTarget target = AnnouncementTarget.All,
        string? targetValue = null,
        DateTime? publishAt = null,
        DateTime? expiresAt = null,
        bool isPinned = false,
        bool isDismissible = true,
        string? actionUrl = null,
        string? actionLabel = null,
        string? imageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Announcement(
            title, content, type, target, targetValue,
            publishAt, expiresAt, isPinned, isDismissible,
            actionUrl, actionLabel, imageUrl);
    }

    public void Update(
        string title,
        string content,
        AnnouncementType type,
        AnnouncementTarget target,
        string? targetValue,
        DateTime? publishAt,
        DateTime? expiresAt,
        bool isPinned,
        bool isDismissible,
        string? actionUrl,
        string? actionLabel,
        string? imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Title = title;
        Content = content;
        Type = type;
        Target = target;
        TargetValue = targetValue;
        PublishAt = publishAt;
        ExpiresAt = expiresAt;
        IsPinned = isPinned;
        IsDismissible = isDismissible;
        ActionUrl = actionUrl;
        ActionLabel = actionLabel;
        ImageUrl = imageUrl;
        SetUpdated();
    }

    public void Publish()
    {
        if (Status == AnnouncementStatus.Published)
        {
            return;
        }

        Status = AnnouncementStatus.Published;
        PublishAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Expire()
    {
        Status = AnnouncementStatus.Expired;
        SetUpdated();
    }

    public void Archive()
    {
        Status = AnnouncementStatus.Archived;
        SetUpdated();
    }
}
