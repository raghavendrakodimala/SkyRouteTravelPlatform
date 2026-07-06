import { expect, test } from '@playwright/test';
import { searchAndGoToResults, selectFlightByNumber } from './support/helpers';

/**
 * US-002 AC8 / feature-search-results-and-sorting.md Section 3: "results persist until a new
 * search is initiated" — navigating to the booking screen and back (or any other in-app
 * navigation that does not re-submit the search form) must not clear the previously displayed
 * result set, and must not trigger a new /api/search call.
 *
 * Default-sort/re-sort-without-a-new-call and active-sort-indicator coverage (US-003 AC2/AC3)
 * already live in full-journey-domestic.spec.ts — not duplicated here.
 */
test.describe('Results persistence (US-002 AC8)', () => {
  test('results remain displayed after navigating away and back, with no new search call', async ({ page }) => {
    let searchRequestCount = 0;
    page.on('request', (req) => {
      if (req.method() === 'POST' && req.url().includes('/api/search')) {
        searchRequestCount += 1;
      }
    });

    await searchAndGoToResults(page, { origin: 'LHR', destination: 'JFK', passengerCount: 1 });
    expect(searchRequestCount).toBe(1);

    const cards = page.locator('li.result-card');
    await expect(cards).toHaveCount(8);
    const firstCardTextBefore = await cards.first().innerText();

    await selectFlightByNumber(page, 'GA412');
    await page.waitForURL('**/booking');

    await page.goBack();
    await page.waitForURL('**/results');

    // Still the same 8 results, same order, and no new search request was fired.
    // useInnerText matches how firstCardTextBefore was captured (layout-based line breaks).
    await expect(cards).toHaveCount(8);
    await expect(cards.first()).toHaveText(firstCardTextBefore, { useInnerText: true });
    expect(searchRequestCount).toBe(1);
  });
});
