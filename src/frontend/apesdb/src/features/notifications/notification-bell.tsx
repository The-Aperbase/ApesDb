import {
  Badge,
  Button,
  Popover,
  PopoverContent,
  PopoverDescription,
  PopoverHeader,
  PopoverTitle,
  PopoverTrigger,
  Skeleton,
} from "@apesdb/ui";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Bell, Check, CheckCheck, Inbox, Loader2, RefreshCw, X } from "lucide-react";
import { toast } from "sonner";
import { formatDateTime } from "../../lib/date";
import { respondToCalendarInvitation } from "../calendar/calendar.api";
import { calendarQueryKeys } from "../calendar/calendar-query-keys";
import { notificationQueryKeys } from "./notification-query-keys";
import type { Notification } from "./notifications.schemas";
import { useMarkNotificationsRead } from "./use-mark-notifications-read";
import { useNotifications } from "./use-notifications";

type NotificationBellProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

function NotificationSkeleton() {
  return (
    <div className="flex items-start gap-3 p-2">
      <Skeleton className="size-8" />
      <div className="grid flex-1 gap-2">
        <Skeleton className="h-3.5 w-3/4" />
        <Skeleton className="h-3 w-1/3" />
      </div>
    </div>
  );
}

type NotificationRowProps = {
  notification: Notification;
  responding: boolean;
  onRespond: (inviteId: string, accept: boolean) => void;
};

function NotificationRow({ notification, responding, onRespond }: NotificationRowProps) {
  const isCalendarInvite = notification.type === "CalendarInvite" && notification.isActionable;

  return (
    <div className="flex items-start gap-3 p-2">
      <div className="grid min-w-0 flex-1 gap-1">
        <p className="text-xs">
          {isCalendarInvite
            ? "Someone invited you to connect calendars."
            : "You have a new notification."}
        </p>
        <p className="text-[0.625rem] text-muted-foreground">
          {formatDateTime(notification.createdAt)}
        </p>
        {isCalendarInvite ? (
          <div className="mt-1 flex gap-2">
            <Button
              disabled={responding}
              onClick={() => onRespond(notification.resourceId, false)}
              size="xs"
              type="button"
              variant="ghost"
            >
              <X data-icon="inline-start" />
              Decline
            </Button>
            <Button
              disabled={responding}
              onClick={() => onRespond(notification.resourceId, true)}
              size="xs"
              type="button"
            >
              {responding ? (
                <Loader2 className="animate-spin" />
              ) : (
                <Check data-icon="inline-start" />
              )}
              Accept
            </Button>
          </div>
        ) : null}
      </div>
      {notification.isUnread ? (
        <span aria-label="Unread" className="mt-1.5 size-2 shrink-0 rounded-full bg-primary" />
      ) : null}
    </div>
  );
}

export function NotificationBell({ open, onOpenChange }: NotificationBellProps) {
  const queryClient = useQueryClient();
  const notifications = useNotifications();
  const markRead = useMarkNotificationsRead();
  const respond = useMutation({
    mutationFn: ({ inviteId, accept }: { inviteId: string; accept: boolean }) =>
      respondToCalendarInvitation(inviteId, accept),
    onSuccess: async (_result, input) => {
      toast.success(input.accept ? "Calendars connected" : "Calendar invitation declined");
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: notificationQueryKeys.list }),
        queryClient.invalidateQueries({ queryKey: calendarQueryKeys.all }),
      ]);
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Unable to respond to the invitation.");
    },
  });

  const unreadCount = notifications.data?.metadata.unreadCount ?? 0;
  const items = notifications.data?.items ?? [];

  return (
    <Popover open={open} onOpenChange={onOpenChange}>
      <PopoverTrigger
        render={
          <Button
            aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ""}`}
            className="relative"
            size="icon-lg"
            type="button"
            variant="ghost"
          />
        }
      >
        <Bell />
        {unreadCount > 0 ? (
          <Badge className="absolute -top-1.5 -right-1.5 h-4 min-w-4 px-1">
            {unreadCount > 99 ? "99+" : unreadCount}
          </Badge>
        ) : null}
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80" sideOffset={8}>
        <PopoverHeader className="flex-row items-start justify-between gap-2">
          <div className="grid gap-1">
            <PopoverTitle>Notifications</PopoverTitle>
            <PopoverDescription>
              {items.length === 0 ? "Nothing needs your attention." : "Your latest activity."}
            </PopoverDescription>
          </div>
          {unreadCount > 0 ? (
            <Button
              disabled={markRead.isPending}
              onClick={() => markRead.mutate()}
              size="xs"
              type="button"
              variant="ghost"
            >
              <CheckCheck data-icon="inline-start" />
              Mark all read
            </Button>
          ) : null}
        </PopoverHeader>
        {notifications.isLoading ? (
          <div className="grid gap-1">
            <NotificationSkeleton />
            <NotificationSkeleton />
          </div>
        ) : null}
        {!notifications.isLoading && notifications.error !== null ? (
          <div className="grid justify-items-center gap-2 py-4 text-center">
            <p className="text-xs text-muted-foreground">{notifications.error}</p>
            <Button onClick={notifications.retry} size="xs" type="button" variant="outline">
              <RefreshCw data-icon="inline-start" />
              Retry
            </Button>
          </div>
        ) : null}
        {!notifications.isLoading && notifications.error === null && items.length === 0 ? (
          <div className="grid justify-items-center gap-2 py-6 text-center">
            <Inbox className="size-5 text-muted-foreground" />
            <p className="text-xs text-muted-foreground">You&apos;re all caught up.</p>
          </div>
        ) : null}
        {items.length > 0 ? (
          <div className="-mx-1 grid max-h-80 gap-0.5 overflow-y-auto">
            {items.map((notification) => (
              <NotificationRow
                key={notification.id}
                notification={notification}
                responding={
                  respond.isPending && respond.variables?.inviteId === notification.resourceId
                }
                onRespond={(inviteId, accept) => respond.mutate({ inviteId, accept })}
              />
            ))}
          </div>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}
