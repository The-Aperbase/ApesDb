import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { listQueryKeys } from "../list-query-keys";
import { updateGameState } from "../lists.api";
import type { GamesListEntryState } from "../lists.schemas";

type UpdateVariables = {
  gameId: number;
  state: GamesListEntryState;
};

export function useUpdateGameState(teamId: string, listId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ gameId, state }: UpdateVariables) =>
      updateGameState({ teamId, listId, gameId, state }),
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Could not update the game state. Try again.",
      );
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: listQueryKeys.all(teamId) });
    },
  });
}
