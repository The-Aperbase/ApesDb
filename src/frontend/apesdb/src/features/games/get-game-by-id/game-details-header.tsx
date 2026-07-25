import { useState } from "react";
import { Link } from "@tanstack/react-router";
import { Badge, Button, Tooltip, TooltipContent, TooltipTrigger } from "@apesdb/ui";
import { Copy, Gamepad2, ListPlus, Star } from "lucide-react";
import { toast } from "sonner";
import { formatDate } from "../../../lib/date";
import { GameStoreLinks } from "./game-store-links";
import type { GameDetails } from "./game-details.schemas";

type GameDetailsHeaderProps = {
  game: GameDetails;
  onAddToBoard: () => void;
};

function GameCover({ game }: { game: GameDetails }) {
  const coverUrl = game.cover?.largeUrl ?? game.cover?.smallUrl ?? null;
  const [imageAvailable, setImageAvailable] = useState(coverUrl !== null);

  if (coverUrl && imageAvailable) {
    return (
      <img
        alt={`${game.name} cover`}
        className="aspect-3/4 w-full bg-muted object-cover"
        src={coverUrl}
        onError={() => setImageAvailable(false)}
      />
    );
  }

  return (
    <div className="flex aspect-3/4 w-full flex-col items-center justify-center gap-2 bg-muted text-muted-foreground">
      <Gamepad2 className="size-10" />
      <span className="text-xs">No cover available</span>
    </div>
  );
}

function CompanySummary({ game }: { game: GameDetails }) {
  if (game.developers.length === 0 && game.publishers.length === 0) {
    return null;
  }

  return (
    <dl className="grid gap-2 text-sm sm:grid-cols-2">
      {game.developers.length > 0 && (
        <div>
          <dt className="text-xs uppercase tracking-wide text-muted-foreground">Developed by</dt>
          <dd className="mt-1">{game.developers.map((company) => company.name).join(", ")}</dd>
        </div>
      )}
      {game.publishers.length > 0 && (
        <div>
          <dt className="text-xs uppercase tracking-wide text-muted-foreground">Published by</dt>
          <dd className="mt-1">{game.publishers.map((company) => company.name).join(", ")}</dd>
        </div>
      )}
    </dl>
  );
}

async function copyGameUrl() {
  try {
    await navigator.clipboard.writeText(window.location.href);
    toast.success("Game link copied");
  } catch {
    toast.error("Game link could not be copied");
  }
}

export function GameDetailsHeader({ game, onAddToBoard }: GameDetailsHeaderProps) {
  return (
    <section className="grid gap-6 md:grid-cols-[14rem_minmax(0,1fr)]">
      <div className="w-full max-w-56 md:max-w-none">
        <GameCover key={game.cover?.largeUrl ?? game.cover?.smallUrl ?? "missing"} game={game} />
      </div>
      <div className="flex min-w-0 flex-col gap-5">
        <div>
          <div className="mb-3 flex flex-wrap gap-2">
            {game.gameType && <Badge variant="secondary">{game.gameType.name}</Badge>}
            {game.gameStatus && <Badge variant="outline">{game.gameStatus.name}</Badge>}
          </div>
          <div className="flex items-start gap-2">
            <h1 className="min-w-0 text-3xl font-semibold tracking-tight sm:text-4xl">
              {game.name}
            </h1>
            <Tooltip>
              <TooltipTrigger
                render={
                  <Button
                    aria-label="Copy game link"
                    className="mt-1"
                    size="icon-lg"
                    type="button"
                    variant="ghost"
                    onClick={() => void copyGameUrl()}
                  />
                }
              >
                <Copy aria-hidden="true" />
              </TooltipTrigger>
              <TooltipContent>Copy link</TooltipContent>
            </Tooltip>
          </div>
          {game.releaseDate && (
            <p className="mt-2 text-sm text-muted-foreground">
              Released {formatDate(game.releaseDate)}
            </p>
          )}
        </div>

        {(game.totalRating !== null || game.popularity) && (
          <div className="flex flex-wrap gap-x-8 gap-y-3">
            {game.totalRating !== null && (
              <div className="flex items-center gap-2">
                <Star className="size-5" aria-hidden="true" />
                <div>
                  <p className="font-medium">{game.totalRating.toFixed(1)} / 100</p>
                  {game.totalRatingCount !== null && (
                    <p className="text-xs text-muted-foreground">
                      {game.totalRatingCount.toLocaleString()} ratings
                    </p>
                  )}
                </div>
              </div>
            )}
            {game.popularity && (
              <div>
                <p className="font-medium">Popularity rank #{game.popularity.rank}</p>
                <p className="text-xs text-muted-foreground">{game.popularity.type.name}</p>
              </div>
            )}
          </div>
        )}

        <CompanySummary game={game} />

        {game.versionParentId !== null && (
          <p className="text-sm text-muted-foreground">
            Version of{" "}
            <Link
              className="font-medium text-foreground underline underline-offset-4"
              params={{ gameId: game.versionParentId.toString() }}
              search
              to="/games/$gameId"
            >
              game #{game.versionParentId}
            </Link>
          </p>
        )}

        <div className="flex flex-wrap items-center gap-2">
          <Button onClick={onAddToBoard} type="button" variant="outline">
            <ListPlus data-icon="inline-start" />
            Add to board
          </Button>
          <GameStoreLinks storePages={game.storePages} />
        </div>
      </div>
    </section>
  );
}
