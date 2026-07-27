import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, AlertDescription, Button, Skeleton } from "@apesdb/ui";
import {
  EventCalendar,
  EventCalendarContent,
  EventCalendarNav,
  EventCalendarToolbar,
  expandRecurrence,
  type CalendarEvent as ReuiCalendarEvent,
  type EventCalendarOccurrence,
  type EventCalendarProposedUpdate,
  type EventCalendarRangeInfo,
  type EventCalendarRecurrenceRule,
  type EventCalendarRenderEventProps,
  type EventCalendarSlotDraft,
  type EventCalendarSlotInfo,
} from "@apesdb/ui/event-calendar";
import { CalendarPlus, RefreshCw, Share2, UserRound } from "lucide-react";
import { toast } from "sonner";
import {
  createCalendarEvent,
  deleteCalendarEvent,
  fetchCalendarRange,
  updateCalendarEvent,
  type CalendarEventInput,
  type DeleteCalendarEventInput,
  type UpdateCalendarEventInput,
} from "./calendar.api";
import { CalendarEventSheet, type CalendarEditorTarget } from "./calendar-event-sheet";
import { calendarQueryKeys } from "./calendar-query-keys";
import type {
  CalendarEvent as CalendarEventContract,
  CalendarRange,
  CalendarRecurrence,
} from "./calendar.schemas";
import { CalendarSharingSheet } from "./calendar-sharing-sheet";
import { addMinutes, addZonedDays, browserTimeZone } from "./calendar-time";

const resourceColors = [
  "oklch(0.55 0.18 260)",
  "oklch(0.58 0.16 150)",
  "oklch(0.62 0.18 45)",
  "oklch(0.58 0.18 320)",
  "oklch(0.58 0.14 205)",
  "oklch(0.62 0.15 95)",
];

type MutationInput =
  | { kind: "create"; input: CalendarEventInput }
  | { kind: "duplicate"; input: CalendarEventInput }
  | { kind: "update"; input: UpdateCalendarEventInput }
  | { kind: "delete"; input: DeleteCalendarEventInput };

function initialRange() {
  const start = new Date();
  start.setDate(start.getDate() - 28);
  const end = new Date();
  end.setDate(end.getDate() + 28);
  return { start: start.toISOString(), end: end.toISOString() };
}

function parseWeekday(
  value: string,
): NonNullable<EventCalendarRecurrenceRule["byWeekday"]>[number] | null {
  const match = /^([+-]?[1-5])?(MO|TU|WE|TH|FR|SA|SU)$/.exec(value);
  if (match === null) {
    return null;
  }

  const day = match[2] as "MO" | "TU" | "WE" | "TH" | "FR" | "SA" | "SU";
  if (!match[1]) {
    return day;
  }

  return { day, ordinal: Number(match[1]) };
}

function toReuiRecurrence(
  recurrence: CalendarRecurrence,
  exDates: string[],
): EventCalendarRecurrenceRule {
  const byWeekday = recurrence.byWeekday.map(parseWeekday).filter((value) => value !== null);
  const weekStart = parseWeekday(recurrence.weekStart ?? "");

  return {
    freq: recurrence.frequency,
    interval: recurrence.interval,
    count: recurrence.count ?? undefined,
    until: recurrence.until ? new Date(recurrence.until) : undefined,
    byWeekday,
    byMonthDay: recurrence.byMonthDay,
    byMonth: recurrence.byMonth,
    weekStart: typeof weekStart === "string" ? weekStart : undefined,
    exDates: exDates.map((value) => new Date(value)),
  };
}

function toReuiEvents(range: CalendarRange | undefined) {
  if (!range) {
    return [] satisfies ReuiCalendarEvent<CalendarEventContract>[];
  }

  const resources = new Map(range.resources.map((resource) => [resource.id, resource]));
  const colors = new Map(
    range.resources.map((resource, index) => [
      resource.id,
      resourceColors[index % resourceColors.length],
    ]),
  );

  return range.events.map((event): ReuiCalendarEvent<CalendarEventContract> => {
    const owner = resources.get(event.resourceId);
    let title = event.title;
    if (event.readOnly && owner) {
      title = `${event.title} · ${owner.title}`;
    }

    return {
      id: event.id,
      title,
      start: new Date(event.start),
      end: new Date(event.end),
      allDay: event.allDay,
      recurrence:
        event.recurrence === null ? undefined : toReuiRecurrence(event.recurrence, event.exDates),
      recurringEventId: event.recurringEventId ?? undefined,
      originalStart: event.originalStart ? new Date(event.originalStart) : undefined,
      resourceId: event.resourceId,
      color: colors.get(event.resourceId),
      readOnly: event.readOnly,
      draggable: !event.readOnly,
      resizable: !event.readOnly,
      data: event,
    };
  });
}

