import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@apesdb/ui";
import { CalendarEventRecurrenceFields } from "./calendar-event-recurrence-fields";
import { CalendarEventScheduleFields } from "./calendar-event-schedule-fields";
import { calendarEventFormOptions, withCalendarEventForm } from "./calendar-event-form.context";
import { maximumTitleLength, type EventScope } from "./calendar-event-form.model";
import type { CalendarResource } from "./calendar.schemas";

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
}

function OwnerNotice({ owner }: { owner: CalendarResource }) {
  return (
    <div className="flex items-center gap-3 rounded-lg border bg-muted/40 p-3">
      <Avatar className="size-10">
        <AvatarImage alt="" src={owner.pictureUrl ?? undefined} />
        <AvatarFallback>{initials(owner.title)}</AvatarFallback>
      </Avatar>
      <div className="min-w-0">
        <p className="text-xs text-muted-foreground">Owned by</p>
        <p className="truncate font-medium">{owner.title}</p>
      </div>
    </div>
  );
}

export const CalendarEventForm = withCalendarEventForm({
  ...calendarEventFormOptions,
  props: {
    defaultTimeZone: "Etc/UTC",
    disabled: false,
    error: null as string | null,
    isRecurring: false,
    onScopeChange: (_scope: EventScope) => {},
    owner: null as CalendarResource | null,
    readOnly: false,
    scope: "event" as EventScope,
  },
  render: function RenderCalendarEventForm({
    form,
    defaultTimeZone,
    disabled,
    error,
    isRecurring,
    onScopeChange,
    owner,
    readOnly,
    scope,
  }) {
    function clearValidationError() {
      form.setErrorMap({ onSubmit: undefined });
    }

    return (
      <FieldGroup className="p-6">
        {readOnly && owner ? <OwnerNotice owner={owner} /> : null}

        {isRecurring ? (
          <Field>
            <FieldLabel htmlFor="calendar-edit-scope">Apply changes to</FieldLabel>
            <Select
              disabled={disabled}
              value={scope}
              onValueChange={(value) => onScopeChange(value as EventScope)}
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

        <form.AppField
          name="title"
          children={(field) => (
            <Field>
              <FieldLabel htmlFor="calendar-entry-title">Title</FieldLabel>
              <Input
                id="calendar-entry-title"
                autoFocus
                disabled={disabled}
                maxLength={maximumTitleLength}
                onChange={(event) => {
                  field.handleChange(event.target.value);
                  clearValidationError();
                }}
                placeholder="Work, sleep, appointment…"
                value={field.state.value}
              />
            </Field>
          )}
        />

        <CalendarEventScheduleFields
          form={form}
          defaultTimeZone={defaultTimeZone}
          disabled={disabled}
        />
        <CalendarEventRecurrenceFields form={form} disabled={disabled} scope={scope} />

        <form.Subscribe
          selector={(state) => state.errorMap.onSubmit}
          children={(validationError) =>
            typeof validationError === "string" ? <FieldError>{validationError}</FieldError> : null
          }
        />
        {error !== null ? <FieldError>{error}</FieldError> : null}
      </FieldGroup>
    );
  },
});
