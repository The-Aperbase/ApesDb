namespace ApesDb.Api.Features.Notifications.GetNotifications;

public sealed record NotificationActorResponse(Guid Id, string Name, string? PictureUrl);

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    Guid ResourceId,
    DateTime CreatedAt,
    DateTime? ReadAt,
    bool IsUnread,
    bool IsActionable,
    NotificationActorResponse? Actor
);

public sealed record NotificationMetadataResponse(
    int TotalCount,
    int UnreadCount,
    int ActionableCount,
    int AttentionCount
);

public sealed record NotificationsResponse(NotificationResponse[] Items, NotificationMetadataResponse Metadata);
