import { defineConfig } from "vitest/config";

// Unit tests live next to the code (src/**/*.test.ts). The Playwright specs in e2e/ are run by Playwright.
export default defineConfig({
  test: { include: ["src/**/*.test.ts"] },
});
