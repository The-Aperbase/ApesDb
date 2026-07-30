import type { EventCalendarOccurrence } from "@apesdb/ui/event-calendar";
import { z } from "zod";
import type {
  CalendarEventInput,
  DeleteCalendarEventInput,
  UpdateCalendarEventInput,
} from "./calendar.api";
import type { CalendarEvent, CalendarRecurrence, CalendarResource } from "./calendar.schemas";
import {
  addZonedDays,
  fromDateInputValue,
  fromDateTimeInputValue,
  recurrenceUntilFromDate,
  toDateInputValue,
  toDateTimeInputValue,
} from "./calendar-time";

export const maximumTitleLength = 128;

export const weekdays = [
  { value: "MO", label: "M" },
  { value: "TU", label: "T" },
  { value: "WE", label: "W" },
  { value: "TH", label: "T" },
  { value: "FR", label: "F" },
  { value: "SA", label: "S" },
  { value: "SU", label: "S" },
] as const;

export const timePresets = [
  { id: "early-a", label: "Early A", description: "7am–7pm", start: "07:00", end: "19:00" },
  { id: "early-b", label: "Early B", description: "10am–10pm", start: "10:00", end: "22:00" },
  { id: "late-a", label: "Late A", description: "7pm–7am", start: "19:00", end: "07:00" },
  { id: "late-b", label: "Late B", description: "10pm–7am", start: "22:00", end: "07:00" },
  { id: "human", label: "Human", description: "9am–5pm", start: "09:00", end: "17:00" },
] as const;

export type RecurrenceFrequency = "none" | CalendarRecurrence["frequency"];
export type RecurrenceEnd = "never" | "on" | "count";
export type EventScope = "event" | "series" | "occurrence";
export type TimePreset = (typeof timePresets)[number];

export type CalendarEditorTarget =
  | {
      kind: "create";
      start: Date;
      end: Date;
      allDay: boolean;
    }
  | {
      kind: "edit";
      occurrence: EventCalendarOccurrence<CalendarEvent>;
      series: CalendarEvent;
      owner: CalendarResource | null;
    };

export type CalendarEventFormValues = {
  title: string;
  allDay: boolean;
  startDate: string;
  startTime: string;
  endDate: string;
  endTime: string;
  timeZone: string;
  frequency: RecurrenceFrequency;
  interval: string;
  recurrenceEnd: RecurrenceEnd;
  until: string;
  count: string;
  byWeekday: string[];
};

export type CalendarEventInputResult =
  | { success: true; input: CalendarEventInput }
  | { success: false; error: string };

function weekdayFor(date: Date, timeZone: string): string {
  const short = new Intl.DateTimeFormat("en-US", { timeZone, weekday: "short" }).format(date);
  const values: Record<string, string> = {
    Mon: "MO",
    Tue: "TU",
    Wed: "WE",
    Thu: "TH",
    Fri: "FR",
    Sat: "SA",
    Sun: "SU",
  };
  return values[short] ?? "MO";
}

export function calendarEventFormValuesForTarget(
  target: CalendarEditorTarget,
  scope: EventScope,
  defaultTimeZone: string,
): CalendarEventFormValues {
  let sourceStart: Date;
  let sourceEnd: Date;
  let sourceAllDay: boolean;
  let title = "";
  let timeZone = defaultTimeZone;
  let recurrence: CalendarRecurrence | null = null;

  if (target.kind === "create") {
    sourceStart = target.start;
    sourceEnd = target.end;
    sourceAllDay = target.allDay;
  } else if (scope === "series") {
    sourceStart = new Date(target.series.start);
    sourceEnd = new Date(target.series.end);
    sourceAllDay = target.series.allDay;
    title = target.series.title;
    timeZone = target.series.timeZoneId;
    recurrence = target.series.recurrence;
  } else {
    const source = target.occurrence.event.data;
    sourceStart = target.occurrence.start;
    sourceEnd = target.occurrence.end;
    sourceAllDay = target.occurrence.allDay;
    title = source?.title ?? target.occurrence.event.title;
    timeZone = source?.timeZoneId ?? defaultTimeZone;
  }

  let recurrenceEnd: RecurrenceEnd = "never";
  if (recurrence?.until) {
    recurrenceEnd = "on";
  } else if (recurrence?.count) {
    recurrenceEnd = "count";
  }

  const byWeekday = recurrence?.byWeekday.filter((value) => value.length === 2) ?? [
    weekdayFor(sourceStart, timeZone),
  ];
  const startValue = toDateTimeInputValue(sourceStart, timeZone);
  const endValue = toDateTimeInputValue(sourceEnd, timeZone);

  return {
    title,
    allDay: sourceAllDay,
    startDate: startValue.slice(0, 10),
    startTime: startValue.slice(11, 16),
    endDate: endValue.slice(0, 10),
    endTime: endValue.slice(11, 16),
    timeZone,
    frequency: recurrence?.frequency ?? "none",
    interval: (recurrence?.interval ?? 1).toString(),
    recurrenceEnd,
    until: recurrence?.until ? toDateInputValue(new Date(recurrence.until), timeZone) : "",
    count: (recurrence?.count ?? 10).toString(),
    byWeekday,
  };
}

