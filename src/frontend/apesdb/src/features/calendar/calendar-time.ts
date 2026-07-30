import { TZDate } from "@date-fns/tz";

export function browserTimeZone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone || "Etc/UTC";
}

function pad(value: number): string {
  return value.toString().padStart(2, "0");
}

export function toDateTimeInputValue(date: Date, timeZone: string): string {
  const zoned = new TZDate(date.getTime(), timeZone);
  return `${zoned.getFullYear()}-${pad(zoned.getMonth() + 1)}-${pad(zoned.getDate())}T${pad(
    zoned.getHours(),
  )}:${pad(zoned.getMinutes())}`;
}

export function toDateInputValue(date: Date, timeZone: string): string {
  return toDateTimeInputValue(date, timeZone).slice(0, 10);
}

export function fromDateTimeInputValue(value: string, timeZone: string): Date | null {
  const match =
    /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})T(?<hour>\d{2}):(?<minute>\d{2})$/.exec(value);
  if (!match?.groups) {
    return null;
  }

  const date = new TZDate(
    Number(match.groups.year),
    Number(match.groups.month) - 1,
    Number(match.groups.day),
    Number(match.groups.hour),
    Number(match.groups.minute),
    0,
    timeZone,
  );
  return new Date(date.getTime());
}

export function fromDateInputValue(value: string, timeZone: string): Date | null {
  const match = /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$/.exec(value);
  if (!match?.groups) {
    return null;
  }

  const date = new TZDate(
    Number(match.groups.year),
    Number(match.groups.month) - 1,
    Number(match.groups.day),
    0,
    0,
    0,
    timeZone,
  );
  return new Date(date.getTime());
}

export function recurrenceUntilFromDate(value: string, timeZone: string): Date | null {
  const start = fromDateInputValue(value, timeZone);
  if (start === null) {
    return null;
  }

  const zoned = new TZDate(start.getTime(), timeZone);
  zoned.setHours(23, 59, 59, 999);
  return new Date(zoned.getTime());
}

export function addMinutes(date: Date, minutes: number): Date {
  return new Date(date.getTime() + minutes * 60_000);
}

export function addZonedDays(date: Date, days: number, timeZone: string): Date {
  const zoned = new TZDate(date.getTime(), timeZone);
  zoned.setDate(zoned.getDate() + days);
  return new Date(zoned.getTime());
}
