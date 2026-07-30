import { createRoute, lazyRouteComponent } from "@tanstack/react-router";
import { appRoute } from "../app-shell/app-shell-routes";

const calendarRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "calendar",
  component: lazyRouteComponent(() => import("./calendar-page"), "CalendarPage"),
  staticData: {
    breadcrumbs: [{ label: "Calendar", to: "/calendar" }],
  },
});

export function addCalendarRoutes() {
  return calendarRoute;
}
