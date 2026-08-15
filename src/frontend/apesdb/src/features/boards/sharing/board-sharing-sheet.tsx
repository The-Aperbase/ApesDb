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
import { Loader2, MailPlus, UserMinus, Users } from "lucide-react";
import { toast } from "sonner";
import { notificationQueryKeys } from "../../notifications/notification-query-keys";
import { boardQueryKeys } from "../board-query-keys";
import {
  cancelBoardInvitation,
  fetchBoardSharing,
  inviteToBoard,
  removeBoardCollaborator,
} from "../boards.api";
import type { BoardDetails } from "../boards.schemas";

type BoardSharingSheetProps = {
  board: BoardDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

type RemovalTarget =
  | { kind: "collaborator"; id: string; label: string }
  | { kind: "invitation"; id: string; label: string };

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
}

export function BoardSharingSheet({ board, open, onOpenChange }: BoardSharingSheetProps) {
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("");
  const [emailError, setEmailError] = useState<string | null>(null);
  const [removalTarget, setRemovalTarget] = useState<RemovalTarget | null>(null);
  const sharing = useQuery({
    queryKey: boardQueryKeys.sharing(board.id),
    queryFn: ({ signal }) => fetchBoardSharing(board.id, signal),
    enabled: open,
  });

  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: boardQueryKeys.all }),
      queryClient.invalidateQueries({ queryKey: notificationQueryKeys.list }),
    ]);
  };

  const invite = useMutation({
    mutationFn: (inviteeEmail: string) => inviteToBoard({ boardId: board.id, email: inviteeEmail }),
    onSuccess: async () => {
      setEmail("");
      toast.success("Board invitation sent");
      await refresh();
    },
  });
  const remove = useMutation({
    mutationFn: async (target: RemovalTarget) => {
      if (target.kind === "collaborator") {
        await removeBoardCollaborator({ boardId: board.id, userId: target.id });
        return;
      }

      await cancelBoardInvitation({ boardId: board.id, invitationId: target.id });
    },
    onSuccess: async (_result, target) => {
      setRemovalTarget(null);
      toast.success(
        target.kind === "collaborator" ? "Collaborator removed" : "Invitation cancelled",
      );
      await refresh();
    },
  });

  function handleInvite(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedEmail = email.trim().toLocaleLowerCase();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalizedEmail)) {
      setEmailError("Enter a valid email address.");
      return;
    }

    setEmailError(null);
    invite.mutate(normalizedEmail);
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen && (invite.isPending || remove.isPending)) {
      return;
    }

    if (!nextOpen) {
      setEmail("");
      setEmailError(null);
      setRemovalTarget(null);
    }
    onOpenChange(nextOpen);
  }

  const mutationError =
    invite.error instanceof Error
      ? invite.error.message
      : remove.error instanceof Error
        ? remove.error.message
        : null;

  return (
    <>
      <Sheet open={open} onOpenChange={handleOpenChange}>
        <SheetContent className="w-full max-w-none gap-0 p-0 sm:max-w-lg" side="right">
          <SheetHeader className="shrink-0 border-b pr-14">
            <div className="flex items-center gap-2">
              <Users className="size-4 text-muted-foreground" />
              <SheetTitle>Share {board.name}</SheetTitle>
            </div>
            <SheetDescription>
              Collaborators can add, move, and remove games, but cannot edit or delete the board.
            </SheetDescription>
          </SheetHeader>

          <ScrollArea className="min-h-0 flex-1">
            <div className="grid gap-8 p-6">
              <form className="grid gap-3" onSubmit={handleInvite}>
                <Field data-invalid={emailError !== null}>
                  <FieldLabel htmlFor="board-invite-email">Invite by email</FieldLabel>
                  <div className="flex gap-2">
                    <Input
                      id="board-invite-email"
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
                    Invitations to new users appear when that email first signs in.
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
                      <h3 className="font-medium">Collaborators</h3>
                      <p className="text-sm text-muted-foreground">
                        People who currently have access to this board.
                      </p>
                    </div>
                    {sharing.data.collaborators.length === 0 ? (
                      <p className="rounded-lg border border-dashed p-4 text-sm text-muted-foreground">
                        No collaborators yet.
                      </p>
                    ) : (
                      <div className="grid gap-2">
                        {sharing.data.collaborators.map((collaborator) => (
                          <div
                            key={collaborator.user.id}
                            className="flex items-center gap-3 rounded-lg border p-3"
                          >
                            <Avatar className="size-9">
                              <AvatarImage
                                alt={collaborator.user.name}
                                src={collaborator.user.pictureUrl ?? undefined}
                              />
                              <AvatarFallback>{initials(collaborator.user.name)}</AvatarFallback>
                            </Avatar>
                            <span className="min-w-0 flex-1 truncate font-medium">
                              {collaborator.user.name}
                            </span>
                            <Button
                              aria-label={`Remove ${collaborator.user.name}`}
                              disabled={remove.isPending}
                              onClick={() =>
                                setRemovalTarget({
                                  kind: "collaborator",
                                  id: collaborator.user.id,
                                  label: collaborator.user.name,
                                })
                              }
                              size="icon-sm"
                              type="button"
                              variant="ghost"
                            >
                              <UserMinus />
                            </Button>
                          </div>
                        ))}
                      </div>
                    )}
                  </section>

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
                                setRemovalTarget({
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
        open={removalTarget !== null}
        onOpenChange={(nextOpen) => {
          if (!nextOpen && !remove.isPending) {
            setRemovalTarget(null);
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {removalTarget?.kind === "collaborator"
                ? `Remove ${removalTarget.label}?`
                : "Cancel this invitation?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {removalTarget?.kind === "collaborator"
                ? "They will immediately lose access to this board."
                : `${removalTarget?.label ?? "This person"} will no longer be able to accept it.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={remove.isPending}>Keep</AlertDialogCancel>
            <AlertDialogAction
              disabled={remove.isPending || removalTarget === null}
              variant="destructive"
              onClick={() => {
                if (removalTarget !== null) {
                  remove.mutate(removalTarget);
                }
              }}
            >
              {remove.isPending ? <Loader2 className="animate-spin" /> : null}
              {removalTarget?.kind === "collaborator" ? "Remove access" : "Cancel invitation"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
