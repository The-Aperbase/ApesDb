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
import { gameCountLabel } from "../list-labels";
import { useGamesLists } from "../list-lists/use-lists";
import { useToggleGameListMembership } from "./use-toggle-game-list-membership";

type AddToListGame = {
  id: number;
  name: string;
};

type AddToListDialogProps = {
  teamId: string;
  game: AddToListGame | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

function ListRowsSkeleton() {
  return (
    <div className="grid gap-2">
      <Skeleton className="h-14 w-full rounded-lg" />
      <Skeleton className="h-14 w-full rounded-lg" />
      <Skeleton className="h-14 w-full rounded-lg" />
    </div>
  );
}

export function AddToListDialog({ teamId, game, open, onOpenChange }: AddToListDialogProps) {
  const lists = useGamesLists(teamId, {
    gameId: game?.id,
    enabled: open && game !== null,
  });
  const toggle = useToggleGameListMembership(teamId, game?.id ?? 0);

  function handleToggle(listId: string, checked: boolean) {
    toggle.mutate({ listId, add: checked });
  }

  let content;
  if (lists.isLoading || game === null) {
    content = <ListRowsSkeleton />;
  } else if (lists.error !== null) {
    content = (
      <div className="grid justify-items-center gap-2 py-4 text-center">
        <p className="text-sm font-medium">Lists could not be loaded</p>
        <p className="text-xs text-muted-foreground">{lists.error}</p>
        <Button className="w-fit" onClick={lists.retry} type="button" variant="outline">
          <RefreshCw data-icon="inline-start" />
          Retry
        </Button>
      </div>
    );
  } else if (lists.data === null || lists.data.length === 0) {
    content = (
      <div className="grid justify-items-center gap-2 py-4 text-center">
        <p className="text-sm font-medium">This team has no lists yet</p>
        <p className="text-xs text-muted-foreground">
          Create a list first, then add games to it from here.
        </p>
        <Button
          className="w-fit"
          render={<Link params={{ teamId }} to="/teams/$teamId/lists" />}
          type="button"
          variant="outline"
        >
          <Library data-icon="inline-start" />
          Go to lists
        </Button>
      </div>
    );
  } else {
    content = (
      <div className="grid max-h-80 gap-2 overflow-y-auto">
        {lists.data.map((list) => (
          <label
            key={list.id}
            className="flex cursor-pointer items-center gap-3 rounded-lg border border-border px-3 py-2 transition-colors hover:bg-muted/40"
          >
            <Checkbox
              checked={list.containsGame}
              disabled={toggle.isPending}
              onCheckedChange={(checked) => handleToggle(list.id, checked)}
              aria-label={`Add ${game.name} to ${list.name}`}
            />
            <Avatar className="size-9 rounded-md">
              <AvatarImage alt="" src={list.pictureUrl ?? undefined} />
              <AvatarFallback className="rounded-md bg-muted text-muted-foreground">
                <Library className="size-4" />
              </AvatarFallback>
            </Avatar>
            <span className="min-w-0 flex-1 truncate font-medium">{list.name}</span>
            <span className="shrink-0 text-xs text-muted-foreground">
              {gameCountLabel(list.gameCount)}
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
          <DialogTitle>Add to lists</DialogTitle>
          <DialogDescription>
            Choose which of this team&apos;s lists include {game?.name ?? "this game"}.
          </DialogDescription>
        </DialogHeader>
        {content}
      </DialogContent>
    </Dialog>
  );
}