export function emptyCalendarEventFormValues(timeZone: string): CalendarEventFormValues {
  const now = new Date();
  return calendarEventFormValuesForTarget(
    {
      kind: "create",
      start: now,
      end: new Date(now.getTime() + 60 * 60_000),
      allDay: false,
    },
    "event",
    timeZone,
  );
}

function isValidTimeZone(value: string): boolean {
  if (value.length === 0) {
    return false;
  }

  try {
    new Intl.DateTimeFormat("en-US", { timeZone: value }).format();
    return true;
  } catch {
    return false;
  }
}

export const calendarEventFormSchema = z
  .object({
    title: z.string(),
    allDay: z.boolean(),
    startDate: z.string(),
    startTime: z.string(),
    endDate: z.string(),
    endTime: z.string(),
    timeZone: z.string(),
    frequency: z.enum(["none", "daily", "weekly", "monthly", "yearly"]),
    interval: z.string(),
    recurrenceEnd: z.enum(["never", "on", "count"]),
    until: z.string(),
    count: z.string(),
    byWeekday: z.array(z.string()),
  })
  .superRefine((values, context) => {
    const title = values.title.trim();
    if (title.length === 0) {
      context.addIssue({ code: "custom", message: "Enter a title.", path: ["title"] });
      return;
    }

    if (title.length > maximumTitleLength) {
      context.addIssue({
        code: "custom",
        message: `Titles must be ${maximumTitleLength} characters or fewer.`,
        path: ["title"],
      });
      return;
    }

    if (!isValidTimeZone(values.timeZone)) {
      context.addIssue({
        code: "custom",
        message: "Choose a valid IANA time zone.",
        path: ["timeZone"],
      });
      return;
    }

    const start = values.allDay
      ? fromDateInputValue(values.startDate, values.timeZone)
      : fromDateTimeInputValue(`${values.startDate}T${values.startTime}`, values.timeZone);
    const end = values.allDay
      ? fromDateInputValue(values.endDate, values.timeZone)
      : fromDateTimeInputValue(`${values.endDate}T${values.endTime}`, values.timeZone);
    if (start === null || end === null || end <= start) {
      const message = values.allDay
        ? "The “ends before” date must follow the start date."
        : "End must be after start.";
      context.addIssue({ code: "custom", message, path: ["endDate"] });
      return;
    }

    if (values.frequency === "none") {
      return;
    }

    const interval = Number(values.interval);
    if (!Number.isInteger(interval) || interval < 1 || interval > 365) {
      context.addIssue({
        code: "custom",
        message: "Repeat interval must be between 1 and 365.",
        path: ["interval"],
      });
      return;
    }

    if (values.frequency === "weekly" && values.byWeekday.length === 0) {
      context.addIssue({
        code: "custom",
        message: "Choose at least one weekday.",
        path: ["byWeekday"],
      });
      return;
    }

    if (values.recurrenceEnd === "count") {
      const count = Number(values.count);
      if (!Number.isInteger(count) || count < 1 || count > 1000) {
        context.addIssue({
          code: "custom",
          message: "Occurrence count must be between 1 and 1,000.",
          path: ["count"],
        });
      }
      return;
    }

    if (values.recurrenceEnd === "on") {
      const untilDate = recurrenceUntilFromDate(values.until, values.timeZone);
      if (untilDate === null || untilDate < start) {
        context.addIssue({
          code: "custom",
          message: "The recurrence end date must be on or after the first occurrence.",
          path: ["until"],
        });
      }
    }
  });

export function validateCalendarEventForm(values: CalendarEventFormValues): string | undefined {
  const result = calendarEventFormSchema.safeParse(values);
  if (result.success) {
    return undefined;
  }

  return result.error.issues[0]?.message;
}

