import { Link } from "@tanstack/react-router";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  Skeleton,
} from "@apesdb/ui";
import { Library, RefreshCw } from "lucide-react";
import { gameCountLabel } from "../board-labels";
import { useBoards } from "../get-boards/use-boards";
import { useToggleGameBoardMembership } from "./use-toggle-game-board-membership";

type AddToBoardGame = {
  id: number;
  name: string;
};

type AddToBoardDialogProps = {
  game: AddToBoardGame | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

function BoardRowsSkeleton() {
  return (
    <div className="grid gap-2">
      <Skeleton className="h-14 w-full rounded-lg" />
      <Skeleton className="h-14 w-full rounded-lg" />
      <Skeleton className="h-14 w-full rounded-lg" />
    </div>
  );
}

export function AddToBoardDialog({ game, open, onOpenChange }: AddToBoardDialogProps) {
  const boards = useBoards({
    gameId: game?.id,
    enabled: open && game !== null,
  });
  const toggle = useToggleGameBoardMembership(game?.id ?? 0);

  function handleToggle(boardId: string, checked: boolean) {
    toggle.mutate({ boardId, add: checked });
  }

  let content;
  if (boards.isLoading || game === null) {
    content = <BoardRowsSkeleton />;
  } else if (boards.error !== null) {
    content = (
      <div className="grid justify-items-center gap-2 py-4 text-center">
        <p className="text-sm font-medium">Boards could not be loaded</p>
        <p className="text-xs text-muted-foreground">{boards.error}</p>
        <Button className="w-fit" onClick={boards.retry} type="button" variant="outline">
          <RefreshCw data-icon="inline-start" />
          Retry
        </Button>
      </div>
    );
  } else if (boards.data === null || boards.data.length === 0) {
    content = (
      <div className="grid justify-items-center gap-2 py-4 text-center">
        <p className="text-sm font-medium">No boards yet</p>
        <p className="text-xs text-muted-foreground">
          Create a board first, then add games to it from here.
        </p>
        <Button className="w-fit" render={<Link to="/boards" />} type="button" variant="outline">
          <Library data-icon="inline-start" />
          Go to boards
        </Button>
      </div>
    );
  } else {
    content = (
      <div className="grid max-h-80 gap-2 overflow-y-auto">
        {boards.data.map((board) => (
          <label
            key={board.id}
            className="flex cursor-pointer items-center gap-3 rounded-lg border border-border px-3 py-2 transition-colors hover:bg-muted/40"
          >
            <Checkbox
              checked={board.containsGame}
              disabled={toggle.isPending}
              onCheckedChange={(checked) => handleToggle(board.id, checked)}
              aria-label={`Add ${game.name} to ${board.name}`}
            />
            <Avatar className="size-9 rounded-md">
              <AvatarImage alt="" src={board.pictureUrl ?? undefined} />
              <AvatarFallback className="rounded-md bg-muted text-muted-foreground">
                <Library className="size-4" />
              </AvatarFallback>
            </Avatar>
            <span className="min-w-0 flex-1 truncate font-medium">{board.name}</span>
            <span className="shrink-0 text-xs text-muted-foreground">
              {gameCountLabel(board.gameCount)}
            </span>
          </label>
        ))}
      </div>
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Add to boards</DialogTitle>
          <DialogDescription>
            Choose which boards include {game?.name ?? "this game"}.
          </DialogDescription>
        </DialogHeader>
        {content}
      </DialogContent>
    </Dialog>
  );
}
