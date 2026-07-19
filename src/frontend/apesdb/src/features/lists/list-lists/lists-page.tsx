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
import { Library, Plus, RefreshCw } from "lucide-react";
import { useActiveTeam } from "../../teams/team-context";
import { gameCountLabel } from "../list-labels";
import type { GamesListSummary } from "../lists.schemas";
import { CreateListDialog } from "../create-list/create-list-dialog";
import { useGamesLists } from "./use-lists";

type ListsPageProps = {
  teamId: string;
};

function ListsSkeleton() {
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

function ListCard({ teamId, list }: { teamId: string; list: GamesListSummary }) {
  return (
    <Link
      className="group rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30"
      params={{ teamId, listId: list.id }}
      to="/teams/$teamId/lists/$listId"
    >
      <Card className="h-full transition-colors group-hover:bg-muted/40">
        <CardContent className="flex items-center gap-3">
          <Avatar className="size-12 rounded-lg">
            <AvatarImage alt={list.name} src={list.pictureUrl ?? undefined} />
            <AvatarFallback className="rounded-lg bg-muted text-muted-foreground">
              <Library className="size-5" />
            </AvatarFallback>
          </Avatar>
          <div className="grid min-w-0 gap-0.5">
            <CardTitle className="truncate group-hover:underline group-hover:underline-offset-4">
              {list.name}
            </CardTitle>
            <CardDescription>{gameCountLabel(list.gameCount)}</CardDescription>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}

export function ListsPage({ teamId }: ListsPageProps) {
  const lists = useGamesLists(teamId);
  const { setActiveTeamId } = useActiveTeam();
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (lists.data !== null) {
      setActiveTeamId(teamId);
    }
  }, [lists.data, setActiveTeamId, teamId]);

  if (lists.isInvalid) {
    return <Navigate to="/" replace />;
  }

  if (lists.isLoading) {
    return <ListsSkeleton />;
  }

  if (lists.error !== null) {
    return (
      <div className="mx-auto w-full max-w-5xl">
        <Item className="min-h-60 justify-center text-center" variant="outline">
          <ItemContent className="items-center">
            <ItemTitle>Lists could not be loaded</ItemTitle>
            <ItemDescription>{lists.error}</ItemDescription>
          </ItemContent>
          <Button onClick={lists.retry} type="button" variant="outline">
            <RefreshCw data-icon="inline-start" />
            Retry
          </Button>
        </Item>
      </div>
    );
  }

  if (lists.data === null) {
    return null;
  }

  return (
    <main className="mx-auto grid w-full max-w-5xl gap-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Lists</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Games lists shared by every member of this team.
          </p>
        </div>
        <Button onClick={() => setIsCreateDialogOpen(true)} type="button" variant="outline">
          <Plus data-icon="inline-start" />
          Create list
        </Button>
      </div>
      {lists.data.length === 0 ? (
        <Item className="min-h-60 justify-center text-center" variant="outline">
          <ItemContent className="items-center">
            <ItemTitle>No lists yet</ItemTitle>
            <ItemDescription>
              Create a list, then add games to it from the games page.
            </ItemDescription>
          </ItemContent>
        </Item>
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {lists.data.map((list) => (
            <ListCard key={list.id} teamId={teamId} list={list} />
          ))}
        </div>
      )}
      <CreateListDialog
        teamId={teamId}
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={(listId) =>
          void navigate({ to: "/teams/$teamId/lists/$listId", params: { teamId, listId } })
        }
      />
    </main>
  );
}
