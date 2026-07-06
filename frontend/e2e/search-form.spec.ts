import { expect, test } from '@playwright/test';
import { fillSearchForm, tomorrowDateString } from './support/helpers';

/** Mirrors frontend/src/app/shared/constants/airports.constants.ts (single source of truth;
 * duplicated here only as literal expected-value fixtures for assertions, never imported into
 * app code). */
const AIRPORTS = [
  { code: 'LHR', city: 'London', country: 'United Kingdom' },
  { code: 'MAN', city: 'Manchester', country: 'United Kingdom' },
  { code: 'JFK', city: 'New York', country: 'United States' },
  { code: 'LAX', city: 'Los Angeles', country: 'United States' },
  { code: 'DXB', city: 'Dubai', country: 'United Arab Emirates' },
  { code: 'SYD', city: 'Sydney', country: 'Australia' },
];

test.describe('Search form (US-001, US-008)', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/search');
  });

  test('US-008 AC1/AC2: airport dropdown shows code, city, and country for every entry, across 2+ countries', async ({
    page,
  }) => {
    // Exclude the disabled "Select origin airport" placeholder option, which also carries a
    // (empty) `value` attribute and would otherwise be counted.
    const originOptions = page.locator('#origin option[value]:not([value=""])');
    await expect(originOptions).toHaveCount(AIRPORTS.length);

    for (const airport of AIRPORTS) {
      const optionText = await page.locator(`#origin option[value="${airport.code}"]`).innerText();
      expect(optionText).toContain(airport.code);
      expect(optionText).toContain(airport.city);
      expect(optionText).toContain(airport.country);
    }

    const countries = new Set(AIRPORTS.map((a) => a.country));
    expect(countries.size).toBeGreaterThanOrEqual(2);
  });

  test('US-001 AC8 / US-008 AC4: the same airport cannot be selected as both origin and destination', async ({
    page,
  }) => {
    // Selecting distinct airports first (button enabled), then changing destination to match
    // origin, proves the guard reacts live to the form value change, not just to initial state.
    await fillSearchForm(page, { origin: 'LHR', destination: 'JFK', passengerCount: 1 });
    await expect(page.getByRole('button', { name: 'Search' })).toBeEnabled();

    await page.locator('#destination').selectOption('LHR');
    // The group-level `sameAirport` validator (search-form.component.ts) makes the whole form
    // invalid immediately, disabling submission before the user can ever trigger a submit
    // attempt — see QA-003 (docs/handoffs) for why the inline
    // "Origin and destination airports must be different." paragraph, which is gated on
    // `submitted()`, is consequently never reachable through normal interaction: the Search
    // button is always disabled at the exact moment that condition is true, so `onSubmit()`
    // (which sets `submitted`) can never run while it is true. The button-disabled behavior
    // itself — which is the actual AC8/AC4 requirement — is fully verified below.
    await expect(page.getByRole('button', { name: 'Search' })).toBeDisabled();
  });

  test('US-001 AC5: submit button is disabled while any required field is invalid or empty', async ({ page }) => {
    await expect(page.getByRole('button', { name: 'Search' })).toBeDisabled(); // form starts empty
    await fillSearchForm(page, { origin: 'LHR', destination: 'JFK' });
    // departureDate defaults to tomorrow via the helper's fallback, so the form should now be valid.
    await expect(page.getByRole('button', { name: 'Search' })).toBeEnabled();

    // Clearing departure date re-disables the button.
    await page.locator('#departureDate').fill('');
    await expect(page.getByRole('button', { name: 'Search' })).toBeDisabled();
  });

  test('US-001 AC6: loading indicator is shown during the search call', async ({ page }) => {
    // Real backend, real request — artificially delayed at the network layer only, so the
    // otherwise near-instant local in-memory response is reliably observable mid-flight. The
    // request still reaches and is answered by the real backend (route.continue()), it is not
    // faked or short-circuited.
    await page.route('**/api/search', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 700));
      await route.continue();
    });

    await fillSearchForm(page, { origin: 'LHR', destination: 'JFK', departureDate: tomorrowDateString() });
    const searchButton = page.getByRole('button', { name: 'Search' });
    await searchButton.click();

    await expect(page.getByRole('button', { name: 'Searching…' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Searching…' })).toBeDisabled();

    await page.waitForURL('**/results');
    await expect(page.locator('li.result-card')).toHaveCount(8);
  });
});
