export const boardQueryKeys = {
  all: ["boards"] as const,
  collection: (gameId?: number) => ["boards", "collection", gameId ?? null] as const,
  page: (url: string) => ["boards", "page", url] as const,
  details: (boardId: string) => ["boards", "details", boardId] as const,
};
