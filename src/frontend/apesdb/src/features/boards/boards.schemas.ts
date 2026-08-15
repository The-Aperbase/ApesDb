import { z } from "zod";
import type { Pageable } from "@apesdb/common";

export const boardPictureSchema = z.object({
  contentType: z.string(),
  data: z.string(),
});

export const boardUserSchema = z.object({
  id: z.string(),
  name: z.string(),
  pictureUrl: z.string().nullable(),
});

export const boardRoleSchema = z.enum(["owner", "collaborator"]);

export const boardSummarySchema = z
  .object({
    id: z.string(),
    name: z.string(),
    createdAt: z.string(),
    updatedAt: z.string(),
    picture: boardPictureSchema.nullable(),
    owner: boardUserSchema,
    role: boardRoleSchema,
    gameCount: z.number().int().nonnegative(),
    containsGame: z.boolean(),
  })
  .transform(({ picture, ...board }) => ({
    ...board,
    pictureUrl: picture === null ? null : `data:${picture.contentType};base64,${picture.data}`,
  }));

export type BoardSummary = z.infer<typeof boardSummarySchema>;

export const boardSummariesResponseSchema: z.ZodType<Pageable<BoardSummary>> = z.object({
  items: z.array(boardSummarySchema),
  total: z.number().int().nonnegative(),
  filteredTotal: z.number().int().nonnegative(),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
});

export type BoardsResponse = Pageable<BoardSummary>;

export const boardEntryStates = ["todo", "in-progress", "completed", "dnf"] as const;

export const boardEntryStateSchema = z.enum(boardEntryStates);

export type BoardEntryState = z.infer<typeof boardEntryStateSchema>;

export const boardGameSchema = z.object({
  gameId: z.number().int().nonnegative(),
  name: z.string(),
  coverSmallUrl: z.string().nullable(),
  coverLargeUrl: z.string().nullable(),
  gameType: z.string().nullable(),
  addedAt: z.string(),
});

export type BoardGame = z.infer<typeof boardGameSchema>;

const boardGameOrderSchema = z
  .record(z.string().regex(/^(0|[1-9]\d*)$/), boardGameSchema)
  .superRefine((games, context) => {
    const positions = Object.keys(games)
      .map((position) => Number.parseInt(position, 10))
      .toSorted((left, right) => left - right);

    for (const [expectedPosition, position] of positions.entries()) {
      if (position !== expectedPosition) {
        context.addIssue({
          code: "custom",
          message: "Game order must be contiguous and zero-based.",
        });
        return;
      }
    }
  });

export type BoardGameOrder = z.infer<typeof boardGameOrderSchema>;

export const boardGamesSchema = z.object({
  todo: boardGameOrderSchema,
  "in-progress": boardGameOrderSchema,
  completed: boardGameOrderSchema,
  dnf: boardGameOrderSchema,
});

export type BoardGames = z.infer<typeof boardGamesSchema>;

export function getOrderedBoardGames(games: BoardGameOrder): BoardGame[] {
  return Object.entries(games)
    .map(([order, game]) => ({ order: Number.parseInt(order, 10), game }))
    .toSorted((left, right) => left.order - right.order)
    .map(({ game }) => game);
}

export function getAllBoardGames(games: BoardGames): BoardGame[] {
  return boardEntryStates.flatMap((state) => getOrderedBoardGames(games[state]));
}

export function createBoardGameOrder(games: BoardGame[]): BoardGameOrder {
  return Object.fromEntries(games.map((game, position) => [position.toString(), game]));
}

export const boardDetailsSchema = z
  .object({
    id: z.string(),
    name: z.string(),
    createdAt: z.string(),
    updatedAt: z.string(),
    picture: boardPictureSchema.nullable(),
    owner: boardUserSchema,
    role: boardRoleSchema,
    games: boardGamesSchema,
  })
  .transform(({ picture, ...board }) => ({
    ...board,
    pictureUrl: picture === null ? null : `data:${picture.contentType};base64,${picture.data}`,
  }));

export type BoardDetails = z.infer<typeof boardDetailsSchema>;

export const boardSharingSchema = z.object({
  collaborators: z.array(
    z.object({
      user: boardUserSchema,
      joinedAt: z.string(),
    }),
  ),
  outgoingInvitations: z.array(
    z.object({
      id: z.string(),
      email: z.string(),
      createdAt: z.string(),
    }),
  ),
});

export type BoardSharing = z.infer<typeof boardSharingSchema>;
