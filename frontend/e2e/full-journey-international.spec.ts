import { expect, test } from '@playwright/test';
import { expectBookingReferenceFormat, fillPassenger, searchAndGoToResults, selectFlightByNumber } from './support/helpers';

/**
 * Full happy-path journey — international route, multi-passenger (3 passengers).
 *
 * Real backend, real frontend, no mocking. GA101 (GlobalAir, LHR -> JFK, United Kingdom vs
 * United States -> International route type) is the exact worked example from
 * feature-booking-flow.md Section 2.3.1. Economy pricePerPassenger for GA101 is USD 287.50
 * (base fare 250.00 x 1.15, per the GlobalAir BR-001 worked example already unit-tested in
 * Phase 13's backend suite) -> total for 3 passengers = USD 862.50.
 */
test.describe('Full booking journey — international, multi-passenger', () => {
  test('search -> results -> select -> 3 passengers -> confirm -> reference shown', async ({ page }) => {
    await searchAndGoToResults(page, { origin: 'LHR', destination: 'JFK', passengerCount: 3, cabinClass: 'Economy' });

    const cards = page.locator('li.result-card');
    await expect(cards).toHaveCount(8);

    // US-004: select an international flight (GA101, LHR -> JFK).
    await selectFlightByNumber(page, 'GA101');

    // US-004 AC3: price breakdown (287.50 per person x 3 passengers = 862.50 total).
    await expect(page.locator('.price-breakdown')).toContainText('USD 862.50 total');
    await expect(page.locator('.price-breakdown')).toContainText('USD 287.50 per person');

    // US-005 AC5: document field label is "Passport Number" for an international route,
    // rendered identically for all 3 passenger sections (route type is fixed once per booking).
    await expect(page.locator('label[for="documentNumber-0"]')).toHaveText('Passport Number');
    await expect(page.locator('label[for="documentNumber-1"]')).toHaveText('Passport Number');
    await expect(page.locator('label[for="documentNumber-2"]')).toHaveText('Passport Number');

    // US-005 AC10: submit is blocked until all 3 passenger sections are valid — verified here
    // by filling them one at a time and observing the gate (dedicated deeper coverage of the
    // blocking behavior itself lives in booking-validation.spec.ts).
    const confirmButton = page.getByRole('button', { name: 'Confirm Booking' });
    await expect(confirmButton).toBeDisabled();

    await fillPassenger(page, 0, { fullName: 'Jane Doe', email: 'jane@example.com', documentNumber: 'AB1234C' });
    await expect(confirmButton).toBeDisabled();
    await fillPassenger(page, 1, { fullName: 'John Doe', email: 'john@example.com', documentNumber: 'CD5678E' });
    await expect(confirmButton).toBeDisabled();
    await fillPassenger(page, 2, { fullName: 'Amy Lee', email: 'amy@example.com', documentNumber: 'EF9012G' });
    await expect(confirmButton).toBeEnabled();

    await confirmButton.click();
    await page.waitForURL('**/confirmation');

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
