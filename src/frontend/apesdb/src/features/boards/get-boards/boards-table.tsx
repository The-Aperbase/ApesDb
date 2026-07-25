import type { ReactNode } from "react";
import { Link } from "@tanstack/react-router";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  Card,
  CardContent,
  CardDescription,
  CardTitle,
  PageableDataView,
  Skeleton,
  type DataViewMode,
} from "@apesdb/ui";
import type { ColumnDef } from "@tanstack/react-table";
import { Library } from "lucide-react";
import { gameCountLabel } from "../board-labels";
import { boardsPageSize } from "../boards.api";
import type { BoardSummary, BoardsResponse } from "../boards.schemas";

type BoardsTableProps = {
  data: BoardsResponse | null;
  error: string | null;
  hasFilters: boolean;
  header: ReactNode;
  isLoading: boolean;
  mode: DataViewMode;
  page: number;
  onModeChange: (mode: DataViewMode) => void;
  onPageChange: (page: number) => void;
  onRetry: () => void;
};

function BoardAvatar({ board }: { board: BoardSummary }) {
  return (
    <Avatar className="size-12 rounded-lg">
      <AvatarImage alt={board.name} src={board.pictureUrl ?? undefined} />
      <AvatarFallback className="rounded-lg bg-muted text-muted-foreground">
        <Library className="size-5" />
      </AvatarFallback>
    </Avatar>
  );
}

const columns: ColumnDef<BoardSummary>[] = [
  {
    id: "board",
    header: "Board",
    cell: ({ row }) => (
      <Link
        className="group flex min-w-56 items-center gap-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30"
        params={{ boardId: row.original.id }}
        to="/boards/$boardId"
      >
        <BoardAvatar board={row.original} />
        <span className="font-medium whitespace-normal group-hover:underline group-hover:underline-offset-4">
          {row.original.name}
        </span>
      </Link>
    ),
    meta: {
      skeleton: (
        <div className="flex items-center gap-3">
          <Skeleton className="size-12 rounded-lg" />
          <Skeleton className="h-4 w-44" />
        </div>
      ),
    },
  },
  {
    id: "gameCount",
    accessorKey: "gameCount",
    header: "Games",
    cell: ({ row }) => gameCountLabel(row.original.gameCount),
    meta: {
      skeleton: <Skeleton className="h-4 w-16" />,
    },
  },
];

function BoardGridCard({ board }: { board: BoardSummary }) {
  return (
    <Link
      className="group rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30"
      params={{ boardId: board.id }}
      to="/boards/$boardId"
    >
      <Card className="h-24 py-0 transition-colors group-hover:bg-muted/40">
        <CardContent className="flex h-full items-center gap-3 px-0">
          <Avatar className="h-full w-24 rounded-none">
            <AvatarImage
              alt={board.name}
              className="transition-transform group-hover:scale-[1.02]"
              src={board.pictureUrl ?? undefined}
            />
            <AvatarFallback className="rounded-none bg-muted text-muted-foreground">
              <Library className="size-7" />
            </AvatarFallback>
          </Avatar>
          <div className="grid min-w-0 gap-0.5 pr-4">
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

function BoardGridCardSkeleton() {
  return (
    <Card className="h-24 py-0">
      <CardContent className="flex h-full items-center gap-3 px-0">
        <Skeleton className="h-full w-24 rounded-none" />
        <div className="grid flex-1 gap-2 pr-4">
          <Skeleton className="h-4 w-3/5" />
          <Skeleton className="h-3 w-2/5" />
        </div>
      </CardContent>
    </Card>
  );
}

export function BoardsTable({
  data,
  error,
  hasFilters,
  header,
  isLoading,
  mode,
  page,
  onModeChange,
  onPageChange,
  onRetry,
}: BoardsTableProps) {
  return (
    <PageableDataView
      columns={columns}
      data={data}
      emptyDescription={
        hasFilters
          ? "Try changing or clearing the active filters."
          : "Create a board, then add games to it from the games page."
      }
      emptyTitle={hasFilters ? "No boards found" : "No boards yet"}
      error={error}
      getRowId={(board) => board.id}
      gridClassName="grid-cols-1 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3"
      hasFilters={hasFilters}
      header={header}
      isLoading={isLoading}
      itemLabel="boards"
      mode={mode}
      onModeChange={onModeChange}
      onPageChange={onPageChange}
      onRetry={onRetry}
      renderGridItem={(board) => <BoardGridCard board={board} />}
      renderGridSkeleton={() => <BoardGridCardSkeleton />}
      requestedPage={page}
      requestedPageSize={boardsPageSize}
    />
  );
}
