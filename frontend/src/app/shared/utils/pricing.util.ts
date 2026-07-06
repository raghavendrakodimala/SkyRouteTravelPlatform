/**
 * The single frontend location where total price = per-passenger price × passenger count is
 * calculated (DP-011). Every screen that displays a total (results, booking) calls
 * calculateTotalPrice() — the function must not be re-implemented inline anywhere else.
 * The confirmation screen displays a server-supplied totalPrice value directly (it does not
 * recompute it) and uses only formatUsd()/formatTotalAndPerPersonLabel() for display.
 */

/** Rounds to 2 decimal places defensively; pricePerPassenger already arrives 2dp from the
 * backend, so this introduces no new precision (feature-search-results-and-sorting.md
 * Section 2.1). */
export function calculateTotalPrice(pricePerPassenger: number, passengerCount: number): number {
  const total = pricePerPassenger * passengerCount;
  return Math.round(total * 100) / 100;
}

/** "USD {amount}" formatted to exactly 2 decimal places (FR-017/US-002 AC2) — not a
 * currency-symbol format. */
export function formatUsd(amount: number): string {
  return `USD ${amount.toFixed(2)}`;
}

/** "USD 320.00 total / USD 160.00 per person" (FR-018, verbatim example format). */
export function formatTotalAndPerPersonLabel(pricePerPassenger: number, passengerCount: number): string {
  const total = calculateTotalPrice(pricePerPassenger, passengerCount);
  return `${formatUsd(total)} total / ${formatUsd(pricePerPassenger)} per person`;
}
