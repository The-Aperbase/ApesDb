import {
  createRootRouteWithContext,
  createRoute,
  lazyRouteComponent,
  redirect,
} from "@tanstack/react-router";
import type { AnyRoute } from "@tanstack/react-router";
import type { AuthContextValue } from "../../auth-context";
import { RootLayout } from "./root-layout";

export const rootRoute = createRootRouteWithContext<{
  auth: AuthContextValue;
}>()({
  component: RootLayout,
});

export const appRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "_app",
  component: lazyRouteComponent(() => import("./app-layout"), "AppLayout"),
  staticData: {
    breadcrumbs: [{ icon: "home", label: "Home", to: "/" }],
  },
  beforeLoad: ({ context, location }) => {
    if (!context.auth.isAuthenticated) {
      throw redirect({
        to: "/login",
        search: {
          redirect: location.href,
        },
        replace: true,
      });
    }
  },
});

export function addAppShellRoutes<
  const TAuthenticatedRoutes extends AnyRoute[],
  const TPublicRoutes extends AnyRoute[],
>(authenticatedRoutes: TAuthenticatedRoutes, publicRoutes: TPublicRoutes) {
  return rootRoute.addChildren([appRoute.addChildren(authenticatedRoutes), ...publicRoutes]);
}
