import { useMutation, useQueryClient } from "@tanstack/react-query";
import { boardQueryKeys } from "../board-query-keys";
import { addGameToBoard } from "../boards.api";

export function useAddGameToBoard(boardId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (gameId: number) => addGameToBoard({ boardId, gameId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: boardQueryKeys.all }),
  });
}
