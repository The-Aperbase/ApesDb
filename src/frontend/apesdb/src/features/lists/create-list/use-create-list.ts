import { useMutation, useQueryClient } from "@tanstack/react-query";
import { listQueryKeys } from "../list-query-keys";
import { createGamesList } from "../lists.api";
import type { GamesListSummary } from "../lists.schemas";

export function useCreateGamesList(teamId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { name: string; picture: File | null }) =>
      createGamesList({ teamId, ...input }),
    onSuccess: (createdList) => {
      queryClient.setQueryData<GamesListSummary[]>(listQueryKeys.list(teamId), (lists) => {
        if (!lists) {
          return [createdList];
        }

        const existingListIndex = lists.findIndex((list) => list.id === createdList.id);

        if (existingListIndex === -1) {
          return [...lists, createdList];
        }

        return lists.map((list) => {
          if (list.id === createdList.id) {
            return createdList;
          }

          return list;
        });
      });

      void queryClient.invalidateQueries({ queryKey: listQueryKeys.all(teamId) });
    },
  });
}
