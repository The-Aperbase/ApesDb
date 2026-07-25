import { useMutation, useQueryClient } from "@tanstack/react-query";
import { boardQueryKeys } from "../board-query-keys";
import { updateBoard } from "../boards.api";
import type { BoardDetails } from "../boards.schemas";

export function useEditBoard(boardId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { name: string; picture: File | null; removePicture: boolean }) =>
      updateBoard({ boardId, ...input }),
    onSuccess: (updatedBoard) => {
      queryClient.setQueryData<BoardDetails>(boardQueryKeys.details(boardId), updatedBoard);

      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
