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
import {
  boardEntryStates,
  getOrderedBoardGames,
  type BoardDetails,
  type BoardEntryState,
  type BoardGame,
} from "../boards.schemas";
import { useMoveGameOnBoard } from "./use-move-game-on-board";
import { useRemoveGameFromBoard } from "./use-remove-game-from-board";

type BoardKanbanProps = {
  board: BoardDetails;
};

type BoardColumns = Record<BoardEntryState, BoardGame[]>;

const kanbanColumns: { state: BoardEntryState; label: string }[] = [
  { state: "todo", label: "Todo" },
  { state: "in-progress", label: "In progress" },
  { state: "completed", label: "Completed" },
  { state: "dnf", label: "DNF" },
];

function getBoardColumns(board: BoardDetails): BoardColumns {
  return {
    todo: getOrderedBoardGames(board.games.todo),
    "in-progress": getOrderedBoardGames(board.games["in-progress"]),
    completed: getOrderedBoardGames(board.games.completed),
    dnf: getOrderedBoardGames(board.games.dnf),
  };
}

function GameCover({ game }: { game: BoardGame }) {
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
  isSortingDisabled,
  isRemoving,
  onRemove,
}: {
  game: BoardGame;
  isOverlay?: boolean;
  isSortingDisabled: boolean;
  isRemoving: boolean;
  onRemove: (game: BoardGame) => void;
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
        aria-label={`Remove ${game.name} from the board`}
        disabled={isRemoving || isSortingDisabled}
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
    <KanbanItem disabled={isSortingDisabled} value={game.gameId.toString()}>
      <KanbanItemHandle className="rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30">
        {content}
      </KanbanItemHandle>
    </KanbanItem>
  );
}

export function BoardKanban({ board }: BoardKanbanProps) {
  const groupedGames = useMemo(() => getBoardColumns(board), [board]);
  const [columns, setColumns] = useState(groupedGames);
  const moveGame = useMoveGameOnBoard(board.id);
  const removeGame = useRemoveGameFromBoard(board.id);

  useEffect(() => {
    setColumns(groupedGames);
  }, [groupedGames]);

  async function handleRemoveGame(game: BoardGame) {
    try {
      await removeGame.mutateAsync(game.gameId);
      toast.success(`${game.name} removed from the board`);
    } catch {
      toast.error(`Could not remove ${game.name}. Try again.`);
    }
  }

  function handleValueChange(value: Record<string, BoardGame[]>) {
    if (moveGame.isPending) {
      return;
    }

    setColumns({
      todo: value.todo ?? [],
      "in-progress": value["in-progress"] ?? [],
      completed: value.completed ?? [],
      dnf: value.dnf ?? [],
    });
  }

  function handleMove({ activeContainer, overContainer, activeIndex, overIndex }: KanbanMoveEvent) {
    if (moveGame.isPending || removeGame.isPending || activeIndex < 0 || overIndex < 0) {
      return;
    }

    const sourceState = activeContainer as BoardEntryState;
    const nextState = overContainer as BoardEntryState;
    const previous = columns;

    if (sourceState === nextState) {
      const items = [...columns[sourceState]];
      const [movedGame] = items.splice(activeIndex, 1);
      const nextPosition = Math.min(overIndex, items.length);
      if (activeIndex === nextPosition) {
        return;
      }

      items.splice(nextPosition, 0, movedGame);
      setColumns({ ...columns, [sourceState]: items });
      moveGame.mutate(
        { gameId: movedGame.gameId, state: nextState, position: nextPosition },
        { onError: () => setColumns(previous) },
      );
      return;
    }

    const sourceItems = [...columns[sourceState]];
    const [movedGame] = sourceItems.splice(activeIndex, 1);
    const targetItems = [...columns[nextState]];
    targetItems.splice(overIndex, 0, movedGame);
    setColumns({ ...columns, [sourceState]: sourceItems, [nextState]: targetItems });
    moveGame.mutate(
      { gameId: movedGame.gameId, state: nextState, position: overIndex },
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
                    isSortingDisabled={moveGame.isPending || removeGame.isPending}
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

          const game = boardEntryStates
            .flatMap((state) => columns[state])
            .find((entry) => entry.gameId.toString() === value);

          if (!game) {
            return null;
          }

          return (
            <GameCard
              game={game}
              isOverlay
              isSortingDisabled={false}
              isRemoving={false}
              onRemove={() => {}}
            />
          );
        }}
      </KanbanOverlay>
    </Kanban>
  );
}
