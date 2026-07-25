namespace ApesDb.Api.Features.Notifications.NotificationsStream;

public static class NotificationStreamEventKinds
{
    public const string Created = "notification.created";
    public const string Read = "notification.read";
    public const string Resolved = "notification.resolved";
}

public sealed record NotificationStreamEvent(string Kind, object Data);

public sealed record NotificationReadEventData(DateTime ReadAt);

public sealed record NotificationResolvedEventData(Guid NotificationId);
