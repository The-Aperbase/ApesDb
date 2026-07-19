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
import { ListNotFoundError } from "../lists.api";
import type { GamesListDetails } from "../lists.schemas";
import { useDeleteGamesList } from "./use-delete-list";

type DeleteListDialogProps = {
  teamId: string;
  list: GamesListDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onDeleted: () => void;
};

export function DeleteListDialog({
  teamId,
  list,
  open,
  onOpenChange,
  onDeleted,
}: DeleteListDialogProps) {
  const deleteList = useDeleteGamesList(teamId);

  function handleOpenChange(nextOpen: boolean) {
    if (deleteList.isPending && !nextOpen) {
      return;
    }

    if (!nextOpen) {
      deleteList.reset();
    }

    onOpenChange(nextOpen);
  }

  async function handleDelete() {
    try {
      await deleteList.mutateAsync(list.id);
      toast.success(`${list.name} deleted`);
      deleteList.reset();
      onOpenChange(false);
      onDeleted();
    } catch (error) {
      if (error instanceof ListNotFoundError) {
        onOpenChange(false);
        onDeleted();
      }
    }
  }

  let requestError: string | null = null;
  if (deleteList.error instanceof Error && !(deleteList.error instanceof ListNotFoundError)) {
    requestError = deleteList.error.message;
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent showCloseButton={!deleteList.isPending}>
        <DialogHeader>
          <DialogTitle>Delete list</DialogTitle>
          <DialogDescription>
            This permanently deletes {list.name} and removes every game from it. This cannot be
            undone.
          </DialogDescription>
        </DialogHeader>

        {requestError !== null ? <FieldError>{requestError}</FieldError> : null}

        <DialogFooter>
          <Button
            disabled={deleteList.isPending}
            onClick={() => handleOpenChange(false)}
            type="button"
            variant="outline"
          >
            Cancel
          </Button>
          <Button
            disabled={deleteList.isPending}
            onClick={handleDelete}
            type="button"
            variant="outline"
          >
            {deleteList.isPending ? <Loader2 className="animate-spin" /> : null}
            {deleteList.isPending ? "Deleting…" : "Delete list"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
