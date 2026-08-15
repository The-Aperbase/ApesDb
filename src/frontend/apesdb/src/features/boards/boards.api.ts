import { z } from "zod";
import {
  boardDetailsSchema,
  boardSharingSchema,
  boardSummariesResponseSchema,
  boardSummarySchema,
  type BoardDetails,
  type BoardEntryState,
  type BoardSummary,
  type BoardSharing,
  type BoardsResponse,
} from "./boards.schemas";

export class BoardNotFoundError extends Error {
  constructor() {
    super("The requested board was not found.");
    this.name = "BoardNotFoundError";
  }
}

export type CreateBoardInput = {
  name: string;
  picture: File | null;
};

export type UpdateBoardInput = {
  boardId: string;
  name: string;
  picture: File | null;
  removePicture: boolean;
};

export type DeleteBoardInput = {
  boardId: string;
};

export type BoardEntryInput = {
  boardId: string;
  gameId: number;
};

export type UpdateBoardEntryInput = BoardEntryInput & {
  state: BoardEntryState;
  position: number;
};

export type BoardInvitationInput = {
  boardId: string;
  email: string;
};

export type RespondToBoardInvitationInput = {
  boardId: string;
  invitationId: string;
  accept: boolean;
};

export type BoardInvitationTarget = {
  boardId: string;
  invitationId: string;
};

export type BoardCollaboratorTarget = {
  boardId: string;
  userId: string;
};

export type BoardsRequest = {
  gameId?: number;
  page?: number;
  pageSize?: number;
  search?: string;
};

export const boardsPageSize = 50;

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
    return new Error("Check the board details and try again.");
  }

  return new Error(`${fallback} (status ${response.status}).`);
}

function boardUrl(boardId: string): string {
  return `/api/boards/${encodeURIComponent(boardId)}`;
}

export function createBoardsRequestUrl(request: BoardsRequest = {}): string {
  const base = "/api/boards";
  const params = new URLSearchParams();

  if (request.gameId !== undefined) {
    params.set("gameId", request.gameId.toString());
  }

  const search = request.search?.trim();
  if (search) {
    params.set("search", search);
  }

  if (request.page !== undefined) {
    params.set("page", Math.max(1, request.page).toString());
  }

  if (request.pageSize !== undefined) {
    params.set("pageSize", request.pageSize.toString());
  }

  const query = params.toString();
  if (query.length === 0) {
    return base;
  }

  return `${base}?${query}`;
}