export function calendarEventInputFromValues(
  values: CalendarEventFormValues,
): CalendarEventInputResult {
  const result = calendarEventFormSchema.safeParse(values);
  if (!result.success) {
    return {
      success: false,
      error: result.error.issues[0]?.message ?? "Check the calendar entry and try again.",
    };
  }

  const parsed = result.data;
  const start = parsed.allDay
    ? fromDateInputValue(parsed.startDate, parsed.timeZone)
    : fromDateTimeInputValue(`${parsed.startDate}T${parsed.startTime}`, parsed.timeZone);
  const end = parsed.allDay
    ? fromDateInputValue(parsed.endDate, parsed.timeZone)
    : fromDateTimeInputValue(`${parsed.endDate}T${parsed.endTime}`, parsed.timeZone);
  if (start === null || end === null) {
    return { success: false, error: "Check the calendar entry and try again." };
  }

  let recurrence: CalendarRecurrence | null = null;
  if (parsed.frequency !== "none") {
    let count: number | null = null;
    let until: string | null = null;
    if (parsed.recurrenceEnd === "count") {
      count = Number(parsed.count);
    } else if (parsed.recurrenceEnd === "on") {
      const untilDate = recurrenceUntilFromDate(parsed.until, parsed.timeZone);
      if (untilDate === null) {
        return { success: false, error: "Check the calendar entry and try again." };
      }
      until = untilDate.toISOString();
    }

    recurrence = {
      frequency: parsed.frequency,
      interval: Number(parsed.interval),
      count,
      until,
      byWeekday: parsed.frequency === "weekly" ? parsed.byWeekday : [],
      byMonthDay: [],
      byMonth: [],
      weekStart: "MO",
    };
  }

  return {
    success: true,
    input: {
      title: parsed.title.trim(),
      start: start.toISOString(),
      end: end.toISOString(),
      allDay: parsed.allDay,
      timeZoneId: parsed.timeZone,
      recurrence,
    },
  };
}

export function updateCalendarEventInput(
  input: CalendarEventInput,
  target: Extract<CalendarEditorTarget, { kind: "edit" }>,
  scope: EventScope,
): UpdateCalendarEventInput {
  const source = target.occurrence.event.data;
  let originalStart: string | null = null;
  if (scope === "occurrence") {
    originalStart = source?.originalStart ?? target.occurrence.start.toISOString();
  }

  return {
    ...input,
    eventId: target.series.id,
    scope,
    originalStart,
  };
}

export function deleteCalendarEventInput(
  target: Extract<CalendarEditorTarget, { kind: "edit" }>,
  scope: EventScope,
): DeleteCalendarEventInput {
  const source = target.occurrence.event.data;
  let originalStart: string | null = null;
  if (scope === "occurrence") {
    originalStart = source?.originalStart ?? target.occurrence.start.toISOString();
  }

  return {
    eventId: target.series.id,
    scope,
    originalStart,
  };
}

export function valuesWithAllDay(
  values: CalendarEventFormValues,
  checked: boolean,
): CalendarEventFormValues {
  if (checked === values.allDay) {
    return values;
  }

  if (checked) {
    const startDate = values.startDate;
    let endDate = values.endDate;
    if (endDate <= startDate) {
      const start = fromDateInputValue(startDate, values.timeZone);
      if (start !== null) {
        endDate = toDateInputValue(addZonedDays(start, 1, values.timeZone), values.timeZone);
      }
    }

    return {
      ...values,
      allDay: true,
      startDate,
      endDate,
    };
  }

  return {
    ...values,
    allDay: false,
    startTime: "09:00",
    endDate: values.startDate,
    endTime: "10:00",
  };
}

export function timePresetEndDate(values: CalendarEventFormValues, preset: TimePreset): string {
  if (preset.end > preset.start) {
    return values.startDate;
  }

  const start = fromDateInputValue(values.startDate, values.timeZone);
  if (start === null) {
    return values.startDate;
  }

  return toDateInputValue(addZonedDays(start, 1, values.timeZone), values.timeZone);
}

export function valuesWithTimePreset(
  values: CalendarEventFormValues,
  preset: TimePreset,
): CalendarEventFormValues {
  return {
    ...values,
    allDay: false,
    startTime: preset.start,
    endDate: timePresetEndDate(values, preset),
    endTime: preset.end,
  };
}

export function isTimePresetActive(values: CalendarEventFormValues, preset: TimePreset): boolean {
  return (
    !values.allDay &&
    values.startTime === preset.start &&
    values.endTime === preset.end &&
    values.endDate === timePresetEndDate(values, preset)
  );
}

export function valuesWithToggledWeekday(
  values: CalendarEventFormValues,
  weekday: string,
): CalendarEventFormValues {
  let byWeekday: string[];
  if (values.byWeekday.includes(weekday)) {
    byWeekday = values.byWeekday.filter((value) => value !== weekday);
  } else {
    byWeekday = [...values.byWeekday, weekday];
  }

  return { ...values, byWeekday };
}
