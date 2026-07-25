import type {
  ListNotificationsResponse,
  Notification,
  NotificationMetadata,
} from "./notifications.schemas";

function recomputeMetadata(items: Notification[]): NotificationMetadata {
  let unreadCount = 0;
  let actionableCount = 0;
  let attentionCount = 0;

  for (const item of items) {
    if (item.isUnread) {
      unreadCount += 1;
    }

    if (item.isActionable) {
      actionableCount += 1;
    }

    if (item.isUnread || item.isActionable) {
      attentionCount += 1;
    }
  }

  return {
    totalCount: items.length,
    unreadCount,
    actionableCount,
    attentionCount,
  };
}

export function withNotificationAdded(
  current: ListNotificationsResponse | undefined,
  notification: Notification,
): ListNotificationsResponse {
  const items = current?.items ?? [];
  const nextItems = [notification, ...items.filter((item) => item.id !== notification.id)];

  return {
    items: nextItems,
    metadata: recomputeMetadata(nextItems),
  };
}

export function withAllNotificationsRead(
  current: ListNotificationsResponse | undefined,
  readAt: string,
): ListNotificationsResponse {
  const items = current?.items ?? [];
  const nextItems = items.map((item): Notification => {
    if (!item.isUnread) {
      return item;
    }

    return {
      id: item.id,
      type: item.type,
      resourceId: item.resourceId,
      createdAt: item.createdAt,
      readAt: item.readAt ?? readAt,
      isUnread: false,
      isActionable: item.isActionable,
    };
  });

  return {
    items: nextItems,
    metadata: recomputeMetadata(nextItems),
  };
}

export function withNotificationResolved(
  current: ListNotificationsResponse | undefined,
  notificationId: string,
): ListNotificationsResponse {
  const items = current?.items ?? [];
  const nextItems = items.filter((item) => item.id !== notificationId);

  return {
    items: nextItems,
    metadata: recomputeMetadata(nextItems),
  };
}
