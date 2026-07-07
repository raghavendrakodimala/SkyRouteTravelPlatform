import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiError, FieldErrors } from '../../shared/models/api-error.model';
import { BookingRequest, BookingResponse } from '../../shared/models/booking-request.model';
import { CabinClass } from '../../shared/models/search-request.model';
import { FlightResult } from '../../shared/models/flight-result.model';
import { BookingService } from './booking.service';

export type BookingOutcome = 'success' | 'validation' | 'error';

/**
 * Signal-based shared state carrying the selected flight (populated by ResultsListComponent
 * with no API call, US-004 AC4) and the booking response after confirmation (BL-032, AD-006).
 * Same one-Observable→Signal-conversion-point rule as SearchStateService — firstValueFrom(...)
 * inside submitBooking() is the ONE conversion point for the booking data flow.
 */
@Injectable({ providedIn: 'root' })
export class BookingStateService {
  private readonly bookingService = inject(BookingService);

  private readonly _selectedFlight = signal<FlightResult | null>(null);
  private readonly _passengerCount = signal(1);
  private readonly _loading = signal(false);
  private readonly _errorMessage = signal<string | null>(null);
  private readonly _fieldErrors = signal<FieldErrors | null>(null);
  private readonly _bookingResponse = signal<BookingResponse | null>(null);

  readonly selectedFlight = this._selectedFlight.asReadonly();
  readonly passengerCount = this._passengerCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();
  readonly fieldErrors = this._fieldErrors.asReadonly();
  readonly bookingResponse = this._bookingResponse.asReadonly();

  /** Called by ResultsListComponent's "Select"/"Book" action — no API call (FR-025, US-004 AC4). */
  selectFlight(flight: FlightResult, passengerCount: number, _cabinClass: CabinClass): void {
    this._selectedFlight.set(flight);
    this._passengerCount.set(passengerCount);
    this._bookingResponse.set(null);
    this._errorMessage.set(null);
    this._fieldErrors.set(null);
  }

  async submitBooking(request: BookingRequest): Promise<BookingOutcome> {
    // US-006 AC7/FR-038: once a booking has already been confirmed for the current selection,
    // block re-submission without a deliberate navigation back to search.
    if (this._bookingResponse()) {
      return 'success';
    }

    this._loading.set(true);
    this._errorMessage.set(null);
    this._fieldErrors.set(null);

    try {
      const response = await firstValueFrom(this.bookingService.createBooking(request));
      this._bookingResponse.set(response);
      this._loading.set(false);
      return 'success';
    } catch (err) {
      this._loading.set(false);
      const apiError = err as ApiError;
      if (apiError.kind === 'validation') {
        this._fieldErrors.set(apiError.errors);
        return 'validation';
      }
      this._errorMessage.set(apiError.message);
      return 'error';
    }
  }
}
