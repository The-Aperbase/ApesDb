import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { boardQueryKeys } from "../board-query-keys";
import { addGameToBoard, removeGameFromBoard } from "../boards.api";
import type { BoardSummary } from "../boards.schemas";

type ToggleVariables = {
  boardId: string;
  add: boolean;
};

export function useToggleGameBoardMembership(gameId: number) {
  const queryClient = useQueryClient();
  const summariesKey = boardQueryKeys.collection(gameId);

  return useMutation({
    mutationFn: async ({ boardId, add }: ToggleVariables) => {
      if (add) {
        await addGameToBoard({ boardId, gameId });
        return;
      }

      await removeGameFromBoard({ boardId, gameId });
    },
    onMutate: async ({ boardId, add }) => {
      await queryClient.cancelQueries({ queryKey: summariesKey });
      const previous = queryClient.getQueryData<BoardSummary[]>(summariesKey);
      queryClient.setQueryData<BoardSummary[]>(summariesKey, (boards) => {
        if (!boards) {
          return boards;
        }

        return boards.map((board) => {
          if (board.id !== boardId) {
            return board;
          }

          return {
            ...board,
            containsGame: add,
            gameCount: add ? board.gameCount + 1 : Math.max(0, board.gameCount - 1),
          };
        });
      });
      return { previous };
    },
    onError: (error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(summariesKey, context.previous);
      }

      toast.error(
        error instanceof Error ? error.message : "Could not update the board. Try again.",
      );
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
