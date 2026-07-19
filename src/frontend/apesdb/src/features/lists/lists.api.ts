import { z } from "zod";
import {
  gamesListDetailsSchema,
  gamesListSummariesSchema,
  gamesListSummarySchema,
  type GamesListDetails,
  type GamesListEntryState,
  type GamesListSummary,
} from "./lists.schemas";

export class ListNotFoundError extends Error {
  constructor() {
    super("The requested games list was not found.");
    this.name = "ListNotFoundError";
  }
}

export type CreateGamesListInput = {
  teamId: string;
  name: string;
  picture: File | null;
};

export type UpdateGamesListInput = {
  teamId: string;
  listId: string;
  name: string;
  picture: File | null;
  removePicture: boolean;
};

export type DeleteGamesListInput = {
  teamId: string;
  listId: string;
};

export type GamesListEntryInput = {
  teamId: string;
  listId: string;
  gameId: number;
};

export type UpdateGameStateInput = GamesListEntryInput & {
  state: GamesListEntryState;
};

const validationErrorSchema = z.object({
  message: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
});

async function mutationRequestError(response: Response, fallback: string): Promise<Error> {
  if (response.status === 413) {
    return new Error("The picture must be 5 MB or smaller.");
  }

  try {
    const result = validationErrorSchema.safeParse(await response.json());

    if (result.success && result.data.errors) {
      const message = Object.values(result.data.errors).flat()[0];

      if (message) {
        return new Error(message);
      }
    }

    if (result.success && result.data.message) {
      return new Error(result.data.message);
    }
  } catch {
    // Fall through to the status-based message when the response is not JSON.
  }

  if (response.status === 400) {
    return new Error("Check the list details and try again.");
  }

  return new Error(`${fallback} (status ${response.status}).`);
}

function gamesListUrl(teamId: string, listId: string): string {
  return `/api/teams/${encodeURIComponent(teamId)}/games-lists/${encodeURIComponent(listId)}`;
}

export function createGamesListsRequestUrl(teamId: string, gameId?: number): string {
  const base = `/api/teams/${encodeURIComponent(teamId)}/games-lists`;

  if (gameId === undefined) {
    return base;
  }

  return `${base}?gameId=${gameId}`;
}

export async function fetchGamesLists(
  url: string,
  signal: AbortSignal,
): Promise<GamesListSummary[]> {
  const response = await fetch(url, {
    credentials: "include",
    signal,
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}.`);
  }

  return gamesListSummariesSchema.parse(await response.json());
}

export async function fetchGamesListDetails(
  teamId: string,
  listId: string,
  signal: AbortSignal,
): Promise<GamesListDetails> {
  const response = await fetch(gamesListUrl(teamId, listId), {
    credentials: "include",
    signal,
  });

  if (response.status === 404) {
    throw new ListNotFoundError();
  }

  if (!response.ok) {
    throw new Error(`Unable to load the games list (status ${response.status}).`);
  }

  const result = gamesListDetailsSchema.safeParse(await response.json());

  if (!result.success) {
    throw new Error("The server returned an unexpected games list response.");
  }

  return result.data;
}

export async function createGamesList(input: CreateGamesListInput): Promise<GamesListSummary> {
  const formData = new FormData();
  formData.set("Name", input.name);

  if (input.picture !== null) {
    formData.set("Picture", input.picture);
  }

  let response: Response;

  try {
    response = await fetch(`/api/teams/${encodeURIComponent(input.teamId)}/games-lists`, {
      method: "POST",
      credentials: "include",
      body: formData,
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw await mutationRequestError(response, "Unable to create the list");
  }

  const result = gamesListSummarySchema.safeParse(await response.json());

  if (!result.success) {
    throw new Error("The server returned an unexpected games list response.");
  }

  return result.data;
}

export async function updateGamesList(input: UpdateGamesListInput): Promise<GamesListDetails> {
  const formData = new FormData();
  formData.set("Name", input.name);

  if (input.picture !== null) {
    formData.set("Picture", input.picture);
  }

  if (input.removePicture) {
    formData.set("RemovePicture", "true");
  }

  let response: Response;

  try {
    response = await fetch(gamesListUrl(input.teamId, input.listId), {
      method: "PUT",
      credentials: "include",
      body: formData,
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (response.status === 404) {
    throw new ListNotFoundError();
  }

  if (!response.ok) {
    throw await mutationRequestError(response, "Unable to update the list");
  }

  const result = gamesListDetailsSchema.safeParse(await response.json());

  if (!result.success) {
    throw new Error("The server returned an unexpected games list response.");
  }

  return result.data;
}

export async function deleteGamesList(input: DeleteGamesListInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(gamesListUrl(input.teamId, input.listId), {
      method: "DELETE",
      credentials: "include",
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (response.status === 404) {
    throw new ListNotFoundError();
  }

  if (!response.ok) {
    throw new Error(`Unable to delete the list (status ${response.status}).`);
  }
}

export async function addGameToList(input: GamesListEntryInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(`${gamesListUrl(input.teamId, input.listId)}/entries`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ gameId: input.gameId }),
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw new Error(`Unable to add the game to the list (status ${response.status}).`);
  }
}

export async function updateGameState(input: UpdateGameStateInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(`${gamesListUrl(input.teamId, input.listId)}/entries/${input.gameId}`, {
      method: "PUT",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ state: input.state }),
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw new Error(`Unable to update the game state (status ${response.status}).`);
  }
}

export async function removeGameFromList(input: GamesListEntryInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(`${gamesListUrl(input.teamId, input.listId)}/entries/${input.gameId}`, {
      method: "DELETE",
      credentials: "include",
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw new Error(`Unable to remove the game from the list (status ${response.status}).`);
  }
}
