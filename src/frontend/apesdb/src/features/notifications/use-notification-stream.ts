import { createElement, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { z } from "zod";
import { notificationQueryKeys } from "./notification-query-keys";
import { notificationSchema, type ListNotificationsResponse } from "./notifications.schemas";
import {
  withAllNotificationsRead,
  withNotificationAdded,
  withNotificationResolved,
} from "./notifications.cache";

const readEventSchema = z.object({
  readAt: z.string(),
});

const resolvedEventSchema = z.object({
  notificationId: z.string(),
});

const notificationToastId = "notification-created";

function parseEventData(event: Event): unknown {
  if (!(event instanceof MessageEvent) || typeof event.data !== "string") {
    return null;
  }

  try {
    return JSON.parse(event.data);
  } catch {
    return null;
  }
}

function showNotificationToast(openNotifications: () => void) {
  const handleClick = () => {
    openNotifications();
    toast.dismiss(notificationToastId);
  };

  const message = createElement(
    "button",
    {
      "aria-label": "Open notifications",
      className: "w-full cursor-pointer text-left before:absolute before:inset-0",
      onClick: handleClick,
      type: "button",
    },
    "You have a new notification.",
  );

  toast(message, {
    closeButton: true,
    duration: Number.POSITIVE_INFINITY,
    id: notificationToastId,
    position: "top-right",
  });
}

export function useNotificationStream(openNotifications: () => void) {
  const queryClient = useQueryClient();

  useEffect(() => {
    const source = new EventSource("/api/notifications/stream");

    const onCreated = (event: Event) => {
      const parsed = notificationSchema.safeParse(parseEventData(event));

      if (!parsed.success) {
        return;
      }

      queryClient.setQueryData<ListNotificationsResponse>(notificationQueryKeys.list, (current) =>
        withNotificationAdded(current, parsed.data),
      );
      showNotificationToast(openNotifications);
    };

    const onRead = (event: Event) => {
      const parsed = readEventSchema.safeParse(parseEventData(event));

      if (!parsed.success) {
        return;
      }

      queryClient.setQueryData<ListNotificationsResponse>(notificationQueryKeys.list, (current) =>
        withAllNotificationsRead(current, parsed.data.readAt),
      );
    };

    const onResolved = (event: Event) => {
      const parsed = resolvedEventSchema.safeParse(parseEventData(event));

      if (!parsed.success) {
        return;
      }

      queryClient.setQueryData<ListNotificationsResponse>(notificationQueryKeys.list, (current) =>
        withNotificationResolved(current, parsed.data.notificationId),
      );
    };

    source.addEventListener("notification.created", onCreated);
    source.addEventListener("notification.read", onRead);
    source.addEventListener("notification.resolved", onResolved);

    return () => {
      source.close();
    };
  }, [openNotifications, queryClient]);
}
