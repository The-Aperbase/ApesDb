import { createRoute, lazyRouteComponent } from "@tanstack/react-router";
import { appRoute } from "../app-shell/app-shell-routes";

const boardsRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "boards",
  staticData: {
    breadcrumbs: [{ label: "Boards", to: "/boards" }],
  },
});

const boardsIndexRoute = createRoute({
  getParentRoute: () => boardsRoute,
  path: "/",
  component: lazyRouteComponent(() => import("./get-boards/boards-page"), "BoardsPage"),
});

const boardDetailsRoute = createRoute({
  getParentRoute: () => boardsRoute,
  path: "$boardId",
  component: lazyRouteComponent(
    () => import("./get-board-by-id/board-details-page"),
    "BoardDetailsPage",
  ),
  staticData: {
    breadcrumbs: [{ param: "boardId" }],
  },
});

export function addBoardsRoutes() {
  return boardsRoute.addChildren([boardsIndexRoute, boardDetailsRoute]);
}
