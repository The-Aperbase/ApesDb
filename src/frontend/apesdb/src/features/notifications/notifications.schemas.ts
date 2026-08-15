import { z } from "zod";

const notificationActorSchema = z.object({
  id: z.string(),
  name: z.string(),
  pictureUrl: z.string().nullable(),
});

const notificationBoardSchema = z.object({
  id: z.string(),
  name: z.string(),
});

export const notificationSchema = z.object({
  id: z.string(),
  type: z.string().min(1),
  resourceId: z.string(),
  createdAt: z.string(),
  readAt: z.string().nullable(),
  isUnread: z.boolean(),
  isActionable: z.boolean(),
  actor: notificationActorSchema.nullable().optional(),
  board: notificationBoardSchema.nullable().optional(),
});

export const notificationMetadataSchema = z.object({
  totalCount: z.number(),
  unreadCount: z.number(),
  actionableCount: z.number(),
  attentionCount: z.number(),
});

export const listNotificationsResponseSchema = z.object({
  items: z.array(notificationSchema),
  metadata: notificationMetadataSchema,
});

export type Notification = z.infer<typeof notificationSchema>;
export type NotificationMetadata = z.infer<typeof notificationMetadataSchema>;
export type ListNotificationsResponse = z.infer<typeof listNotificationsResponseSchema>;
