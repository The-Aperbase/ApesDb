import { useMutation, useQueryClient } from "@tanstack/react-query";
import { boardQueryKeys } from "../board-query-keys";
import { deleteBoard } from "../boards.api";

export function useDeleteBoard() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (boardId: string) => deleteBoard({ boardId }),
    onSuccess: (_data, boardId) => {
      queryClient.removeQueries({ queryKey: boardQueryKeys.details(boardId) });

      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
