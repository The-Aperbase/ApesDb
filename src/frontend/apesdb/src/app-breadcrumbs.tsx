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

type BreadcrumbDestination = "/games";

type BreadcrumbLabelSegment = {
  label: string;
  to?: BreadcrumbDestination;
};

type BreadcrumbParamSegment = {
  param: string;
};

type BreadcrumbSegment = BreadcrumbLabelSegment | BreadcrumbParamSegment;

type ResolvedBreadcrumb = {
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
        breadcrumbs.push({ label: value, truncate: true });
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

          return (
            <Fragment key={`${breadcrumb.label}-${index}`}>
              {index > 0 ? <BreadcrumbSeparator className="shrink-0" /> : null}
              <BreadcrumbItem className={itemClassName}>
                {isCurrent ? (
                  <BreadcrumbPage className={labelClassName} title={breadcrumb.label}>
                    {breadcrumb.label}
                  </BreadcrumbPage>
                ) : breadcrumb.to ? (
                  <BreadcrumbLink
                    className={labelClassName}
                    render={<Link activeOptions={{ exact: true }} to={breadcrumb.to} />}
                    title={breadcrumb.label}
                  >
                    {breadcrumb.label}
                  </BreadcrumbLink>
                ) : (
                  <span className={labelClassName} title={breadcrumb.label}>
                    {breadcrumb.label}
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
