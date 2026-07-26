import { useEffect, useMemo, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  Input,
  Item,
  ItemActions,
  ItemContent,
  ItemGroup,
  ItemMedia,
  ItemTitle,
  Skeleton,
} from "@apesdb/ui";
import { Gamepad2, LoaderCircle, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import type { Game } from "../../games/list-games/games.schemas";
import type { BoardDetails } from "../boards.schemas";
import { useAddGameToBoard } from "./use-add-game-to-board";
import { boardGamePickerQueryKey, useBoardGamePicker } from "./use-board-game-picker";

type BoardGamePickerDialogProps = {
  board: BoardDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

function GameRowsSkeleton() {
  return (
    <div className="grid gap-2">
      {Array.from({ length: 5 }, (_, index) => (
        <Skeleton className="h-14 w-full rounded-md" key={index} />
      ))}
    </div>
  );
}

function GameCover({ game }: { game: Game }) {
  if (game.coverSmallUrl !== null) {
    return (
      <ItemMedia variant="image" className="h-10 w-8">
        <img alt="" loading="lazy" src={game.coverSmallUrl} />
      </ItemMedia>
    );
  }

  return (
    <ItemMedia className="h-10 w-8 rounded-sm bg-muted text-muted-foreground" variant="icon">
      <Gamepad2 />
      <span className="sr-only">No cover available</span>
    </ItemMedia>
  );
}

export function BoardGamePickerDialog({ board, open, onOpenChange }: BoardGamePickerDialogProps) {
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const loadMoreRef = useRef<HTMLDivElement>(null);
  const queryClient = useQueryClient();
  const existingGameIds = useMemo(
    () => new Set(board.games.map((game) => game.gameId)),
    [board.games],
  );
  const picker = useBoardGamePicker(search, open);
  const addGame = useAddGameToBoard(board.id);

  useEffect(() => {
    if (!open) {
      setSearchDraft("");
      setSearch("");
      queryClient.removeQueries({ queryKey: boardGamePickerQueryKey });
      return;
    }

    const timeout = window.setTimeout(() => setSearch(searchDraft), 300);
    return () => window.clearTimeout(timeout);
  }, [open, queryClient, searchDraft]);

  useEffect(() => {
    const loadMore = loadMoreRef.current;
    if (!open || !loadMore || !picker.hasNextPage || picker.isFetchingNextPage) {
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          void picker.loadNextPage();
        }
      },
      { rootMargin: "160px" },
    );
    observer.observe(loadMore);
    return () => observer.disconnect();
  }, [open, picker.hasNextPage, picker.isFetchingNextPage, picker.loadNextPage]);

  async function handleAdd(game: Game) {
    if (existingGameIds.has(game.id) || addGame.isPending) {
      return;
    }

    try {
      await addGame.mutateAsync(game.id);
      toast.success(`${game.name} added to ${board.name}`);
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : `Could not add ${game.name}. Try again.`,
      );
    }
  }

  let content;
  if (picker.isLoading) {
    content = <GameRowsSkeleton />;
  } else if (picker.isError && picker.games.length === 0) {
    content = (
      <div className="grid justify-items-center gap-2 py-8 text-center">
        <p className="text-sm font-medium">Games could not be loaded</p>
        <p className="text-xs text-muted-foreground">{picker.error}</p>
        <Button onClick={picker.retry} type="button" variant="outline">
          <RefreshCw data-icon="inline-start" />
          Retry
        </Button>
      </div>
    );
  } else if (picker.games.length === 0) {
    content = (
      <div className="grid justify-items-center gap-1 py-8 text-center">
        <p className="text-sm font-medium">No games found</p>
        <p className="text-xs text-muted-foreground">Try a different game name.</p>
      </div>
    );
  } else {
    content = (
      <ItemGroup className="gap-2">
        {picker.games.map((game) => {
          const isAlreadyAdded = existingGameIds.has(game.id);
          const isAdding = addGame.isPending && addGame.variables === game.id;

          return (
            <Item
              aria-label={
                isAlreadyAdded
                  ? `${game.name} is already on ${board.name}`
                  : `Add ${game.name} to ${board.name}`
              }
              className="cursor-pointer text-left hover:bg-muted/40 disabled:cursor-not-allowed disabled:opacity-60"
              disabled={isAlreadyAdded || addGame.isPending}
              key={game.id}
              onClick={() => void handleAdd(game)}
              render={<button type="button" />}
              size="sm"
              variant="outline"
            >
              <GameCover game={game} />
              <ItemContent>
                <ItemTitle>{game.name}</ItemTitle>
              </ItemContent>
              {isAlreadyAdded ? (
                <ItemActions>
                  <Badge variant="secondary">Already added</Badge>
                </ItemActions>
              ) : isAdding ? (
                <ItemActions className="text-muted-foreground">
                  <LoaderCircle className="size-4 animate-spin" />
                  <span className="sr-only">Adding {game.name}</span>
                </ItemActions>
              ) : null}
            </Item>
          );
        })}
        <div aria-hidden="true" className="h-px" ref={loadMoreRef} />
        {picker.isFetchingNextPage ? (
          <div className="flex items-center justify-center gap-2 py-3 text-muted-foreground">
            <LoaderCircle className="size-4 animate-spin" />
            <span>Loading more games…</span>
          </div>
        ) : null}
        {picker.isFetchNextPageError ? (
          <div className="grid justify-items-center gap-2 py-3 text-center">
            <p className="text-xs text-muted-foreground">More games could not be loaded.</p>
            <Button onClick={() => void picker.loadNextPage()} type="button" variant="outline">
              <RefreshCw data-icon="inline-start" />
              Retry
            </Button>
          </div>
        ) : null}
      </ItemGroup>
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] grid-rows-[auto_auto_minmax(0,1fr)] sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Add game</DialogTitle>
          <DialogDescription>
            Search for a game to add to {board.name}. Games already on this board cannot be
            selected.
          </DialogDescription>
        </DialogHeader>
        <Input
          aria-label="Search games"
          autoFocus
          disabled={addGame.isPending}
          onChange={(event) => setSearchDraft(event.target.value)}
          placeholder="Search games…"
          type="search"
          value={searchDraft}
        />
        <div className="min-h-0 max-h-96 overflow-y-auto pr-1">{content}</div>
      </DialogContent>
    </Dialog>
  );
}
