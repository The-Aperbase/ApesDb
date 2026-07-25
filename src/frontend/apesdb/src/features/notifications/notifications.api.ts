import {
  listNotificationsResponseSchema,
  type ListNotificationsResponse,
} from "./notifications.schemas";

export async function fetchNotifications(signal: AbortSignal): Promise<ListNotificationsResponse> {
  const response = await fetch("/api/notifications", {
    credentials: "include",
    signal,
  });

  if (!response.ok) {
    throw new Error(`Unable to load notifications (status ${response.status}).`);
  }

  const result = listNotificationsResponseSchema.safeParse(await response.json());

  if (!result.success) {
    throw new Error("The server returned an unexpected notifications response.");
  }

  return result.data;
}

export async function markAllNotificationsRead(): Promise<void> {
  const response = await fetch("/api/notifications/read", {
    method: "POST",
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(`Unable to mark notifications as read (status ${response.status}).`);
  }
}
