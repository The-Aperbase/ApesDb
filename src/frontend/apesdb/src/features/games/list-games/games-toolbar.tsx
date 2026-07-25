import { Badge, Button } from "@apesdb/ui";
import { ListFilter, Search } from "lucide-react";
import { DebouncedFilterInput } from "../../../lib/debounced-filter-input";
import type { GameFilterPatch, GameFilters } from "./games-query-state";

type GamesToolbarProps = {
  filters: GameFilters;
  advancedFilterCount: number;
  onFiltersChange: (patch: GameFilterPatch) => void;
  onOpenAdvancedFilters: () => void;
};

export function GamesToolbar({
  filters,
  advancedFilterCount,
  onFiltersChange,
  onOpenAdvancedFilters,
}: GamesToolbarProps) {
  return (
    <div className="flex items-center gap-2">
      <div className="relative min-w-0 flex-1 sm:max-w-md">
        <Search className="pointer-events-none absolute top-1/2 left-2 size-4 -translate-y-1/2 text-muted-foreground" />
        <DebouncedFilterInput
          aria-label="Search games"
          className="h-8 pl-8"
          placeholder="Search games…"
          value={filters.search}
          onValueChange={(search) => onFiltersChange({ search })}
        />
      </div>
      <Button
        aria-label="Advanced filters"
        className="shrink-0"
        type="button"
        variant={advancedFilterCount > 0 ? "secondary" : "outline"}
        size="lg"
        onClick={onOpenAdvancedFilters}
      >
        <ListFilter />
        <span className="hidden sm:inline">Advanced</span>
        {advancedFilterCount > 0 && <Badge>{advancedFilterCount}</Badge>}
      </Button>
    </div>
  );
}
