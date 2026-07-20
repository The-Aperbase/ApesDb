import { useEffect, useMemo, useState } from "react";
import { Link } from "@tanstack/react-router";
import {
  Badge,
  Button,
  Kanban,
  KanbanBoard,
  KanbanColumn,
  KanbanColumnContent,
  KanbanItem,
  KanbanItemHandle,
  KanbanOverlay,
  type KanbanMoveEvent,
} from "@apesdb/ui";
import { Gamepad2, GripVertical, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { GamesListDetails, GamesListEntryState, GamesListGame } from "../lists.schemas";
import { useRemoveGameFromList } from "./use-remove-game-from-list";
import { useUpdateGameState } from "./use-update-game-state";

type ListKanbanBoardProps = {
  teamId: string;
  list: GamesListDetails;
};

type BoardColumns = Record<GamesListEntryState, GamesListGame[]>;

const kanbanColumns: { state: GamesListEntryState; label: string }[] = [
  { state: "todo", label: "Todo" },
  { state: "in-progress", label: "In progress" },
  { state: "completed", label: "Completed" },
  { state: "dnf", label: "DNF" },
];

function groupGames(games: GamesListGame[]): BoardColumns {
  const grouped: BoardColumns = { todo: [], "in-progress": [], completed: [], dnf: [] };

  for (const game of games) {
    grouped[game.state].push(game);
  }

  return grouped;
}

function GameCover({ game }: { game: GamesListGame }) {
  if (game.coverSmallUrl) {
    return (
      <img
        alt=""
        className="h-12 w-9 shrink-0 rounded-xs bg-muted object-cover"
        loading="lazy"
        src={game.coverSmallUrl}
      />
    );
  }

  return (
    <div className="flex h-12 w-9 shrink-0 items-center justify-center rounded-xs bg-muted text-muted-foreground">
      <Gamepad2 className="size-4" />
      <span className="sr-only">No cover available</span>
    </div>
  );
}

function GameCard({
  game,
  isOverlay = false,
  isRemoving,
  onRemove,
}: {
  game: GamesListGame;
  isOverlay?: boolean;
  isRemoving: boolean;
  onRemove: (game: GamesListGame) => void;
}) {
  const content = (
    <div className="flex items-center gap-2 rounded-lg border border-border bg-card p-2">
      <GripVertical className="size-4 shrink-0 text-muted-foreground" />
      <GameCover game={game} />
      <div className="grid min-w-0 flex-1 gap-0.5">
        <Link
          className="truncate text-xs font-medium hover:underline hover:underline-offset-4"
          params={{ gameId: game.gameId.toString() }}
          to="/games/$gameId"
        >
          {game.name}
        </Link>
        {game.gameType !== null ? (
          <span className="truncate text-xs text-muted-foreground">{game.gameType}</span>
        ) : null}
      </div>
      <Button
        aria-label={`Remove ${game.name} from the list`}
        disabled={isRemoving}
        onClick={() => onRemove(game)}
        size="icon-xs"
        type="button"
        variant="ghost"
      >
        <Trash2 />
      </Button>
    </div>
  );

  if (isOverlay) {
    return <div className="shadow-lg">{content}</div>;
  }

  return (
    <KanbanItem value={game.gameId.toString()}>
      <KanbanItemHandle className="rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30">
        {content}
      </KanbanItemHandle>
    </KanbanItem>
  );
}

export function ListKanbanBoard({ teamId, list }: ListKanbanBoardProps) {
  const groupedGames = useMemo(() => groupGames(list.games), [list.games]);
  const [columns, setColumns] = useState(groupedGames);
  const updateGameState = useUpdateGameState(teamId, list.id);
  const removeGame = useRemoveGameFromList(teamId, list.id);

  useEffect(() => {
    setColumns(groupedGames);
  }, [groupedGames]);

  async function handleRemoveGame(game: GamesListGame) {
    try {
      await removeGame.mutateAsync(game.gameId);
      toast.success(`${game.name} removed from the list`);
    } catch {
      toast.error(`Could not remove ${game.name}. Try again.`);
    }
  }

  function handleValueChange(value: Record<string, GamesListGame[]>) {
    setColumns({
      todo: value.todo ?? [],
      "in-progress": value["in-progress"] ?? [],
      completed: value.completed ?? [],
      dnf: value.dnf ?? [],
    });
  }

  function handleMove({ activeContainer, overContainer, activeIndex, overIndex }: KanbanMoveEvent) {
    if (activeContainer === overContainer) {
      // Within-column ordering is not persisted, so the item snaps back.
      return;
    }

    const sourceState = activeContainer as GamesListEntryState;
    const nextState = overContainer as GamesListEntryState;
    const sourceItems = [...columns[sourceState]];
    const [movedGame] = sourceItems.splice(activeIndex, 1);
    const targetItems = [...columns[nextState]];
    targetItems.splice(overIndex, 0, movedGame);

    const previous = columns;
    setColumns({ ...columns, [sourceState]: sourceItems, [nextState]: targetItems });
    updateGameState.mutate(
      { gameId: movedGame.gameId, state: nextState },
      { onError: () => setColumns(previous) },
    );
  }

  return (
    <Kanban
      className="flex min-h-0 flex-1 flex-col"
      value={columns}
      onValueChange={handleValueChange}
      getItemValue={(game) => game.gameId.toString()}
      onMove={handleMove}
    >
      <KanbanBoard className="min-h-0 flex-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {kanbanColumns.map((column) => {
          const games = columns[column.state];

          return (
            <KanbanColumn className="min-h-0" key={column.state} value={column.state}>
              <div className="mb-2 flex items-center gap-2 px-1">
                <h2 className="text-sm font-medium">{column.label}</h2>
                <Badge variant="secondary">{games.length}</Badge>
              </div>
              <KanbanColumnContent
                value={column.state}
                className="min-h-32 flex-1 rounded-lg border border-dashed border-border p-2"
              >
                {games.map((game) => (
                  <GameCard
                    key={game.gameId}
                    game={game}
                    isRemoving={removeGame.isPending}
                    onRemove={handleRemoveGame}
                  />
                ))}
                {games.length === 0 ? (
                  <p className="py-6 text-center text-xs text-muted-foreground">Drop games here</p>
                ) : null}
              </KanbanColumnContent>
            </KanbanColumn>
          );
        })}
      </KanbanBoard>
      <KanbanOverlay>
        {({ value, variant }) => {
          if (variant === "column") {
            return null;
          }

          const game = list.games.find((entry) => entry.gameId.toString() === value);

          if (!game) {
            return null;
          }

          return <GameCard game={game} isOverlay isRemoving={false} onRemove={() => {}} />;
        }}
      </KanbanOverlay>
    </Kanban>
  );
}
