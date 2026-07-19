import { useState } from "react";
import { Button, Item, ItemContent, ItemDescription, ItemTitle, Skeleton } from "@apesdb/ui";
import { getRouteApi } from "@tanstack/react-router";
import { RefreshCw } from "lucide-react";
import { AddToListDialog } from "../../lists/add-game-to-list/add-to-list-dialog";
import { GameDetailsHeader } from "./game-details-header";
import { GameDetailsSections } from "./game-details-sections";
import { useGameDetails } from "./use-game-details";

const routeApi = getRouteApi("/_app/games/$gameId");

function GameDetailsSkeleton() {
  return (
    <div className="grid gap-6 md:grid-cols-[14rem_minmax(0,1fr)]">
      <Skeleton className="aspect-3/4 w-full max-w-56 md:max-w-none" />
      <div className="space-y-5">
        <Skeleton className="h-5 w-32" />
        <Skeleton className="h-10 w-3/4" />
        <Skeleton className="h-4 w-40" />
        <div className="flex gap-6">
          <Skeleton className="h-12 w-28" />
          <Skeleton className="h-12 w-36" />
        </div>
        <Skeleton className="h-20 w-full" />
      </div>
    </div>
  );
}

function UnavailableGame({ invalid }: { invalid: boolean }) {
  return (
    <Item variant="outline" className="min-h-60 justify-center text-center">
      <ItemContent className="items-center">
        <ItemTitle>{invalid ? "Invalid game ID" : "Game not found"}</ItemTitle>
        <ItemDescription>
          {invalid
            ? "The game ID in this address is not valid."
            : "The requested game is not available in the synchronized catalog."}
        </ItemDescription>
      </ItemContent>
    </Item>
  );
}

export function GameDetailsPage() {
  const params = routeApi.useParams();
  const gameDetails = useGameDetails(Number(params.gameId));
  const [addToListOpen, setAddToListOpen] = useState(false);

  let content;
  if (gameDetails.isInvalid || gameDetails.isNotFound) {
    content = <UnavailableGame invalid={gameDetails.isInvalid} />;
  } else if (gameDetails.isLoading) {
    content = <GameDetailsSkeleton />;
  } else if (gameDetails.error) {
    content = (
      <Item variant="outline" className="min-h-60 justify-center text-center">
        <ItemContent className="items-center">
          <ItemTitle>Game details could not be loaded</ItemTitle>
          <ItemDescription>{gameDetails.error}</ItemDescription>
        </ItemContent>
        <Button type="button" variant="outline" onClick={gameDetails.retry}>
          <RefreshCw />
          Retry
        </Button>
      </Item>
    );
  } else if (gameDetails.data) {
    content = (
      <div className="space-y-6">
        <GameDetailsHeader game={gameDetails.data} onAddToList={() => setAddToListOpen(true)} />
        <GameDetailsSections game={gameDetails.data} />
      </div>
    );
  } else {
    content = null;
  }

  return (
    <div className="mx-auto w-full max-w-7xl">
      {content}
      {gameDetails.data !== null ? (
        <AddToListDialog
          game={{ id: gameDetails.data.id, name: gameDetails.data.name }}
          open={addToListOpen}
          onOpenChange={setAddToListOpen}
        />
      ) : null}
    </div>
  );
}
