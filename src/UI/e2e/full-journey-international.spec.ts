import { expect, test } from '@playwright/test';
import {
  clickAddAnotherPassenger,
  expectBookingReferenceFormat,
  fillActivePassengerForm,
  confirmBooking,
  searchAndGoToResults,
  selectFlightByNumber,
} from './support/helpers';

/**
 * Full happy-path journey — international route, multi-passenger (3 passengers), exercising
 * the single-button in-place booking flow end to end: fill -> "Add another passenger" (card
 * appears, same form resets in place) -> ... -> Confirm Booking with a BLANK form, which
 * submits all saved passengers as-is.
 *
 * Real backend, real frontend, no mocking. Route filtering: LHR -> JFK matches exactly GA101
 * (GlobalAir, per-person 287.50 = base 250.00 × 1.15, BR-001) and BW210 (BudgetWings,
 * per-person 198.00 = base 220.00 × 0.90, BR-002). United Kingdom -> United States =>
 * International route type. Search always runs for passengerCount 1 (PO 2026-07-07) — the
 * passenger count is built up at BOOKING time, so results totals equal per-person price and
 * the booking totals grow as passengers are added: 287.50 × 3 = 862.50.
 */
test.describe('Full booking journey — international, multi-passenger', () => {
  test('search -> results -> select -> add 3 passengers in place -> confirm with blank form', async ({ page }) => {
    await searchAndGoToResults(page, { origin: 'LHR', destination: 'JFK', cabinClass: 'Economy' });

    // Route filtering: only the two LHR -> JFK fixtures come back. Default sort is price
    // low-to-high, so BW210 (198.00) precedes GA101 (287.50) — the true filtered order.
    const cards = page.locator('li.result-card');
    await expect(cards).toHaveCount(2);
    await expect(cards.first()).toContainText('BW210');
    await expect(cards.nth(1)).toContainText('GA101');
    // Results totals are per-person × 1 — no passenger count exists at search time.
    await expect(page.locator('li.result-card', { hasText: 'GA101' }).locator('.total-price')).toHaveText(
      'USD 287.50 total',
    );

    // US-004: select the international flight GA101.
    await selectFlightByNumber(page, 'GA101');

    // Opening state: passenger 1's in-place form (index-0 ids) plus the two persistent
    // actions. PO count rule max(saved, 1): the breakdown starts at × 1.
    await expect(page.locator('#fullName-0')).toBeVisible();
    await expect(page.locator('#add-another-btn')).toBeVisible();
    await expect(page.locator('#confirm-booking-btn')).toBeVisible();
    await expect(page.locator('.price-breakdown .total-price')).toHaveText('USD 287.50 total');
    await expect(page.locator('.price-breakdown .per-person-price')).toContainText(
      'USD 287.50 per person × 1 passenger',
    );

    // US-005 AC5: document field label is "Passport Number" for an international route.
    await expect(page.locator('label[for="documentNumber-0"]')).toContainText('Passport Number');

    // Passenger 1: fill -> "Add another passenger" -> card 1 + the SAME form resets in place.
    await fillActivePassengerForm(page, 0, { fullName: 'Jane Doe', email: 'jane@example.com', documentNumber: 'AB1234C' });
    await clickAddAnotherPassenger(page);
    await expect(page.locator('li.passenger-card')).toHaveCount(1);
    await expect(page.locator('.saved-count-status')).toHaveText('1 passenger added');
    // Passenger 1 is the lead/primary contact on their card.
    await expect(page.locator('li.passenger-card').first()).toContainText('Primary Contact');

    // Fresh in-place form: index-1 ids, empty fields, focus on the new Full Name input, and
    // the label/validation regime is identical for every passenger (route type fixed per
    // booking). Breakdown counts SAVED passengers (max(saved, 1)): still × 1 with 1 saved.
    await expect(page.locator('#fullName-1')).toHaveValue('');
    await expect(page.locator('#fullName-1')).toBeFocused();
    await expect(page.locator('label[for="documentNumber-1"]')).toContainText('Passport Number');
    await expect(page.locator('.price-breakdown .total-price')).toHaveText('USD 287.50 total');

    // Passenger 2: fill -> add. Breakdown now × 2 = 575.00.
    await fillActivePassengerForm(page, 1, { fullName: 'John Doe', email: 'john@example.com', documentNumber: 'CD5678E' });
    await clickAddAnotherPassenger(page);
    await expect(page.locator('li.passenger-card')).toHaveCount(2);
    await expect(page.locator('.price-breakdown .total-price')).toHaveText('USD 575.00 total');
    await expect(page.locator('.price-breakdown .per-person-price')).toContainText(
      'USD 287.50 per person × 2 passengers',
    );

    // Passenger 3: fill -> add. Breakdown × 3 = 862.50; blank index-3 form remains open.
    await fillActivePassengerForm(page, 2, { fullName: 'Amy Lee', email: 'amy@example.com', documentNumber: 'EF9012G' });
    await clickAddAnotherPassenger(page);
    await expect(page.locator('li.passenger-card')).toHaveCount(3);
    await expect(page.locator('.saved-count-status')).toHaveText('3 passengers added');
    await expect(page.locator('.price-breakdown .total-price')).toHaveText('USD 862.50 total');
    await expect(page.locator('#fullName-3')).toHaveValue('');

    // Confirm Booking with the BLANK form: the 3 saved passengers submit as-is (the blank
    // in-progress form is ignored, never treated as a 4th passenger).
    const bookingRequestPromise = page.waitForRequest(
      (req) => req.method() === 'POST' && req.url().includes('/api/v1/bookings'),
    );
    await confirmBooking(page);
    const bookingRequest = await bookingRequestPromise;
    const body = bookingRequest.postDataJSON() as { passengerCount: number; passengers: unknown[] };
    expect(body.passengerCount).toBe(3);
    expect(body.passengers).toHaveLength(3);

    // US-006 AC4/AC5: booking reference format (SKY-INT-XXXXXX), total price, all 3 names.
    const reference = await page.locator('.booking-reference').innerText();
    expectBookingReferenceFormat(reference, 'INT');
    await expect(page.locator('.total-price')).toContainText('USD 862.50 total');
    const passengerListText = await page.locator('.passengers').innerText();
    expect(passengerListText).toContain('Jane Doe');
    expect(passengerListText).toContain('John Doe');
    expect(passengerListText).toContain('Amy Lee');
  });
});
