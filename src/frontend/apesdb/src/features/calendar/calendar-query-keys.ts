export const calendarQueryKeys = {
  all: ["calendar"] as const,
  range: (start: string, end: string) => ["calendar", "range", start, end] as const,
  sharing: ["calendar", "sharing"] as const,
};
