import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@apesdb/ui";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "../../../auth-context";
import { boardQueryKeys } from "../board-query-keys";
import { removeBoardCollaborator } from "../boards.api";
import type { BoardDetails } from "../boards.schemas";

type LeaveBoardDialogProps = {
  board: BoardDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onLeft: () => void;
};

export function LeaveBoardDialog({ board, open, onOpenChange, onLeft }: LeaveBoardDialogProps) {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const leave = useMutation({
    mutationFn: async () => {
      if (auth.user === null) {
        throw new Error("Your account could not be identified.");
      }

      await removeBoardCollaborator({ boardId: board.id, userId: auth.user.id });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
      toast.success(`Left ${board.name}`);
      onOpenChange(false);
      onLeft();
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Unable to leave the board.");
    },
  });

  return (
    <AlertDialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!leave.isPending) {
          onOpenChange(nextOpen);
        }
      }}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Leave {board.name}?</AlertDialogTitle>
          <AlertDialogDescription>
            You will lose access immediately. The owner can invite you again later.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={leave.isPending}>Stay</AlertDialogCancel>
          <AlertDialogAction
            disabled={leave.isPending}
            variant="destructive"
            onClick={() => leave.mutate()}
          >
            {leave.isPending ? <Loader2 className="animate-spin" /> : null}
            Leave board
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
