import { useMemo, useState } from "react";
import {
  Button,
  Combobox,
  ComboboxChip,
  ComboboxChips,
  ComboboxChipsInput,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxItem,
  ComboboxList,
  Field,
  FieldGroup,
  FieldLabel,
  Item,
  ItemContent,
  ItemDescription,
  ItemTitle,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  Skeleton,
  Switch,
  useComboboxAnchor,
} from "@apesdb/ui";
import { RefreshCw } from "lucide-react";
import { DebouncedFilterInput } from "../../../lib/debounced-filter-input";
import {
  defaultAdvancedFilters,
  type GameFilterPatch,
  type GameFilters,
} from "./games-query-state";
import type { GameLookups, GameLookupValue } from "./games.schemas";

type AdvancedFiltersSheetProps = {
  open: boolean;
  filters: GameFilters;
  lookups: GameLookups | null;
  lookupsError: string | null;
  lookupsLoading: boolean;
  onOpenChange: (open: boolean) => void;
  onFiltersChange: (patch: GameFilterPatch) => void;
  onRetryLookups: () => void;
};

type ComboboxOption = {
  value: string;
  label: string;
};

type LookupMultiSelectProps = {
  id: string;
  label: string;
  values: number[];
  options: GameLookupValue[];
  onValueChange: (values: number[]) => void;
};

function LookupMultiSelect({ id, label, values, options, onValueChange }: LookupMultiSelectProps) {
  const anchor = useComboboxAnchor();
  const [search, setSearch] = useState("");
  const comboboxOptions = useMemo<ComboboxOption[]>(
    () => options.map((option) => ({ value: option.id.toString(), label: option.name })),
    [options],
  );
  const selectedOptions = useMemo(
    () => comboboxOptions.filter((option) => values.includes(Number(option.value))),
    [comboboxOptions, values],
  );
  const filteredOptions = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase();
    if (normalizedSearch.length === 0) {
      return comboboxOptions;
    }

    return comboboxOptions.filter((option) =>
      option.label.toLocaleLowerCase().includes(normalizedSearch),
    );
  }, [comboboxOptions, search]);

  return (
    <Field>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <Combobox
        items={comboboxOptions}
        filteredItems={filteredOptions}
        inputValue={search}
        multiple
        value={selectedOptions}
        isItemEqualToValue={(item, value) => item.value === value.value}
        onInputValueChange={setSearch}
        onValueChange={(selected) => onValueChange(selected.map((option) => Number(option.value)))}
      >
        <ComboboxChips ref={anchor}>
          {selectedOptions.map((option) => (
            <ComboboxChip key={option.value}>{option.label}</ComboboxChip>
          ))}
          <ComboboxChipsInput id={id} placeholder={`Search ${label.toLowerCase()}…`} />
        </ComboboxChips>
        <ComboboxContent anchor={anchor}>
          <ComboboxEmpty>No values found.</ComboboxEmpty>
          <ComboboxList>
            {filteredOptions.map((option) => (
              <ComboboxItem key={option.value} value={option}>
                {option.label}
              </ComboboxItem>
            ))}
          </ComboboxList>
        </ComboboxContent>
      </Combobox>
    </Field>
  );
}

function LookupSkeletons() {
  return (
    <div className="space-y-4" aria-label="Loading advanced filter values">
      {Array.from({ length: 7 }, (_, index) => (
        <div className="space-y-2" key={index}>
          <Skeleton className="h-3 w-24" />
          <Skeleton className="h-8 w-full" />
        </div>
      ))}
    </div>
  );
}

