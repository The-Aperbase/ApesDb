import { useCallback, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { listQueryKeys } from "../list-query-keys";
import { createGamesListsRequestUrl, fetchGamesLists } from "../lists.api";

const teamIdSchema = z.uuid();

type UseGamesListsOptions = {
  gameId?: number;
  enabled?: boolean;
};

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  return "An unexpected error occurred.";
}

export function useGamesLists(teamId: string, options?: UseGamesListsOptions) {
  const gameId = options?.gameId;
  const enabled = options?.enabled ?? true;
  const isValid = teamIdSchema.safeParse(teamId).success;
  const url = useMemo(() => createGamesListsRequestUrl(teamId, gameId), [teamId, gameId]);
  const { data, error, isLoading, refetch } = useQuery({
    queryKey: listQueryKeys.list(teamId, gameId),
    queryFn: ({ signal }) => fetchGamesLists(url, signal),
    enabled: isValid && enabled,
  });
  const retry = useCallback(() => {
    void refetch();
  }, [refetch]);

  return {
    data: data ?? null,
    error: error ? errorMessage(error) : null,
    isInvalid: !isValid,
    isLoading,
    retry,
  };
}
