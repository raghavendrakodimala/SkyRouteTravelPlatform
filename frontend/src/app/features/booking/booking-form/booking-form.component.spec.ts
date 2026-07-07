import { FormGroup } from '@angular/forms';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BookingResponse } from '../../../shared/models/booking-request.model';
import { FieldErrors } from '../../../shared/models/api-error.model';
import { FlightResult } from '../../../shared/models/flight-result.model';
import { BookingStateService } from '../booking-state.service';
import { BookingFormComponent } from './booking-form.component';

// `passengersForm` and `passengerGroups()` are `protected` on BookingFormComponent
// (template-only access by design). An `any` cast is used here purely to reach them from the
// test without weakening the component's public API — this is test-only code, never
// application code.
function passengersForm(c: BookingFormComponent) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return (c as any).passengersForm as { invalid: boolean };
}

function passengerGroups(c: BookingFormComponent): FormGroup[] {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return (c as any).passengerGroups();
}

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

describe('BookingFormComponent', () => {
  let fixture: ComponentFixture<BookingFormComponent>;
  let selectedFlightSignal: ReturnType<typeof signal<FlightResult | null>>;
  let passengerCountSignal: ReturnType<typeof signal<number>>;
  let loadingSignal: ReturnType<typeof signal<boolean>>;
  let errorMessageSignal: ReturnType<typeof signal<string | null>>;
  let fieldErrorsSignal: ReturnType<typeof signal<FieldErrors | null>>;
  let bookingResponseSignal: ReturnType<typeof signal<BookingResponse | null>>;
  let fakeBookingState: {
    selectedFlight: typeof selectedFlightSignal;
    passengerCount: typeof passengerCountSignal;
    loading: typeof loadingSignal;
    errorMessage: typeof errorMessageSignal;
    fieldErrors: typeof fieldErrorsSignal;
    bookingResponse: typeof bookingResponseSignal;
    submitBooking: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    selectedFlightSignal = signal<FlightResult | null>(buildFlight());
    passengerCountSignal = signal(2);
    loadingSignal = signal(false);
    errorMessageSignal = signal<string | null>(null);
    fieldErrorsSignal = signal<FieldErrors | null>(null);
    bookingResponseSignal = signal<BookingResponse | null>(null);

    fakeBookingState = {
      selectedFlight: selectedFlightSignal,
      passengerCount: passengerCountSignal,
      loading: loadingSignal,
      errorMessage: errorMessageSignal,
      fieldErrors: fieldErrorsSignal,
      bookingResponse: bookingResponseSignal,
      submitBooking: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [BookingFormComponent],
      providers: [provideRouter([]), { provide: BookingStateService, useValue: fakeBookingState }],
    }).compileComponents();

    fixture = TestBed.createComponent(BookingFormComponent);
    fixture.detectChanges();
  });

  function submitButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[type="submit"]');
  }

  it('renders the flight summary from BookingStateService with no additional HTTP call', () => {
    const heading = fixture.nativeElement.querySelector('.flight-summary h2');
    expect(heading.textContent).toContain('LHR');
    expect(heading.textContent).toContain('JFK');
    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
  });

  it('renders the price breakdown total and per-person figures from the shared pricing calculation', () => {
    const total = fixture.nativeElement.querySelector('.price-breakdown .total-price');
    const perPerson = fixture.nativeElement.querySelector('.price-breakdown .per-person-price');
    expect(total.textContent).toContain('USD 500.00 total');
    expect(perPerson.textContent).toContain('USD 250.00 per person');
    expect(perPerson.textContent).toContain('2 passenger(s)');
  });

  it('renders one passenger-form-section per passengerCount', () => {
    const sections = fixture.nativeElement.querySelectorAll('app-passenger-form-section');
    expect(sections.length).toBe(2);
  });

  it('disables the submit button while the passenger form array is invalid', () => {
    expect(passengersForm(fixture.componentInstance).invalid).toBe(true);
    expect(submitButton().disabled).toBe(true);
  });

  it('enables the submit button once all passenger groups are validly filled', () => {
    const groups = passengerGroups(fixture.componentInstance);
    groups.forEach((group, index) => {
      group.patchValue({
        fullName: `Passenger ${index}`,
        email: `passenger${index}@example.com`,
        documentNumber: 'AB123456',
      });
    });
    fixture.detectChanges();

    expect(submitButton().disabled).toBe(false);
  });

  it('disables the submit button once bookingResponse() is already set (re-submission guard)', () => {
    const groups = passengerGroups(fixture.componentInstance);
    groups.forEach((group, index) => {
      group.patchValue({
        fullName: `Passenger ${index}`,
        email: `passenger${index}@example.com`,
        documentNumber: 'AB123456',
      });
    });
    bookingResponseSignal.set({
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
      totalPrice: 500,
      passengers: [{ fullName: 'Passenger 0' }, { fullName: 'Passenger 1' }],
      createdAtUtc: '2026-07-06T00:00:00Z',
    });
    fixture.detectChanges();

    expect(submitButton().disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('.banner-info')).toBeTruthy();
  });

  it('does not call submitBooking merely from rendering the form', () => {
    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
  });
});
