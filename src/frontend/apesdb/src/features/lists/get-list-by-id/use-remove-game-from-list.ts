import { useMutation, useQueryClient } from "@tanstack/react-query";
import { listQueryKeys } from "../list-query-keys";
import { removeGameFromList } from "../lists.api";
import type { GamesListDetails } from "../lists.schemas";

export function useRemoveGameFromList(teamId: string, listId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (gameId: number) => removeGameFromList({ teamId, listId, gameId }),
    onSuccess: (_data, gameId) => {
      queryClient.setQueryData<GamesListDetails>(
        listQueryKeys.details(teamId, listId),
        (details) => {
          if (!details) {
            return details;
          }

          return {
            ...details,
            games: details.games.filter((game) => game.gameId !== gameId),
          };
        },
      );

      void queryClient.invalidateQueries({ queryKey: listQueryKeys.all(teamId) });
    },
  });
}
