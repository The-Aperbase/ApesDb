import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Avatar,
  AvatarFallback,
  AvatarImage,
  Button,
  Calendar,
  Checkbox,
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  Input,
  Popover,
  PopoverContent,
  PopoverTrigger,
  ScrollArea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@apesdb/ui";
import type { EventCalendarOccurrence } from "@apesdb/ui/event-calendar";
import { CalendarClock, CalendarDays, Loader2, Trash2 } from "lucide-react";
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

const maximumTitleLength = 128;
const weekdays = [
  { value: "MO", label: "M" },
  { value: "TU", label: "T" },
  { value: "WE", label: "W" },
  { value: "TH", label: "T" },
  { value: "FR", label: "F" },
  { value: "SA", label: "S" },
  { value: "SU", label: "S" },
] as const;
const timePresets = [
  { id: "early-a", label: "Early A", description: "7am–7pm", start: "07:00", end: "19:00" },
  { id: "early-b", label: "Early B", description: "10am–10pm", start: "10:00", end: "22:00" },
  { id: "late-a", label: "Late A", description: "7pm–7am", start: "19:00", end: "07:00" },
  { id: "late-b", label: "Late B", description: "10pm–7am", start: "22:00", end: "07:00" },
  { id: "human", label: "Human", description: "9am–5pm", start: "09:00", end: "17:00" },
] as const;

type RecurrenceFrequency = "none" | CalendarRecurrence["frequency"];
type RecurrenceEnd = "never" | "on" | "count";
type EventScope = "event" | "series" | "occurrence";
type TimePreset = (typeof timePresets)[number];

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

type CalendarEventSheetProps = {
  open: boolean;
  target: CalendarEditorTarget | null;
  defaultTimeZone: string;
  pending: boolean;
  error: string | null;
  onOpenChange: (open: boolean) => void;
  onCreate: (input: CalendarEventInput) => Promise<void>;
  onUpdate: (input: UpdateCalendarEventInput) => Promise<void>;
  onDelete: (input: DeleteCalendarEventInput) => Promise<void>;
};

type FormValues = {
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

type TimeZoneOption = {
  value: string;
  label: string;
  searchText: string;
};

type DatePickerControlProps = {
  id: string;
  value: string;
  disabled: boolean;
  onValueChange: (value: string) => void;
};

function parseCalendarDate(value: string): Date | undefined {
  const match = /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$/.exec(value);
  if (!match?.groups) {
    return undefined;
  }

  return new Date(
    Number(match.groups.year),
    Number(match.groups.month) - 1,
    Number(match.groups.day),
    12,
  );
}

function calendarDateValue(date: Date): string {
  const year = date.getFullYear().toString().padStart(4, "0");
  const month = (date.getMonth() + 1).toString().padStart(2, "0");
  const day = date.getDate().toString().padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatCalendarDate(value: string): string {
  const date = parseCalendarDate(value);
  if (!date) {
    return "Select date";
  }

  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(date);
}

function DatePickerControl({ id, value, disabled, onValueChange }: DatePickerControlProps) {
  const [open, setOpen] = useState(false);
  const selected = parseCalendarDate(value);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={
          <Button
            id={id}
            className="w-full min-w-0 justify-start overflow-hidden text-left font-normal"
            disabled={disabled}
            type="button"
            variant="outline"
          />
        }
      >
        <CalendarDays />
        <span className={selected ? "min-w-0 truncate" : "min-w-0 truncate text-muted-foreground"}>
          {formatCalendarDate(value)}
        </span>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-auto p-0">
        <Calendar
          mode="single"
          defaultMonth={selected}
          selected={selected}
          onSelect={(date) => {
            if (!date) {
              return;
            }

            onValueChange(calendarDateValue(date));
            setOpen(false);
          }}
        />
      </PopoverContent>
    </Popover>
  );
}

function timeZoneOffsetLabel(timeZone: string, dateValue: string, timeValue: string): string {
  const localValue = `${dateValue}T${timeValue || "12:00"}`;
  const instant = fromDateTimeInputValue(localValue, timeZone) ?? new Date();
  const offset = new Intl.DateTimeFormat("en-US", {
    timeZone,
    timeZoneName: "longOffset",
  })
    .formatToParts(instant)
    .find((part) => part.type === "timeZoneName")?.value;

  if (!offset || offset === "GMT") {
    return "UTC+00:00";
  }

  return offset.replace("GMT", "UTC");
}

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
}

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

