import { useMutation, useQueryClient } from "@tanstack/react-query";
import { boardQueryKeys } from "../board-query-keys";
import { removeGameFromBoard } from "../boards.api";
import {
  boardEntryStates,
  createBoardGameOrder,
  getOrderedBoardGames,
  type BoardDetails,
} from "../boards.schemas";

export function useRemoveGameFromBoard(boardId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (gameId: number) => removeGameFromBoard({ boardId, gameId }),
    onSuccess: (_data, gameId) => {
      queryClient.setQueryData<BoardDetails>(boardQueryKeys.details(boardId), (details) => {
        if (!details) {
          return details;
        }

        const games = { ...details.games };

        for (const state of boardEntryStates) {
          games[state] = createBoardGameOrder(
            getOrderedBoardGames(games[state]).filter((game) => game.gameId !== gameId),
          );
        }

        return { ...details, games };
      });

      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
