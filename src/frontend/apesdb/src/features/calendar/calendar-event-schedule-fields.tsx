import { Button, Checkbox, Field, FieldDescription, FieldLabel, Input } from "@apesdb/ui";
import { DatePickerControl, TimeZoneControl } from "./calendar-event-form-controls";
import { calendarEventFormOptions, withCalendarEventForm } from "./calendar-event-form.context";
import {
  isTimePresetActive,
  timePresets,
  valuesWithAllDay,
  valuesWithTimePreset,
  type CalendarEventFormValues,
} from "./calendar-event-form.model";

export const CalendarEventScheduleFields = withCalendarEventForm({
  ...calendarEventFormOptions,
  props: {
    defaultTimeZone: "Etc/UTC",
    disabled: false,
  },
  render: function RenderCalendarEventScheduleFields({ form, defaultTimeZone, disabled }) {
    function clearValidationError() {
      form.setErrorMap({ onSubmit: undefined });
    }

    function applyValues(nextValues: CalendarEventFormValues) {
      form.setFieldValue("allDay", nextValues.allDay);
      form.setFieldValue("startDate", nextValues.startDate);
      form.setFieldValue("startTime", nextValues.startTime);
      form.setFieldValue("endDate", nextValues.endDate);
      form.setFieldValue("endTime", nextValues.endTime);
      clearValidationError();
    }

    return (
      <>
        <form.AppField
          name="allDay"
          children={(field) => (
            <label className="flex items-center gap-3 rounded-lg border p-3">
              <Checkbox
                checked={field.state.value}
                disabled={disabled}
                onCheckedChange={(checked) =>
                  applyValues(valuesWithAllDay(form.state.values, checked))
                }
              />
              <span>
                <span className="block font-medium">All day</span>
                <span className="block text-muted-foreground">
                  Use dates instead of exact times.
                </span>
              </span>
            </label>
          )}
        />

        <Field>
          <FieldLabel>Quick times</FieldLabel>
          <form.Subscribe
            selector={(state) => state.values}
            children={(values) => (
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                {timePresets.map((preset) => {
                  const active = isTimePresetActive(values, preset);
                  return (
                    <Button
                      key={preset.id}
                      aria-pressed={active}
                      className="h-auto flex-col items-start gap-0 py-2"
                      disabled={disabled}
                      onClick={() => applyValues(valuesWithTimePreset(values, preset))}
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
            )}
          />
          <FieldDescription>
            Apply a common shift, then adjust the dates or times if needed.
          </FieldDescription>
        </Field>

        <form.Subscribe
          selector={(state) => state.values.allDay}
          children={(allDay) => (
            <div className="grid gap-4">
              <Field>
                <FieldLabel htmlFor="calendar-entry-start">Starts</FieldLabel>
                <div className={allDay ? undefined : "grid grid-cols-[minmax(0,1fr)_7rem] gap-2"}>
                  <form.AppField
                    name="startDate"
                    children={(field) => (
                      <DatePickerControl
                        id="calendar-entry-start"
                        disabled={disabled}
                        onValueChange={(value) => {
                          field.handleChange(value);
                          clearValidationError();
                        }}
                        value={field.state.value}
                      />
                    )}
                  />
                  {!allDay ? (
                    <form.AppField
                      name="startTime"
                      children={(field) => (
                        <Input
                          aria-label="Start time"
                          disabled={disabled}
                          onChange={(event) => {
                            field.handleChange(event.target.value);
                            clearValidationError();
                          }}
                          step={60}
                          type="time"
                          value={field.state.value}
                        />
                      )}
                    />
                  ) : null}
                </div>
              </Field>

              <Field>
                <FieldLabel htmlFor="calendar-entry-end">
                  {allDay ? "Ends before" : "Ends"}
                </FieldLabel>
                <div className={allDay ? undefined : "grid grid-cols-[minmax(0,1fr)_7rem] gap-2"}>
                  <form.AppField
                    name="endDate"
                    children={(field) => (
                      <DatePickerControl
                        id="calendar-entry-end"
                        disabled={disabled}
                        onValueChange={(value) => {
                          field.handleChange(value);
                          clearValidationError();
                        }}
                        value={field.state.value}
                      />
                    )}
                  />
                  {!allDay ? (
                    <form.AppField
                      name="endTime"
                      children={(field) => (
                        <Input
                          aria-label="End time"
                          disabled={disabled}
                          onChange={(event) => {
                            field.handleChange(event.target.value);
                            clearValidationError();
                          }}
                          step={60}
                          type="time"
                          value={field.state.value}
                        />
                      )}
                    />
                  ) : null}
                </div>
              </Field>
            </div>
          )}
        />

        <Field>
          <FieldLabel htmlFor="calendar-entry-time-zone">Time zone</FieldLabel>
          <form.Subscribe
            selector={(state) =>
              [state.values.timeZone, state.values.startDate, state.values.startTime] as const
            }
            children={([timeZone, startDate, startTime]) => (
              <TimeZoneControl
                defaultTimeZone={defaultTimeZone}
                disabled={disabled}
                onInteraction={clearValidationError}
                onValueChange={(value) => form.setFieldValue("timeZone", value)}
                startDate={startDate}
                startTime={startTime}
                value={timeZone}
              />
            )}
          />
          <FieldDescription>
            Recurring entries keep this local time across daylight-saving changes.
          </FieldDescription>
        </Field>
      </>
    );
  },
});
