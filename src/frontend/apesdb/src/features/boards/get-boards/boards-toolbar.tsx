import { Search } from "lucide-react";
import { DebouncedFilterInput } from "../../../lib/debounced-filter-input";
import type { BoardFilterPatch, BoardFilters } from "./boards-query-state";

type BoardsToolbarProps = {
  filters: BoardFilters;
  onFiltersChange: (patch: BoardFilterPatch) => void;
};

export function BoardsToolbar({ filters, onFiltersChange }: BoardsToolbarProps) {
  return (
    <div className="relative min-w-0 sm:max-w-md">
      <Search className="pointer-events-none absolute top-1/2 left-2 size-4 -translate-y-1/2 text-muted-foreground" />
      <DebouncedFilterInput
        aria-label="Search boards"
        className="h-8 pl-8"
        placeholder="Search boards…"
        value={filters.search}
        onValueChange={(search) => onFiltersChange({ search })}
      />
    </div>
  );
}
