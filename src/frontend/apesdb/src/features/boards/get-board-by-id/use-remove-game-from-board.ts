import { useMutation, useQueryClient } from "@tanstack/react-query";
import { boardQueryKeys } from "../board-query-keys";
import { removeGameFromBoard } from "../boards.api";
import type { BoardDetails } from "../boards.schemas";

export function useRemoveGameFromBoard(boardId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (gameId: number) => removeGameFromBoard({ boardId, gameId }),
    onSuccess: (_data, gameId) => {
      queryClient.setQueryData<BoardDetails>(boardQueryKeys.details(boardId), (details) => {
        if (!details) {
          return details;
        }

        return {
          ...details,
          games: details.games.filter((game) => game.gameId !== gameId),
        };
      });

      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
