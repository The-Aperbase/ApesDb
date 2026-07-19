import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { listQueryKeys } from "../list-query-keys";
import { addGameToList, removeGameFromList } from "../lists.api";
import type { GamesListSummary } from "../lists.schemas";

type ToggleVariables = {
  listId: string;
  add: boolean;
};

export function useToggleGameListMembership(teamId: string, gameId: number) {
  const queryClient = useQueryClient();
  const summariesKey = listQueryKeys.list(teamId, gameId);

  return useMutation({
    mutationFn: async ({ listId, add }: ToggleVariables) => {
      if (add) {
        await addGameToList({ teamId, listId, gameId });
        return;
      }

      await removeGameFromList({ teamId, listId, gameId });
    },
    onMutate: async ({ listId, add }) => {
      await queryClient.cancelQueries({ queryKey: summariesKey });
      const previous = queryClient.getQueryData<GamesListSummary[]>(summariesKey);
      queryClient.setQueryData<GamesListSummary[]>(summariesKey, (lists) => {
        if (!lists) {
          return lists;
        }

        return lists.map((list) => {
          if (list.id !== listId) {
            return list;
          }

          return {
            ...list,
            containsGame: add,
            gameCount: add ? list.gameCount + 1 : Math.max(0, list.gameCount - 1),
          };
        });
      });
      return { previous };
    },
    onError: (error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(summariesKey, context.previous);
      }

      toast.error(error instanceof Error ? error.message : "Could not update the list. Try again.");
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: listQueryKeys.all(teamId) });
    },
  });
}
