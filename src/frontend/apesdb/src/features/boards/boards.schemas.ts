import { z } from "zod";
import type { Pageable } from "@apesdb/common";

export const boardPictureSchema = z.object({
  contentType: z.string(),
  data: z.string(),
});

export const boardSummarySchema = z
  .object({
    id: z.string(),
    name: z.string(),
    createdAt: z.string(),
    updatedAt: z.string(),
    picture: boardPictureSchema.nullable(),
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

export const boardEntryStateSchema = z
  .enum(["todo", "in-progress", "completed", "dnf"])
  .default("todo");

export type BoardEntryState = z.infer<typeof boardEntryStateSchema>;

export const boardGameSchema = z.object({
  gameId: z.number().int().nonnegative(),
  name: z.string(),
  coverSmallUrl: z.string().nullable(),
  coverLargeUrl: z.string().nullable(),
  gameType: z.string().nullable(),
  state: boardEntryStateSchema,
  addedAt: z.string(),
});

export type BoardGame = z.infer<typeof boardGameSchema>;

export const boardDetailsSchema = z
  .object({
    id: z.string(),
    name: z.string(),
    createdAt: z.string(),
    updatedAt: z.string(),
    picture: boardPictureSchema.nullable(),
    games: z.array(boardGameSchema),
  })
  .transform(({ picture, ...board }) => ({
    ...board,
    pictureUrl: picture === null ? null : `data:${picture.contentType};base64,${picture.data}`,
  }));

export type BoardDetails = z.infer<typeof boardDetailsSchema>;
