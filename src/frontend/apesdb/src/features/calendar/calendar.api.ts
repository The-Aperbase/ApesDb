import { z } from "zod";
import {
  calendarEventSchema,
  calendarRangeSchema,
  calendarSharingSchema,
  type CalendarEvent,
  type CalendarRange,
  type CalendarRecurrence,
  type CalendarSharing,
} from "./calendar.schemas";

export type CalendarEventInput = {
  title: string;
  start: string;
  end: string;
  allDay: boolean;
  timeZoneId: string;
  recurrence: CalendarRecurrence | null;
};

export type UpdateCalendarEventInput = CalendarEventInput & {
  eventId: string;
  scope: "event" | "series" | "occurrence";
  originalStart: string | null;
};

export type DeleteCalendarEventInput = {
  eventId: string;
  scope: "event" | "series" | "occurrence";
  originalStart: string | null;
};

const validationErrorSchema = z.object({
  message: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
});

async function responseError(response: Response, fallback: string): Promise<Error> {
  try {
    const result = validationErrorSchema.safeParse(await response.json());
    if (result.success && result.data.errors) {
      const message = Object.values(result.data.errors).flat()[0];
      if (message) {
        return new Error(message);
      }
    }

    if (result.success && result.data.message) {
      return new Error(result.data.message);
    }
  } catch {
    // Use the status-based fallback for an empty or non-JSON response.
  }

  return new Error(`${fallback} (status ${response.status}).`);
}

async function calendarFetch(
  input: RequestInfo | URL,
  init: RequestInit,
  fallback: string,
): Promise<Response> {
  let response: Response;
  try {
    response = await fetch(input, { ...init, credentials: "include" });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw await responseError(response, fallback);
  }

  return response;
}

export async function fetchCalendarRange(
  start: string,
  end: string,
  signal: AbortSignal,
): Promise<CalendarRange> {
  const params = new URLSearchParams({ start, end });
  const response = await calendarFetch(
    `/api/calendar/events?${params.toString()}`,
    { signal },
    "Unable to load the calendar",
  );
  return calendarRangeSchema.parse(await response.json());
}

export async function createCalendarEvent(input: CalendarEventInput): Promise<CalendarEvent> {
  const response = await calendarFetch(
    "/api/calendar/events",
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(input),
    },
    "Unable to create the calendar entry",
  );
  return calendarEventSchema.parse(await response.json());
}

export async function updateCalendarEvent(input: UpdateCalendarEventInput): Promise<CalendarEvent> {
  const response = await calendarFetch(
    `/api/calendar/events/${encodeURIComponent(input.eventId)}`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(input),
    },
    "Unable to update the calendar entry",
  );
  return calendarEventSchema.parse(await response.json());
}

export async function deleteCalendarEvent(input: DeleteCalendarEventInput): Promise<void> {
  const params = new URLSearchParams({ scope: input.scope });
  if (input.originalStart !== null) {
    params.set("originalStart", input.originalStart);
  }

  await calendarFetch(
    `/api/calendar/events/${encodeURIComponent(input.eventId)}?${params.toString()}`,
    { method: "DELETE" },
    "Unable to delete the calendar entry",
  );
}

export async function fetchCalendarSharing(signal?: AbortSignal): Promise<CalendarSharing> {
  const response = await calendarFetch(
    "/api/calendar/sharing",
    { signal },
    "Unable to load calendar sharing",
  );
  return calendarSharingSchema.parse(await response.json());
}

export async function inviteToCalendar(email: string): Promise<void> {
  await calendarFetch(
    "/api/calendar/invites",
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email }),
    },
    "Unable to send the calendar invitation",
  );
}

export async function respondToCalendarInvitation(
  inviteId: string,
  accept: boolean,
): Promise<void> {
  await calendarFetch(
    `/api/calendar/invites/${encodeURIComponent(inviteId)}/respond`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accept }),
    },
    "Unable to respond to the calendar invitation",
  );
}

export async function cancelCalendarInvitation(inviteId: string): Promise<void> {
  await calendarFetch(
    `/api/calendar/invites/${encodeURIComponent(inviteId)}`,
    { method: "DELETE" },
    "Unable to cancel the calendar invitation",
  );
}

export async function disconnectCalendar(connectionId: string): Promise<void> {
  await calendarFetch(
    `/api/calendar/connections/${encodeURIComponent(connectionId)}`,
    { method: "DELETE" },
    "Unable to disconnect the calendar",
  );
}
