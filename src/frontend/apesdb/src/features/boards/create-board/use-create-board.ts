import { useMutation, useQueryClient } from "@tanstack/react-query";
import { boardQueryKeys } from "../board-query-keys";
import { createBoard } from "../boards.api";
import type { BoardSummary } from "../boards.schemas";

export function useCreateBoard() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { name: string; picture: File | null }) => createBoard(input),
    onSuccess: (createdBoard) => {
      queryClient.setQueryData<BoardSummary[]>(boardQueryKeys.collection(), (boards) => {
        if (!boards) {
          return [createdBoard];
        }

        const existingBoardIndex = boards.findIndex((board) => board.id === createdBoard.id);

        if (existingBoardIndex === -1) {
          return [...boards, createdBoard];
        }

        return boards.map((board) => {
          if (board.id === createdBoard.id) {
            return createdBoard;
          }

          return board;
        });
      });

      void queryClient.invalidateQueries({ queryKey: boardQueryKeys.all });
    },
  });
}
