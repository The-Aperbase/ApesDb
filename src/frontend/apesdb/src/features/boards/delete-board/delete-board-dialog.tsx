import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FieldError,
} from "@apesdb/ui";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { BoardNotFoundError } from "../boards.api";
import type { BoardDetails } from "../boards.schemas";
import { useDeleteBoard } from "./use-delete-board";

type DeleteBoardDialogProps = {
  board: BoardDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onDeleted: () => void;
};

export function DeleteBoardDialog({
  board,
  open,
  onOpenChange,
  onDeleted,
}: DeleteBoardDialogProps) {
  const deleteBoard = useDeleteBoard();

  function handleOpenChange(nextOpen: boolean) {
    if (deleteBoard.isPending && !nextOpen) {
      return;
    }

    if (!nextOpen) {
      deleteBoard.reset();
    }

    onOpenChange(nextOpen);
  }

  async function handleDelete() {
    try {
      await deleteBoard.mutateAsync(board.id);
      toast.success(`${board.name} deleted`);
      deleteBoard.reset();
      onOpenChange(false);
      onDeleted();
    } catch (error) {
      if (error instanceof BoardNotFoundError) {
        onOpenChange(false);
        onDeleted();
      }
    }
  }

  let requestError: string | null = null;
  if (deleteBoard.error instanceof Error && !(deleteBoard.error instanceof BoardNotFoundError)) {
    requestError = deleteBoard.error.message;
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent showCloseButton={!deleteBoard.isPending}>
        <DialogHeader>
          <DialogTitle>Delete board</DialogTitle>
          <DialogDescription>
            This permanently deletes {board.name} and removes every game from it. This cannot be
            undone.
          </DialogDescription>
        </DialogHeader>

        {requestError !== null ? <FieldError>{requestError}</FieldError> : null}

        <DialogFooter>
          <Button
            disabled={deleteBoard.isPending}
            onClick={() => handleOpenChange(false)}
            type="button"
            variant="outline"
          >
            Cancel
          </Button>
          <Button
            disabled={deleteBoard.isPending}
            onClick={handleDelete}
            type="button"
            variant="outline"
          >
            {deleteBoard.isPending ? <Loader2 className="animate-spin" /> : null}
            {deleteBoard.isPending ? "Deleting…" : "Delete board"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
