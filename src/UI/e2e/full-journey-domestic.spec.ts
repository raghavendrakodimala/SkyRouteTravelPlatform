import { expect, test } from '@playwright/test';
import {
  expectBookingReferenceFormat,
  fillActivePassengerForm,
  confirmBooking,
  searchAndGoToResults,
  selectFlightByNumber,
} from './support/helpers';

/**
 * Full happy-path journey — domestic route, single passenger (US-001 through US-006).
 *
 * Real backend, real frontend, no mocking. Route filtering (ProviderScheduleMapper.BuildResults):
 * providers return only schedule entries matching the requested origin/destination.
 * MAN -> LHR is the one domestic route with fixtures: GA412 (GlobalAir, dep 06:45, 70m,
 * Economy per-person 92.00 = base 80.00 × 1.15, BR-001) and BW241 (BudgetWings, dep 14:00,
 * 65m, Economy per-person 54.00 = base 60.00 × 0.90, BR-002) — both United Kingdom, so
 * Domestic route type.
 *
 * Also serves as a light smoke check for US-007 (provider extensibility/fault isolation):
 * a single merged response containing one matching flight from each registered provider is
 * itself proof the aggregation layer queried both providers, filtered each schedule to the
 * requested route, and merged their results (BR-007/FR-049). Full fault-isolation (one
 * provider throwing) is covered at unit/integration level and is not re-proven here.
 *
 * Search carries no passenger count (PO 2026-07-07: passengerCount is always 1 at search;
 * results totals equal per-person price). The booking screen is the single-button in-place
 * flow: for one passenger, fill the form and click Confirm Booking directly — the filled
 * form is validated, saved, and submitted in one action.
 */
