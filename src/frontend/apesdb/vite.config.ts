import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath, URL } from "node:url";
import { defineConfig, type HtmlTagDescriptor } from "vite";
import { VitePWA } from "vite-plugin-pwa";

const appName = "ApesDb";
const darkThemeColor = "#171717";
const pwaDevTempDir = join(tmpdir(), "apesdb-pwa-dev");

function createFaviconTags(isLocalServe: boolean): HtmlTagDescriptor[] {
  if (isLocalServe) {
    return [
      {
        tag: "link",
        attrs: {
          rel: "icon",
          type: "image/png",
          sizes: "32x32",
          href: "/dev-32x32.png",
        },
        injectTo: "head",
      },
    ];
  }

  return [
    {
      tag: "link",
      attrs: {
        rel: "icon",
        type: "image/png",
        sizes: "32x32",
        href: "/light-32x32.png",
        media: "(prefers-color-scheme: light)",
      },
      injectTo: "head",
    },
    {
      tag: "link",
      attrs: {
        rel: "icon",
        type: "image/png",
        sizes: "32x32",
        href: "/32x32.png",
        media: "(prefers-color-scheme: dark)",
      },
      injectTo: "head",
    },
    {
      tag: "link",
      attrs: {
        rel: "icon",
        type: "image/png",
        sizes: "32x32",
        href: "/32x32.png",
      },
      injectTo: "head",
    },
  ];
}

function createManifestIcons(isLocalServe: boolean) {
  if (isLocalServe) {
    return [
      {
        src: "/dev-192x192.png",
        sizes: "192x192",
        type: "image/png",
      },
      {
        src: "/dev-512x512.png",
        sizes: "512x512",
        type: "image/png",
      },
      {
        src: "/dev-maskable-icon-512x512.png",
        sizes: "512x512",
        type: "image/png",
        purpose: "maskable",
      },
    ];
  }

  return [
    {
      src: "/192x192.png",
      sizes: "192x192",
      type: "image/png",
    },
    {
      src: "/512x512.png",
      sizes: "512x512",
      type: "image/png",
    },
    {
      src: "/maskable-icon-512x512.png",
      sizes: "512x512",
      type: "image/png",
      purpose: "maskable",
    },
  ];
}

function createIncludeAssets(isLocalServe: boolean) {
  if (isLocalServe) {
    return ["dev-32x32.png"];
  }

  return ["32x32.png", "light-32x32.png"];
}

export default defineConfig(({ command }) => {
  const isLocalServe = command === "serve";
  const isPwaDevEnabled = isLocalServe && process.env.APESDB_PWA_DEV === "true";

  return {
    plugins: [
      {
        name: "apesdb-favicons",
        transformIndexHtml() {
          return createFaviconTags(isLocalServe);
        },
      },
      react(),
      tailwindcss(),
      VitePWA({
        registerType: "prompt",
        includeAssets: createIncludeAssets(isLocalServe),
        devOptions: {
          enabled: isPwaDevEnabled,
          resolveTempFolder: () => pwaDevTempDir,
          type: "module",
        },
        manifest: {
          name: appName,
          short_name: appName,
          description: "ApesDb",
          display: "standalone",
          scope: "/",
          start_url: "/",
          background_color: darkThemeColor,
          theme_color: darkThemeColor,
          icons: createManifestIcons(isLocalServe),
        },
        workbox: {
          cleanupOutdatedCaches: true,
          globIgnores: ["**/*-login-banner.png"], // cache limit is 2mb these are bigger get them from server its cheaper.
          globPatterns: isLocalServe ? [] : ["**/*.{js,css,html,png,svg,woff2}"],
          navigateFallback: "/index.html",
          navigateFallbackDenylist: [/^\/api\//],
        },
      }),
    ],
    resolve: {
      alias: {
        "@apesdb/common": fileURLToPath(new URL("../common/src/index.ts", import.meta.url)),
        "@apesdb/ui/lib": fileURLToPath(new URL("../ui/src/lib", import.meta.url)),
        "@apesdb/ui/hooks": fileURLToPath(new URL("../ui/src/hooks", import.meta.url)),
        "@apesdb/ui": fileURLToPath(new URL("../ui/src/index.ts", import.meta.url)),
      },
    },
    root: __dirname,
    build: {
      outDir: "dist",
      emptyOutDir: true,
    },
  };
});
