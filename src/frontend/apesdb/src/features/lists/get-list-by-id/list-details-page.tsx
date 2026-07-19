import { useEffect, useState } from "react";
import { Link, Navigate, useNavigate } from "@tanstack/react-router";
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
import { ArrowLeft, Gamepad2, Library, Pencil, RefreshCw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { formatDate } from "../../../lib/date";
import { useActiveTeam } from "../../teams/team-context";
import { DeleteListDialog } from "../delete-list/delete-list-dialog";
import { EditListDialog } from "../edit-list/edit-list-dialog";
import { gameCountLabel } from "../list-labels";
import type { GamesListDetails, GamesListGame } from "../lists.schemas";
import { useGamesListDetails } from "./use-list-details";
import { useRemoveGameFromList } from "./use-remove-game-from-list";

type ListDetailsPageProps = {
  teamId: string;
  listId: string;
};

function BackToListsButton({ teamId }: { teamId: string }) {
  return (
    <Button render={<Link params={{ teamId }} to="/teams/$teamId/lists" />} variant="ghost">
      <ArrowLeft />
      Back to lists
    </Button>
  );
}

function ListDetailsSkeleton({ teamId }: { teamId: string }) {
  return (
    <div className="mx-auto w-full max-w-5xl space-y-4">
      <BackToListsButton teamId={teamId} />
      <div className="flex items-center gap-4">
        <Skeleton className="size-20 rounded-xl" />
        <div className="grid gap-2">
          <Skeleton className="h-7 w-48" />
          <Skeleton className="h-4 w-32" />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5">
        <Skeleton className="aspect-3/4 w-full rounded-lg" />
        <Skeleton className="aspect-3/4 w-full rounded-lg" />
        <Skeleton className="aspect-3/4 w-full rounded-lg" />
        <Skeleton className="aspect-3/4 w-full rounded-lg" />
        <Skeleton className="aspect-3/4 w-full rounded-lg" />
      </div>
    </div>
  );
}

function ListGameCard({
  game,
  onRemove,
  isRemoving,
}: {
  game: GamesListGame;
  onRemove: (game: GamesListGame) => void;
  isRemoving: boolean;
}) {
  const coverUrl = game.coverLargeUrl ?? game.coverSmallUrl;

  return (
    <div className="group relative">
      <Link
        className="block rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30"
        params={{ gameId: game.gameId.toString() }}
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
          <CardContent className="grid gap-1 px-3 pb-3">
            <CardTitle className="line-clamp-2 text-xs leading-snug group-hover:underline group-hover:underline-offset-4">
              {game.name}
            </CardTitle>
            {game.gameType !== null ? (
              <CardDescription className="truncate">{game.gameType}</CardDescription>
            ) : null}
          </CardContent>
        </Card>
      </Link>
      <Button
        aria-label={`Remove ${game.name} from the list`}
        className="absolute top-2 right-2"
        disabled={isRemoving}
        onClick={() => onRemove(game)}
        size="icon-sm"
        type="button"
        variant="secondary"
      >
        <Trash2 />
      </Button>
    </div>
  );
}

function ListHeader({
  list,
  onEdit,
  onDelete,
}: {
  list: GamesListDetails;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <header className="flex flex-wrap items-center gap-4">
      <Avatar className="size-20 rounded-xl">
        <AvatarImage alt={list.name} src={list.pictureUrl ?? undefined} />
        <AvatarFallback className="rounded-xl bg-muted text-muted-foreground">
          <Library className="size-7" />
        </AvatarFallback>
      </Avatar>
      <div className="grid min-w-0 flex-1 gap-1.5">
        <h1 className="truncate text-2xl font-semibold tracking-tight">{list.name}</h1>
        <p className="text-sm text-muted-foreground">
          {gameCountLabel(list.games.length)} · Created {formatDate(list.createdAt)}
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

export function ListDetailsPage({ teamId, listId }: ListDetailsPageProps) {
  const listDetails = useGamesListDetails(teamId, listId);
  const removeGame = useRemoveGameFromList(teamId, listId);
  const { setActiveTeamId } = useActiveTeam();
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (listDetails.data !== null) {
      setActiveTeamId(teamId);
    }
  }, [listDetails.data, setActiveTeamId, teamId]);

  async function handleRemoveGame(game: GamesListGame) {
    try {
      await removeGame.mutateAsync(game.gameId);
      toast.success(`${game.name} removed from the list`);
    } catch {
      toast.error(`Could not remove ${game.name}. Try again.`);
    }
  }

  function handleDeleted() {
    void navigate({ to: "/teams/$teamId/lists", params: { teamId } });
  }

  if (listDetails.isInvalid || listDetails.isNotFound) {
    return <Navigate to="/" replace />;
  }

  if (listDetails.isLoading) {
    return <ListDetailsSkeleton teamId={teamId} />;
  }

  if (listDetails.error !== null) {
    return (
      <div className="mx-auto w-full max-w-5xl space-y-4">
        <BackToListsButton teamId={teamId} />
        <Item className="min-h-60 justify-center text-center" variant="outline">
          <ItemContent className="items-center">
            <ItemTitle>List could not be loaded</ItemTitle>
            <ItemDescription>{listDetails.error}</ItemDescription>
          </ItemContent>
          <Button onClick={listDetails.retry} type="button" variant="outline">
            <RefreshCw data-icon="inline-start" />
            Retry
          </Button>
        </Item>
      </div>
    );
  }

  if (listDetails.data === null) {
    return null;
  }

  const list = listDetails.data;

  return (
    <div className="mx-auto w-full max-w-5xl space-y-4">
      <BackToListsButton teamId={teamId} />
      <ListHeader
        list={list}
        onEdit={() => setIsEditDialogOpen(true)}
        onDelete={() => setIsDeleteDialogOpen(true)}
      />
      {list.games.length === 0 ? (
        <Item className="min-h-60 justify-center text-center" variant="outline">
          <ItemContent className="items-center">
            <ItemTitle>No games yet</ItemTitle>
            <ItemDescription>Add games to this list from the games page.</ItemDescription>
          </ItemContent>
          <Button render={<Link to="/games" />} type="button" variant="outline">
            <Gamepad2 data-icon="inline-start" />
            Browse games
          </Button>
        </Item>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5">
          {list.games.map((game) => (
            <ListGameCard
              key={game.gameId}
              game={game}
              isRemoving={removeGame.isPending}
              onRemove={handleRemoveGame}
            />
          ))}
        </div>
      )}
      <EditListDialog
        teamId={teamId}
        list={list}
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
      />
      <DeleteListDialog
        teamId={teamId}
        list={list}
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        onDeleted={handleDeleted}
      />
    </div>
  );
}
