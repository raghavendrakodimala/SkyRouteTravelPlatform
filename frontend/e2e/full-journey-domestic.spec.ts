import { expect, test } from '@playwright/test';
import { expectBookingReferenceFormat, fillPassenger, searchAndGoToResults, selectFlightByNumber } from './support/helpers';

/**
 * Full happy-path journey — domestic route, single passenger (US-001 through US-006).
 *
 * Real backend, real frontend, no mocking. Exercises against the fixed mock provider
 * schedule (ASM-006): GA412 (GlobalAir, MAN -> LHR, both United Kingdom -> Domestic route
 * type) is used as the selected flight because it is a known, worked domestic example
 * (feature-booking-flow.md Section 2.3.1). The values actually searched for (origin/
 * destination/date) do not filter the fixed result set per ASM-006 — this is documented,
 * approved MVP behavior, not a defect (see docs/features/feature-provider-aggregation.md
 * Section 3 and docs/testing/test-strategy.md).
 *
 * Also serves as a light smoke check for US-007 (provider extensibility/fault isolation):
 * a single merged response containing flights from both registered providers is itself proof
 * the aggregation layer queried both providers and merged their results, per BR-007/FR-049.
 * Full fault-isolation (one provider throwing) is already covered at unit/integration level
 * (test-strategy.md Section 2, US-007 row) and is not re-proven here.
 */
test.describe('Full booking journey — domestic, single passenger', () => {
  test('search -> results -> sort -> select -> passenger details -> confirm -> reference shown', async ({ page }) => {
    let searchRequestCount = 0;
    page.on('request', (req) => {
      if (req.method() === 'POST' && req.url().includes('/api/search')) {
        searchRequestCount += 1;
      }
    });

    // US-001: search form happy path.
    await searchAndGoToResults(page, { origin: 'LHR', destination: 'MAN', passengerCount: 1, cabinClass: 'Economy' });
    expect(searchRequestCount).toBe(1);

    // US-002: results display — all 8 fixed flights from both providers are present.
    const cards = page.locator('li.result-card');
    await expect(cards).toHaveCount(8);
    await expect(page.locator('li.result-card', { hasText: 'GlobalAir' }).first()).toBeVisible();
    await expect(page.locator('li.result-card', { hasText: 'BudgetWings' }).first()).toBeVisible();

    // US-003 AC5: default sort is price low-to-high (cheapest overall is BW241 at USD 54.00).
    await expect(cards.first()).toContainText('BW241');
    await expect(cards.nth(3)).toContainText('BW210');
    await expect(page.getByRole('button', { name: 'Price: low to high' })).toHaveAttribute('aria-pressed', 'true');

    // US-003 AC2/AC3: switching sort option re-orders (verified by a position that genuinely
    // differs between the two orderings, not just re-reading the same cheapest/shortest item
    // twice), the newly active option is visually/ARIA indicated, and no new /api/search call
    // is made for a pure client-side re-sort.
    const requestsBeforeSort = searchRequestCount;
    await page.getByRole('button', { name: 'Duration: shortest first' }).click();
    await expect(cards.first()).toContainText('BW241'); // also the shortest-duration flight (65m)
    await expect(cards.nth(3)).toContainText('GA309'); // differs from the price-sorted 4th card (BW210)
    await expect(page.getByRole('button', { name: 'Duration: shortest first' })).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByRole('button', { name: 'Price: low to high' })).toHaveAttribute('aria-pressed', 'false');
    expect(searchRequestCount).toBe(requestsBeforeSort);

    // US-004: select a domestic flight (GA412, MAN -> LHR) — carries state, no new API call.
    await selectFlightByNumber(page, 'GA412');
    expect(searchRequestCount).toBe(requestsBeforeSort);

    // US-004 AC3: price breakdown (GA412 Economy: base 80.00 -> pricePerPassenger 92.00).
    await expect(page.locator('.price-breakdown')).toContainText('USD 92.00 total');
    await expect(page.locator('.price-breakdown')).toContainText('USD 92.00 per person');

    // US-005 AC5: document field label is "National ID" for a domestic route.
    await expect(page.locator('label[for="documentNumber-0"]')).toHaveText('National ID');

    // US-005: fill the single passenger's details and submit.
    await fillPassenger(page, 0, { fullName: 'Jane Doe', email: 'jane@example.com', documentNumber: 'AB-1234' });
    await page.getByRole('button', { name: 'Confirm Booking' }).click();
    await page.waitForURL('**/confirmation');

    // US-006 AC4/AC5: booking reference format (SKY-DOM-XXXXXX), total price, passenger name.
    const reference = await page.locator('.booking-reference').innerText();
    expectBookingReferenceFormat(reference, 'DOM');
    await expect(page.locator('.total-price')).toContainText('USD 92.00 total');
    await expect(page.locator('.passengers')).toContainText('Jane Doe');

    // US-006 AC7: confirmation screen offers no resubmit control — only "Start a New Search".
    await expect(page.getByRole('button', { name: /confirm/i })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Start a New Search' })).toBeVisible();

    // Navigating back to /booking (selectedFlight still set) still cannot resubmit — the
    // state-layer guard (BookingStateService.submitBooking) and disabled submit button block it.
    await page.goBack();
    await page.waitForURL('**/booking');
    await expect(page.getByText('This booking has already been confirmed.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Confirm Booking' })).toBeDisabled();
  });
});
