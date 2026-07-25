import { useState } from "react";
import { Link, useNavigate } from "@tanstack/react-router";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardTitle,
  Item,
  ItemContent,
  ItemDescription,
  ItemTitle,
  Skeleton,
} from "@apesdb/ui";
import { Library, Plus, RefreshCw } from "lucide-react";
import { gameCountLabel } from "../board-labels";
import type { BoardSummary } from "../boards.schemas";
import { CreateBoardDialog } from "../create-board/create-board-dialog";
import { useBoards } from "./use-boards";

function BoardsSkeleton() {
  return (
    <div className="mx-auto grid w-full max-w-5xl gap-6">
      <div className="flex items-start justify-between gap-3">
        <div className="grid gap-2">
          <Skeleton className="h-7 w-24" />
          <Skeleton className="h-4 w-56" />
        </div>
        <Skeleton className="h-7 w-24" />
      </div>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Skeleton className="h-20 w-full rounded-lg" />
        <Skeleton className="h-20 w-full rounded-lg" />
        <Skeleton className="h-20 w-full rounded-lg" />
      </div>
    </div>
  );
}

function BoardCard({ board }: { board: BoardSummary }) {
  return (
    <Link
      className="group rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30"
      params={{ boardId: board.id }}
      to="/boards/$boardId"
    >
      <Card className="h-full transition-colors group-hover:bg-muted/40">
        <CardContent className="flex items-center gap-3">
          <Avatar className="size-12 rounded-lg">
            <AvatarImage alt={board.name} src={board.pictureUrl ?? undefined} />
            <AvatarFallback className="rounded-lg bg-muted text-muted-foreground">
              <Library className="size-5" />
            </AvatarFallback>
          </Avatar>
          <div className="grid min-w-0 gap-0.5">
            <CardTitle className="truncate group-hover:underline group-hover:underline-offset-4">
              {board.name}
            </CardTitle>
            <CardDescription>{gameCountLabel(board.gameCount)}</CardDescription>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}

export function BoardsPage() {
  const boards = useBoards();
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const navigate = useNavigate();

  if (boards.isLoading) {
    return <BoardsSkeleton />;
  }

  if (boards.error !== null) {
    return (
      <div className="mx-auto w-full max-w-5xl">
        <Item className="min-h-60 justify-center text-center" variant="outline">
          <ItemContent className="items-center">
            <ItemTitle>Boards could not be loaded</ItemTitle>
            <ItemDescription>{boards.error}</ItemDescription>
          </ItemContent>
          <Button onClick={boards.retry} type="button" variant="outline">
            <RefreshCw data-icon="inline-start" />
            Retry
          </Button>
        </Item>
      </div>
    );
  }

  if (boards.data === null) {
    return null;
  }

  return (
    <main className="mx-auto grid w-full max-w-5xl gap-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Boards</h1>
          <p className="mt-1 text-sm text-muted-foreground">Organize games by what to play next.</p>
        </div>
        <Button onClick={() => setIsCreateDialogOpen(true)} type="button" variant="outline">
          <Plus data-icon="inline-start" />
          Create board
        </Button>
      </div>
      {boards.data.length === 0 ? (
        <Item className="min-h-60 justify-center text-center" variant="outline">
          <ItemContent className="items-center">
            <ItemTitle>No boards yet</ItemTitle>
            <ItemDescription>
              Create a board, then add games to it from the games page.
            </ItemDescription>
          </ItemContent>
        </Item>
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {boards.data.map((board) => (
            <BoardCard key={board.id} board={board} />
          ))}
        </div>
      )}
      <CreateBoardDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={(boardId) => void navigate({ to: "/boards/$boardId", params: { boardId } })}
      />
    </main>
  );
}
