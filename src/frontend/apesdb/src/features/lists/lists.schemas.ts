import { z } from "zod";

export const gamesListPictureSchema = z.object({
  contentType: z.string(),
  data: z.string(),
});

export const gamesListSummarySchema = z
  .object({
    id: z.string(),
    name: z.string(),
    createdAt: z.string(),
    updatedAt: z.string(),
    picture: gamesListPictureSchema.nullable(),
    gameCount: z.number().int().nonnegative(),
    containsGame: z.boolean(),
  })
  .transform(({ picture, ...list }) => ({
    ...list,
    pictureUrl: picture === null ? null : `data:${picture.contentType};base64,${picture.data}`,
  }));

export type GamesListSummary = z.infer<typeof gamesListSummarySchema>;

export const gamesListSummariesSchema = z.array(gamesListSummarySchema);

export const gamesListEntryStateSchema = z
  .enum(["todo", "in-progress", "completed", "dnf"])
  .default("todo");

export type GamesListEntryState = z.infer<typeof gamesListEntryStateSchema>;

export const gamesListGameSchema = z.object({
  gameId: z.number().int().nonnegative(),
  name: z.string(),
  coverSmallUrl: z.string().nullable(),
  coverLargeUrl: z.string().nullable(),
  gameType: z.string().nullable(),
  state: gamesListEntryStateSchema,
  addedAt: z.string(),
});

export type GamesListGame = z.infer<typeof gamesListGameSchema>;

export const gamesListDetailsSchema = z
  .object({
    id: z.string(),
    name: z.string(),
    createdAt: z.string(),
    updatedAt: z.string(),
    picture: gamesListPictureSchema.nullable(),
    games: z.array(gamesListGameSchema),
  })
  .transform(({ picture, ...list }) => ({
    ...list,
    pictureUrl: picture === null ? null : `data:${picture.contentType};base64,${picture.data}`,
  }));

export type GamesListDetails = z.infer<typeof gamesListDetailsSchema>;
