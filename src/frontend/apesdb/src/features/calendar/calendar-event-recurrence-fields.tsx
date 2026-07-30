import {
  Button,
  Field,
  FieldLabel,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@apesdb/ui";
import { DatePickerControl } from "./calendar-event-form-controls";
import { calendarEventFormOptions, withCalendarEventForm } from "./calendar-event-form.context";
import {
  valuesWithToggledWeekday,
  weekdays,
  type EventScope,
  type RecurrenceEnd,
  type RecurrenceFrequency,
} from "./calendar-event-form.model";

export const CalendarEventRecurrenceFields = withCalendarEventForm({
  ...calendarEventFormOptions,
  props: {
    disabled: false,
    scope: "event" as EventScope,
  },
  render: function RenderCalendarEventRecurrenceFields({ form, disabled, scope }) {
    function clearValidationError() {
      form.setErrorMap({ onSubmit: undefined });
    }

    if (scope === "occurrence") {
      return null;
    }

    return (
      <>
        <form.AppField
          name="frequency"
          children={(field) => (
            <Field>
              <FieldLabel htmlFor="calendar-entry-repeat">Repeats</FieldLabel>
              <Select
                disabled={disabled}
                value={field.state.value}
                onValueChange={(value) => {
                  field.handleChange(value as RecurrenceFrequency);
                  clearValidationError();
                }}
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
          )}
        />

        <form.Subscribe
          selector={(state) => state.values.frequency}
          children={(frequency) => {
            if (frequency === "none") {
              return null;
            }

            let intervalLabel = "year(s)";
            if (frequency === "daily") {
              intervalLabel = "day(s)";
            } else if (frequency === "weekly") {
              intervalLabel = "week(s)";
            } else if (frequency === "monthly") {
              intervalLabel = "month(s)";
            }

            return (
              <>
                <form.AppField
                  name="interval"
                  children={(field) => (
                    <Field>
                      <FieldLabel htmlFor="calendar-entry-interval">Repeat every</FieldLabel>
                      <div className="flex items-center gap-2">
                        <Input
                          id="calendar-entry-interval"
                          className="w-24"
                          disabled={disabled}
                          max={365}
                          min={1}
                          onChange={(event) => {
                            field.handleChange(event.target.value);
                            clearValidationError();
                          }}
                          type="number"
                          value={field.state.value}
                        />
                        <span className="text-muted-foreground">{intervalLabel}</span>
                      </div>
                    </Field>
                  )}
                />

                {frequency === "weekly" ? (
                  <form.AppField
                    name="byWeekday"
                    children={(field) => (
                      <Field>
                        <FieldLabel>On weekdays</FieldLabel>
                        <div className="flex flex-wrap gap-2">
                          {weekdays.map((weekday) => (
                            <Button
                              key={weekday.value}
                              aria-pressed={field.state.value.includes(weekday.value)}
                              className="size-8 rounded-full"
                              disabled={disabled}
                              onClick={() => {
                                const values = valuesWithToggledWeekday(
                                  form.state.values,
                                  weekday.value,
                                );
                                field.handleChange(values.byWeekday);
                                clearValidationError();
                              }}
                              size="icon-sm"
                              type="button"
                              variant={
                                field.state.value.includes(weekday.value) ? "default" : "outline"
                              }
                            >
                              {weekday.label}
                            </Button>
                          ))}
                        </div>
                      </Field>
                    )}
                  />
                ) : null}

                <form.AppField
                  name="recurrenceEnd"
                  children={(field) => (
                    <Field>
                      <FieldLabel htmlFor="calendar-entry-repeat-end">Ends</FieldLabel>
                      <Select
                        disabled={disabled}
                        value={field.state.value}
                        onValueChange={(value) => {
                          field.handleChange(value as RecurrenceEnd);
                          clearValidationError();
                        }}
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
                  )}
                />

                <form.Subscribe
                  selector={(state) => state.values.recurrenceEnd}
                  children={(recurrenceEnd) => {
                    if (recurrenceEnd === "on") {
                      return (
                        <form.AppField
                          name="until"
                          children={(field) => (
                            <Field>
                              <FieldLabel htmlFor="calendar-entry-until">Last date</FieldLabel>
                              <DatePickerControl
                                id="calendar-entry-until"
                                disabled={disabled}
                                onValueChange={(value) => {
                                  field.handleChange(value);
                                  clearValidationError();
                                }}
                                value={field.state.value}
                              />
                            </Field>
                          )}
                        />
                      );
                    }

                    if (recurrenceEnd === "count") {
                      return (
                        <form.AppField
                          name="count"
                          children={(field) => (
                            <Field>
                              <FieldLabel htmlFor="calendar-entry-count">Occurrences</FieldLabel>
                              <Input
                                id="calendar-entry-count"
                                disabled={disabled}
                                max={1000}
                                min={1}
                                onChange={(event) => {
                                  field.handleChange(event.target.value);
                                  clearValidationError();
                                }}
                                type="number"
                                value={field.state.value}
                              />
                            </Field>
                          )}
                        />
                      );
                    }

                    return null;
                  }}
                />
              </>
            );
          }}
        />
      </>
    );
  },
});
