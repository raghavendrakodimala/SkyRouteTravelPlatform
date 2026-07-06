import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BookingRequest, BookingResponse } from '../../shared/models/booking-request.model';
import { mapHttpError } from '../../shared/utils/http-error.util';

/**
 * The sole place HttpClient is injected for booking (DP-010, DP-PROTOCOL-006). No component
 * injects HttpClient directly. Same HTTP-boundary error-mapping rule as FlightSearchService.
 */
@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);

  createBooking(request: BookingRequest): Observable<BookingResponse> {
    return this.http.post<BookingResponse>(`${environment.apiBaseUrl}/bookings`, request).pipe(
      catchError((error: HttpErrorResponse) =>
        throwError(() => mapHttpError(error, "We couldn't complete your booking. Please try again.")),
      ),
    );
  }
}
