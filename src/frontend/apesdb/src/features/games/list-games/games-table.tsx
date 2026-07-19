import { useMemo, type ReactNode } from "react";
import { Link } from "@tanstack/react-router";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardTitle,
  PageableDataView,
  Skeleton,
  type DataViewMode,
} from "@apesdb/ui";
import type { ColumnDef } from "@tanstack/react-table";
import { Gamepad2, ListPlus } from "lucide-react";

import { gamesPageSize } from "./games.api";
import type { Game, GamesResponse } from "./games.schemas";

type GamesTableProps = {
  data: GamesResponse | null;
  error: string | null;
  hasFilters: boolean;
  header: ReactNode;
  isLoading: boolean;
  mode: DataViewMode;
  page: number;
  onAddToList: (game: Game) => void;
  onModeChange: (mode: DataViewMode) => void;
  onPageChange: (page: number) => void;
  onRetry: () => void;
};

function valueOrDash(value: string | undefined | null) {
  if (value && value.length > 0) {
    return value;
  }

  return <span className="text-muted-foreground">—</span>;
}

function GameCover({ game }: { game: Game }) {
  if (game.coverSmallUrl) {
    return (
      <img
        alt=""
        className="h-12 w-9 shrink-0 bg-muted object-cover"
        loading="lazy"
        src={game.coverSmallUrl}
      />
    );
  }

  return (
    <div className="flex h-12 w-9 shrink-0 items-center justify-center bg-muted text-muted-foreground">
      <Gamepad2 className="size-4" />
      <span className="sr-only">No cover available</span>
    </div>
  );
}

function createColumns(onAddToList: (game: Game) => void): ColumnDef<Game>[] {
  return [
    {
      id: "game",
      header: "Game",
      cell: ({ row }) => (
        <Link
          className="group flex min-w-56 items-center gap-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30"
          params={{ gameId: row.original.id.toString() }}
          search
          to="/games/$gameId"
        >
          <GameCover game={row.original} />
          <span className="font-medium whitespace-normal group-hover:underline group-hover:underline-offset-4">
            {row.original.name}
          </span>
        </Link>
      ),
      meta: {
        skeleton: (
          <div className="flex items-center gap-3">
            <Skeleton className="h-12 w-9" />
            <Skeleton className="h-4 w-44" />
          </div>
        ),
      },
    },
    {
      id: "gameType",
      accessorFn: (game) => game.gameType?.name,
      header: "Type",
      cell: ({ getValue }) => valueOrDash(getValue<string | undefined>()),
      meta: {
        className: "hidden sm:table-cell",
        skeleton: <Skeleton className="h-4 w-24" />,
      },
    },
    {
      id: "developer",
      accessorFn: (game) => game.developers[0],
      header: "Developer",
      cell: ({ getValue }) => valueOrDash(getValue<string | undefined>()),
      meta: {
        className: "hidden lg:table-cell",
        skeleton: <Skeleton className="h-4 w-32" />,
      },
    },
    {
      id: "publisher",
      accessorFn: (game) => game.publishers[0],
      header: "Publisher",
      cell: ({ getValue }) => valueOrDash(getValue<string | undefined>()),
      meta: {
        className: "hidden lg:table-cell",
        skeleton: <Skeleton className="h-4 w-32" />,
      },
    },
    {
      id: "availability",
      header: "Details",
      cell: ({ row }) => (
        <div className="flex gap-1">
          {row.original.isCoop && <Badge variant="secondary">Co-op</Badge>}
          {row.original.isSteam && <Badge variant="outline">Steam</Badge>}
          {!row.original.isCoop && !row.original.isSteam && (
            <span className="text-muted-foreground">—</span>
          )}
        </div>
      ),
      meta: {
        skeleton: <Skeleton className="h-5 w-16" />,
      },
    },
    {
      id: "actions",
      cell: ({ row }) => (
        <Button
          aria-label={`Add ${row.original.name} to a list`}
          onClick={() => onAddToList(row.original)}
          size="icon"
          type="button"
          variant="ghost"
        >
          <ListPlus />
        </Button>
      ),
      meta: {
        skeleton: <Skeleton className="size-7" />,
      },
    },
  ];
}

function GameGridCard({ game, onAddToList }: { game: Game; onAddToList: (game: Game) => void }) {
  const coverUrl = game.coverLargeUrl ?? game.coverSmallUrl;

  return (
    <div className="group relative">
      <Link
        className="block rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30"
        params={{ gameId: game.id.toString() }}
        search
        to="/games/$gameId"
      >
        <Card className="h-full gap-3 py-0 transition-colors group-hover:bg-muted/40">
          <div className="aspect-3/4 overflow-hidden bg-muted">
            {coverUrl ? (
              <img
                alt=""
                className="size-full object-cover transition-transform group-hover:scale-[1.02]"
                loading="lazy"
                src={coverUrl}
              />
            ) : (
              <div className="flex size-full items-center justify-center text-muted-foreground">
                <Gamepad2 className="size-8" />
                <span className="sr-only">No cover available</span>
              </div>
            )}
          </div>
          <CardContent className="grid gap-1 px-3">
            <CardTitle className="line-clamp-2 text-xs leading-snug group-hover:underline group-hover:underline-offset-4">
              {game.name}
            </CardTitle>
            <CardDescription className="truncate">
              {valueOrDash(game.gameType?.name)}
            </CardDescription>
          </CardContent>
          {(game.isCoop || game.isSteam) && (
            <CardFooter className="mt-auto gap-1 px-3 pb-3">
              {game.isCoop && <Badge variant="secondary">Co-op</Badge>}
              {game.isSteam && <Badge variant="outline">Steam</Badge>}
            </CardFooter>
          )}
        </Card>
      </Link>
      <Button
        aria-label={`Add ${game.name} to a list`}
        className="absolute top-2 right-2"
        onClick={() => onAddToList(game)}
        size="icon"
        type="button"
        variant="secondary"
      >
        <ListPlus />
      </Button>
    </div>
  );
}

function GameGridCardSkeleton() {
  return (
    <Card className="gap-3 py-0">
      <Skeleton className="aspect-3/4 w-full rounded-none" />
      <CardContent className="grid gap-2 px-3 pb-3">
        <Skeleton className="h-4 w-4/5" />
        <Skeleton className="h-3 w-2/5" />
      </CardContent>
    </Card>
  );
}

export function GamesTable({
  data,
  error,
  hasFilters,
  header,
  isLoading,
  mode,
  page,
  onAddToList,
  onModeChange,
  onPageChange,
  onRetry,
}: GamesTableProps) {
  const columns = useMemo(() => createColumns(onAddToList), [onAddToList]);

  return (
    <PageableDataView
      columns={columns}
      data={data}
      error={error}
      getRowId={(game) => game.id.toString()}
      hasFilters={hasFilters}
      header={header}
      isLoading={isLoading}
      itemLabel="games"
      mode={mode}
      onModeChange={onModeChange}
      onPageChange={onPageChange}
      onRetry={onRetry}
      renderGridItem={(game) => <GameGridCard game={game} onAddToList={onAddToList} />}
      renderGridSkeleton={() => <GameGridCardSkeleton />}
      requestedPage={page}
      requestedPageSize={gamesPageSize}
    />
  );
}
