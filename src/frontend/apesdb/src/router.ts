import { createRouter, defaultParseSearch } from "@tanstack/react-router";
import { addAppShellRoutes } from "./features/app-shell/app-shell-routes";
import { addAuthRoutes } from "./features/auth/auth-routes";
import { addGamesRoutes } from "./features/games/games-routes";
import { addHomeRoutes } from "./features/home/home-routes";
import { addPrivacyRoutes } from "./features/privacy/privacy-routes";

const routeTree = addAppShellRoutes(
  [addHomeRoutes(), addGamesRoutes()],
  [addAuthRoutes(), addPrivacyRoutes()],
);

export const router = createRouter({
  routeTree,
  parseSearch: (search) => defaultParseSearch(search.replaceAll("+", "%20")),
  context: {
    auth: undefined!,
  },
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
