import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E configuration (Phase 13 extension — automated E2E, superseding the
 * manual-only approach in docs/testing/test-strategy.md Section 1.4, per explicit Human PO
 * approval 2026-07-06 resolving QA-STRAT-OQ-002).
 *
 * This config deliberately does NOT use Playwright's `webServer` auto-start feature: the
 * suite exercises a real ASP.NET Core backend (`dotnet run`, port 5094) alongside the real
 * Angular dev server (`ng serve`, port 4200), and both are started/stopped manually by the
 * operator around a `npx playwright test` run (see docs/handoffs for the exact commands used).
 * `baseURL` below assumes both are already running when the suite executes.
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: {
    timeout: 5_000,
  },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
