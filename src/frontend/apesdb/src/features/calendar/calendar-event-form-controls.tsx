import { useEffect, useMemo, useState } from "react";
import {
  Button,
  Calendar,
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@apesdb/ui";
import { CalendarDays } from "lucide-react";
import { fromDateTimeInputValue } from "./calendar-time";

type DatePickerControlProps = {
  id: string;
  value: string;
  disabled: boolean;
  onValueChange: (value: string) => void;
};

type TimeZoneOption = {
  value: string;
  label: string;
  searchText: string;
};

type TimeZoneControlProps = {
  value: string;
  startDate: string;
  startTime: string;
  defaultTimeZone: string;
  disabled: boolean;
  onValueChange: (value: string) => void;
  onInteraction: () => void;
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

export function DatePickerControl({ id, value, disabled, onValueChange }: DatePickerControlProps) {
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

export function TimeZoneControl({
  value,
  startDate,
  startTime,
  defaultTimeZone,
  disabled,
  onValueChange,
  onInteraction,
}: TimeZoneControlProps) {
  const [query, setQuery] = useState("");
  const timeZones = useMemo(
    () =>
      Array.from(
        new Set(
          [defaultTimeZone, value, ...Intl.supportedValuesOf("timeZone")].filter(
            (timeZone) => timeZone.length > 0,
          ),
        ),
      ),
    [defaultTimeZone, value],
  );
  const options = useMemo<TimeZoneOption[]>(
    () =>
      timeZones.map((timeZone) => {
        const offset = timeZoneOffsetLabel(timeZone, startDate, startTime);
        const label = `${offset} · ${timeZone}`;
        return {
          value: timeZone,
          label,
          searchText: `${offset} ${timeZone}`.toLocaleLowerCase(),
        };
      }),
    [startDate, startTime, timeZones],
  );
  const selected = options.find((option) => option.value === value) ?? null;

  useEffect(() => {
    if (selected) {
      setQuery(selected.label);
    }
  }, [selected?.label, selected?.value]);

  function changeQuery(nextQuery: string) {
    setQuery(nextQuery);
    onInteraction();

    const normalized = nextQuery.trim().toLocaleLowerCase();
    const match = options.find(
      (option) =>
        option.label.toLocaleLowerCase() === normalized ||
        option.value.toLocaleLowerCase() === normalized,
    );
    onValueChange(match?.value ?? "");
  }

  return (
    <Combobox
      autoHighlight
      filter={(option, search) => option.searchText.includes(search.trim().toLocaleLowerCase())}
      isItemEqualToValue={(option, selectedValue) => option.value === selectedValue.value}
      inputValue={query}
      items={options}
      onInputValueChange={changeQuery}
      value={selected}
      onValueChange={(option) => {
        if (!option) {
          return;
        }

        setQuery(option.label);
        onInteraction();
        onValueChange(option.value);
      }}
    >
      <ComboboxInput
        id="calendar-entry-time-zone"
        autoComplete="off"
        className="w-full"
        disabled={disabled}
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
  );
}
