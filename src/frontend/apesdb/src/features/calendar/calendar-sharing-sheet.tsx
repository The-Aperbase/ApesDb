import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
  Input,
  ScrollArea,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  Skeleton,
} from "@apesdb/ui";
import { CalendarDays, Check, Loader2, MailPlus, Unlink, X } from "lucide-react";
import { toast } from "sonner";
import { notificationQueryKeys } from "../notifications/notification-query-keys";
import {
  cancelCalendarInvitation,
  disconnectCalendar,
  fetchCalendarSharing,
  inviteToCalendar,
  respondToCalendarInvitation,
} from "./calendar.api";
import { calendarQueryKeys } from "./calendar-query-keys";

type CalendarSharingSheetProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

type Confirmation =
  | { kind: "connection"; id: string; label: string }
  | { kind: "invitation"; id: string; label: string };

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
}

export function CalendarSharingSheet({ open, onOpenChange }: CalendarSharingSheetProps) {
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("");
  const [emailError, setEmailError] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<Confirmation | null>(null);

  const sharing = useQuery({
    queryKey: calendarQueryKeys.sharing,
    queryFn: ({ signal }) => fetchCalendarSharing(signal),
    enabled: open,
  });

  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: calendarQueryKeys.all }),
      queryClient.invalidateQueries({ queryKey: notificationQueryKeys.list }),
    ]);
  };

  const invite = useMutation({
    mutationFn: inviteToCalendar,
    onSuccess: async () => {
      setEmail("");
      toast.success("Calendar invitation sent");
      await refresh();
    },
  });

  const respond = useMutation({
    mutationFn: ({ id, accept }: { id: string; accept: boolean }) =>
      respondToCalendarInvitation(id, accept),
    onSuccess: async (_result, input) => {
      toast.success(input.accept ? "Calendars connected" : "Invitation declined");
      await refresh();
    },
  });

  const remove = useMutation({
    mutationFn: async (target: Confirmation) => {
      if (target.kind === "connection") {
        await disconnectCalendar(target.id);
        return;
      }

      await cancelCalendarInvitation(target.id);
    },
    onSuccess: async (_result, target) => {
      setConfirmation(null);
      toast.success(
        target.kind === "connection" ? "Calendar disconnected" : "Invitation cancelled",
      );
      await refresh();
    },
  });

  function handleInvite(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalized = email.trim().toLocaleLowerCase();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalized)) {
      setEmailError("Enter a valid email address.");
      return;
    }

    setEmailError(null);
    invite.mutate(normalized);
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen && (invite.isPending || respond.isPending || remove.isPending)) {
      return;
    }

    if (!nextOpen) {
      setEmail("");
      setEmailError(null);
      setConfirmation(null);
    }
    onOpenChange(nextOpen);
  }

  const mutationError =
    invite.error instanceof Error
      ? invite.error.message
      : respond.error instanceof Error
        ? respond.error.message
        : remove.error instanceof Error
          ? remove.error.message
          : null;

  return (
    <>
      <Sheet open={open} onOpenChange={handleOpenChange}>
        <SheetContent className="w-full max-w-none gap-0 p-0 sm:max-w-lg" side="right">
          <SheetHeader className="shrink-0 border-b pr-14">
            <div className="flex items-center gap-2">
              <CalendarDays className="size-4 text-muted-foreground" />
              <SheetTitle>Share calendars</SheetTitle>
            </div>
            <SheetDescription>
              Connect calendars to compare busy time when planning gaming sessions.
            </SheetDescription>
          </SheetHeader>

          <ScrollArea className="min-h-0 flex-1">
            <div className="grid gap-8 p-6">
              <form className="grid gap-3" onSubmit={handleInvite}>
                <Field data-invalid={emailError !== null}>
                  <FieldLabel htmlFor="calendar-invite-email">Invite by email</FieldLabel>
                  <div className="flex gap-2">
                    <Input
                      id="calendar-invite-email"
                      autoComplete="email"
                      disabled={invite.isPending}
                      onChange={(event) => {
                        setEmail(event.target.value);
                        setEmailError(null);
                        invite.reset();
                      }}
                      placeholder="friend@example.com"
                      type="email"
                      value={email}
                    />
                    <Button disabled={invite.isPending} type="submit">
                      {invite.isPending ? (
                        <Loader2 className="animate-spin" />
                      ) : (
                        <MailPlus data-icon="inline-start" />
                      )}
                      Invite
                    </Button>
                  </div>
                  <FieldDescription>
                    Once accepted, you will both see each other’s calendar entries.
                  </FieldDescription>
                  {emailError !== null ? <FieldError>{emailError}</FieldError> : null}
                </Field>
              </form>

              {sharing.isLoading ? (
                <div className="grid gap-3">
                  <Skeleton className="h-4 w-32" />
                  <Skeleton className="h-14 w-full" />
                  <Skeleton className="h-14 w-full" />
                </div>
              ) : null}

              {sharing.error instanceof Error ? (
                <div className="grid justify-items-start gap-2">
                  <FieldError>{sharing.error.message}</FieldError>
                  <Button onClick={() => void sharing.refetch()} size="sm" variant="outline">
                    Try again
                  </Button>
                </div>
              ) : null}

              {sharing.data ? (
                <>
                  <section className="grid gap-3">
                    <div>
                      <h3 className="font-medium">Connected calendars</h3>
                      <p className="text-muted-foreground">
                        Connected people can view titles and times, but cannot edit your entries.
                      </p>
                    </div>
                    {sharing.data.connections.length === 0 ? (
                      <p className="rounded-lg border border-dashed p-4 text-muted-foreground">
                        No calendars connected yet.
                      </p>
                    ) : (
                      <div className="grid gap-2">
                        {sharing.data.connections.map((connection) => (
                          <div
                            key={connection.id}
                            className="flex items-center gap-3 rounded-lg border p-3"
                          >
                            <Avatar className="size-9">
                              <AvatarImage alt="" src={connection.user.pictureUrl ?? undefined} />
                              <AvatarFallback>{initials(connection.user.name)}</AvatarFallback>
                            </Avatar>
                            <span className="min-w-0 flex-1 truncate font-medium">
                              {connection.user.name}
                            </span>
                            <Button
                              aria-label={`Disconnect ${connection.user.name}`}
                              disabled={remove.isPending}
                              onClick={() =>
                                setConfirmation({
                                  kind: "connection",
                                  id: connection.id,
                                  label: connection.user.name,
                                })
                              }
                              size="icon-sm"
                              type="button"
                              variant="ghost"
                            >
                              <Unlink />
                            </Button>
                          </div>
                        ))}
                      </div>
                    )}
                  </section>

                  {sharing.data.incomingInvitations.length > 0 ? (
                    <section className="grid gap-3">
                      <h3 className="font-medium">Invitations for you</h3>
                      <div className="grid gap-2">
                        {sharing.data.incomingInvitations.map((invitation) => (
                          <div
                            key={invitation.id}
                            className="flex items-center gap-3 rounded-lg border p-3"
                          >
                            <Avatar className="size-9">
                              <AvatarImage
                                alt=""
                                src={invitation.invitedBy.pictureUrl ?? undefined}
                              />
                              <AvatarFallback>{initials(invitation.invitedBy.name)}</AvatarFallback>
                            </Avatar>
                            <span className="min-w-0 flex-1 truncate font-medium">
                              {invitation.invitedBy.name}
                            </span>
                            <Button
                              aria-label={`Decline ${invitation.invitedBy.name}`}
                              disabled={respond.isPending}
                              onClick={() => respond.mutate({ id: invitation.id, accept: false })}
                              size="icon-sm"
                              type="button"
                              variant="ghost"
                            >
                              <X />
                            </Button>
                            <Button
                              aria-label={`Accept ${invitation.invitedBy.name}`}
                              disabled={respond.isPending}
                              onClick={() => respond.mutate({ id: invitation.id, accept: true })}
                              size="icon-sm"
                              type="button"
                            >
                              <Check />
                            </Button>
                          </div>
                        ))}
                      </div>
                    </section>
                  ) : null}

                  {sharing.data.outgoingInvitations.length > 0 ? (
                    <section className="grid gap-3">
                      <h3 className="font-medium">Pending invitations</h3>
                      <div className="grid gap-2">
                        {sharing.data.outgoingInvitations.map((invitation) => (
                          <div
                            key={invitation.id}
                            className="flex items-center gap-3 rounded-lg border p-3"
                          >
                            <MailPlus className="size-4 text-muted-foreground" />
                            <span className="min-w-0 flex-1 truncate">{invitation.email}</span>
                            <Button
                              disabled={remove.isPending}
                              onClick={() =>
                                setConfirmation({
                                  kind: "invitation",
                                  id: invitation.id,
                                  label: invitation.email,
                                })
                              }
                              size="xs"
                              type="button"
                              variant="ghost"
                            >
                              Cancel
                            </Button>
                          </div>
                        ))}
                      </div>
                    </section>
                  ) : null}
                </>
              ) : null}

              {mutationError !== null ? <FieldError>{mutationError}</FieldError> : null}
            </div>
          </ScrollArea>
        </SheetContent>
      </Sheet>

      <AlertDialog
        open={confirmation !== null}
        onOpenChange={(nextOpen) => {
          if (!nextOpen && !remove.isPending) {
            setConfirmation(null);
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {confirmation?.kind === "connection"
                ? `Disconnect ${confirmation.label}?`
                : "Cancel this invitation?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirmation?.kind === "connection"
                ? "You will both stop seeing each other’s calendar entries."
                : `${confirmation?.label ?? "This person"} will no longer be able to accept it.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={remove.isPending}>Keep</AlertDialogCancel>
            <AlertDialogAction
              disabled={remove.isPending || confirmation === null}
              variant="destructive"
              onClick={() => {
                if (confirmation !== null) {
                  remove.mutate(confirmation);
                }
              }}
            >
              {remove.isPending ? <Loader2 className="animate-spin" /> : null}
              {confirmation?.kind === "connection" ? "Disconnect" : "Cancel invitation"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
