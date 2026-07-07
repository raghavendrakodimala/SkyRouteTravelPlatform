import { expect, test } from '@playwright/test';
import { clickAddAnotherPassenger, fillActivePassengerForm, searchAndGoToResults, selectFlightByNumber } from './support/helpers';

/**
 * US-005 AC10 / FR-031 — passenger validation in the single-button in-place flow (PO UX
 * correction 2026-07-07). Validity gates BOTH persistent actions on the one active form:
 * an invalid "Add another passenger" keeps the form open (no card is saved), surfaces the
 * inline `role="alert"` errors, and focuses the first offending control; an invalid or blank
 * "Confirm Booking" with nothing saved does the same and never calls the API. Saved
 * passengers remain correctable via their card's Edit/Remove buttons, with edit saves
 * ("Save changes") re-validated under the same rules and "Cancel edit" discarding cleanly.
 *
 * Uses the domestic worked example (GA412 on MAN -> LHR, National ID) so the document-number
 * boundary case is distinct from the Passport case exercised in
 * full-journey-international.spec.ts.
 */
test.describe('Passenger validation — in-place form (US-005 AC10)', () => {
  test('invalid add/confirm keep the form open with inline errors; edit, cancel edit, and remove allow correction', async ({
    page,
  }) => {
    let bookingRequestCount = 0;
    page.on('request', (req) => {
      if (req.method() === 'POST' && req.url().includes('/api/bookings')) {
        bookingRequestCount += 1;
      }
    });

    await searchAndGoToResults(page, { origin: 'MAN', destination: 'LHR' });
    await selectFlightByNumber(page, 'GA412');

    const fullName = page.locator('#fullName-0');
    const email = page.locator('#email-0');
    const documentNumber = page.locator('#documentNumber-0');
    const savedCards = page.locator('li.passenger-card');

    // Full name: numeric-only is rejected (FR-064) even though non-empty — inline error on blur.
    await fullName.fill('12');
    await fullName.blur();
    await expect(
      page.getByText('Full name is required, must be 2–100 characters, and must contain at least one letter.'),
    ).toBeVisible();

    // "Add another passenger" while invalid keeps the form open: no saved card, all remaining
    // errors surfaced (markAllAsTouched), focus on the first invalid control.
    await clickAddAnotherPassenger(page);
    await expect(savedCards).toHaveCount(0);
    await expect(page.getByText('A valid email address is required.')).toBeVisible();
    await expect(page.getByText('National ID does not match the required format.')).toBeVisible();
    await expect(fullName).toBeFocused();

    // Fix the name; a malformed email still blocks the add, focus moves to the email field.
    await fullName.fill('Jane Doe');
    await email.fill('not-an-email');
    await clickAddAnotherPassenger(page);
    await expect(savedCards).toHaveCount(0);
    await expect(page.getByText('A valid email address is required.')).toBeVisible();
    await expect(email).toBeFocused();

    // Fix the email; a National ID below the 5-char minimum still blocks the add.
    await email.fill('jane@example.com');
    await documentNumber.fill('AB12');
    await clickAddAnotherPassenger(page);
    await expect(savedCards).toHaveCount(0);
    await expect(page.getByText('National ID does not match the required format.')).toBeVisible();
    await expect(documentNumber).toBeFocused();

    // All three fields valid -> the add is accepted: summary card appears, the SAME form
    // resets in place for passenger 2 (index-1 ids) and receives focus.
    await documentNumber.fill('AB-1234');
    await clickAddAnotherPassenger(page);
    await expect(savedCards).toHaveCount(1);
    await expect(savedCards.first()).toContainText('Jane Doe');
    await expect(savedCards.first()).toContainText('National ID: AB-1234');
    await expect(page.locator('#fullName-1')).toHaveValue('');
    await expect(page.locator('#fullName-1')).toBeFocused();

    // Error correction via Edit: the saved card's values load into the in-place form (its ids
    // re-suffix to the edited index) and the actions become Save changes / Cancel edit.
    await page.getByRole('button', { name: 'Edit passenger 1, Jane Doe' }).click();
    await expect(fullName).toHaveValue('Jane Doe'); // pre-filled with the saved values
    await expect(email).toHaveValue('jane@example.com');
    await expect(page.locator('#save-changes-btn')).toBeVisible();
    await expect(page.locator('#cancel-edit-btn')).toBeVisible();
    await expect(page.locator('#add-another-btn')).toHaveCount(0); // add path hidden mid-edit

    // An invalid edit is blocked by the same rules — the card is NOT updated.
    await fullName.fill('9');
    await page.locator('#save-changes-btn').click();
    await expect(
      page.getByText('Full name is required, must be 2–100 characters, and must contain at least one letter.'),
    ).toBeVisible();
    await expect(fullName).toBeFocused(); // edit not accepted, form still open
    await expect(savedCards.first()).toContainText('Jane Doe');

    // A valid edit updates the card; focus returns to that card's Edit button (the edit form
    // left this slot, focus must not drop to <body>), and the blank collecting form is back.
    await fullName.fill('Janet Doe');
    await page.locator('#save-changes-btn').click();
    await expect(savedCards.first()).toContainText('Janet Doe');
    await expect(page.getByRole('button', { name: 'Edit passenger 1, Janet Doe' })).toBeFocused();
    await expect(page.locator('#fullName-1')).toHaveValue('');
    await expect(page.locator('#add-another-btn')).toBeVisible();

    // Cancel edit: a pristine edit discards cleanly — card unchanged, focus restored.
    await page.getByRole('button', { name: 'Edit passenger 1, Janet Doe' }).click();
    await expect(fullName).toHaveValue('Janet Doe');
    await page.locator('#cancel-edit-btn').click();
    await expect(savedCards.first()).toContainText('Janet Doe');
    await expect(page.getByRole('button', { name: 'Edit passenger 1, Janet Doe' })).toBeFocused();

    // Error correction via Remove: removing the last remaining passenger returns to a fresh
    // passenger 1 form (Remove is never disabled; the collecting form re-ids to index 0).
    await page.getByRole('button', { name: 'Remove passenger 1, Janet Doe' }).click();
    await expect(savedCards).toHaveCount(0);
    await expect(fullName).toHaveValue('');
    await expect(fullName).toBeFocused();
    // The persistent polite live region announced the removal (aria-live mandate).
    await expect(page.locator('p.visually-hidden[role="status"]')).toHaveText(
      'Passenger removed. Add at least one passenger to continue.',
    );

    // Confirm Booking with a blank form and nothing saved: treated as an invalid first
    // passenger — required errors surface, focus lands on the first field, no API call.
    await page.locator('#confirm-booking-btn').click();
    await expect(
      page.getByText('Full name is required, must be 2–100 characters, and must contain at least one letter.'),
    ).toBeVisible();
    await expect(page.getByText('A valid email address is required.')).toBeVisible();
    await expect(page.getByText('National ID does not match the required format.')).toBeVisible();
    await expect(fullName).toBeFocused();
    await expect(page).toHaveURL(/\/booking$/);

    // No invalid state — add, save-changes, or confirm — ever produced a booking POST.
    expect(bookingRequestCount).toBe(0);
  });
});
