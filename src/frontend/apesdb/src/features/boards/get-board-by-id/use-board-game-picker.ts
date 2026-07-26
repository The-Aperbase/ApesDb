import { useCallback, useMemo } from "react";
import { useInfiniteQuery } from "@tanstack/react-query";
import { createGamePickerRequestUrl, fetchGames } from "../../games/list-games/games.api";

export const boardGamePickerQueryKey = ["games", "board-picker"] as const;

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  return "An unexpected error occurred.";
}

export function useBoardGamePicker(search: string, enabled: boolean) {
  const normalizedSearch = search.trim();
  const query = useInfiniteQuery({
    queryKey: [...boardGamePickerQueryKey, normalizedSearch],
    queryFn: ({ pageParam, signal }) =>
      fetchGames(createGamePickerRequestUrl({ page: pageParam, search: normalizedSearch }), signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      if (lastPage.page * lastPage.pageSize >= lastPage.filteredTotal) {
        return undefined;
      }

      return lastPage.page + 1;
    },
    enabled,
  });
  const games = useMemo(() => query.data?.pages.flatMap((page) => page.items) ?? [], [query.data]);
  const retry = useCallback(() => {
    void query.refetch();
  }, [query]);

  return {
    error: query.error ? errorMessage(query.error) : null,
    games,
    hasNextPage: query.hasNextPage,
    isError: query.isError,
    isFetchNextPageError: query.isFetchNextPageError,
    isFetchingNextPage: query.isFetchingNextPage,
    isLoading: query.isLoading,
    loadNextPage: query.fetchNextPage,
    retry,
  };
}
