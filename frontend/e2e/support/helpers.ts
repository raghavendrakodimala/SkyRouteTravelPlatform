import { Page, expect } from '@playwright/test';

/**
 * Shared E2E helpers (Phase 13 extension — Playwright automated E2E suite).
 *
 * These helpers drive the real running app (real Angular dev server, real ASP.NET Core
 * backend) exactly as a user would — selecting `<select>` options, filling inputs, clicking
 * buttons. No frontend or backend source code is touched by these helpers.
 *
 * Route/network interception (`page.route(...)`) is used ONLY in `e2e/error-states.spec.ts`,
 * to reach two response shapes that are structurally unreachable through valid user input
 * against the current fixed-mock-data backend (see that file's header comment for the full
 * rationale, and docs/testing/test-strategy.md Section 1.4 for the documented decision).
 * Every other spec in this suite performs a full real round trip with no interception.
 */

export const API_SEARCH_URL = '**/api/search';
export const API_BOOKINGS_URL = '**/api/bookings';

export interface SearchFormInput {
  origin: string;
  destination: string;
  departureDate?: string;
  passengerCount?: number;
  cabinClass?: 'Economy' | 'Business' | 'First Class';
}

/** Tomorrow's date as YYYY-MM-DD — always valid against the "not in the past" rule regardless
 * of what time of day the suite happens to run (avoids a same-day midnight-boundary flake). */
export function tomorrowDateString(): string {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().slice(0, 10);
}

/** Fills the /search form fields without submitting (US-001 Section 1 fields). */
export async function fillSearchForm(page: Page, input: SearchFormInput): Promise<void> {
  await page.locator('#origin').selectOption(input.origin);
  await page.locator('#destination').selectOption(input.destination);
  await page.locator('#departureDate').fill(input.departureDate ?? tomorrowDateString());
  if (input.passengerCount) {
    await page.locator('#passengerCount').selectOption(String(input.passengerCount));
  }
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

/** Fills the Nth (0-based) passenger section on the /booking screen. */
export async function fillPassenger(page: Page, index: number, passenger: PassengerInput): Promise<void> {
  await page.locator(`#fullName-${index}`).fill(passenger.fullName);
  await page.locator(`#email-${index}`).fill(passenger.email);
  await page.locator(`#documentNumber-${index}`).fill(passenger.documentNumber);
}

/** Asserts a booking reference matches the exact BR-004 format: SKY-[INT|DOM]-XXXXXX, 14 chars. */
export function expectBookingReferenceFormat(reference: string, routeType: 'INT' | 'DOM'): void {
  expect(reference).toMatch(new RegExp(`^SKY-${routeType}-[A-Z0-9]{6}$`));
  expect(reference).toHaveLength(14);
}
