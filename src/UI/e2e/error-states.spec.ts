import { expect, test } from '@playwright/test';
import {
  fillActivePassengerForm,
  fillSearchForm,
  searchAndGoToResults,
  selectFlightByNumber,
} from './support/helpers';

/**
 * US-002 AC6/AC7 and US-006 AC6 — empty-result and backend-failure UI states.
 *
 * Empty state: reachable through a REAL search. The route-coverage fix
 * (RouteScheduleGenerator — PO defect fix, 2026-07-07) gives every ordered pair of the 6
 * supported airports flights, with one deliberate exception: MAN <-> SYD is the DEC-021
 * (PO 2026-07-08) no-direct-service route — no fixture and no generated entry in either
 * direction from either provider (true to the real world: no nonstop Manchester–Sydney
 * exists) — kept unserved precisely so the styled empty state stays demonstrable through a
 * real search (challenge PDF 3.2). The empty-state test below runs with NO interception.
 *
 * IMPORTANT — deliberate, documented use of `page.route(...).fulfill(...)` remains for the
 * two 5xx tests only: a real 500 is still not producible by any request reachable through the
 * UI (the known QA findings, QA-001/QA-002, both require a hand-crafted direct API request,
 * not a UI action). To still exercise the frontend's failure handling end-to-end (real
 * frontend, real router/state services, real DOM), those two tests substitute the HTTP
 * *response* for exactly the one call under test — they do not disable, stub, or bypass any
 * frontend code path. Every other spec file in this suite performs a full, uninterrupted real
 * round trip against the real backend.
 */
test.describe('Error and empty states (US-002 AC6/AC7, US-006 AC6)', () => {
  test('US-002 AC6: a real search for the no-direct-service route shows the empty-state message', async ({
    page,
  }) => {
    // No interception: MAN -> SYD is the deliberate DEC-021 no-direct-service route (no
    // fixture, no generated entry, both providers), so the real backend genuinely returns
    // 200 with zero results.
    const searchResponsePromise = page.waitForResponse(
      (res) => res.url().includes('/api/v1/search') && res.request().method() === 'POST',
    );
    await searchAndGoToResults(page, { origin: 'MAN', destination: 'SYD' });

    const searchResponse = await searchResponsePromise;
    expect(searchResponse.status()).toBe(200);
    expect(await searchResponse.json()).toEqual([]); // real, un-mocked empty response

    const emptyState = page.locator('.empty-state');
    await expect(emptyState).toBeVisible();
    await expect(emptyState).toContainText('No flights found for your search.');
    await expect(emptyState).toContainText('Try a different route, date, or cabin class.');
    // The empty state offers a way back to the form.
    await expect(emptyState.getByRole('link', { name: 'Modify search' })).toBeVisible();
    await expect(page.locator('li.result-card')).toHaveCount(0);
  });

  test('US-002 AC7: a search API failure shows a generic user-facing error, no backend detail leaked', async ({
    page,
  }) => {
    const leakedDetail = 'NullReferenceException: Object reference not set to an instance of an object.';
    await page.route('**/api/v1/search', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Internal Server Error', status: 500, detail: leakedDetail }),
      }),
    );

    await page.goto('/search');
    await fillSearchForm(page, { origin: 'LHR', destination: 'JFK' });
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
    await searchAndGoToResults(page, { origin: 'MAN', destination: 'LHR' });
    await selectFlightByNumber(page, 'GA412');

    // Fill passenger 1 in the in-place form; Confirm Booking validates + saves the filled
    // form (the card appears) and then submits — the single-button flow has no review step.
    await fillActivePassengerForm(page, 0, { fullName: 'Jane Doe', email: 'jane@example.com', documentNumber: 'AB-1234' });

    const leakedDetail = 'System.InvalidOperationException at BookingService.CreateBookingAsync';
    await page.route('**/api/v1/bookings', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Internal Server Error', status: 500, detail: leakedDetail }),
      }),
    );

    await page.locator('#confirm-booking-btn').click();

    const banner = page.locator('#generic-error-banner');
    await expect(banner).toHaveText("We couldn't complete your booking. Please try again.");
    // The generic failure banner receives focus (it carries tabindex="-1").
    await expect(banner).toBeFocused();
    await expect(page.locator('body')).not.toContainText(leakedDetail);
    await expect(page.locator('body')).not.toContainText('500');
    // Stays on /booking — a failed booking does not navigate to /confirmation, and the
    // passenger saved by the confirm attempt is still there as a card for the user to retry.
    await expect(page).toHaveURL(/\/booking$/);
    await expect(page.locator('li.passenger-card')).toHaveCount(1);
    await expect(page.locator('#confirm-booking-btn')).toBeEnabled(); // retry possible
  });
});
