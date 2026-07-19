import { useCallback } from "react";
import { useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { listQueryKeys } from "../list-query-keys";
import { fetchGamesListDetails, ListNotFoundError } from "../lists.api";

const idSchema = z.uuid();

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  return "An unexpected error occurred.";
}

export function useGamesListDetails(teamId: string, listId: string) {
  const isValid = idSchema.safeParse(teamId).success && idSchema.safeParse(listId).success;
  const { data, error, isLoading, refetch } = useQuery({
    queryKey: listQueryKeys.details(teamId, listId),
    queryFn: ({ signal }) => fetchGamesListDetails(teamId, listId, signal),
    enabled: isValid,
    retry: (failureCount, requestError) => {
      if (requestError instanceof ListNotFoundError) {
        return false;
      }

      return failureCount < 2;
    },
  });
  const retry = useCallback(() => {
    void refetch();
  }, [refetch]);
  const isNotFound = error instanceof ListNotFoundError;

  return {
    data: data ?? null,
    error: error && !isNotFound ? errorMessage(error) : null,
    isInvalid: !isValid,
    isLoading,
    isNotFound,
    retry,
  };
}
