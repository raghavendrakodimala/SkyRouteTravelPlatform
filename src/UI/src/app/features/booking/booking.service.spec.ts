import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { ApiError } from '../../shared/models/api-error.model';
import { BookingRequest, BookingResponse } from '../../shared/models/booking-request.model';
import { BookingService } from './booking.service';

function buildRequest(): BookingRequest {
  return {
    flight: {
      provider: 'GlobalAir',
      flightNumber: 'GA100',
      origin: 'LHR',
      destination: 'JFK',
      departureDateTime: '2026-08-01T09:00:00Z',
      arrivalDateTime: '2026-08-01T17:00:00Z',
      durationMinutes: 480,
      cabinClass: 'Economy',
      baseFare: 200,
      pricePerPassenger: 250,
    },
    passengerCount: 1,
    passengers: [
      { fullName: 'Jane Doe', age: 34, email: 'jane@example.com', documentType: 'Passport', documentNumber: 'AB123456' },
    ],
  };
}

function buildResponse(): BookingResponse {
  return {
    bookingReference: 'SR-000001',
    flight: {
      provider: 'GlobalAir',
      flightNumber: 'GA100',
      origin: 'LHR',
      destination: 'JFK',
      departureDateTime: '2026-08-01T09:00:00Z',
      arrivalDateTime: '2026-08-01T17:00:00Z',
      cabinClass: 'Economy',
      pricePerPassenger: 250,
    },
    totalPrice: 250,
    passengers: [{ fullName: 'Jane Doe', age: 34 }],
    createdAtUtc: '2026-07-06T00:00:00Z',
  };
}

describe('BookingService', () => {
  let service: BookingService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), BookingService],
    });

    service = TestBed.inject(BookingService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('issues exactly one POST to {apiBaseUrl}/bookings with the request body', () => {
    const request = buildRequest();

    service.createBooking(request).subscribe();

    const requests = httpMock.match(`${environment.apiBaseUrl}/bookings`);
    expect(requests.length).toBe(1);
    const req = requests[0];
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);

    req.flush(buildResponse());
  });

  it('resolves with the BookingResponse returned by a successful (201) response', () => {
    const request = buildRequest();
    const response = buildResponse();
    let actual: BookingResponse | undefined;

    service.createBooking(request).subscribe((value) => (actual = value));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/bookings`);
    req.flush(response, { status: 201, statusText: 'Created' });

    expect(actual).toEqual(response);
  });

  it('maps a 400 validation error response to a validation ApiError via the catchError pipeline', () => {
    const request = buildRequest();
    let actualError: ApiError | undefined;

    service.createBooking(request).subscribe({
      next: () => {
        throw new Error('expected an error, got a success value');
      },
      error: (err) => (actualError = err as ApiError),
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/bookings`);
    req.flush(
      { errors: { 'passengers[0].email': ['A valid email address is required.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(actualError).toEqual({
      kind: 'validation',
      errors: { 'passengers[0].email': ['A valid email address is required.'] },
    });
  });

  it('maps a 500 error response to a message ApiError with the service-specific generic message', () => {
    const request = buildRequest();
    let actualError: ApiError | undefined;

    service.createBooking(request).subscribe({
      next: () => {
        throw new Error('expected an error, got a success value');
      },
      error: (err) => (actualError = err as ApiError),
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/bookings`);
    req.flush({ message: 'internal details' }, { status: 500, statusText: 'Internal Server Error' });

    expect(actualError).toEqual({ kind: 'message', message: "We couldn't complete your booking. Please try again." });
  });
});
