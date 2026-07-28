import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { boardQueryKeys } from "../board-query-keys";
import { updateBoardEntry } from "../boards.api";
import type { BoardEntryState } from "../boards.schemas";

type UpdateVariables = {
  gameId: number;
  state: BoardEntryState;
  position: number;
};

export function useMoveGameOnBoard(boardId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ gameId, state, position }: UpdateVariables) =>
      updateBoardEntry({ boardId, gameId, state, position }),
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "Could not move the game. Try again.");
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
