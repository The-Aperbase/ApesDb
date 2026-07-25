import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { boardQueryKeys } from "../board-query-keys";
import { updateGameState } from "../boards.api";
import type { BoardEntryState } from "../boards.schemas";

type UpdateVariables = {
  gameId: number;
  state: BoardEntryState;
};

export function useUpdateGameState(boardId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ gameId, state }: UpdateVariables) => updateGameState({ boardId, gameId, state }),
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Could not update the game state. Try again.",
      );
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
