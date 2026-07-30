import { useEffect, useState, type FormEvent } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  ScrollArea,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@apesdb/ui";
import { CalendarClock, Loader2, Trash2 } from "lucide-react";
import type {
  CalendarEventInput,
  DeleteCalendarEventInput,
  UpdateCalendarEventInput,
} from "./calendar.api";
import { CalendarEventForm } from "./calendar-event-form";
import { calendarEventFormOptions, useCalendarEventForm } from "./calendar-event-form.context";
import {
  calendarEventFormValuesForTarget,
  calendarEventInputFromValues,
  deleteCalendarEventInput,
  emptyCalendarEventFormValues,
  updateCalendarEventInput,
  type CalendarEditorTarget,
  type EventScope,
} from "./calendar-event-form.model";

export type { CalendarEditorTarget } from "./calendar-event-form.model";

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
  const [scope, setScope] = useState<EventScope>("event");
  const [confirmDiscard, setConfirmDiscard] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [formDefaults, setFormDefaults] = useState(() =>
    emptyCalendarEventFormValues(defaultTimeZone),
  );
  const form = useCalendarEventForm({
    ...calendarEventFormOptions,
    defaultValues: formDefaults,
    onSubmit: async ({ value }) => {
      const result = calendarEventInputFromValues(value);
      if (!result.success || target === null) {
        return;
      }

      try {
        if (target.kind === "create") {
          await onCreate(result.input);
          return;
        }

        await onUpdate(updateCalendarEventInput(result.input, target, scope));
      } catch {
        // The mutation error remains visible in the sheet for correction or retry.
      }
    },
  });

  const isRecurring = target?.kind === "edit" && target.series.recurrence !== null;
  const readOnly = target?.kind === "edit" && target.occurrence.event.readOnly === true;
  const owner = target?.kind === "edit" ? target.owner : null;

  useEffect(() => {
    if (!open || target === null) {
      return;
    }

    let nextScope: EventScope = "event";
    if (target.kind === "edit" && target.series.recurrence !== null) {
      nextScope = "occurrence";
    }

    const nextValues = calendarEventFormValuesForTarget(target, nextScope, defaultTimeZone);
    setScope(nextScope);
    setFormDefaults(nextValues);
    form.reset(nextValues);
    setConfirmDiscard(false);
    setConfirmDelete(false);
  }, [defaultTimeZone, form, open, target]);

  function changeScope(nextScope: EventScope) {
    if (target === null) {
      return;
    }

    const nextValues = calendarEventFormValuesForTarget(target, nextScope, defaultTimeZone);
    setScope(nextScope);
    setFormDefaults(nextValues);
    form.reset(nextValues);
  }

  function requestClose() {
    if (pending || form.state.isSubmitting) {
      return;
    }

    if (form.state.isDirty) {
      setConfirmDiscard(true);
      return;
    }

    onOpenChange(false);
  }

  async function handleDelete() {
    if (target?.kind !== "edit") {
      return;
    }

    try {
      await onDelete(deleteCalendarEventInput(target, scope));
      setConfirmDelete(false);
    } catch {
      setConfirmDelete(false);
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    event.stopPropagation();
    void form.handleSubmit();
  }

  let title = "New calendar entry";
  let description = "Add busy time to your calendar.";
  if (target?.kind === "edit") {
    title = target.occurrence.event.readOnly ? "Calendar entry" : "Edit calendar entry";
    description = target.occurrence.event.readOnly
      ? "This entry belongs to a connected calendar."
      : "Change the time, recurrence, or title.";
  }

  return (
    <>
      <Sheet
        open={open}
        onOpenChange={(nextOpen) => {
          if (nextOpen) {
            onOpenChange(true);
            return;
          }

          requestClose();
        }}
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

            <form.Subscribe
              selector={(state) => state.isSubmitting}
              children={(isSubmitting) => {
                const disabled = pending || isSubmitting || readOnly;
                return (
                  <>
                    <ScrollArea className="min-h-0 flex-1">
                      <CalendarEventForm
                        form={form}
                        defaultTimeZone={defaultTimeZone}
                        disabled={disabled}
                        error={error}
                        isRecurring={isRecurring}
                        onScopeChange={changeScope}
                        owner={owner}
                        readOnly={readOnly}
                        scope={scope}
                      />
                    </ScrollArea>

                    {!readOnly ? (
                      <SheetFooter className="shrink-0 flex-row border-t bg-popover">
                        {target?.kind === "edit" ? (
                          <Button
                            disabled={pending || isSubmitting}
                            onClick={() => setConfirmDelete(true)}
                            type="button"
                            variant="destructive"
                          >
                            <Trash2 data-icon="inline-start" />
                            Delete
                          </Button>
                        ) : null}
                        <div className="flex-1" />
                        <Button
                          disabled={pending || isSubmitting}
                          onClick={requestClose}
                          type="button"
                          variant="outline"
                        >
                          Cancel
                        </Button>
                        <Button disabled={pending || isSubmitting} type="submit">
                          {pending || isSubmitting ? <Loader2 className="animate-spin" /> : null}
                          {pending || isSubmitting ? "Saving…" : "Save"}
                        </Button>
                      </SheetFooter>
                    ) : null}
                  </>
                );
              }}
            />
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
