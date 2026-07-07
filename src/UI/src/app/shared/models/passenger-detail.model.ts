/**
 * Per-passenger booking record fields (US-005, FR-040). Field names match the backend's
 * PassengerRequest contract (SkyRoute.Application/Contracts/PassengerRequest.cs).
 */

/** Exactly "Passport" or "National ID" — note the space (FR-040). */
export type DocumentType = 'Passport' | 'National ID';

export interface PassengerDetail {
  fullName: string;
  /** Whole number 0–120 (PO age feature 2026-07-08). Pure data capture — no business rule
   * is bound to age (DEC-022, PO 2026-07-08). Always submitted as a number, never a string. */
  age: number;
  email: string;
  documentType: DocumentType;
  documentNumber: string;
}

/**
 * Data-minimised passenger entry echoed back in a booking response — full name and age only
 * (feature-booking-flow.md Section 4; matches backend PassengerNameResponse.cs — email/
 * document data is never echoed back).
 */
export interface PassengerNameResponse {
  fullName: string;
  age: number;
}
