import { CabinClass } from './search-request.model';
import { PassengerDetail, PassengerNameResponse } from './passenger-detail.model';

/**
 * The "flight" object nested in a POST /api/v1/bookings request — the full flight-detail
 * snapshot carried from search results (AD-004), not an opaque flight identifier. Matches
 * the backend's BookingFlightRequest contract field-for-field
 * (SkyRoute.Application/Contracts/BookingFlightRequest.cs).
 */
export interface BookingFlightSnapshot {
  provider: string;
  flightNumber: string;
  origin: string;
  destination: string;
  departureDateTime: string;
  arrivalDateTime: string;
  durationMinutes?: number;
  cabinClass: CabinClass;
  baseFare?: number;
  pricePerPassenger: number;
}

/** POST /api/v1/bookings request body — docs/features/feature-booking-flow.md Section 3. */
export interface BookingRequest {
  flight: BookingFlightSnapshot;
  passengerCount: number;
  passengers: PassengerDetail[];
}

/**
 * The "flight" object nested in a POST /api/v1/bookings 201 response — matches the backend's
 * BookingFlightResponse contract (SkyRoute.Application/Contracts/BookingFlightResponse.cs).
 */
export interface BookingFlightResponse {
  provider: string;
  flightNumber: string;
  origin: string;
  destination: string;
  departureDateTime: string;
  arrivalDateTime: string;
  cabinClass: CabinClass;
  pricePerPassenger: number;
}

/** POST /api/v1/bookings 201 response body — docs/features/feature-booking-flow.md Section 4. */
export interface BookingResponse {
  bookingReference: string;
  flight: BookingFlightResponse;
  totalPrice: number;
  passengers: PassengerNameResponse[];
  createdAtUtc: string;
}
