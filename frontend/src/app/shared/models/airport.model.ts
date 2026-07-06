/**
 * Airport reference record (US-008, FR-057). Shape matches the backend's independent
 * Airport domain model (SkyRoute.Application/Domain/Airport.cs) field-for-field, though the
 * two are separate authoritative sources for their own layer (FR-055).
 */
export interface Airport {
  /** IATA code, 3 uppercase letters, e.g. "LHR". */
  code: string;
  city: string;
  country: string;
  displayName: string;
}
