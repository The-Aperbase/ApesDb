import { useMutation, useQueryClient } from "@tanstack/react-query";
import { listQueryKeys } from "../list-query-keys";
import { deleteGamesList } from "../lists.api";

export function useDeleteGamesList(teamId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (listId: string) => deleteGamesList({ teamId, listId }),
    onSuccess: (_data, listId) => {
      queryClient.removeQueries({ queryKey: listQueryKeys.details(teamId, listId) });

      void queryClient.invalidateQueries({ queryKey: listQueryKeys.all(teamId) });
    },
  });
}
