import { useState } from "react";
import { getRouteApi, Navigate, useNavigate } from "@tanstack/react-router";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  Button,
  Item,
  ItemContent,
  ItemDescription,
  ItemTitle,
  Skeleton,
} from "@apesdb/ui";
import { Library, Pencil, Plus, RefreshCw, Trash2 } from "lucide-react";
import { formatDate } from "../../../lib/date";
import { gameCountLabel } from "../board-labels";
import { getAllBoardGames, type BoardDetails } from "../boards.schemas";
import { DeleteBoardDialog } from "../delete-board/delete-board-dialog";
import { EditBoardDialog } from "../edit-board/edit-board-dialog";
import { BoardGamePickerDialog } from "./board-game-picker-dialog";
import { BoardKanban } from "./board-kanban";
import { useBoardDetails } from "./use-board-details";

const routeApi = getRouteApi("/_app/boards/$boardId");

function BoardDetailsSkeleton() {
  return (
    <div className="flex min-h-full w-full flex-col gap-4">
      <div className="mx-auto w-full max-w-7xl space-y-4">
        <div className="flex items-center gap-4">
          <Skeleton className="size-20 rounded-xl" />
          <div className="grid gap-2">
            <Skeleton className="h-7 w-48" />
            <Skeleton className="h-4 w-32" />
          </div>
        </div>
        <div className="grid gap-3 md:grid-cols-3">
          <Skeleton className="h-64 w-full rounded-lg" />
          <Skeleton className="h-64 w-full rounded-lg" />
          <Skeleton className="h-64 w-full rounded-lg" />
        </div>
      </div>
    </div>
  );
}

function AddGameButton({ onClick }: { onClick: () => void }) {
  return (
    <Button onClick={onClick} type="button">
      <Plus data-icon="inline-start" />
      Add game
    </Button>
  );
}

function BoardHeader({
  board,
  gameCount,
  onAddGame,
  onEdit,
  onDelete,
}: {
  board: BoardDetails;
  gameCount: number;
  onAddGame: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <header className="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-4 gap-y-2 sm:flex sm:flex-wrap sm:gap-4">
      <Avatar className="row-span-2 size-24 rounded-xl sm:row-auto sm:size-20">
        <AvatarImage alt={board.name} src={board.pictureUrl ?? undefined} />
        <AvatarFallback className="rounded-xl bg-muted text-muted-foreground">
          <Library className="size-7" />
        </AvatarFallback>
      </Avatar>
      <div className="grid min-w-0 flex-1 gap-1.5">
        <h1 className="truncate text-2xl font-semibold tracking-tight">{board.name}</h1>
        <p className="text-sm text-muted-foreground">
          {gameCountLabel(gameCount)} · Created {formatDate(board.createdAt)}
        </p>
      </div>
      <div className="col-start-2 flex items-center gap-2 sm:col-auto">
        <AddGameButton onClick={onAddGame} />
        <Button onClick={onEdit} type="button" variant="outline">
          <Pencil data-icon="inline-start" />
          Edit
        </Button>
        <Button onClick={onDelete} type="button" variant="outline">
          <Trash2 data-icon="inline-start" />
          Delete
        </Button>
      </div>
    </header>
  );
}

export function BoardDetailsPage() {
  const { boardId } = routeApi.useParams();
  const boardDetails = useBoardDetails(boardId);
  const [isGamePickerOpen, setIsGamePickerOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const navigate = useNavigate();

  function handleDeleted() {
    void navigate({ to: "/boards" });
  }

  if (boardDetails.isInvalid || boardDetails.isNotFound) {
    return <Navigate to="/boards" replace />;
  }

  if (boardDetails.isLoading) {
    return <BoardDetailsSkeleton />;
  }

  if (boardDetails.error !== null) {
    return (
      <div className="flex min-h-full w-full flex-col gap-4">
        <Item
          className="mx-auto min-h-60 w-full max-w-7xl justify-center text-center"
          variant="outline"
        >
          <ItemContent className="items-center">
            <ItemTitle>Board could not be loaded</ItemTitle>
            <ItemDescription>{boardDetails.error}</ItemDescription>
          </ItemContent>
          <Button onClick={boardDetails.retry} type="button" variant="outline">
            <RefreshCw data-icon="inline-start" />
            Retry
          </Button>
        </Item>
      </div>
    );
  }

  if (boardDetails.data === null) {
    return null;
  }

  const board = boardDetails.data;
  const gameCount = getAllBoardGames(board.games).length;

  return (
    <div className="flex min-h-full w-full flex-col gap-4">
      <div className="mx-auto flex min-h-0 w-full max-w-7xl flex-1 flex-col gap-4">
        <BoardHeader
          board={board}
          gameCount={gameCount}
          onAddGame={() => setIsGamePickerOpen(true)}
          onEdit={() => setIsEditDialogOpen(true)}
          onDelete={() => setIsDeleteDialogOpen(true)}
        />
        {gameCount === 0 ? (
          <Item className="min-h-60 flex-col justify-center text-center" variant="outline">
            <ItemContent className="flex-none items-center">
              <ItemTitle>No games yet</ItemTitle>
              <ItemDescription>Add games directly to this board.</ItemDescription>
            </ItemContent>
            <AddGameButton onClick={() => setIsGamePickerOpen(true)} />
          </Item>
        ) : (
          <BoardKanban board={board} />
        )}
      </div>
      <BoardGamePickerDialog
        board={board}
        open={isGamePickerOpen}
        onOpenChange={setIsGamePickerOpen}
      />
      <EditBoardDialog board={board} open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen} />
      <DeleteBoardDialog
        board={board}
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        onDeleted={handleDeleted}
      />
    </div>
  );
}