export async function fetchBoardsPage(url: string, signal: AbortSignal): Promise<BoardsResponse> {
  const response = await fetch(url, {
    credentials: "include",
    signal,
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}.`);
  }

  return boardSummariesResponseSchema.parse(await response.json());
}

export async function fetchBoards(url: string, signal: AbortSignal): Promise<BoardSummary[]> {
  return (await fetchBoardsPage(url, signal)).items;
}

export async function fetchBoardDetails(
  boardId: string,
  signal: AbortSignal,
): Promise<BoardDetails> {
  const response = await fetch(boardUrl(boardId), {
    credentials: "include",
    signal,
  });

  if (response.status === 404) {
    throw new BoardNotFoundError();
  }

  if (!response.ok) {
    throw new Error(`Unable to load the board (status ${response.status}).`);
  }

  const result = boardDetailsSchema.safeParse(await response.json());

  if (!result.success) {
    throw new Error("The server returned an unexpected board response.");
  }

  return result.data;
}

export async function createBoard(input: CreateBoardInput): Promise<BoardSummary> {
  const formData = new FormData();
  formData.set("Name", input.name);

  if (input.picture !== null) {
    formData.set("Picture", input.picture);
  }

  let response: Response;

  try {
    response = await fetch("/api/boards", {
      method: "POST",
      credentials: "include",
      body: formData,
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw await mutationRequestError(response, "Unable to create the board");
  }

  const result = boardSummarySchema.safeParse(await response.json());

  if (!result.success) {
    throw new Error("The server returned an unexpected board response.");
  }

  return result.data;
}

export async function updateBoard(input: UpdateBoardInput): Promise<BoardDetails> {
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
    response = await fetch(boardUrl(input.boardId), {
      method: "PUT",
      credentials: "include",
      body: formData,
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (response.status === 404) {
    throw new BoardNotFoundError();
  }

  if (!response.ok) {
    throw await mutationRequestError(response, "Unable to update the board");
  }

  const result = boardDetailsSchema.safeParse(await response.json());

  if (!result.success) {
    throw new Error("The server returned an unexpected board response.");
  }

  return result.data;
}

export async function deleteBoard(input: DeleteBoardInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(boardUrl(input.boardId), {
      method: "DELETE",
      credentials: "include",
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (response.status === 404) {
    throw new BoardNotFoundError();
  }

  if (!response.ok) {
    throw new Error(`Unable to delete the board (status ${response.status}).`);
  }
}

export async function addGameToBoard(input: BoardEntryInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(`${boardUrl(input.boardId)}/entries`, {
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
    throw new Error(`Unable to add the game to the board (status ${response.status}).`);
  }
}

export async function updateBoardEntry(input: UpdateBoardEntryInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(`${boardUrl(input.boardId)}/entries/${input.gameId}`, {
      method: "PUT",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ state: input.state, position: input.position }),
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw new Error(`Unable to move the game (status ${response.status}).`);
  }
}

export async function removeGameFromBoard(input: BoardEntryInput): Promise<void> {
  let response: Response;

  try {
    response = await fetch(`${boardUrl(input.boardId)}/entries/${input.gameId}`, {
      method: "DELETE",
      credentials: "include",
    });
  } catch {
    throw new Error("Unable to reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    throw new Error(`Unable to remove the game from the board (status ${response.status}).`);
  }
}

export async function fetchBoardSharing(
  boardId: string,
  signal?: AbortSignal,
): Promise<BoardSharing> {
  const response = await fetch(`${boardUrl(boardId)}/sharing`, {
    credentials: "include",
    signal,
  });

  if (response.status === 404) {
    throw new BoardNotFoundError();
  }

  if (!response.ok) {
    throw new Error(`Unable to load board sharing (status ${response.status}).`);
  }

  return boardSharingSchema.parse(await response.json());
}

export async function inviteToBoard(input: BoardInvitationInput): Promise<void> {
  const response = await fetch(`${boardUrl(input.boardId)}/invitations`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email: input.email }),
  });

  if (response.status === 404) {
    throw new BoardNotFoundError();
  }

  if (!response.ok) {
    throw await mutationRequestError(response, "Unable to send the board invitation");
  }
}

export async function respondToBoardInvitation(
  input: RespondToBoardInvitationInput,
): Promise<void> {
  const response = await fetch(
    `${boardUrl(input.boardId)}/invitations/${encodeURIComponent(input.invitationId)}/respond`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accept: input.accept }),
    },
  );

  if (!response.ok) {
    throw new Error(`Unable to respond to the board invitation (status ${response.status}).`);
  }
}

export async function cancelBoardInvitation(input: BoardInvitationTarget): Promise<void> {
  const response = await fetch(
    `${boardUrl(input.boardId)}/invitations/${encodeURIComponent(input.invitationId)}`,
    { method: "DELETE", credentials: "include" },
  );

  if (!response.ok) {
    throw new Error(`Unable to cancel the board invitation (status ${response.status}).`);
  }
}

export async function removeBoardCollaborator(input: BoardCollaboratorTarget): Promise<void> {
  const response = await fetch(
    `${boardUrl(input.boardId)}/collaborators/${encodeURIComponent(input.userId)}`,
    { method: "DELETE", credentials: "include" },
  );

  if (!response.ok) {
    throw new Error(`Unable to remove board access (status ${response.status}).`);
  }
}
