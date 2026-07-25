import * as React from "react";
import type { Pageable } from "@apesdb/common";
import {
  flexRender,
  functionalUpdate,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
  type PaginationState,
  type RowData,
} from "@tanstack/react-table";
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  LayoutGrid,
  List,
  RefreshCw,
  type LucideIcon,
} from "lucide-react";

import { Button } from "./button";
import { cn } from "./utils";
import { Item, ItemContent, ItemDescription, ItemTitle } from "./item";
import { Pagination, PaginationContent, PaginationItem } from "./pagination";
import { Skeleton } from "./skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "./table";

export type DataViewMode = "rows" | "grid";

export type PageableDataViewColumnMeta = {
  className?: string;
  skeleton?: React.ReactNode | ((rowIndex: number) => React.ReactNode);
};

declare module "@tanstack/react-table" {
  interface ColumnMeta<TData extends RowData, TValue> extends PageableDataViewColumnMeta {}
}

export type PageableDataViewProps<TData> = {
  columns: ColumnDef<TData>[];
  data: Pageable<TData> | null;
  emptyDescription?: React.ReactNode;
  emptyTitle?: React.ReactNode;
  error: string | null;
  getRowId?: (item: TData, index: number) => string;
  gridClassName?: string;
  hasFilters: boolean;
  header?: React.ReactNode;
  isLoading: boolean;
  itemLabel: string;
  mode: DataViewMode;
  requestedPage: number;
  requestedPageSize: number;
  renderGridItem: (item: TData) => React.ReactNode;
  renderGridSkeleton: (index: number) => React.ReactNode;
  onModeChange: (mode: DataViewMode) => void;
  onPageChange: (page: number) => void;
  onRetry: () => void;
};

type DataViewModeSelectorProps = React.ComponentProps<"div"> & {
  buttonClassName?: string;
  buttonSize?: React.ComponentProps<typeof Button>["size"];
  mode: DataViewMode;
  onModeChange: (mode: DataViewMode) => void;
  showLabels?: boolean;
};

const dataViewModeOptions = [
  { value: "rows", label: "Rows", Icon: List },
  { value: "grid", label: "Grid", Icon: LayoutGrid },
] satisfies ReadonlyArray<{
  value: DataViewMode;
  label: string;
  Icon: LucideIcon;
}>;

export function DataViewModeSelector({
  buttonClassName,
  buttonSize = "sm",
  className,
  mode,
  onModeChange,
  showLabels = true,
  ...props
}: DataViewModeSelectorProps) {
  return (
    <div
      className={cn(
        "flex items-center gap-1 rounded-sm border border-border bg-muted p-1",
        className,
      )}
      {...props}
    >
      {dataViewModeOptions.map(({ value, label, Icon }) => (
        <Button
          aria-label={showLabels ? undefined : `Show ${label.toLowerCase()}`}
          aria-pressed={mode === value}
          className={buttonClassName}
          key={value}
          onClick={() => onModeChange(value)}
          size={buttonSize}
          type="button"
          variant={mode === value ? "default" : "ghost"}
        >
          <Icon data-icon={showLabels ? "inline-start" : undefined} />
          {showLabels && label}
        </Button>
      ))}
    </div>
  );
}

function renderSkeleton(skeleton: PageableDataViewColumnMeta["skeleton"], rowIndex: number) {
  if (typeof skeleton === "function") {
    return skeleton(rowIndex);
  }

  if (skeleton) {
    return skeleton;
  }

  return <Skeleton className="h-4 w-24" />;
}

function resultSummary<TData>(
  data: Pageable<TData>,
  hasFilters: boolean,
  itemLabel: string,
): string {
  if (data.filteredTotal === 0) {
    if (hasFilters) {
      return `Showing 0 matches, from ${data.total.toLocaleString()} total.`;
    }

    return `Showing 0 ${itemLabel}.`;
  }

  const start = (data.page - 1) * data.pageSize + 1;
  const end = Math.min(data.page * data.pageSize, data.filteredTotal);
  if (hasFilters) {
    return `Showing ${start.toLocaleString()}–${end.toLocaleString()} of ${data.filteredTotal.toLocaleString()} matches, from ${data.total.toLocaleString()} total.`;
  }

  return `Showing ${start.toLocaleString()}–${end.toLocaleString()} of ${data.total.toLocaleString()} ${itemLabel}.`;
}

