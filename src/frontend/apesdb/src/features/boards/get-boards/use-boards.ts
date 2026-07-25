import { useCallback, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { boardQueryKeys } from "../board-query-keys";
import {
  boardsPageSize,
  createBoardsRequestUrl,
  fetchBoards,
  fetchBoardsPage,
} from "../boards.api";
import type { BoardFilters } from "./boards-query-state";

type UseBoardsOptions = {
  gameId?: number;
  enabled?: boolean;
};

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  return "An unexpected error occurred.";
}

export function useBoards(options?: UseBoardsOptions) {
  const gameId = options?.gameId;
  const enabled = options?.enabled ?? true;
  const url = useMemo(() => createBoardsRequestUrl({ gameId }), [gameId]);
  const { data, error, isLoading, refetch } = useQuery({
    queryKey: boardQueryKeys.collection(gameId),
    queryFn: ({ signal }) => fetchBoards(url, signal),
    enabled,
  });
  const retry = useCallback(() => {
    void refetch();
  }, [refetch]);

  return {
    data: data ?? null,
    error: error ? errorMessage(error) : null,
    isLoading,
    retry,
  };
}

export function useBoardsPage(filters: BoardFilters) {
  const url = useMemo(
    () =>
      createBoardsRequestUrl({
        page: filters.page,
        pageSize: boardsPageSize,
        search: filters.search,
      }),
    [filters],
  );
  const { data, error, isLoading, refetch } = useQuery({
    queryKey: boardQueryKeys.page(url),
    queryFn: ({ signal }) => fetchBoardsPage(url, signal),
  });
  const retry = useCallback(() => {
    void refetch();
  }, [refetch]);

  return {
    data: data ?? null,
    error: error ? errorMessage(error) : null,
    isLoading,
    retry,
  };
}
