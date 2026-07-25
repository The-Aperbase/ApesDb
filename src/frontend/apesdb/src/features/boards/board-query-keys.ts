export const boardQueryKeys = {
  all: ["boards"] as const,
  collection: (gameId?: number) => ["boards", "collection", gameId ?? null] as const,
  details: (boardId: string) => ["boards", "details", boardId] as const,
};