function valuesForSource(
  target: CalendarEditorTarget,
  scope: EventScope,
  defaultTimeZone: string,
): FormValues {
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

function emptyValues(timeZone: string): FormValues {
  const now = new Date();
  return valuesForSource(
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

export function CalendarEventSheet({
  open,
  target,
  defaultTimeZone,
  pending,
  error,
  onOpenChange,
  onCreate,
  onUpdate,
  onDelete,
}: CalendarEventSheetProps) {
  const isRecurring = target?.kind === "edit" && target.series.recurrence !== null;
  const [scope, setScope] = useState<EventScope>("event");
  const [values, setValues] = useState<FormValues>(() => emptyValues(defaultTimeZone));
  const [initialSignature, setInitialSignature] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [confirmDiscard, setConfirmDiscard] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [timeZoneQuery, setTimeZoneQuery] = useState("");
  const timeZones = useMemo(
    () =>
      Array.from(
        new Set([defaultTimeZone, values.timeZone, ...Intl.supportedValuesOf("timeZone")]),
      ),
    [defaultTimeZone, values.timeZone],
  );
  const timeZoneOptions = useMemo<TimeZoneOption[]>(
    () =>
      timeZones.map((timeZone) => {
        const offset = timeZoneOffsetLabel(timeZone, values.startDate, values.startTime);
        const label = `${offset} · ${timeZone}`;
        return {
          value: timeZone,
          label,
          searchText: `${offset} ${timeZone}`.toLocaleLowerCase(),
        };
      }),
    [timeZones, values.startDate, values.startTime],
  );
  const selectedTimeZoneOption =
    timeZoneOptions.find((option) => option.value === values.timeZone) ?? null;

  useEffect(() => {
    if (selectedTimeZoneOption) {
      setTimeZoneQuery(selectedTimeZoneOption.label);
    }
  }, [selectedTimeZoneOption?.label, selectedTimeZoneOption?.value]);

  useEffect(() => {
    if (!open || target === null) {
      return;
    }

    let nextScope: EventScope = "event";
    if (target.kind === "edit" && target.series.recurrence !== null) {
      nextScope = "occurrence";
    }

    const nextValues = valuesForSource(target, nextScope, defaultTimeZone);
    setScope(nextScope);
    setValues(nextValues);
    setInitialSignature(JSON.stringify({ scope: nextScope, values: nextValues }));
    setValidationError(null);
    setConfirmDiscard(false);
    setConfirmDelete(false);
  }, [defaultTimeZone, open, target]);

  const currentSignature = JSON.stringify({ scope, values });
  const isDirty = initialSignature.length > 0 && currentSignature !== initialSignature;

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
    setValidationError(null);
  }

  function changeScope(nextScope: EventScope) {
    if (target === null) {
      return;
    }

    const nextValues = valuesForSource(target, nextScope, defaultTimeZone);
    setScope(nextScope);
    setValues(nextValues);
    setInitialSignature(JSON.stringify({ scope: nextScope, values: nextValues }));
    setValidationError(null);
  }

  function changeAllDay(checked: boolean) {
    if (checked === values.allDay) {
      return;
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

      setValues((current) => ({
        ...current,
        allDay: true,
        startDate,
        endDate,
      }));
      return;
    }

    setValues((current) => ({
      ...current,
      allDay: false,
      startTime: "09:00",
      endDate: current.startDate,
      endTime: "10:00",
    }));
  }

  function presetEndDate(preset: TimePreset): string {
    if (preset.end > preset.start) {
      return values.startDate;
    }

    const start = fromDateInputValue(values.startDate, values.timeZone);
    if (start === null) {
      return values.startDate;
    }

    return toDateInputValue(addZonedDays(start, 1, values.timeZone), values.timeZone);
  }

  function applyTimePreset(preset: TimePreset) {
    setValues((current) => ({
      ...current,
      allDay: false,
      startTime: preset.start,
      endDate: presetEndDate(preset),
      endTime: preset.end,
    }));
    setValidationError(null);
  }

  function isTimePresetActive(preset: TimePreset): boolean {
    return (
      !values.allDay &&
      values.startTime === preset.start &&
      values.endTime === preset.end &&
      values.endDate === presetEndDate(preset)
    );
  }

  function requestClose() {
    if (pending) {
      return;
    }

    if (isDirty) {
      setConfirmDiscard(true);
      return;
    }

    onOpenChange(false);
  }

  function toggleWeekday(value: string) {
    const next = values.byWeekday.includes(value)
      ? values.byWeekday.filter((weekday) => weekday !== value)
      : [...values.byWeekday, value];
    updateValue("byWeekday", next);
  }

  function buildInput(): CalendarEventInput | null {
    const title = values.title.trim();
    if (title.length === 0) {
      setValidationError("Enter a title.");
      return null;
    }

    if (title.length > maximumTitleLength) {
      setValidationError(`Titles must be ${maximumTitleLength} characters or fewer.`);
      return null;
    }

    const normalizedTimeZoneQuery = timeZoneQuery.trim().toLocaleLowerCase();
    const hasSelectedTimeZone =
      selectedTimeZoneOption !== null &&
      (selectedTimeZoneOption.label.toLocaleLowerCase() === normalizedTimeZoneQuery ||
        selectedTimeZoneOption.value.toLocaleLowerCase() === normalizedTimeZoneQuery);
    if (!timeZones.includes(values.timeZone) || !hasSelectedTimeZone) {
      setValidationError("Choose a valid IANA time zone.");
      return null;
    }

    const start = values.allDay
      ? fromDateInputValue(values.startDate, values.timeZone)
      : fromDateTimeInputValue(`${values.startDate}T${values.startTime}`, values.timeZone);
    const end = values.allDay
      ? fromDateInputValue(values.endDate, values.timeZone)
      : fromDateTimeInputValue(`${values.endDate}T${values.endTime}`, values.timeZone);
    if (start === null || end === null || end <= start) {
      setValidationError(
        values.allDay
          ? "The “ends before” date must follow the start date."
          : "End must be after start.",
      );
      return null;
    }

    let recurrence: CalendarRecurrence | null = null;
    if (values.frequency !== "none") {
      const interval = Number(values.interval);
      if (!Number.isInteger(interval) || interval < 1 || interval > 365) {
        setValidationError("Repeat interval must be between 1 and 365.");
        return null;
      }

      if (values.frequency === "weekly" && values.byWeekday.length === 0) {
        setValidationError("Choose at least one weekday.");
        return null;
      }

      let count: number | null = null;
      let until: string | null = null;
      if (values.recurrenceEnd === "count") {
        count = Number(values.count);
        if (!Number.isInteger(count) || count < 1 || count > 1000) {
          setValidationError("Occurrence count must be between 1 and 1,000.");
          return null;
        }
      } else if (values.recurrenceEnd === "on") {
        const untilDate = recurrenceUntilFromDate(values.until, values.timeZone);
        if (untilDate === null || untilDate < start) {
          setValidationError("The recurrence end date must be on or after the first occurrence.");
          return null;
        }
        until = untilDate.toISOString();
      }

      recurrence = {
        frequency: values.frequency,
        interval,
        count,
        until,
        byWeekday: values.frequency === "weekly" ? values.byWeekday : [],
        byMonthDay: [],
        byMonth: [],
        weekStart: "MO",
      };
    }

    return {
      title,
      start: start.toISOString(),
      end: end.toISOString(),
      allDay: values.allDay,
      timeZoneId: values.timeZone,
      recurrence,
    };
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const input = buildInput();
    if (input === null || target === null) {
      return;
    }

    try {
      if (target.kind === "create") {
        await onCreate(input);
        return;
      }

      const source = target.occurrence.event.data;
      const eventId = target.series.id;
      let originalStart: string | null = null;
      if (scope === "occurrence") {
        originalStart = source?.originalStart ?? target.occurrence.start.toISOString();
      }

      await onUpdate({ ...input, eventId, scope, originalStart });
    } catch {
      // The mutation error remains visible in the sheet for correction or retry.
    }
  }

  async function handleDelete() {
    if (target?.kind !== "edit") {
      return;
    }

    const source = target.occurrence.event.data;
    try {
      await onDelete({
        eventId: target.series.id,
        scope,
        originalStart:
          scope === "occurrence"
            ? (source?.originalStart ?? target.occurrence.start.toISOString())
            : null,
      });
      setConfirmDelete(false);
    } catch {
      setConfirmDelete(false);
    }
  }

  let title = "New calendar entry";
  let description = "Add busy time to your calendar.";
  if (target?.kind === "edit") {
    title = target.occurrence.event.readOnly ? "Calendar entry" : "Edit calendar entry";
    description = target.occurrence.event.readOnly
      ? "This entry belongs to a connected calendar."
      : "Change the time, recurrence, or title.";
  }
  const readOnly = target?.kind === "edit" && target.occurrence.event.readOnly === true;

  return (
    <>
      <Sheet
        open={open}
        onOpenChange={(nextOpen) => (nextOpen ? onOpenChange(true) : requestClose())}
      >
        <SheetContent
          className="w-full max-w-none gap-0 p-0 sm:max-w-lg"
          showCloseButton={!pending}
          side="right"
        >
          <form className="flex min-h-0 flex-1 flex-col" onSubmit={handleSubmit}>
            <SheetHeader className="shrink-0 border-b pr-14">
              <div className="flex items-center gap-2">
                <CalendarClock className="size-4 text-muted-foreground" />
                <SheetTitle>{title}</SheetTitle>
              </div>
              <SheetDescription>{description}</SheetDescription>
            </SheetHeader>

            <ScrollArea className="min-h-0 flex-1">
              <FieldGroup className="p-6">
                {readOnly && target?.kind === "edit" && target.owner ? (
                  <div className="flex items-center gap-3 rounded-lg border bg-muted/40 p-3">
                    <Avatar className="size-10">
                      <AvatarImage alt="" src={target.owner.pictureUrl ?? undefined} />
                      <AvatarFallback>{initials(target.owner.title)}</AvatarFallback>
                    </Avatar>
                    <div className="min-w-0">
                      <p className="text-xs text-muted-foreground">Owned by</p>
                      <p className="truncate font-medium">{target.owner.title}</p>
                    </div>
                  </div>
                ) : null}

                {isRecurring ? (
                  <Field>
                    <FieldLabel htmlFor="calendar-edit-scope">Apply changes to</FieldLabel>
                    <Select
                      disabled={pending || readOnly}
                      value={scope}
                      onValueChange={(value) => changeScope(value as EventScope)}
                    >
                      <SelectTrigger id="calendar-edit-scope" className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="occurrence">This occurrence</SelectItem>
                        <SelectItem value="series">Entire series</SelectItem>
                      </SelectContent>
                    </Select>
                    <FieldDescription>
                      Moving or resizing a single occurrence keeps the rest of the series unchanged.
                    </FieldDescription>
                  </Field>
                ) : null}

                <Field>
                  <FieldLabel htmlFor="calendar-entry-title">Title</FieldLabel>
                  <Input
                    id="calendar-entry-title"
                    autoFocus
                    disabled={pending || readOnly}
                    maxLength={maximumTitleLength}
                    onChange={(event) => updateValue("title", event.target.value)}
                    placeholder="Work, sleep, appointment…"
                    value={values.title}
                  />
                </Field>

                <label className="flex items-center gap-3 rounded-lg border p-3">
                  <Checkbox
                    checked={values.allDay}
                    disabled={pending || readOnly}
                    onCheckedChange={changeAllDay}
                  />
                  <span>
                    <span className="block font-medium">All day</span>
                    <span className="block text-muted-foreground">
                      Use dates instead of exact times.
                    </span>
                  </span>
                </label>

                <Field>
                  <FieldLabel>Quick times</FieldLabel>
                  <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                    {timePresets.map((preset) => {
                      const active = isTimePresetActive(preset);
                      return (
                        <Button
                          key={preset.id}
                          aria-pressed={active}
                          className="h-auto flex-col items-start gap-0 py-2"
                          disabled={pending || readOnly}
                          onClick={() => applyTimePreset(preset)}
                          type="button"
                          variant={active ? "secondary" : "outline"}
                        >
                          <span>{preset.label}</span>
                          <span className="font-normal text-muted-foreground">
                            {preset.description}
                          </span>
                        </Button>
                      );
                    })}
                  </div>
                  <FieldDescription>
                    Apply a common shift, then adjust the dates or times if needed.
                  </FieldDescription>
                </Field>

                <div className="grid gap-4">
                  <Field>
                    <FieldLabel htmlFor="calendar-entry-start">Starts</FieldLabel>
                    <div
                      className={
                        values.allDay ? undefined : "grid grid-cols-[minmax(0,1fr)_7rem] gap-2"
                      }
                    >
                      <DatePickerControl
                        id="calendar-entry-start"
                        disabled={pending || readOnly}
                        onValueChange={(value) => updateValue("startDate", value)}
                        value={values.startDate}
                      />
                      {!values.allDay ? (
                        <Input
                          aria-label="Start time"
                          disabled={pending || readOnly}
                          onChange={(event) => updateValue("startTime", event.target.value)}
                          step={60}
                          type="time"
                          value={values.startTime}
                        />
                      ) : null}
                    </div>
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="calendar-entry-end">
                      {values.allDay ? "Ends before" : "Ends"}
                    </FieldLabel>
                    <div
                      className={
                        values.allDay ? undefined : "grid grid-cols-[minmax(0,1fr)_7rem] gap-2"
                      }
                    >
                      <DatePickerControl
                        id="calendar-entry-end"
                        disabled={pending || readOnly}
                        onValueChange={(value) => updateValue("endDate", value)}
                        value={values.endDate}
                      />
                      {!values.allDay ? (
                        <Input
                          aria-label="End time"
                          disabled={pending || readOnly}
                          onChange={(event) => updateValue("endTime", event.target.value)}
                          step={60}
                          type="time"
                          value={values.endTime}
                        />
                      ) : null}
                    </div>
                  </Field>
                </div>

                <Field>
                  <FieldLabel htmlFor="calendar-entry-time-zone">Time zone</FieldLabel>
                  <Combobox
                    autoHighlight
                    filter={(option, query) =>
                      option.searchText.includes(query.trim().toLocaleLowerCase())
                    }
                    isItemEqualToValue={(option, value) => option.value === value.value}
                    inputValue={timeZoneQuery}
                    items={timeZoneOptions}
                    onInputValueChange={setTimeZoneQuery}
                    value={selectedTimeZoneOption}
                    onValueChange={(option) => {
                      if (option) {
                        updateValue("timeZone", option.value);
                      }
                    }}
                  >
                    <ComboboxInput
                      id="calendar-entry-time-zone"
                      autoComplete="off"
                      className="w-full"
                      disabled={pending || readOnly}
                      placeholder="Search time zones…"
                    />
                    <ComboboxContent>
                      <ComboboxEmpty>No time zones found.</ComboboxEmpty>
                      <ComboboxList>
                        {(option: TimeZoneOption) => (
                          <ComboboxItem key={option.value} value={option}>
                            {option.label}
                          </ComboboxItem>
                        )}
                      </ComboboxList>
                    </ComboboxContent>
                  </Combobox>
                  <FieldDescription>
                    Recurring entries keep this local time across daylight-saving changes.
                  </FieldDescription>
                </Field>

                {scope !== "occurrence" ? (
                  <>
                    <Field>
                      <FieldLabel htmlFor="calendar-entry-repeat">Repeats</FieldLabel>
                      <Select
                        disabled={pending || readOnly}
                        value={values.frequency}
                        onValueChange={(value) =>
                          updateValue("frequency", value as RecurrenceFrequency)
                        }
                      >
                        <SelectTrigger id="calendar-entry-repeat" className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="none">Does not repeat</SelectItem>
                          <SelectItem value="daily">Daily</SelectItem>
                          <SelectItem value="weekly">Weekly</SelectItem>
                          <SelectItem value="monthly">Monthly</SelectItem>
                          <SelectItem value="yearly">Yearly</SelectItem>
                        </SelectContent>
                      </Select>
                    </Field>

                    {values.frequency !== "none" ? (
                      <>
                        <Field>
                          <FieldLabel htmlFor="calendar-entry-interval">Repeat every</FieldLabel>
                          <div className="flex items-center gap-2">
                            <Input
                              id="calendar-entry-interval"
                              className="w-24"
                              disabled={pending || readOnly}
                              max={365}
                              min={1}
                              onChange={(event) => updateValue("interval", event.target.value)}
                              type="number"
                              value={values.interval}
                            />
                            <span className="text-muted-foreground">
                              {values.frequency === "daily"
                                ? "day(s)"
                                : values.frequency === "weekly"
                                  ? "week(s)"
                                  : values.frequency === "monthly"
                                    ? "month(s)"
                                    : "year(s)"}
                            </span>
                          </div>
                        </Field>

                        {values.frequency === "weekly" ? (
                          <Field>
                            <FieldLabel>On weekdays</FieldLabel>
                            <div className="flex flex-wrap gap-2">
                              {weekdays.map((weekday) => (
                                <Button
                                  key={weekday.value}
                                  aria-pressed={values.byWeekday.includes(weekday.value)}
                                  className="size-8 rounded-full"
                                  disabled={pending || readOnly}
                                  onClick={() => toggleWeekday(weekday.value)}
                                  size="icon-sm"
                                  type="button"
                                  variant={
                                    values.byWeekday.includes(weekday.value) ? "default" : "outline"
                                  }
                                >
                                  {weekday.label}
                                </Button>
                              ))}
                            </div>
                          </Field>
                        ) : null}

                        <Field>
                          <FieldLabel htmlFor="calendar-entry-repeat-end">Ends</FieldLabel>
                          <Select
                            disabled={pending || readOnly}
                            value={values.recurrenceEnd}
                            onValueChange={(value) =>
                              updateValue("recurrenceEnd", value as RecurrenceEnd)
                            }
                          >
                            <SelectTrigger id="calendar-entry-repeat-end" className="w-full">
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="never">Never</SelectItem>
                              <SelectItem value="on">On a date</SelectItem>
                              <SelectItem value="count">After occurrences</SelectItem>
                            </SelectContent>
                          </Select>
                        </Field>

                        {values.recurrenceEnd === "on" ? (
                          <Field>
                            <FieldLabel htmlFor="calendar-entry-until">Last date</FieldLabel>
                            <DatePickerControl
                              id="calendar-entry-until"
                              disabled={pending || readOnly}
                              onValueChange={(value) => updateValue("until", value)}
                              value={values.until}
                            />
                          </Field>
                        ) : null}

                        {values.recurrenceEnd === "count" ? (
                          <Field>
                            <FieldLabel htmlFor="calendar-entry-count">Occurrences</FieldLabel>
                            <Input
                              id="calendar-entry-count"
                              disabled={pending || readOnly}
                              max={1000}
                              min={1}
                              onChange={(event) => updateValue("count", event.target.value)}
                              type="number"
                              value={values.count}
                            />
                          </Field>
                        ) : null}
                      </>
                    ) : null}
                  </>
                ) : null}

                {validationError !== null ? <FieldError>{validationError}</FieldError> : null}
                {error !== null ? <FieldError>{error}</FieldError> : null}
              </FieldGroup>
            </ScrollArea>

            {!readOnly ? (
              <SheetFooter className="shrink-0 flex-row border-t bg-popover">
                {target?.kind === "edit" ? (
                  <Button
                    disabled={pending}
                    onClick={() => setConfirmDelete(true)}
                    type="button"
                    variant="destructive"
                  >
                    <Trash2 data-icon="inline-start" />
                    Delete
                  </Button>
                ) : null}
                <div className="flex-1" />
                <Button disabled={pending} onClick={requestClose} type="button" variant="outline">
                  Cancel
                </Button>
                <Button disabled={pending} type="submit">
                  {pending ? <Loader2 className="animate-spin" /> : null}
                  {pending ? "Saving…" : "Save"}
                </Button>
              </SheetFooter>
            ) : null}
          </form>
        </SheetContent>
      </Sheet>

      <AlertDialog open={confirmDiscard} onOpenChange={setConfirmDiscard}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Discard unsaved changes?</AlertDialogTitle>
            <AlertDialogDescription>
              Your changes to this calendar entry will be lost.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Keep editing</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => {
                setConfirmDiscard(false);
                onOpenChange(false);
              }}
            >
              Discard
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {scope === "occurrence" ? "Delete this occurrence?" : "Delete this entry?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {scope === "series"
                ? "The entire recurring series and its exceptions will be removed."
                : "This busy time will no longer appear on your calendar."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={pending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={pending}
              variant="destructive"
              onClick={() => void handleDelete()}
            >
              {pending ? <Loader2 className="animate-spin" /> : null}
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
