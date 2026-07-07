/**
 * POST /api/v1/search request body — exact shape per docs/features/feature-flight-search.md
 * Section 2 and docs/architecture/architecture-plan.md Section 5.
 */
export type CabinClass = 'Economy' | 'Business' | 'First Class';

/** BR-013: the only currently valid trip type. Not a visible form control (Gap-fill FS-01). */
export type TripType = 'OneWay';

export interface SearchRequest {
  origin: string;
  destination: string;
  /** ISO 8601 date only, e.g. "2026-08-01" (no time component). */
  departureDate: string;
  passengerCount: number;
  cabinClass: CabinClass;
  tripType: TripType;
}