function updateCachedRange(
  current: CalendarRange | undefined,
  changed: CalendarEventContract,
): CalendarRange | undefined {
  if (!current) {
    return current;
  }

  const existing = current.events.filter((event) => {
    if (event.id === changed.id) {
      return false;
    }

    if (
      changed.recurringEventId !== null &&
      event.recurringEventId === changed.recurringEventId &&
      event.originalStart === changed.originalStart
    ) {
      return false;
    }

    return true;
  });

  let events = [...existing, changed];
  if (changed.recurringEventId !== null && changed.originalStart !== null) {
    events = events.map((event) => {
      if (event.id !== changed.recurringEventId) {
        return event;
      }

      const exDates = Array.from(new Set([...event.exDates, changed.originalStart!]));
      return { ...event, exDates };
    });
  }

  return { ...current, events };
}

function roundedNewEntry(): CalendarEditorTarget {
  const now = new Date();
  const rounded = new Date(Math.ceil(now.getTime() / (15 * 60_000)) * 15 * 60_000);
  return {
    kind: "create",
    start: rounded,
    end: addMinutes(rounded, 60),
    allDay: false,
  };
}

export function CalendarPage() {
  const queryClient = useQueryClient();
  const displayTimeZone = useMemo(browserTimeZone, []);
  const [range, setRange] = useState(initialRange);
  const [editorTarget, setEditorTarget] = useState<CalendarEditorTarget | null>(null);
  const [sharingOpen, setSharingOpen] = useState(false);

  const calendar = useQuery({
    queryKey: calendarQueryKeys.range(range.start, range.end),
    queryFn: ({ signal }) => fetchCalendarRange(range.start, range.end, signal),
  });

  const mutation = useMutation({
    mutationFn: async (mutationInput: MutationInput) => {
      if (mutationInput.kind === "create" || mutationInput.kind === "duplicate") {
        return createCalendarEvent(mutationInput.input);
      }
      if (mutationInput.kind === "update") {
        return updateCalendarEvent(mutationInput.input);
      }

      await deleteCalendarEvent(mutationInput.input);
      return null;
    },
    onSuccess: (changed, mutationInput) => {
      if (changed !== null) {
        queryClient.setQueriesData<CalendarRange>({ queryKey: ["calendar", "range"] }, (current) =>
          updateCachedRange(current, changed),
        );
      }
      void queryClient.invalidateQueries({ queryKey: calendarQueryKeys.all });

      if (mutationInput.kind === "create") {
        toast.success("Calendar entry created");
      } else if (mutationInput.kind === "duplicate") {
        toast.success("Calendar entry duplicated");
      } else if (mutationInput.kind === "update") {
        toast.success(
          mutationInput.input.scope === "occurrence"
            ? "Occurrence updated"
            : "Calendar entry updated",
        );
      } else {
        toast.success(
          mutationInput.input.scope === "occurrence"
            ? "Occurrence deleted"
            : "Calendar entry deleted",
        );
      }
    },
    onError: (error, mutationInput) => {
      if (mutationInput.kind === "duplicate") {
        toast.error(error instanceof Error ? error.message : "Unable to duplicate calendar entry");
      }
    },
  });

  const events = useMemo(() => toReuiEvents(calendar.data), [calendar.data]);
  const resourcesById = useMemo(
    () => new Map((calendar.data?.resources ?? []).map((resource) => [resource.id, resource])),
    [calendar.data],
  );
  const resources = useMemo(
    () =>
      (calendar.data?.resources ?? []).map((resource, index) => ({
        id: resource.id,
        title: resource.isCurrentUser ? `${resource.title} (you)` : resource.title,
        color: resourceColors[index % resourceColors.length],
      })),
    [calendar.data],
  );
  const eventTimeFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(undefined, {
        hour: "numeric",
        minute: "2-digit",
        timeZone: displayTimeZone,
      }),
    [displayTimeZone],
  );

  function renderConnectedEvent({
    occurrence,
    segment,
    view,
  }: EventCalendarRenderEventProps<CalendarEventContract>) {
    const source = occurrence.event.data;
    if (!source?.readOnly || occurrence.allDay) {
      return undefined;
    }

    const inTimeGrid = view === "week" || view === "day" || view === "days" || view === "resource";
    const durationMinutes = (segment.endMin ?? 0) - (segment.startMin ?? 0);
    if (!inTimeGrid || durationMinutes < 60) {
      return undefined;
    }

    const owner = resourcesById.get(source.resourceId);
    if (!owner) {
      return undefined;
    }

    return (
      <div className="flex min-w-0 flex-1 flex-col self-start">
        <span className="truncate font-medium leading-tight">{source.title}</span>
        <span className="flex min-w-0 items-center gap-1 text-xs text-muted-foreground">
          <UserRound className="size-3 shrink-0" aria-hidden="true" />
          <span className="truncate">{owner.title}</span>
        </span>
        <span className="truncate text-muted-foreground">
          {eventTimeFormatter.format(occurrence.start)} –{" "}
          {eventTimeFormatter.format(occurrence.end)}
        </span>
      </div>
    );
  }

  function handleRangeChange(info: EventCalendarRangeInfo) {
    const start = info.range.start.toISOString();
    const end = info.range.end.toISOString();
    setRange((current) => {
      if (current.start === start && current.end === end) {
        return current;
      }
      return { start, end };
    });
  }

  function openCreate(start: Date, end: Date, allDay: boolean) {
    setEditorTarget({ kind: "create", start, end, allDay });
    mutation.reset();
  }

  function handleSlotClick(slot: EventCalendarSlotInfo) {
    let end = slot.end;
    if (!end) {
      end = slot.allDay ? addZonedDays(slot.date, 1, displayTimeZone) : addMinutes(slot.date, 60);
    }
    openCreate(slot.date, end, slot.allDay);
  }

  function handleSelectSlot(slot: EventCalendarSlotDraft) {
    openCreate(slot.start, slot.end, slot.allDay);
  }

  function handleEventClick(occurrence: EventCalendarOccurrence<CalendarEventContract>) {
    const source = occurrence.event.data;
    if (!source || !calendar.data) {
      return;
    }

    const seriesId = source.recurringEventId ?? source.id;
    const series = calendar.data.events.find((event) => event.id === seriesId);
    if (!series) {
      return;
    }

    const owner = calendar.data.resources.find((resource) => resource.id === source.resourceId);
    setEditorTarget({ kind: "edit", occurrence, series, owner: owner ?? null });
    mutation.reset();
  }

  function canDropEvent(proposal: EventCalendarProposedUpdate<CalendarEventContract>): boolean {
    if (proposal.resourceId === undefined) {
      return true;
    }

    return resourcesById.get(proposal.resourceId)?.isCurrentUser === true;
  }

  function handleEventUpdate(proposal: EventCalendarProposedUpdate<CalendarEventContract>): false {
    const source = proposal.event.data;
    if (!source || source.readOnly) {
      toast.info("Connected calendar entries are view-only");
      return false;
    }

    if (proposal.copy === true) {
      mutation.mutate({
        kind: "duplicate",
        input: {
          title: source.title,
          start: proposal.start.toISOString(),
          end: proposal.end.toISOString(),
          allDay: proposal.allDay,
          timeZoneId: source.timeZoneId,
          recurrence: null,
        },
      });
      return false;
    }

    const recurring = source.recurrence !== null || source.recurringEventId !== null;
    const eventId = source.recurringEventId ?? source.id;
    const originalStart = recurring
      ? (source.originalStart ?? proposal.occurrence?.start.toISOString() ?? source.start)
      : null;

    mutation.mutate({
      kind: "update",
      input: {
        eventId,
        scope: recurring ? "occurrence" : "event",
        originalStart,
        title: source.title,
        start: proposal.start.toISOString(),
        end: proposal.end.toISOString(),
        allDay: proposal.allDay,
        timeZoneId: source.timeZoneId,
        recurrence: recurring ? null : source.recurrence,
      },
    });
    return false;
  }

  async function handleCreate(input: CalendarEventInput) {
    await mutation.mutateAsync({ kind: "create", input });
    setEditorTarget(null);
  }

  async function handleUpdate(input: UpdateCalendarEventInput) {
    await mutation.mutateAsync({ kind: "update", input });
    setEditorTarget(null);
  }

  async function handleDelete(input: DeleteCalendarEventInput) {
    await mutation.mutateAsync({ kind: "delete", input });
    setEditorTarget(null);
  }

  const mutationError = mutation.error instanceof Error ? mutation.error.message : null;

  return (
    <main className="flex h-full min-h-0 w-full flex-col gap-4 overflow-hidden px-2">
      <h1 className="text-2xl font-semibold tracking-tight">Calendar</h1>
      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-xl border bg-background shadow-sm">
        <EventCalendar<CalendarEventContract>
          canDropEvent={canDropEvent}
          className="min-h-0 flex-1"
          defaultDate={new Date()}
          defaultView="week"
          events={events}
          interactions={{ drag: true, resize: true, selectSlot: true }}
          loading={calendar.isFetching}
          getOccurrences={(event, visibleRange) => {
            if (!event.recurrence) {
              return null;
            }

            return expandRecurrence(event, visibleRange, {
              timeZone: event.data?.timeZoneId ?? displayTimeZone,
            });
          }}
          onDragBlocked={() => toast.info("Connected calendar entries are view-only")}
          onEventClick={handleEventClick}
          onEventUpdate={handleEventUpdate}
          onRangeChange={handleRangeChange}
          onSelectSlot={handleSelectSlot}
          onSlotClick={handleSlotClick}
          renderEvent={renderConnectedEvent}
          resources={resources}
          timeZone={displayTimeZone}
          views={resources.length > 1 ? ["month", "week", "day", "agenda", "resource"] : undefined}
          weekStartsOn={1}
        >
          <EventCalendarNav />
          <EventCalendarToolbar className="justify-end border-y px-2 py-2">
            <span className="mr-auto hidden text-muted-foreground sm:inline">
              Times shown in {displayTimeZone}
            </span>
            <Button onClick={() => setSharingOpen(true)} size="sm" type="button" variant="outline">
              <Share2 data-icon="inline-start" />
              Share
            </Button>
            <Button
              onClick={() => {
                setEditorTarget(roundedNewEntry());
                mutation.reset();
              }}
              size="sm"
              type="button"
            >
              <CalendarPlus data-icon="inline-start" />
              New entry
            </Button>
          </EventCalendarToolbar>

          {calendar.isLoading ? (
            <div className="grid min-h-0 flex-1 grid-cols-7 gap-px bg-border p-px">
              {Array.from({ length: 35 }, (_, index) => (
                <Skeleton key={index} className="min-h-20 rounded-none" />
              ))}
            </div>
          ) : calendar.error instanceof Error ? (
            <div className="grid min-h-0 flex-1 place-items-center p-6">
              <Alert className="max-w-md">
                <AlertDescription className="grid gap-3">
                  <span>{calendar.error.message}</span>
                  <Button
                    className="w-fit"
                    onClick={() => void calendar.refetch()}
                    size="sm"
                    type="button"
                    variant="outline"
                  >
                    <RefreshCw data-icon="inline-start" />
                    Try again
                  </Button>
                </AlertDescription>
              </Alert>
            </div>
          ) : (
            <EventCalendarContent />
          )}
        </EventCalendar>
      </div>

      <CalendarEventSheet
        defaultTimeZone={displayTimeZone}
        error={mutationError}
        onCreate={handleCreate}
        onDelete={handleDelete}
        onOpenChange={(open) => {
          if (!open) {
            setEditorTarget(null);
            mutation.reset();
          }
        }}
        onUpdate={handleUpdate}
        open={editorTarget !== null}
        pending={mutation.isPending}
        target={editorTarget}
      />
      <CalendarSharingSheet open={sharingOpen} onOpenChange={setSharingOpen} />
    </main>
  );
}
