/**
 * Per-passenger booking record fields (US-005, FR-040). Field names match the backend's
 * PassengerRequest contract (SkyRoute.Application/Contracts/PassengerRequest.cs).
 */

/** Exactly "Passport" or "National ID" — note the space (FR-040). */
export type DocumentType = 'Passport' | 'National ID';

export interface PassengerDetail {
  fullName: string;
  email: string;
  documentType: DocumentType;
  documentNumber: string;
}

/**
 * Data-minimised passenger entry echoed back in a booking response — full name only
 * (feature-booking-flow.md Section 4; matches backend PassengerNameResponse.cs).
 */
export interface PassengerNameResponse {
  fullName: string;
}
