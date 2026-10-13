import { defineConfig } from "@playwright/test";

const FRONTEND = 3100;
const BACKEND = 5071;
const MOCKS = 5099;
const ci = Boolean(process.env.CI);

/**
 * End-to-end tests run the real backend (against the PostgreSQL in ConnectionStrings__DefaultConnection)
 * and the real production build of the frontend. The two third-party services, Groq and the exchange-rate
 * API, are replaced by a local mock (e2e/mock-services.mjs): no test ever calls the internet.
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 90_000,
  expect: { timeout: 15_000 },
  reporter: [["list"]],
  use: { baseURL: `http://localhost:${FRONTEND}`, trace: "retain-on-failure", screenshot: "only-on-failure" },
  projects: [
    { name: "desktop", use: { browserName: "chromium", viewport: { width: 1280, height: 900 } } },
    { name: "mobile", use: { browserName: "chromium", viewport: { width: 375, height: 812 }, isMobile: true, hasTouch: true } },
  ],
  webServer: [
    {
      command: "node e2e/mock-services.mjs",
      url: `http://localhost:${MOCKS}/health`,
      reuseExistingServer: !ci,
    },
    {
      command: `dotnet run --project ../JobHuntlyBackend/JobHuntly.Api --no-launch-profile --urls http://localhost:${BACKEND}`,
      url: `http://localhost:${BACKEND}/health`,
      timeout: 180_000,
      reuseExistingServer: !ci,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        Groq__ApiKey: "e2e-mock-key",
        Groq__Endpoint: `http://localhost:${MOCKS}/openai/v1/chat/completions`,
        Rates__Endpoint: `http://localhost:${MOCKS}/v6/latest/USD`,
        RateLimit__AuthPerMinute: "10000",
        RateLimit__DemoPerHour: "10000",
      },
    },
    {
      command: `npm run build && npm run start -- -p ${FRONTEND}`,
      url: `http://localhost:${FRONTEND}`,
      timeout: 300_000,
      reuseExistingServer: !ci,
      env: { BACKEND_URL: `http://localhost:${BACKEND}` },
    },
  ],
});
