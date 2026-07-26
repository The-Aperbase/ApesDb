import { z } from "zod";
import type { GameFilters } from "./games-query-state";
import {
  gameLookupValueSchema,
  gameLookupsSchema,
  gamesResponseSchema,
  type GameLookups,
  type GamesResponse,
} from "./games.schemas";

export const gamesPageSize = 50;

const lookupResponseSchema = z.array(gameLookupValueSchema);

type GamePickerRequest = {
  page: number;
  search: string;
};

function appendIds(params: URLSearchParams, key: string, values: number[]) {
  const sanitized = [...new Set(values.filter((value) => Number.isInteger(value) && value >= 0))];
  sanitized.forEach((value) => params.append(key, value.toString()));
}

function appendText(params: URLSearchParams, key: string, value: string) {
  const trimmed = value.trim();
  if (trimmed.length > 0) {
    params.set(key, trimmed);
  }
}

export function createGamesRequestUrl(filters: GameFilters): string {
  const params = new URLSearchParams();
  appendIds(params, "gameTypeIds", filters.gameTypeIds);
  appendIds(params, "gameStatusIds", filters.gameStatusIds);
  appendIds(params, "genreIds", filters.genreIds);
  appendIds(params, "themeIds", filters.themeIds);
  appendIds(params, "gameModeIds", filters.gameModeIds);
  appendIds(params, "playerPerspectiveIds", filters.playerPerspectiveIds);
  appendIds(params, "platformIds", filters.platformIds);
  appendText(params, "developer", filters.developer);
  appendText(params, "publisher", filters.publisher);
  appendText(params, "collection", filters.collection);
  appendText(params, "franchise", filters.franchise);
  appendText(params, "search", filters.search);

  if (filters.isSteam) {
    params.set("isSteam", "true");
  }

  params.set("page", Math.max(1, filters.page).toString());
  params.set("pageSize", gamesPageSize.toString());
  return `/api/games?${params.toString()}`;
}

export function createGamePickerRequestUrl(request: GamePickerRequest): string {
  const params = new URLSearchParams();
  appendText(params, "search", request.search);
  params.set("page", Math.max(1, request.page).toString());
  params.set("pageSize", gamesPageSize.toString());
  return `/api/games?${params.toString()}`;
}

async function fetchAndParse<T>(
  url: string,
  schema: z.ZodType<T>,
  signal: AbortSignal,
): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    signal,
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}.`);
  }

  return schema.parse(await response.json());
}

export function fetchGames(url: string, signal: AbortSignal): Promise<GamesResponse> {
  return fetchAndParse(url, gamesResponseSchema, signal);
}

export async function fetchGameLookups(signal: AbortSignal): Promise<GameLookups> {
  const [gameTypes, gameStatuses, genres, themes, gameModes, playerPerspectives, platforms] =
    await Promise.all([
      fetchAndParse("/api/games/types", lookupResponseSchema, signal),
      fetchAndParse("/api/games/statuses", lookupResponseSchema, signal),
      fetchAndParse("/api/games/genres", lookupResponseSchema, signal),
      fetchAndParse("/api/games/themes", lookupResponseSchema, signal),
      fetchAndParse("/api/games/modes", lookupResponseSchema, signal),
      fetchAndParse("/api/games/player-perspectives", lookupResponseSchema, signal),
      fetchAndParse("/api/games/platforms", lookupResponseSchema, signal),
    ]);

  return gameLookupsSchema.parse({
    gameTypes,
    gameStatuses,
    genres,
    themes,
    gameModes,
    playerPerspectives,
    platforms,
  });
}
