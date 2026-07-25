import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { Button } from "@apesdb/ui";
import { Plus } from "lucide-react";
import { useQueryStates } from "nuqs";
import { usePageTableViewPreference } from "../../../lib/table-view-preferences";
import { CreateBoardDialog } from "../create-board/create-board-dialog";
import { boardFilterParsers, type BoardFilterPatch } from "./boards-query-state";
import { BoardsTable } from "./boards-table";
import { BoardsToolbar } from "./boards-toolbar";
import { useBoardsPage } from "./use-boards";

export function BoardsPage() {
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [tableView, setTableView] = usePageTableViewPreference("boards");
  const [filters, setFilters] = useQueryStates(boardFilterParsers, {
    clearOnDefault: true,
    history: "replace",
  });
  const boards = useBoardsPage(filters);
  const navigate = useNavigate();

  const updateFilters = useCallback(
    (patch: BoardFilterPatch) => {
      void setFilters({ ...patch, page: 1 }, { history: "replace" });
    },
    [setFilters],
  );

  const updatePage = useCallback(
    (page: number) => {
      void setFilters({ page }, { history: "push" });
    },
    [setFilters],
  );

  useEffect(() => {
    if (!boards.data) {
      return;
    }

    const pageCount = Math.max(1, Math.ceil(boards.data.filteredTotal / boards.data.pageSize));
    const normalizedPage = Math.min(Math.max(1, filters.page), pageCount);
    if (normalizedPage !== filters.page) {
      void setFilters({ page: normalizedPage }, { history: "replace" });
    }
  }, [boards.data, filters.page, setFilters]);

  return (
    <main className="mx-auto flex h-full min-h-0 w-full max-w-5xl flex-col gap-4 overflow-hidden">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">Boards</h1>
        <Button onClick={() => setIsCreateDialogOpen(true)} type="button" variant="outline">
          <Plus data-icon="inline-start" />
          Create board
        </Button>
      </div>
      <BoardsTable
        data={boards.data}
        error={boards.error}
        hasFilters={filters.search.trim().length > 0}
        header={<BoardsToolbar filters={filters} onFiltersChange={updateFilters} />}
        isLoading={boards.isLoading}
        mode={tableView}
        page={Math.max(1, filters.page)}
        onModeChange={setTableView}
        onPageChange={updatePage}
        onRetry={boards.retry}
      />
      <CreateBoardDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={(boardId) => void navigate({ to: "/boards/$boardId", params: { boardId } })}
      />
    </main>
  );
}
