import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../shared/models/api-error.model';
import { FlightResult } from '../../shared/models/flight-result.model';
import { BookingRequest, BookingResponse } from '../../shared/models/booking-request.model';
import { BookingService } from './booking.service';
import { BookingStateService } from './booking-state.service';

function buildFlight(): FlightResult {
  return {
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
  };
}

function buildBookingRequest(): BookingRequest {
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
    passengers: [{ fullName: 'Jane Doe', email: 'jane@example.com', documentType: 'Passport', documentNumber: 'AB123456' }],
  };
}

function buildBookingResponse(): BookingResponse {
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
    passengers: [{ fullName: 'Jane Doe' }],
    createdAtUtc: '2026-07-06T00:00:00Z',
  };
}

describe('BookingStateService', () => {
  let fakeBookingService: { createBooking: ReturnType<typeof vi.fn> };
  let service: BookingStateService;

  beforeEach(() => {
    fakeBookingService = { createBooking: vi.fn() };

    TestBed.configureTestingModule({
      providers: [BookingStateService, { provide: BookingService, useValue: fakeBookingService }],
    });

    service = TestBed.inject(BookingStateService);
  });

  describe('selectFlight', () => {
    it('sets selectedFlight and passengerCount', () => {
      const flight = buildFlight();

      service.selectFlight(flight, 3, 'Economy');

      expect(service.selectedFlight()).toEqual(flight);
      expect(service.passengerCount()).toBe(3);
    });

    it('clears any prior bookingResponse, errorMessage, and fieldErrors', async () => {
      const apiError: ApiError = { kind: 'message', message: 'boom' };
      fakeBookingService.createBooking.mockReturnValue(throwError(() => apiError));
      service.selectFlight(buildFlight(), 1, 'Economy');
      await service.submitBooking(buildBookingRequest());
      expect(service.errorMessage()).toBe('boom');

      fakeBookingService.createBooking.mockReturnValue(of(buildBookingResponse()));
      service.selectFlight(buildFlight(), 2, 'Business');

      expect(service.bookingResponse()).toBeNull();
      expect(service.errorMessage()).toBeNull();
      expect(service.fieldErrors()).toBeNull();
    });
  });

  describe('submitBooking', () => {
    it('on success, sets bookingResponse and returns "success"', async () => {
      const response = buildBookingResponse();
      fakeBookingService.createBooking.mockReturnValue(of(response));

      const outcome = await service.submitBooking(buildBookingRequest());

      expect(outcome).toBe('success');
      expect(service.bookingResponse()).toEqual(response);
      expect(service.loading()).toBe(false);
    });

    it('on a validation ApiError, sets fieldErrors and returns "validation"', async () => {
      const apiError: ApiError = { kind: 'validation', errors: { 'passengers[0].email': ['Invalid email.'] } };
      fakeBookingService.createBooking.mockReturnValue(throwError(() => apiError));

      const outcome = await service.submitBooking(buildBookingRequest());

      expect(outcome).toBe('validation');
      expect(service.fieldErrors()).toEqual({ 'passengers[0].email': ['Invalid email.'] });
      expect(service.bookingResponse()).toBeNull();
    });

    it('on a message ApiError, sets errorMessage and returns "error"', async () => {
      const apiError: ApiError = { kind: 'message', message: "We couldn't complete your booking. Please try again." };
      fakeBookingService.createBooking.mockReturnValue(throwError(() => apiError));

      const outcome = await service.submitBooking(buildBookingRequest());

      expect(outcome).toBe('error');
      expect(service.errorMessage()).toBe("We couldn't complete your booking. Please try again.");
      expect(service.bookingResponse()).toBeNull();
    });

    it('re-submission guard: a second call after a successful booking short-circuits to "success" without a new HTTP call', async () => {
      const response = buildBookingResponse();
      fakeBookingService.createBooking.mockReturnValue(of(response));

      const firstOutcome = await service.submitBooking(buildBookingRequest());
      const secondOutcome = await service.submitBooking(buildBookingRequest());

      expect(firstOutcome).toBe('success');
      expect(secondOutcome).toBe('success');
      expect(fakeBookingService.createBooking).toHaveBeenCalledTimes(1);
    });
  });
});
