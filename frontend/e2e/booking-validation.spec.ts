import { expect, test } from '@playwright/test';
import { searchAndGoToResults, selectFlightByNumber } from './support/helpers';

/**
 * US-005 AC10 / FR-031 — the "Confirm Booking" action stays disabled until every rendered
 * passenger section is valid, and inline field errors are shown per the documented rules
 * (feature-booking-flow.md Section 2). Uses the domestic worked example (GA412, National ID)
 * so the document-number boundary case is distinct from the Passport case already exercised
 * in full-journey-international.spec.ts.
 */
test.describe('Passenger details form validation (US-005 AC10)', () => {
  test('submit stays disabled until full name, email, and document number are all valid', async ({ page }) => {
    await searchAndGoToResults(page, { origin: 'LHR', destination: 'MAN', passengerCount: 1 });
    await selectFlightByNumber(page, 'GA412');

    const confirmButton = page.getByRole('button', { name: 'Confirm Booking' });
    const fullName = page.locator('#fullName-0');
    const email = page.locator('#email-0');
    const documentNumber = page.locator('#documentNumber-0');

    await expect(confirmButton).toBeDisabled();

    // Full name: numeric-only is rejected (FR-064) even though non-empty.
    await fullName.fill('12');
    await fullName.blur();
    await expect(
      page.getByText('Full name is required, must be 2–100 characters, and must contain at least one letter.'),
    ).toBeVisible();
    await expect(confirmButton).toBeDisabled();

    await fullName.fill('Jane Doe');
    await fullName.blur();
    await expect(confirmButton).toBeDisabled(); // email/document still invalid

    // Email: malformed address is rejected.
    await email.fill('not-an-email');
    await email.blur();
    await expect(page.getByText('A valid email address is required.')).toBeVisible();
    await expect(confirmButton).toBeDisabled();

    await email.fill('jane@example.com');
    await email.blur();
    await expect(confirmButton).toBeDisabled(); // document still invalid

    // Document number (National ID, domestic route): below the 5-char minimum is rejected.
    await documentNumber.fill('AB12');
    await documentNumber.blur();
    await expect(page.getByText('National ID does not match the required format.')).toBeVisible();
    await expect(confirmButton).toBeDisabled();

    // All three fields now valid -> submit becomes enabled.
    await documentNumber.fill('AB-1234');
    await documentNumber.blur();
    await expect(confirmButton).toBeEnabled();

    // Editing a previously-valid field back to invalid re-disables submit immediately.
    await fullName.fill('9');
    await fullName.blur();
    await expect(confirmButton).toBeDisabled();
  });
});
