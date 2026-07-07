import { Page, expect } from '@playwright/test';

/**
 * Shared E2E helpers (Playwright automated E2E suite).
 *
 * These helpers drive the real running app (real Angular dev server, real ASP.NET Core
 * backend) exactly as a user would — selecting `<select>` options, filling inputs, clicking
 * buttons. No frontend or backend source code is touched by these helpers.
 *
 * Search form (PO decision 2026-07-07): the /search form collects ONLY origin, destination,
 * departure date, and cabin class — there is no passenger count field at search time. Every
 * SearchRequest is submitted with passengerCount: 1, so results totals equal the per-person
 * price. Passenger count is determined during BOOKING instead, via the single in-place
 * passenger form's "Add another passenger" action.
 *
 * Booking screen (single-button in-place flow, PO UX correction 2026-07-07 — supersedes the
 * earlier save→prompt→review wizard): exactly one active passenger form is rendered below the
 * saved-passenger cards (its input ids are index-suffixed: fullName-0, fullName-1, …; for a
 * NEW passenger the index equals the number of already-saved passengers), with exactly two
 * persistent actions — "Add another passenger" (#add-another-btn: validate → save the current
 * form as a card → reset the same form in place) and "Confirm Booking" (#confirm-booking-btn:
 * a filled form is validated + saved first, then ALL saved passengers submit; a blank form
 * with saved passengers submits those as-is). While editing a saved card the actions become
 * "Save changes" (#save-changes-btn) / "Cancel edit" (#cancel-edit-btn). Cap: 9 passengers.
 *
 * Route filtering (ProviderScheduleMapper.BuildResults): providers return only schedule
 * entries matching the requested origin/destination (case-insensitive). A search for a route
 * with no fixture (e.g. LHR -> MAN) therefore reaches the REAL empty-results state — no
 * interception needed. Route/network interception is used only in `e2e/error-states.spec.ts`
 * for the two 5xx response shapes (still not producible through valid UI input) and in
 * `e2e/search-form.spec.ts` to delay (not fake) the real response so the loading state is
 * observable. Every other spec performs a full real round trip.
 */

export const API_SEARCH_URL = '**/api/search';
export const API_BOOKINGS_URL = '**/api/bookings';

export interface SearchFormInput {
  origin: string;
  destination: string;
  departureDate?: string;
  cabinClass?: 'Economy' | 'Business' | 'First Class';
}

/** Tomorrow's date as YYYY-MM-DD — always valid against the "not in the past" rule regardless
 * of what time of day the suite happens to run (avoids a same-day midnight-boundary flake). */
export function tomorrowDateString(): string {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().slice(0, 10);
}

/**
 * Guard: fails fast with an actionable message when a spec asks for a passenger count the UI
 * cannot express. The booking screen caps a booking at 9 passengers (client requirement) —
 * the 9th save removes the "Add another passenger" path entirely.
 */
export function assertValidPassengerCount(passengerCount: number): void {
  if (!Number.isInteger(passengerCount) || passengerCount < 1 || passengerCount > 9) {
    throw new Error(
      `Invalid passengerCount ${passengerCount}: a booking holds 1–9 passengers ` +
        '(client requirement; the 9-passenger cap removes the add action). Fix the spec input.',
    );
  }
}

/** Fills the /search form fields without submitting (US-001 — origin, destination, date,
 * cabin class; there is deliberately no passengers field, PO 2026-07-07). */
export async function fillSearchForm(page: Page, input: SearchFormInput): Promise<void> {
  await page.locator('#origin').selectOption(input.origin);
  await page.locator('#destination').selectOption(input.destination);
  await page.locator('#departureDate').fill(input.departureDate ?? tomorrowDateString());
  if (input.cabinClass) {
    await page.locator('#cabinClass').selectOption(input.cabinClass);
  }
}

/** Fills the form and submits, waiting for navigation to /results (happy-path helper). */
export async function searchAndGoToResults(page: Page, input: SearchFormInput): Promise<void> {
  await page.goto('/search');
  await fillSearchForm(page, input);
  await page.getByRole('button', { name: 'Search' }).click();
  await page.waitForURL('**/results');
}

/** Locates the result card containing a given (unique) flight number, e.g. "GA412". */
export function resultCardByFlightNumber(page: Page, flightNumber: string) {
  return page.locator('li.result-card', { hasText: flightNumber });
}

/** Clicks "Select" on the result card for the given flight number and waits for /booking. */
export async function selectFlightByNumber(page: Page, flightNumber: string): Promise<void> {
  await resultCardByFlightNumber(page, flightNumber).getByRole('button', { name: 'Select' }).click();
  await page.waitForURL('**/booking');
}

export interface PassengerInput {
  fullName: string;
  email: string;
  documentNumber: string;
}

/**
 * Fills the single ACTIVE in-place passenger form on the /booking screen. `index` is the
 * 0-based position of the passenger being entered/edited — exactly one form exists at any
 * moment, and its input ids are suffixed with that index (fullName-0, fullName-1, …: for a
 * NEW passenger the index equals the number of already-saved passengers).
 */
export async function fillActivePassengerForm(page: Page, index: number, passenger: PassengerInput): Promise<void> {
  await page.locator(`#fullName-${index}`).fill(passenger.fullName);
  await page.locator(`#email-${index}`).fill(passenger.email);
  await page.locator(`#documentNumber-${index}`).fill(passenger.documentNumber);
}

/** Clicks "Add another passenger": a valid active form becomes a saved card and the same
 * form resets in place for the next passenger; an invalid form stays open with errors. */
export async function clickAddAnotherPassenger(page: Page): Promise<void> {
  await page.locator('#add-another-btn').click();
}

/**
 * Saves the first N-1 passengers via "Add another passenger" and leaves the LAST passenger
 * filled but unsaved in the active form — Confirm Booking then validates + saves it and
 * submits everything (the filled-form confirm path). Ends with passengers.length - 1 saved
 * cards visible and the last passenger's details in the open form.
 */
export async function enterPassengers(page: Page, passengers: PassengerInput[]): Promise<void> {
  assertValidPassengerCount(passengers.length);
  for (let i = 0; i < passengers.length; i++) {
    await fillActivePassengerForm(page, i, passengers[i]);
    if (i < passengers.length - 1) {
      await clickAddAnotherPassenger(page);
      // The card must exist before the next fill — proves the save was accepted.
      await expect(page.locator('li.passenger-card')).toHaveCount(i + 1);
    }
  }
}

/** Clicks "Confirm Booking" and waits for /confirmation (happy-path helper — any filled
 * active form is validated and saved by the app before the submit fires). */
export async function confirmBooking(page: Page): Promise<void> {
  await page.locator('#confirm-booking-btn').click();
  await page.waitForURL('**/confirmation');
}

/** Asserts a booking reference matches the exact BR-004 format: SKY-[INT|DOM]-XXXXXX, 14 chars. */
export function expectBookingReferenceFormat(reference: string, routeType: 'INT' | 'DOM'): void {
  expect(reference).toMatch(new RegExp(`^SKY-${routeType}-[A-Z0-9]{6}$`));
  expect(reference).toHaveLength(14);
}