export function PageableDataView<TData>({
  columns,
  data,
  emptyDescription = "Try changing or clearing the active filters.",
  emptyTitle,
  error,
  getRowId,
  gridClassName,
  hasFilters,
  header,
  isLoading,
  itemLabel,
  mode,
  requestedPage,
  requestedPageSize,
  renderGridItem,
  renderGridSkeleton,
  onModeChange,
  onPageChange,
  onRetry,
}: PageableDataViewProps<TData>) {
  const pageSize = data?.pageSize ?? requestedPageSize;
  const pageCount = Math.max(1, Math.ceil((data?.filteredTotal ?? 0) / pageSize));
  const pagination = React.useMemo<PaginationState>(
    () => ({ pageIndex: Math.max(0, requestedPage - 1), pageSize }),
    [pageSize, requestedPage],
  );
  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
    getRowId,
    manualPagination: true,
    rowCount: data?.filteredTotal ?? 0,
    pageCount,
    state: { pagination },
    onPaginationChange: (updater) => {
      const next = functionalUpdate(updater, pagination);
      onPageChange(next.pageIndex + 1);
    },
  });

  let content: React.ReactNode;
  if (isLoading) {
    if (mode === "grid") {
      content = (
        <div aria-hidden="true" className="min-h-0 flex-1 overflow-auto border p-3">
          <div
            className={cn(
              "grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6",
              gridClassName,
            )}
          >
            {Array.from({ length: requestedPageSize }, (_, index) => (
              <React.Fragment key={index}>{renderGridSkeleton(index)}</React.Fragment>
            ))}
          </div>
        </div>
      );
    } else {
      content = (
        <div aria-hidden="true" className="min-h-0 flex-1 overflow-hidden border">
          <Table containerClassName="h-full overflow-auto">
            <TableHeader className="sticky top-0 z-10 bg-background">
              {table.getHeaderGroups().map((headerGroup) => (
                <TableRow key={headerGroup.id}>
                  {headerGroup.headers.map((tableHeader) => (
                    <TableHead
                      className={tableHeader.column.columnDef.meta?.className}
                      key={tableHeader.id}
                    >
                      {tableHeader.isPlaceholder
                        ? null
                        : flexRender(tableHeader.column.columnDef.header, tableHeader.getContext())}
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {Array.from({ length: requestedPageSize }, (_, rowIndex) => (
                <TableRow key={rowIndex}>
                  {table.getAllLeafColumns().map((column) => (
                    <TableCell className={column.columnDef.meta?.className} key={column.id}>
                      {renderSkeleton(column.columnDef.meta?.skeleton, rowIndex)}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      );
    }
  } else if (error) {
    content = (
      <Item className="min-h-0 flex-1" variant="outline">
        <ItemContent>
          <ItemTitle>
            {itemLabel[0]?.toUpperCase()}
            {itemLabel.slice(1)} could not be loaded
          </ItemTitle>
          <ItemDescription>{error}</ItemDescription>
        </ItemContent>
        <Button onClick={onRetry} type="button" variant="outline">
          <RefreshCw />
          Retry
        </Button>
      </Item>
    );
  } else if (!data) {
    content = <div className="min-h-0 flex-1" />;
  } else if (data.items.length === 0) {
    content = (
      <Item className="min-h-0 flex-1 justify-center text-center" variant="outline">
        <ItemContent className="items-center">
          <ItemTitle>{emptyTitle ?? `No ${itemLabel} found`}</ItemTitle>
          <ItemDescription>{emptyDescription}</ItemDescription>
        </ItemContent>
      </Item>
    );
  } else if (mode === "grid") {
    content = (
      <div className="min-h-0 flex-1 overflow-auto border p-3">
        <div
          className={cn(
            "grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6",
            gridClassName,
          )}
        >
          {data.items.map((item, index) => (
            <React.Fragment key={getRowId?.(item, index) ?? index}>
              {renderGridItem(item)}
            </React.Fragment>
          ))}
        </div>
      </div>
    );
  } else {
    content = (
      <div className="min-h-0 flex-1 overflow-hidden border">
        <Table containerClassName="h-full overflow-auto">
          <TableHeader className="sticky top-0 z-10 bg-background">
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((tableHeader) => (
                  <TableHead
                    className={tableHeader.column.columnDef.meta?.className}
                    key={tableHeader.id}
                  >
                    {tableHeader.isPlaceholder
                      ? null
                      : flexRender(tableHeader.column.columnDef.header, tableHeader.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows.map((row) => (
              <TableRow key={row.id}>
                {row.getVisibleCells().map((cell) => (
                  <TableCell className={cell.column.columnDef.meta?.className} key={cell.id}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    );
  }

  const paginationDisabled = isLoading || error !== null || data === null;

  return (
    <div aria-busy={isLoading} className="flex min-h-0 flex-1 flex-col gap-3 overflow-hidden">
      {header}
      <div className="flex min-h-8 shrink-0 items-center justify-between gap-3">
        <div className="min-h-4 min-w-0 flex-1 text-xs text-muted-foreground" aria-live="polite">
          {data ? (
            <p>{resultSummary(data, hasFilters, itemLabel)}</p>
          ) : isLoading ? (
            <Skeleton aria-hidden="true" className="h-4 w-72 max-w-full" />
          ) : null}
        </div>
        <DataViewModeSelector
          buttonSize="icon-sm"
          className="shrink-0"
          mode={mode}
          onModeChange={onModeChange}
          showLabels={false}
        />
      </div>
      {content}
      <div className="flex shrink-0 items-center justify-between gap-2">
        <span className="whitespace-nowrap text-xs text-muted-foreground">
          Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
        </span>
        <Pagination className="mx-0 w-auto justify-end">
          <PaginationContent>
            <PaginationItem>
              <Button
                aria-label="Go to first page"
                disabled={paginationDisabled || !table.getCanPreviousPage()}
                onClick={() => table.firstPage()}
                size="icon"
                type="button"
                variant="outline"
              >
                <ChevronsLeft />
              </Button>
            </PaginationItem>
            <PaginationItem>
              <Button
                aria-label="Go to previous page"
                disabled={paginationDisabled || !table.getCanPreviousPage()}
                onClick={() => table.previousPage()}
                size="icon"
                type="button"
                variant="outline"
              >
                <ChevronLeft />
              </Button>
            </PaginationItem>
            <PaginationItem>
              <Button
                aria-label="Go to next page"
                disabled={paginationDisabled || !table.getCanNextPage()}
                onClick={() => table.nextPage()}
                size="icon"
                type="button"
                variant="outline"
              >
                <ChevronRight />
              </Button>
            </PaginationItem>
            <PaginationItem>
              <Button
                aria-label="Go to last page"
                disabled={paginationDisabled || !table.getCanNextPage()}
                onClick={() => table.lastPage()}
                size="icon"
                type="button"
                variant="outline"
              >
                <ChevronsRight />
              </Button>
            </PaginationItem>
          </PaginationContent>
        </Pagination>
      </div>
    </div>
  );
}