export function AdvancedFiltersSheet({
  open,
  filters,
  lookups,
  lookupsError,
  lookupsLoading,
  onOpenChange,
  onFiltersChange,
  onRetryLookups,
}: AdvancedFiltersSheetProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="overflow-y-auto sm:max-w-md">
        <SheetHeader className="border-b">
          <SheetTitle>Advanced filters</SheetTitle>
          <SheetDescription>Changes apply immediately and are saved in the URL.</SheetDescription>
        </SheetHeader>
        <div className="flex-1 space-y-6 p-6">
          {lookupsLoading && <LookupSkeletons />}
          {!lookupsLoading && lookupsError && (
            <Item variant="outline">
              <ItemContent>
                <ItemTitle>Filter values could not be loaded</ItemTitle>
                <ItemDescription>{lookupsError}</ItemDescription>
              </ItemContent>
              <Button type="button" variant="outline" onClick={onRetryLookups}>
                <RefreshCw />
                Retry
              </Button>
            </Item>
          )}
          {!lookupsLoading && lookups && (
            <FieldGroup>
              <LookupMultiSelect
                id="game-types-filter"
                label="Game types"
                values={filters.gameTypeIds}
                options={lookups.gameTypes}
                onValueChange={(gameTypeIds) => onFiltersChange({ gameTypeIds })}
              />
              <LookupMultiSelect
                id="game-statuses-filter"
                label="Statuses"
                values={filters.gameStatusIds}
                options={lookups.gameStatuses}
                onValueChange={(gameStatusIds) => onFiltersChange({ gameStatusIds })}
              />
              <LookupMultiSelect
                id="genres-filter"
                label="Genres"
                values={filters.genreIds}
                options={lookups.genres}
                onValueChange={(genreIds) => onFiltersChange({ genreIds })}
              />
              <LookupMultiSelect
                id="themes-filter"
                label="Themes"
                values={filters.themeIds}
                options={lookups.themes}
                onValueChange={(themeIds) => onFiltersChange({ themeIds })}
              />
              <LookupMultiSelect
                id="game-modes-filter"
                label="Game modes"
                values={filters.gameModeIds}
                options={lookups.gameModes}
                onValueChange={(gameModeIds) => onFiltersChange({ gameModeIds })}
              />
              <LookupMultiSelect
                id="player-perspectives-filter"
                label="Player perspectives"
                values={filters.playerPerspectiveIds}
                options={lookups.playerPerspectives}
                onValueChange={(playerPerspectiveIds) => onFiltersChange({ playerPerspectiveIds })}
              />
              <LookupMultiSelect
                id="platforms-filter"
                label="Platforms"
                values={filters.platformIds}
                options={lookups.platforms}
                onValueChange={(platformIds) => onFiltersChange({ platformIds })}
              />
            </FieldGroup>
          )}
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="developer-filter">Developer</FieldLabel>
              <DebouncedFilterInput
                id="developer-filter"
                placeholder="Developer name…"
                value={filters.developer}
                onValueChange={(developer) => onFiltersChange({ developer })}
              />
            </Field>
            <Field>
              <FieldLabel htmlFor="publisher-filter">Publisher</FieldLabel>
              <DebouncedFilterInput
                id="publisher-filter"
                placeholder="Publisher name…"
                value={filters.publisher}
                onValueChange={(publisher) => onFiltersChange({ publisher })}
              />
            </Field>
            <Field>
              <FieldLabel htmlFor="collection-filter">Collection</FieldLabel>
              <DebouncedFilterInput
                id="collection-filter"
                placeholder="Collection name…"
                value={filters.collection}
                onValueChange={(collection) => onFiltersChange({ collection })}
              />
            </Field>
            <Field>
              <FieldLabel htmlFor="franchise-filter">Franchise</FieldLabel>
              <DebouncedFilterInput
                id="franchise-filter"
                placeholder="Franchise name…"
                value={filters.franchise}
                onValueChange={(franchise) => onFiltersChange({ franchise })}
              />
            </Field>
            <Field orientation="horizontal">
              <Switch
                id="steam-filter"
                checked={filters.isSteam}
                onCheckedChange={(isSteam) => onFiltersChange({ isSteam })}
              />
              <FieldLabel htmlFor="steam-filter">Available on Steam only</FieldLabel>
            </Field>
          </FieldGroup>
        </div>
        <SheetFooter className="sticky bottom-0 border-t bg-popover">
          <Button
            type="button"
            variant="outline"
            onClick={() => onFiltersChange(defaultAdvancedFilters)}
          >
            Clear advanced filters
          </Button>
          <Button type="button" onClick={() => onOpenChange(false)}>
            Close
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
