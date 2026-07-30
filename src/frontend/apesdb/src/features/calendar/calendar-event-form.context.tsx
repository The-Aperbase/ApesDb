import { createFormHook, createFormHookContexts, formOptions } from "@tanstack/react-form";
import {
  emptyCalendarEventFormValues,
  validateCalendarEventForm,
} from "./calendar-event-form.model";

const { fieldContext, formContext } = createFormHookContexts();

export const { useAppForm: useCalendarEventForm, withForm: withCalendarEventForm } = createFormHook(
  {
    fieldComponents: {},
    formComponents: {},
    fieldContext,
    formContext,
  },
);

export const calendarEventFormOptions = formOptions({
  defaultValues: emptyCalendarEventFormValues("Etc/UTC"),
  validators: {
    onSubmit: ({ value }) => validateCalendarEventForm(value),
  },
});
