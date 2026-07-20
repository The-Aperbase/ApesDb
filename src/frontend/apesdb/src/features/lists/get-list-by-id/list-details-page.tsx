import { useEffect, useState } from "react";
import { Link, Navigate, useNavigate } from "@tanstack/react-router";
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
import { ArrowLeft, Gamepad2, Library, Pencil, RefreshCw, Trash2 } from "lucide-react";
import { formatDate } from "../../../lib/date";
import { useActiveTeam } from "../../teams/team-context";
import { DeleteListDialog } from "../delete-list/delete-list-dialog";
import { EditListDialog } from "../edit-list/edit-list-dialog";
import { gameCountLabel } from "../list-labels";
import type { GamesListDetails } from "../lists.schemas";
import { ListKanbanBoard } from "./list-kanban-board";
import { useGamesListDetails } from "./use-list-details";

type ListDetailsPageProps = {
  teamId: string;
  listId: string;
};

function BackToListsButton({ teamId }: { teamId: string }) {
  return (
    <Button
      className="self-start"
      render={<Link params={{ teamId }} to="/teams/$teamId/lists" />}
      variant="ghost"
    >
      <ArrowLeft />
      Back to lists
    </Button>
  );
}

function ListDetailsSkeleton({ teamId }: { teamId: string }) {
  return (
    <div className="flex min-h-full w-full flex-col gap-4">
      <BackToListsButton teamId={teamId} />
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
  const { setActiveTeamId } = useActiveTeam();
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (listDetails.data !== null) {
      setActiveTeamId(teamId);
    }
  }, [listDetails.data, setActiveTeamId, teamId]);

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
      <div className="flex min-h-full w-full flex-col gap-4">
        <BackToListsButton teamId={teamId} />
        <Item
          className="mx-auto min-h-60 w-full max-w-7xl justify-center text-center"
          variant="outline"
        >
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
    <div className="flex min-h-full w-full flex-col gap-4">
      <BackToListsButton teamId={teamId} />
      <div className="mx-auto flex min-h-0 w-full max-w-7xl flex-1 flex-col gap-4">
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
          <ListKanbanBoard teamId={teamId} list={list} />
        )}
      </div>
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
