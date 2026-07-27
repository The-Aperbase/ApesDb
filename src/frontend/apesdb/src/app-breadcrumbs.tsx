import { Fragment } from "react";
import { Link, useMatches } from "@tanstack/react-router";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@apesdb/ui";
import { HomeIcon } from "lucide-react";
import { useBoardDetails } from "./features/boards/get-board-by-id/use-board-details";
import { useGameDetails } from "./features/games/get-game-by-id/use-game-details";

type BreadcrumbDestination = "/" | "/boards" | "/calendar" | "/games";
type BreadcrumbIcon = "home";

type BreadcrumbLabelSegment = {
  icon?: BreadcrumbIcon;
  label: string;
  to?: BreadcrumbDestination;
};

type BreadcrumbParamSegment = {
  param: string;
};

type BreadcrumbSegment = BreadcrumbLabelSegment | BreadcrumbParamSegment;

type ResolvedBreadcrumb = {
  icon?: BreadcrumbIcon;
  label: string;
  to?: BreadcrumbDestination;
  truncate?: boolean;
};

declare module "@tanstack/react-router" {
  interface StaticDataRouteOption {
    breadcrumbs?: BreadcrumbSegment[];
  }
}

export function AppBreadcrumbs() {
  const matches = useMatches();
  const boardMatch = matches.find((match) => match.routeId === "/_app/boards/$boardId");
  const matchedBoardId = boardMatch?.params.boardId;
  const boardId = typeof matchedBoardId === "string" ? matchedBoardId : "";
  const boardDetails = useBoardDetails(boardId);
  const gameMatch = matches.find((match) => match.routeId === "/_app/games/$gameId");
  const matchedGameId = gameMatch?.params.gameId;
  const gameId = typeof matchedGameId === "string" ? Number(matchedGameId) : 0;
  const gameDetails = useGameDetails(gameId);
  const breadcrumbs: ResolvedBreadcrumb[] = [];

  for (const match of matches) {
    const segments = match.staticData.breadcrumbs ?? [];

    for (const segment of segments) {
      if ("label" in segment) {
        breadcrumbs.push(segment);
        continue;
      }

      const params = match.params as Record<string, unknown>;
      const value = params[segment.param];

      if (typeof value === "string") {
        let label = value;

        if (segment.param === "boardId" && boardDetails.data !== null) {
          label = boardDetails.data.name;
        }

        if (segment.param === "gameId" && gameDetails.data !== null) {
          label = gameDetails.data.name;
        }

        breadcrumbs.push({ label, truncate: true });
      }
    }
  }

  return (
    <Breadcrumb className="min-w-0 overflow-hidden">
      <BreadcrumbList className="flex-nowrap overflow-hidden">
        {breadcrumbs.map((breadcrumb, index) => {
          const isCurrent = index === breadcrumbs.length - 1;
          let itemClassName = "shrink-0";
          let labelClassName = "inline-flex items-center whitespace-nowrap";

          if (breadcrumb.truncate) {
            itemClassName = "min-w-0";
            labelClassName = "block max-w-24 truncate sm:max-w-72";
          }

          let content = <>{breadcrumb.label}</>;
          if (breadcrumb.icon === "home") {
            content = (
              <>
                <HomeIcon aria-hidden className="size-4" />
                <span className="sr-only">{breadcrumb.label}</span>
              </>
            );
          }

          return (
            <Fragment key={`${breadcrumb.label}-${index}`}>
              {index > 0 ? <BreadcrumbSeparator className="shrink-0" /> : null}
              <BreadcrumbItem className={itemClassName}>
                {isCurrent ? (
                  <BreadcrumbPage className={labelClassName} title={breadcrumb.label}>
                    {content}
                  </BreadcrumbPage>
                ) : breadcrumb.to ? (
                  <BreadcrumbLink
                    className={labelClassName}
                    render={<Link activeOptions={{ exact: true }} to={breadcrumb.to} />}
                    title={breadcrumb.label}
                  >
                    {content}
                  </BreadcrumbLink>
                ) : (
                  <span className={labelClassName} title={breadcrumb.label}>
                    {content}
                  </span>
                )}
              </BreadcrumbItem>
            </Fragment>
          );
        })}
      </BreadcrumbList>
    </Breadcrumb>
  );
}
