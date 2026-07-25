import { useState } from "react";
import { getRouteApi, Link, Navigate, useNavigate } from "@tanstack/react-router";
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
import { Gamepad2, Library, Pencil, RefreshCw, Trash2 } from "lucide-react";
import { formatDate } from "../../../lib/date";
import { gameCountLabel } from "../board-labels";
import type { BoardDetails } from "../boards.schemas";
import { DeleteBoardDialog } from "../delete-board/delete-board-dialog";
import { EditBoardDialog } from "../edit-board/edit-board-dialog";
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

function BoardHeader({
  board,
  onEdit,
  onDelete,
}: {
  board: BoardDetails;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <header className="flex flex-wrap items-center gap-4">
      <Avatar className="size-20 rounded-xl">
        <AvatarImage alt={board.name} src={board.pictureUrl ?? undefined} />
        <AvatarFallback className="rounded-xl bg-muted text-muted-foreground">
          <Library className="size-7" />
        </AvatarFallback>
      </Avatar>
      <div className="grid min-w-0 flex-1 gap-1.5">
        <h1 className="truncate text-2xl font-semibold tracking-tight">{board.name}</h1>
        <p className="text-sm text-muted-foreground">
          {gameCountLabel(board.games.length)} · Created {formatDate(board.createdAt)}
        </p>
      </div>
      <div className="flex items-center gap-2">
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

  return (
    <div className="flex min-h-full w-full flex-col gap-4">
      <div className="mx-auto flex min-h-0 w-full max-w-7xl flex-1 flex-col gap-4">
        <BoardHeader
          board={board}
          onEdit={() => setIsEditDialogOpen(true)}
          onDelete={() => setIsDeleteDialogOpen(true)}
        />
        {board.games.length === 0 ? (
          <Item className="min-h-60 justify-center text-center" variant="outline">
            <ItemContent className="items-center">
              <ItemTitle>No games yet</ItemTitle>
              <ItemDescription>Add games to this board from the games page.</ItemDescription>
            </ItemContent>
            <Button render={<Link to="/games" />} type="button" variant="outline">
              <Gamepad2 data-icon="inline-start" />
              Browse games
            </Button>
          </Item>
        ) : (
          <BoardKanban board={board} />
        )}
      </div>
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
