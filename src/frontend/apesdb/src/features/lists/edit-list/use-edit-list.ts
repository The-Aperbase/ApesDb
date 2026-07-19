import { useMutation, useQueryClient } from "@tanstack/react-query";
import { listQueryKeys } from "../list-query-keys";
import { updateGamesList } from "../lists.api";
import type { GamesListDetails } from "../lists.schemas";

export function useEditGamesList(teamId: string, listId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { name: string; picture: File | null; removePicture: boolean }) =>
      updateGamesList({ teamId, listId, ...input }),
    onSuccess: (updatedList) => {
      queryClient.setQueryData<GamesListDetails>(
        listQueryKeys.details(teamId, listId),
        updatedList,
      );

      void queryClient.invalidateQueries({ queryKey: listQueryKeys.all(teamId) });
    },
  });
}
