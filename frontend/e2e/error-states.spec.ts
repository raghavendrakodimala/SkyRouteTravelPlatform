import { expect, test } from '@playwright/test';
import { fillPassenger, searchAndGoToResults, selectFlightByNumber } from './support/helpers';

/**
 * US-002 AC6/AC7 and US-006 AC6 — empty-result and backend-failure UI states.
 *
 * IMPORTANT — deliberate, documented use of `page.route(...).fulfill(...)` in this file only:
 * both mock providers (GlobalAirProvider/BudgetWingsProvider) return their fixed schedule
 * regardless of the requested route/date (ASM-006), and the search endpoint applies no
 * route/date filtering (SearchController -> FlightAggregatorService). A genuinely empty
 * `[]` response therefore cannot be produced by any *valid* search request against the real
 * backend as currently implemented — feature-flight-search.md Section 5.2 itself documents
 * this exact fact ("in the current fixed-mock-data design, never in practice"). A real 500 is
 * similarly not producible by any request reachable through the UI (the two known QA
 * findings, QA-001/QA-002, both require a hand-crafted direct API request, not a UI action).
 * To still exercise the frontend's already-implemented handling of these two response shapes
 * end-to-end (real frontend, real router/state services, real DOM), this file substitutes the
 * HTTP *response* for exactly the one call under test via Playwright route interception —
 * it does not disable, stub, or bypass any frontend code path. Every other spec file in this
 * suite performs a full, uninterrupted real round trip against the real backend.
 */
test.describe('Error and empty states (US-002 AC6/AC7, US-006 AC6)', () => {
  test('US-002 AC6: a 200 response with zero results shows the empty-state message', async ({ page }) => {
    await page.route('**/api/search', (route) => route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }));

    await page.goto('/search');
    await page.locator('#origin').selectOption('LHR');
    await page.locator('#destination').selectOption('JFK');
    await page.locator('#departureDate').fill(new Date(Date.now() + 86_400_000).toISOString().slice(0, 10));
    await page.getByRole('button', { name: 'Search' }).click();
    await page.waitForURL('**/results');

    await expect(page.getByText('No flights found for your search. Please try different criteria.')).toBeVisible();
    await expect(page.locator('li.result-card')).toHaveCount(0);
  });

  test('US-002 AC7: a search API failure shows a generic user-facing error, no backend detail leaked', async ({
    page,
  }) => {
    const leakedDetail = 'NullReferenceException: Object reference not set to an instance of an object.';
    await page.route('**/api/search', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Internal Server Error', status: 500, detail: leakedDetail }),
      }),
    );

    await page.goto('/search');
    await page.locator('#origin').selectOption('LHR');
    await page.locator('#destination').selectOption('JFK');
    await page.locator('#departureDate').fill(new Date(Date.now() + 86_400_000).toISOString().slice(0, 10));
    await page.getByRole('button', { name: 'Search' }).click();

    const banner = page.locator('.banner-error');
    await expect(banner).toHaveText("We couldn't complete your search. Please try again.");
    await expect(page.locator('body')).not.toContainText(leakedDetail);
    await expect(page.locator('body')).not.toContainText('500');
    // Stays on /search — a failed search does not navigate to /results.
    await expect(page).toHaveURL(/\/search$/);
  });

  test('US-006 AC6: a booking API failure shows a generic user-facing error, no backend detail leaked', async ({
    page,
  }) => {
    await searchAndGoToResults(page, { origin: 'LHR', destination: 'MAN', passengerCount: 1 });
    await selectFlightByNumber(page, 'GA412');
    await fillPassenger(page, 0, { fullName: 'Jane Doe', email: 'jane@example.com', documentNumber: 'AB-1234' });

    const leakedDetail = 'System.InvalidOperationException at BookingService.CreateBookingAsync';
    await page.route('**/api/bookings', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Internal Server Error', status: 500, detail: leakedDetail }),
      }),
    );

    await page.getByRole('button', { name: 'Confirm Booking' }).click();

    const banner = page.locator('.banner-error');
    await expect(banner).toHaveText("We couldn't complete your booking. Please try again.");
    await expect(page.locator('body')).not.toContainText(leakedDetail);
    await expect(page.locator('body')).not.toContainText('500');
    // Stays on /booking — a failed booking does not navigate to /confirmation.
    await expect(page).toHaveURL(/\/booking$/);
  });
});
