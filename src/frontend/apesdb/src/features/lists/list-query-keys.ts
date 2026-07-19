export const listQueryKeys = {
  all: (teamId: string) => ["lists", teamId] as const,
  list: (teamId: string, gameId?: number) => ["lists", teamId, "list", gameId ?? null] as const,
  details: (teamId: string, listId: string) => ["lists", teamId, "details", listId] as const,
};
