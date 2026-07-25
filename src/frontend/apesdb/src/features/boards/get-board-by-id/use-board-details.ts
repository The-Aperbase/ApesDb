import { useCallback } from "react";
import { useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { boardQueryKeys } from "../board-query-keys";
import { BoardNotFoundError, fetchBoardDetails } from "../boards.api";

const idSchema = z.uuid();

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  return "An unexpected error occurred.";
}

export function useBoardDetails(boardId: string) {
  const isValid = idSchema.safeParse(boardId).success;
  const { data, error, isLoading, refetch } = useQuery({
    queryKey: boardQueryKeys.details(boardId),
    queryFn: ({ signal }) => fetchBoardDetails(boardId, signal),
    enabled: isValid,
    retry: (failureCount, requestError) => {
      if (requestError instanceof BoardNotFoundError) {
        return false;
      }

      return failureCount < 2;
    },
  });
  const retry = useCallback(() => {
    void refetch();
  }, [refetch]);
  const isNotFound = error instanceof BoardNotFoundError;

  return {
    data: data ?? null,
    error: error && !isNotFound ? errorMessage(error) : null,
    isInvalid: !isValid,
    isLoading,
    isNotFound,
    retry,
  };
}
