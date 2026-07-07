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

  test('PO 2026-07-07: the search form has NO passenger count field and always submits passengerCount 1', async ({
    page,
  }) => {
    // Passenger count is determined during booking ("Add another passenger" on /booking),
    // not at search time — the search form collects exactly origin, destination, departure
    // date, and cabin class. No passengers select, label, or leftover counter UI exists.
    await expect(page.locator('#passengerCount')).toHaveCount(0);
    await expect(page.locator('label', { hasText: 'Passengers' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Yes, add another passenger' })).toHaveCount(0);

    // The four real fields are all present and labelled.
    for (const id of ['origin', 'destination', 'departureDate', 'cabinClass']) {
      await expect(page.locator(`#${id}`)).toBeVisible();
      await expect(page.locator(`label[for="${id}"]`)).toBeVisible();
    }

    // The submitted SearchRequest hard-codes passengerCount: 1 (API contract still requires 1–9).
    const requestPromise = page.waitForRequest(
      (req) => req.method() === 'POST' && req.url().includes('/api/search'),
    );
    await fillSearchForm(page, { origin: 'LHR', destination: 'JFK', cabinClass: 'Economy' });
    await page.getByRole('button', { name: 'Search' }).click();
    const request = await requestPromise;
    expect(request.postDataJSON()).toMatchObject({ origin: 'LHR', destination: 'JFK', passengerCount: 1 });

    // Consequence on /results: totals equal the per-person price (× 1). BW210 (LHR -> JFK,
    // BudgetWings) prices at USD 198.00 per person (base 220.00 × 0.90, BR-002).
    await page.waitForURL('**/results');
    const bw210 = page.locator('li.result-card', { hasText: 'BW210' });
    await expect(bw210.locator('.total-price')).toHaveText('USD 198.00 total');
    await expect(bw210.locator('.per-person-price')).toHaveText('/ USD 198.00 per person');
  });

  // A11Y-007/A11Y-008 (2026-07-07): the Search button is never natively disabled — a
  // disabled-until-valid submit both drops keyboard focus to <body> and makes the inline
  // validation alerts unreachable. Instead, an invalid submit attempt reaches onSubmit(),
  // which surfaces the role="alert" message(s), focuses the first offending control, and
  // never calls the API. The two tests below assert that submit-attempt contract
  // (feature-flight-search.md Section 1, amended).

  test('US-001 AC8 / US-008 AC4: submitting with the same origin and destination surfaces the inline error and never calls the API', async ({
    page,
  }) => {
    const searchRequests: string[] = [];
    page.on('request', (request) => {
      if (request.url().includes('/api/search')) {
        searchRequests.push(request.url());
      }
    });

    // Selecting distinct airports first, then changing destination to match origin, proves the
    // guard reacts to the live form value at submit time, not just to initial state.
    await fillSearchForm(page, { origin: 'LHR', destination: 'JFK' });
    await page.locator('#destination').selectOption('LHR');

    const searchButton = page.getByRole('button', { name: 'Search' });
    await expect(searchButton).toBeEnabled(); // never natively disabled, even while invalid
    await searchButton.click();

    await expect(
      page.locator('p.error[role="alert"]', { hasText: 'Origin and destination airports must be different.' }),
    ).toBeVisible();
    // The same-airport rule lives on the form group, so focus lands on the destination select.
    await expect(page.locator('#destination')).toBeFocused();
    await expect(page).toHaveURL(/\/search$/);
    expect(searchRequests).toHaveLength(0); // the invalid submit attempt never reached the API
  });

  test('US-001 AC5: submitting with missing required fields surfaces inline errors and never calls the API', async ({
    page,
  }) => {
    const searchRequests: string[] = [];
    page.on('request', (request) => {
      if (request.url().includes('/api/search')) {
        searchRequests.push(request.url());
      }
    });

    const searchButton = page.getByRole('button', { name: 'Search' });
    await expect(searchButton).toBeEnabled(); // form starts empty, button still focusable/activatable
    await searchButton.click();

    await expect(page.locator('p.error[role="alert"]', { hasText: 'Origin airport is required.' })).toBeVisible();
    await expect(page.locator('p.error[role="alert"]', { hasText: 'Destination airport is required.' })).toBeVisible();
    await expect(
      page.locator('p.error[role="alert"]', { hasText: 'Departure date is required and cannot be in the past.' }),
    ).toBeVisible();
    await expect(page.locator('#origin')).toBeFocused(); // first invalid control receives focus

    // Filling everything else and clearing the departure date re-triggers the same guard.
    await fillSearchForm(page, { origin: 'LHR', destination: 'JFK' });
    await page.locator('#departureDate').fill('');
    await searchButton.click();
    await expect(
      page.locator('p.error[role="alert"]', { hasText: 'Departure date is required and cannot be in the past.' }),
    ).toBeVisible();
    await expect(page.locator('#departureDate')).toBeFocused();

    await expect(page).toHaveURL(/\/search$/);
    expect(searchRequests).toHaveLength(0); // no submit attempt ever produced an API call
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

    const searchingButton = page.getByRole('button', { name: 'Searching…' });
    await expect(searchingButton).toBeVisible();
    // A11Y-007: in flight the button is conveyed unavailable via aria-disabled="true"
    // (Playwright's toBeDisabled honors aria-disabled), never the native `disabled`
    // attribute, so keyboard/AT focus is not dropped to <body> mid-request.
    await expect(searchingButton).toBeDisabled();
    await expect(searchingButton).toHaveAttribute('aria-disabled', 'true');
    await expect(searchingButton).not.toHaveAttribute('disabled');

    await page.waitForURL('**/results');
    // Route filtering (ProviderScheduleMapper): LHR -> JFK matches exactly GA101 (GlobalAir)
    // and BW210 (BudgetWings) — not the full 8-flight fixture set.
    await expect(page.locator('li.result-card')).toHaveCount(2);
  });
});
