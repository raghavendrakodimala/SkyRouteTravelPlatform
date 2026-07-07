import { CabinClass } from './search-request.model';

/**
 * POST /api/v1/search 200 response element — exact shape per
 * docs/features/feature-search-results-and-sorting.md Section 1 / architecture-plan.md
 * Section 5. Field names match the backend's FlightResult domain model
 * (SkyRoute.Application/Domain/FlightResult.cs); System.Text.Json's default camelCase
 * naming policy serialises the PascalCase C# properties to these exact camelCase keys.
 */
export interface FlightResult {
  /** "GlobalAir" or "BudgetWings" (FR-052). */
  provider: string;
  flightNumber: string;
  origin: string;
  destination: string;
  /** ISO 8601 datetime, Z-suffixed. Rendered as the literal HH:mm substring (Gap-fill SR-02). */
  departureDateTime: string;
  arrivalDateTime: string;
  durationMinutes: number;
  cabinClass: CabinClass;
  /** Present for traceability (FR-010); not required to be rendered (Gap-fill SR-04). */
  baseFare: number;
  /** Post-pricing-rule per-passenger price (BR-001/BR-002) — the figure multiplied by
   * passengerCount to produce total price (FR-012, DP-011). */
  pricePerPassenger: number;
}
