import { z } from "zod";

export const calendarRecurrenceSchema = z.object({
  frequency: z.enum(["daily", "weekly", "monthly", "yearly"]),
  interval: z.number().int().min(1),
  count: z.number().int().positive().nullable(),
  until: z.string().nullable(),
  byWeekday: z.array(z.string()),
  byMonthDay: z.array(z.number().int()),
  byMonth: z.array(z.number().int()),
  weekStart: z.string().nullable(),
});

export const calendarResourceSchema = z.object({
  id: z.string(),
  title: z.string(),
  pictureUrl: z.string().nullable(),
  isCurrentUser: z.boolean(),
});

export const calendarEventSchema = z.object({
  id: z.string(),
  resourceId: z.string(),
  title: z.string(),
  start: z.string(),
  end: z.string(),
  allDay: z.boolean(),
  timeZoneId: z.string(),
  recurrence: calendarRecurrenceSchema.nullable(),
  exDates: z.array(z.string()),
  recurringEventId: z.string().nullable(),
  originalStart: z.string().nullable(),
  readOnly: z.boolean(),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export const calendarRangeSchema = z.object({
  resources: z.array(calendarResourceSchema),
  events: z.array(calendarEventSchema),
});

const calendarUserSchema = z.object({
  id: z.string(),
  name: z.string(),
  pictureUrl: z.string().nullable(),
});

const calendarConnectionSchema = z.object({
  id: z.string(),
  user: calendarUserSchema,
  createdAt: z.string(),
});

const incomingCalendarInvitationSchema = z.object({
  id: z.string(),
  invitedBy: calendarUserSchema,
  createdAt: z.string(),
});

const outgoingCalendarInvitationSchema = z.object({
  id: z.string(),
  email: z.string(),
  createdAt: z.string(),
});

export const calendarSharingSchema = z.object({
  connections: z.array(calendarConnectionSchema),
  incomingInvitations: z.array(incomingCalendarInvitationSchema),
  outgoingInvitations: z.array(outgoingCalendarInvitationSchema),
});

export type CalendarRecurrence = z.infer<typeof calendarRecurrenceSchema>;
export type CalendarResource = z.infer<typeof calendarResourceSchema>;
export type CalendarEvent = z.infer<typeof calendarEventSchema>;
export type CalendarRange = z.infer<typeof calendarRangeSchema>;
export type CalendarSharing = z.infer<typeof calendarSharingSchema>;
