import { FlightResult } from '../models/flight-result.model';

/**
 * The four sort options (US-003, FR-020). Sort key is pricePerPassenger, not a computed
 * total, since every result in a given response shares the same passengerCount — multiplying
 * by a constant does not change relative order (feature-search-results-and-sorting.md
 * Section 4.1).
 */
export type SortOption = 'priceAsc' | 'priceDesc' | 'durationAsc' | 'departureAsc';

export const DEFAULT_SORT_OPTION: SortOption = 'priceAsc';

export const SORT_OPTION_LABELS: Record<SortOption, string> = {
  priceAsc: 'Price: low to high',
  priceDesc: 'Price: high to low',
  durationAsc: 'Duration: shortest first',
  departureAsc: 'Departure time: earliest first',
};

const COMPARATORS: Record<SortOption, (a: FlightResult, b: FlightResult) => number> = {
  priceAsc: (a, b) => a.pricePerPassenger - b.pricePerPassenger,
  priceDesc: (a, b) => b.pricePerPassenger - a.pricePerPassenger,
  durationAsc: (a, b) => a.durationMinutes - b.durationMinutes,
  departureAsc: (a, b) => (a.departureDateTime < b.departureDateTime ? -1 : a.departureDateTime > b.departureDateTime ? 1 : 0),
};

/**
 * Pure client-side re-sort of the full result set (FR-021, FR-024) — no API call. Relies on
 * the ECMAScript-guaranteed stability of Array.prototype.sort for tie-breaking (Gap-fill
 * SR-03): the comparator returns 0 for equal keys and no secondary sort key is introduced.
 */
export function sortFlights(results: readonly FlightResult[], option: SortOption): FlightResult[] {
  return [...results].sort(COMPARATORS[option]);
}