test.describe('Full booking journey — domestic, single passenger', () => {
  test('search -> results -> sort -> select -> fill passenger -> confirm -> reference shown', async ({
    page,
  }) => {
    let searchRequestCount = 0;
    page.on('request', (req) => {
      if (req.method() === 'POST' && req.url().includes('/api/v1/search')) {
        searchRequestCount += 1;
      }
    });

    // US-001: search form happy path (origin, destination, date, cabin — nothing else).
    await searchAndGoToResults(page, { origin: 'MAN', destination: 'LHR', cabinClass: 'Economy' });
    expect(searchRequestCount).toBe(1);

    // Recap header + Modify search link on the results screen.
    await expect(page.locator('.search-recap')).toContainText('Manchester (MAN) → London (LHR)');
    await expect(page.locator('.search-recap')).toContainText('Economy');
    await expect(page.locator('.search-recap .result-count')).toHaveText('· 2 flight(s)');
    await expect(page.locator('.page-head').getByRole('link', { name: 'Modify search' })).toBeVisible();

    // US-002: results display — exactly the two MAN -> LHR fixtures, one from each provider;
    // flights on other routes (e.g. GA101, LHR -> JFK) are filtered out by the backend.
    const cards = page.locator('li.result-card');
    await expect(cards).toHaveCount(2);
    await expect(page.locator('li.result-card', { hasText: 'GlobalAir' })).toHaveCount(1);
    await expect(page.locator('li.result-card', { hasText: 'BudgetWings' })).toHaveCount(1);
    await expect(page.locator('li.result-card', { hasText: 'GA101' })).toHaveCount(0);

    // Provider identity badge is rendered per card (initials, e.g. GlobalAir -> "GA").
    await expect(page.locator('li.result-card', { hasText: 'GlobalAir' }).locator('.provider-badge')).toHaveText('GA');
    await expect(page.locator('li.result-card', { hasText: 'BudgetWings' }).locator('.provider-badge')).toHaveText('BW');

    // Search always ran for 1 passenger, so total = per-person on every card.
    const bw241 = page.locator('li.result-card', { hasText: 'BW241' });
    await expect(bw241.locator('.total-price')).toHaveText('USD 54.00 total');
    await expect(bw241.locator('.per-person-price')).toHaveText('/ USD 54.00 per person');

    // US-003 AC5: default sort is price low-to-high (BW241 at USD 54.00 beats GA412 at 92.00).
    await expect(cards.first()).toContainText('BW241');
    await expect(cards.nth(1)).toContainText('GA412');
    await expect(page.getByRole('button', { name: 'Price: low to high' })).toHaveAttribute('aria-pressed', 'true');

    // US-003 AC2/AC3: switching sort option re-orders. On this route the cheaper flight
    // (BW241) is also the shorter one, so the duration sort cannot prove re-ordering —
    // departure time genuinely differs: GA412 departs 06:45, before BW241's 14:00. The newly
    // active option is visually/ARIA indicated, and no new /api/v1/search call is made for a
    // pure client-side re-sort.
    const requestsBeforeSort = searchRequestCount;
    await page.getByRole('button', { name: 'Departure time: earliest first' }).click();
    await expect(cards.first()).toContainText('GA412'); // 06:45 — order genuinely flipped
    await expect(cards.nth(1)).toContainText('BW241'); // 14:00
    await expect(page.getByRole('button', { name: 'Departure time: earliest first' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    await expect(page.getByRole('button', { name: 'Price: low to high' })).toHaveAttribute('aria-pressed', 'false');
    expect(searchRequestCount).toBe(requestsBeforeSort);

    // US-004: select the domestic flight GA412 — carries state, no new API call.
    await selectFlightByNumber(page, 'GA412');
    expect(searchRequestCount).toBe(requestsBeforeSort);

    // US-004 AC3: order-summary price breakdown (GA412 Economy: base 80.00 -> per-person
    // 92.00). PO count rule: max(saved, 1) — while entering passenger 1 it shows × 1.
    await expect(page.locator('.price-breakdown .total-price')).toHaveText('USD 92.00 total');
    await expect(page.locator('.price-breakdown .per-person-price')).toContainText('USD 92.00 per person × 1 passenger');

    // Booking chrome: back link to results + the exactly-two persistent actions.
    await expect(page.getByRole('link', { name: '← Back to results' })).toBeVisible();
    await expect(page.locator('#add-another-btn')).toHaveText(/Add another passenger/);
    await expect(page.locator('#confirm-booking-btn')).toBeVisible();

    // US-005 AC5: document field label is "National ID" for a domestic route (the in-place
    // form renders exactly one active passenger; passenger 1 uses index-0 ids).
    await expect(page.locator('label[for="documentNumber-0"]')).toContainText('National ID');

    // US-005/US-006: fill passenger 1 and Confirm Booking directly — the filled form is
    // validated + saved, then submitted (no separate save/review step exists).
    await fillActivePassengerForm(page, 0, { fullName: 'Jane Doe', email: 'jane@example.com', documentNumber: 'AB-1234' });
    const bookingRequestPromise = page.waitForRequest(
      (req) => req.method() === 'POST' && req.url().includes('/api/v1/bookings'),
    );
    await confirmBooking(page);
    const bookingRequest = await bookingRequestPromise;
    expect(bookingRequest.postDataJSON()).toMatchObject({ passengerCount: 1 });

    // US-006 AC4/AC5: booking reference format (SKY-DOM-XXXXXX), total price, passenger name.
    const reference = await page.locator('.booking-reference').innerText();
    expectBookingReferenceFormat(reference, 'DOM');
    await expect(page.locator('.total-price')).toContainText('USD 92.00 total');
    await expect(page.locator('.passengers')).toContainText('Jane Doe');

    // US-006 AC7: confirmation screen offers no resubmit control — only "Start a New Search".
    await expect(page.getByRole('button', { name: /confirm/i })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Start a New Search' })).toBeVisible();

    // Navigating back to /booking (selectedFlight still set) still cannot resubmit — the
    // state-layer guard (BookingStateService.submitBooking) blocks it and the page opens
    // read-only with Confirm conveyed unavailable via aria-disabled (never natively
    // disabled, so focus survives).
    await page.goBack();
    await page.waitForURL('**/booking');
    await expect(page.getByText('This booking has already been confirmed.')).toBeVisible();
    const confirmAgain = page.getByRole('button', { name: 'Confirm Booking' });
    await expect(confirmAgain).toBeDisabled(); // toBeDisabled honors aria-disabled
    await expect(confirmAgain).not.toHaveAttribute('disabled');
  });
});
