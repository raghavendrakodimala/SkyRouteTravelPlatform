import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BookingResponse } from '../../../shared/models/booking-request.model';
import { BookingStateService } from '../../booking/booking-state.service';
import { ConfirmationComponent } from './confirmation.component';

function buildBookingResponse(overrides: Partial<BookingResponse> = {}): BookingResponse {
  return {
    bookingReference: 'SR-000042',
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
    passengers: [
      { fullName: 'Jane Doe', age: 34 },
      { fullName: 'John Doe', age: 36 },
    ],
    createdAtUtc: '2026-07-06T00:00:00Z',
    ...overrides,
  };
}

describe('ConfirmationComponent', () => {
  let fixture: ComponentFixture<ConfirmationComponent>;
  let bookingResponseSignal: ReturnType<typeof signal<BookingResponse | null>>;
  let fakeBookingState: { bookingResponse: typeof bookingResponseSignal };

  beforeEach(async () => {
    bookingResponseSignal = signal<BookingResponse | null>(null);
    fakeBookingState = { bookingResponse: bookingResponseSignal };

    await TestBed.configureTestingModule({
      imports: [ConfirmationComponent],
      providers: [provideRouter([]), { provide: BookingStateService, useValue: fakeBookingState }],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmationComponent);
  });

  it('renders a fallback message when there is no booking response', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No booking confirmation available');
    expect(fixture.nativeElement.querySelector('.booking-reference')).toBeFalsy();
  });

  it('renders the bookingReference prominently', () => {
    bookingResponseSignal.set(buildBookingResponse());
    fixture.detectChanges();

    const refEl = fixture.nativeElement.querySelector('.booking-reference');
    expect(refEl.textContent).toContain('SR-000042');
  });

  it('renders the flight summary', () => {
    bookingResponseSignal.set(buildBookingResponse());
    fixture.detectChanges();

    const summary = fixture.nativeElement.querySelector('.flight-summary');
    expect(summary.textContent).toContain('LHR');
    expect(summary.textContent).toContain('JFK');
    expect(summary.textContent).toContain('GlobalAir');
    expect(summary.textContent).toContain('GA100');
  });

  it('renders totalPrice via formatUsd from the server response only, with USD prefix and 2dp', () => {
    bookingResponseSignal.set(buildBookingResponse({ totalPrice: 500 }));
    fixture.detectChanges();

    const totalEl = fixture.nativeElement.querySelector('.total-price');
    expect(totalEl.textContent).toContain('USD 500.00 total');
  });

  it('displays the response-supplied totalPrice as-is and does not recompute it from pricePerPassenger/passengerCount', () => {
    // pricePerPassenger is 250 and there are 2 passengers, which would calculate to 500 via
    // calculateTotalPrice — but here the server-supplied totalPrice deliberately differs (e.g.
    // a promotional discount applied server-side) to prove the component displays the response
    // value verbatim rather than recomputing it.
    bookingResponseSignal.set(buildBookingResponse({ totalPrice: 425 }));
    fixture.detectChanges();

    const totalEl = fixture.nativeElement.querySelector('.total-price');
    expect(totalEl.textContent).toContain('USD 425.00 total');
    expect(totalEl.textContent).not.toContain('USD 500.00');
  });

  it('renders the passenger name list', () => {
    bookingResponseSignal.set(buildBookingResponse());
    fixture.detectChanges();

    const names: string[] = Array.from<Element>(fixture.nativeElement.querySelectorAll('.passengers li')).map(
      (el: Element) => el.textContent ?? '',
    );
    expect(names).toEqual(['Jane Doe', 'John Doe']);
  });

  it('AUD-001/AUD-040: renders BOTH passengers when two share a full name (track $index, no key collision)', () => {
    bookingResponseSignal.set(
      buildBookingResponse({
        passengers: [
          { fullName: 'John Smith', age: 40 },
          { fullName: 'John Smith', age: 38 },
        ],
      }),
    );
    fixture.detectChanges();

    const names: string[] = Array.from<Element>(fixture.nativeElement.querySelectorAll('.passengers li')).map(
      (el: Element) => el.textContent?.trim() ?? '',
    );
    // With the old `track passenger.fullName`, the duplicate key collapsed this to one row.
    expect(names).toEqual(['John Smith', 'John Smith']);
  });

  it('AUD-023: the h1 is described by the booking reference (announced on arrival), and the reference is not role=status', () => {
    bookingResponseSignal.set(buildBookingResponse());
    fixture.detectChanges();

    const h1: HTMLElement = fixture.nativeElement.querySelector('h1');
    expect(h1.getAttribute('aria-describedby')).toBe('booking-ref-eyebrow booking-reference');
    const ref: HTMLElement = fixture.nativeElement.querySelector('#booking-reference');
    expect(ref).toBeTruthy();
    expect(ref.getAttribute('role')).toBeNull(); // the ineffective role=status was removed
  });

  it('AUD-017: the no-booking state is a styled card with a "Start a New Search" action, not bare text', () => {
    fixture.detectChanges(); // booking() is null

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    const button: HTMLButtonElement | null = emptyState.querySelector('button');
    expect(button?.textContent).toContain('Start a New Search');
  });

  it('AUD-023: the h1 has no aria-describedby when there is no booking', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('h1').getAttribute('aria-describedby')).toBeNull();
  });

  it('has no control that resubmits the prior booking request (US-006 AC7) — only a "Start a New Search" navigation action', () => {
    bookingResponseSignal.set(buildBookingResponse());
    fixture.detectChanges();

    const buttons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('button'));
    expect(buttons.length).toBe(1);
    expect(buttons[0].textContent).toContain('Start a New Search');
    expect(fixture.nativeElement.querySelector('form')).toBeFalsy();
  });

  it('clicking "Start a New Search" navigates to /search and does not resubmit the booking', () => {
    bookingResponseSignal.set(buildBookingResponse());
    fixture.detectChanges();

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();

    expect(navigateSpy).toHaveBeenCalledWith(['/search']);
  });
});
