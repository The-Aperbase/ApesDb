import { useCallback, useEffect, useState } from "react";
import { useQueryStates } from "nuqs";
import { AddToListDialog } from "../../lists/add-game-to-list/add-to-list-dialog";
import { useActiveTeam } from "../../teams/team-context";
import { AdvancedFiltersSheet } from "./advanced-filters-sheet";
import { usePageTableViewPreference } from "../../../lib/table-view-preferences";
import {
  countAdvancedFilters,
  gameFilterParsers,
  hasGameFilters,
  type GameFilterPatch,
} from "./games-query-state";
import type { Game } from "./games.schemas";
import { GamesTable } from "./games-table";
import { GamesToolbar } from "./games-toolbar";
import { useGameLookups, useGames } from "./use-games";

export function GamesPage() {
  const [advancedFiltersOpen, setAdvancedFiltersOpen] = useState(false);
  const [gameToAddToList, setGameToAddToList] = useState<Game | null>(null);
  const [tableView, setTableView] = usePageTableViewPreference("games");
  const [filters, setFilters] = useQueryStates(gameFilterParsers, {
    clearOnDefault: true,
    history: "replace",
  });
  const games = useGames(filters);
  const lookups = useGameLookups();
  const { activeTeam } = useActiveTeam();

  const openAddToList = useCallback((game: Game) => {
    setGameToAddToList(game);
  }, []);

  const closeAddToList = useCallback((open: boolean) => {
    if (!open) {
      setGameToAddToList(null);
    }
  }, []);

  const updateFilters = useCallback(
    (patch: GameFilterPatch) => {
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
    if (!games.data) {
      return;
    }

    const pageCount = Math.max(1, Math.ceil(games.data.filteredTotal / games.data.pageSize));
    const normalizedPage = Math.min(Math.max(1, filters.page), pageCount);
    if (normalizedPage !== filters.page) {
      void setFilters({ page: normalizedPage }, { history: "replace" });
    }
  }, [filters.page, games.data, setFilters]);

  return (
    <div className="mx-auto flex h-full min-h-0 w-full max-w-7xl flex-col gap-4 overflow-hidden">
      <h1 className="text-2xl font-semibold tracking-tight">Games</h1>
      <GamesTable
        data={games.data}
        error={games.error}
        hasFilters={hasGameFilters(filters)}
        header={
          <GamesToolbar
            advancedFilterCount={countAdvancedFilters(filters)}
            filters={filters}
            onFiltersChange={updateFilters}
            onOpenAdvancedFilters={() => setAdvancedFiltersOpen(true)}
          />
        }
        isLoading={games.isLoading}
        mode={tableView}
        page={Math.max(1, filters.page)}
        onAddToList={openAddToList}
        onModeChange={setTableView}
        onPageChange={updatePage}
        onRetry={games.retry}
      />
      <AdvancedFiltersSheet
        open={advancedFiltersOpen}
        filters={filters}
        lookups={lookups.data}
        lookupsError={lookups.error}
        lookupsLoading={lookups.isLoading}
        onOpenChange={setAdvancedFiltersOpen}
        onFiltersChange={updateFilters}
        onRetryLookups={lookups.retry}
      />
      {activeTeam !== null ? (
        <AddToListDialog
          teamId={activeTeam.id}
          game={gameToAddToList}
          open={gameToAddToList !== null}
          onOpenChange={closeAddToList}
        />
      ) : null}
    </div>
  );
}
