import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { ThemeProvider, Toaster } from "@apesdb/ui";
import { AuthGate, AuthProvider } from "./auth-context";
import { registerPwaUpdateListener } from "./register-pwa-update-listener";
import { router } from "./router";
import { registerTelemetry } from "./telemetry";
import "./styles.css";

registerTelemetry();
registerPwaUpdateListener();

const queryClient = new QueryClient();

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Root element #root was not found.");
}

createRoot(rootElement).render(
  <StrictMode>
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <AuthGate>{(auth) => <RouterProvider router={router} context={{ auth }} />}</AuthGate>
        </AuthProvider>
      </QueryClientProvider>
      <Toaster />
    </ThemeProvider>
  </StrictMode>,
);
