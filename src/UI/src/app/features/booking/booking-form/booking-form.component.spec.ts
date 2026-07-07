import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { routes } from '../../../app.routes';
import { BookingResponse } from '../../../shared/models/booking-request.model';
import { FieldErrors } from '../../../shared/models/api-error.model';
import { FlightResult } from '../../../shared/models/flight-result.model';
import { SearchStateService } from '../../search/search-state.service';
import { BookingStateService } from '../booking-state.service';
import { BookingFormComponent } from './booking-form.component';

/** Navigation target for the post-201 redirect — keeps router.navigate() from erroring. */
@Component({ template: '' })
class BlankComponent {}

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
    totalPrice: 500,
    passengers: [
      { fullName: 'Passenger 1', age: 31 },
      { fullName: 'Passenger 2', age: 32 },
    ],
    createdAtUtc: '2026-07-06T00:00:00Z',
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
    // The service still exposes the searched count; the component no longer reads it
    // (PO decision 2026-07-07: search always sends 1; count is decided during booking).
    passengerCountSignal = signal(1);
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
      providers: [
        provideRouter([{ path: '**', component: BlankComponent }]),
        { provide: BookingStateService, useValue: fakeBookingState },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BookingFormComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function q<T extends HTMLElement>(selector: string): T | null {
    return fixture.nativeElement.querySelector(selector);
  }

  function qa(selector: string): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll(selector));
  }

  function text(selector: string): string {
    return q(selector)?.textContent ?? '';
  }

  function liveRegionText(): string {
    return text('p[aria-live="polite"]');
  }

  function setInput(id: string, value: string): void {
    const input = q<HTMLInputElement>(`#${id}`);
    if (!input) {
      throw new Error(`No input #${id} in the DOM`);
    }
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
  }

  /** Dispatches a real DOM submit on the single in-place form (ngSubmit path — the same
   * path Enter and the type="submit" primary button take). */
  function submitActiveForm(): void {
    const form = q<HTMLFormElement>('form');
    if (!form) {
      throw new Error('No active form in the DOM');
    }
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();
  }

  function click(selector: string): void {
    const el = q<HTMLElement>(selector);
    if (!el) {
      throw new Error(`No element ${selector} in the DOM`);
    }
    el.click();
    fixture.detectChanges();
  }

  /** Fills the single active form (whose input ids carry `index`). Passenger `n` gets the
   * deterministic adult age `30 + n` (always ≥ 18, so valid at any position). */
  function fillForm(n: number, index: number): void {
    setInput(`fullName-${index}`, `Passenger ${n}`);
    setInput(`age-${index}`, String(30 + n));
    setInput(`email-${index}`, `p${n}@example.com`);
    setInput(`documentNumber-${index}`, `AB12345${n}`);
  }

  /** Fills and saves `count` passengers via "Add another passenger" (in-place flow). */
  function addPassengers(count: number): void {
    for (let i = 0; i < count; i += 1) {
      fillForm(i + 1, i);
      click('#add-another-btn');
    }
  }

  /** Flushes pending await-continuations (e.g. onConfirm outcome handling), then rendering +
   * afterRenderEffect (focus application) in zoneless mode. */
  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
    await fixture.whenStable();
  }

  // ── Ported regression assertions ────────────────────────────────────────────

  it('renders the flight summary from BookingStateService with no additional HTTP call', () => {
    const heading = text('.flight-summary h2');
    expect(heading).toContain('LHR');
    expect(heading).toContain('JFK');
    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
  });

  it('does not call submitBooking merely from rendering the form', () => {
    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
  });

  it('shows the empty state when no flight is selected', () => {
    selectedFlightSignal.set(null);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No flight selected. Please start a new search.');
    expect(fixture.nativeElement.textContent).toContain('Start a New Search');
    expect(q('form')).toBeFalsy();
  });

  it('renders the persistent polite live region unconditionally', () => {
    const region = q('p[aria-live="polite"]');
    expect(region).toBeTruthy();
    expect(region!.getAttribute('role')).toBe('status');

    selectedFlightSignal.set(null); // even with no flight the region stays rendered
    fixture.detectChanges();
    expect(q('p[aria-live="polite"]')).toBeTruthy();
  });

  // ── Initial state: single in-place form, two persistent actions ─────────────

  it('opens with a single blank passenger-1 form and exactly the two persistent actions under it', () => {
    expect(qa('app-passenger-form-section').length).toBe(1);
    expect(text('legend')).toContain('Passenger 1');
    expect(q<HTMLInputElement>('#fullName-0')!.value).toBe('');
    expect(qa('.passenger-card').length).toBe(0);

    const actions = qa('.form-actions button');
    expect(actions.length).toBe(2);
    expect(text('#add-another-btn')).toContain('Add another passenger');
    expect(text('#confirm-booking-btn')).toContain('Confirm Booking');
    // Confirm is the primary, submit-typed action; Add another is a plain secondary button.
    expect(q('#confirm-booking-btn')!.getAttribute('type')).toBe('submit');
    expect(q('#add-another-btn')!.getAttribute('type')).toBe('button');
    // The removed save-then-prompt flow must not resurface.
    expect(fixture.nativeElement.textContent).not.toContain('Save passenger');
    expect(fixture.nativeElement.textContent).not.toContain('Add another passenger?');
    expect(fixture.nativeElement.textContent).not.toContain('Review and confirm');
    expect(fixture.nativeElement.textContent).not.toContain('Your search included');
    expect(q('.wizard-steps')).toBeFalsy();
  });

  // ── "Add another passenger": validate → save → reset in place ──────────────

  it('a valid Add another appends a card above, resets the same form blank in place, and focuses its first field', async () => {
    fillForm(1, 0);
    click('#add-another-btn');
    await settle();

    const card = q('.passenger-card')!;
    expect(card.textContent).toContain('Passenger 1');
    expect(card.textContent).toContain('(Primary Contact)');
    expect(card.textContent).toContain('p1@example.com');
    expect(card.textContent).toContain('Passport Number: AB123451');

    // Same form, same slot below the cards, blanked for passenger 2
    expect(qa('app-passenger-form-section').length).toBe(1);
    expect(text('legend')).toContain('Passenger 2');
    expect(q<HTMLInputElement>('#fullName-1')!.value).toBe('');
    expect(liveRegionText()).toContain('Passenger 1 added. Total for 1 passenger: USD 250.00.');
    expect(document.activeElement?.id).toBe('fullName-1');
    expect(text('.saved-count-status')).toContain('1 passenger added');
  });

  it('an invalid Add another saves nothing, shows the touched errors, and focuses the first invalid control', async () => {
    click('#add-another-btn');
    await settle();

    expect(qa('.passenger-card').length).toBe(0);
    expect(q('form')).toBeTruthy();
    expect(qa('.error').length).toBeGreaterThan(0);
    expect(document.activeElement?.id).toBe('fullName-0');
    expect(document.activeElement).not.toBe(document.body);
  });

  it('card action buttons carry name-qualified aria-labels', () => {
    addPassengers(1);

    expect(q('#card-edit-0')!.getAttribute('aria-label')).toBe('Edit passenger 1, Passenger 1');
    expect(q('#card-remove-0')!.getAttribute('aria-label')).toBe('Remove passenger 1, Passenger 1');
  });

  // ── Price breakdown: max(saved, 1) ──────────────────────────────────────────

  it('prices max(saved, 1): a blank in-progress form never inflates the total', () => {
    // 0 saved + blank form → × 1
    expect(text('.total-price')).toContain('USD 250.00 total');
    expect(text('.per-person-price')).toContain('USD 250.00 per person × 1 passenger');

    // typing into the form still does not inflate the count
    setInput('fullName-0', 'Someone');
    expect(text('.per-person-price')).toContain('× 1 passenger');

    setInput('age-0', '31');
    setInput('email-0', 'p1@example.com');
    setInput('documentNumber-0', 'AB123451');
    click('#add-another-btn'); // 1 saved + blank form → still × 1
    expect(text('.total-price')).toContain('USD 250.00 total');
    expect(text('.per-person-price')).toContain('× 1 passenger');

    fillForm(2, 1);
    click('#add-another-btn'); // 2 saved → × 2
    expect(text('.total-price')).toContain('USD 500.00 total');
    expect(text('.per-person-price')).toContain('× 2 passengers');

    click('#card-edit-0'); // an edit form does not change the count
    expect(text('.total-price')).toContain('USD 500.00 total');

    // The old searched-count copy is gone entirely
    expect(fixture.nativeElement.textContent).not.toContain('your search was for');
  });

  // ── Confirm Booking: the three input states ────────────────────────────────

  it('Confirm with a filled form validates and saves it, then submits ALL saved passengers', async () => {
    fakeBookingState.submitBooking.mockResolvedValue('success');
    addPassengers(1);
    fillForm(2, 1); // dirty active form, not yet saved

    submitActiveForm(); // Confirm Booking is the form's submit action
    await settle();

    expect(fakeBookingState.submitBooking).toHaveBeenCalledTimes(1);
    expect(fakeBookingState.submitBooking).toHaveBeenCalledWith({
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
      passengerCount: 2,
      passengers: [
        { fullName: 'Passenger 1', age: 31, email: 'p1@example.com', documentType: 'Passport', documentNumber: 'AB123451' },
        { fullName: 'Passenger 2', age: 32, email: 'p2@example.com', documentType: 'Passport', documentNumber: 'AB123452' },
      ],
    });
    // Age crosses the wire as a NUMBER, never a string (the backend contract is int).
    const submitted = fakeBookingState.submitBooking.mock.calls[0][0];
    expect(typeof submitted.passengers[0].age).toBe('number');
    expect(typeof submitted.passengers[1].age).toBe('number');
  });

  it('Confirm with an INVALID filled form saves nothing and does not submit (same invalid handling as add)', async () => {
    addPassengers(1);
    setInput('fullName-1', 'Only A Name'); // age, email + document missing

    submitActiveForm();
    await settle();

    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
    expect(qa('.passenger-card').length).toBe(1); // nothing new saved
    expect(qa('.error').length).toBeGreaterThan(0);
    // Age sits between Full Name and Email, so it is the first invalid control here.
    expect(document.activeElement?.id).toBe('age-1');
  });

  it('Confirm with a blank form and saved passengers submits the saved passengers as-is', async () => {
    fakeBookingState.submitBooking.mockResolvedValue('success');
    addPassengers(2); // blank form for passenger 3 is open

    submitActiveForm();
    await settle();

    expect(fakeBookingState.submitBooking).toHaveBeenCalledTimes(1);
    expect(fakeBookingState.submitBooking).toHaveBeenCalledWith(
      expect.objectContaining({
        passengerCount: 2,
        passengers: [
          expect.objectContaining({ fullName: 'Passenger 1' }),
          expect.objectContaining({ fullName: 'Passenger 2' }),
        ],
      }),
    );
  });

  it('Confirm with a blank form and nothing saved surfaces the required errors for passenger 1', async () => {
    submitActiveForm();
    await settle();

    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
    expect(qa('.passenger-card').length).toBe(0);
    expect(qa('.error').length).toBeGreaterThan(0);
    expect(document.activeElement?.id).toBe('fullName-0');
  });

  // ── Age (PO age feature 2026-07-08; DEC-022: pure data capture, 0–120 sanity bounds
  // only — the former AGE-LEAD-18 lead-adult rule was removed) ─────────────────

  it('blocks Confirm client-side when an age is out of range, with the range message', async () => {
    setInput('fullName-0', 'Old Timer');
    setInput('age-0', '121');
    setInput('email-0', 'old@example.com');
    setInput('documentNumber-0', 'AB123456');

    submitActiveForm();
    await settle();

    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
    expect(qa('.passenger-card').length).toBe(0); // nothing saved
    expect(qa('.error').some((el) => el.textContent?.includes('Age is required and must be a whole number between 0 and 120.'))).toBe(true);
    expect(document.activeElement?.id).toBe('age-0');
  });

  it('accepts a lead passenger aged 15 — no lead-adult rule exists (DEC-022)', async () => {
    fakeBookingState.submitBooking.mockResolvedValue('success');
    setInput('fullName-0', 'Young Lead');
    setInput('age-0', '15');
    setInput('email-0', 'lead@example.com');
    setInput('documentNumber-0', 'AB123456');

    submitActiveForm();
    await settle();

    expect(fakeBookingState.submitBooking).toHaveBeenCalledTimes(1);
    const request = fakeBookingState.submitBooking.mock.calls[0][0];
    expect(request.passengers[0].age).toBe(15);
    expect(typeof request.passengers[0].age).toBe('number');
  });

  it('accepts a child at any position and submits every age as a number', async () => {
    fakeBookingState.submitBooking.mockResolvedValue('success');
    addPassengers(1); // passenger 1 (age 31)
    setInput('fullName-1', 'Small Child');
    setInput('age-1', '5');
    setInput('email-1', 'guardian@example.com');
    setInput('documentNumber-1', 'CD567890');

    submitActiveForm();
    await settle();

    expect(fakeBookingState.submitBooking).toHaveBeenCalledTimes(1);
    const request = fakeBookingState.submitBooking.mock.calls[0][0];
    expect(request.passengers.map((p: { age: number }) => p.age)).toEqual([31, 5]);
  });

  it('renders no age hint for any active passenger form (DEC-022 — no lead-adult rule)', () => {
    expect(q('#ageHint-0')).toBeFalsy();

    addPassengers(1);
    expect(q('#ageHint-1')).toBeFalsy();
  });

  it('shows "Age {n}" as a card line for each saved passenger', () => {
    addPassengers(2);

    const cardLines = qa('.card-line').map((el) => el.textContent?.trim() ?? '');
    expect(cardLines.some((line) => line === 'Age 31')).toBe(true);
    expect(cardLines.some((line) => line === 'Age 32')).toBe(true);
  });

  it('maps a passengers[i].age server error back onto the offending passenger and reopens it', async () => {
    fakeBookingState.submitBooking.mockImplementation(async () => {
      fieldErrorsSignal.set({ 'passengers[0].age': ['Age is required and must be a whole number between 0 and 120.'] });
      return 'validation';
    });
    addPassengers(1);

    submitActiveForm();
    await settle();

    const summary = q('#error-summary')!;
    expect(summary.textContent).toContain('Passenger 1: Age is required and must be a whole number between 0 and 120.');
    // The flagged passenger reopens in the in-place edit form with its age loaded and the
    // server message rendered inline.
    expect(text('#save-changes-btn')).toContain('Save changes');
    expect(q<HTMLInputElement>('#age-0')!.value).toBe('31');
    expect(qa('.error').some((el) => el.textContent?.includes('Age is required and must be a whole number between 0 and 120.'))).toBe(true);
  });

  it('a parked draft keeps its age across an edit round-trip', async () => {
    addPassengers(1);
    setInput('fullName-1', 'Draft Person');
    setInput('age-1', '44'); // in-progress passenger 2 draft with an age

    click('#card-edit-0'); // dirty draft parked
    submitActiveForm(); // save the (unchanged, valid) edit
    await settle();

    expect(q<HTMLInputElement>('#fullName-1')!.value).toBe('Draft Person');
    expect(q<HTMLInputElement>('#age-1')!.value).toBe('44'); // age survived the park/restore
  });

  it('navigates to /confirmation after a successful submit', async () => {
    fakeBookingState.submitBooking.mockResolvedValue('success');
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    addPassengers(1);

    submitActiveForm();
    await settle();

    expect(navigateSpy).toHaveBeenCalledWith(['/confirmation']);
  });

  // ── Edit round-trip ─────────────────────────────────────────────────────────

  it('Edit loads the card into the in-place form; Save changes updates the card, restores the blank form, and returns focus to Edit', async () => {
    addPassengers(2);
    click('#card-edit-1');
    await settle();

    // Still exactly one form, in the same slot, pre-filled with the card's values
    expect(qa('app-passenger-form-section').length).toBe(1);
    expect(q<HTMLInputElement>('#fullName-1')!.value).toBe('Passenger 2');
    expect(text('#save-changes-btn')).toContain('Save changes');
    expect(text('#cancel-edit-btn')).toContain('Cancel edit');
    expect(q('#add-another-btn')).toBeFalsy(); // the two persistent actions yield while editing
    expect(q('#confirm-booking-btn')).toBeFalsy();
    expect(qa('.passenger-card-editing').length).toBe(1); // card marked as being edited
    expect(liveRegionText()).toContain('Editing passenger 2.');
    expect(document.activeElement?.id).toBe('fullName-1');

    setInput('fullName-1', 'Updated Name');
    submitActiveForm();
    await settle();

    const names = qa('.card-name').map((el) => el.textContent);
    expect(names[1]).toContain('Updated Name');
    // Blank collecting form restored in place, with the persistent actions back
    expect(q<HTMLInputElement>('#fullName-2')!.value).toBe('');
    expect(q('#add-another-btn')).toBeTruthy();
    expect(q('#confirm-booking-btn')).toBeTruthy();
    expect(liveRegionText()).toContain('Passenger 2 updated.');
    expect(document.activeElement?.id).toBe('card-edit-1');
    expect(document.activeElement).not.toBe(document.body);
  });

  it('Cancel edit discards changes, keeps the saved values, and restores the blank form', async () => {
    addPassengers(1);
    click('#card-edit-0');
    setInput('fullName-0', 'Should Be Discarded');

    click('#cancel-edit-btn');
    await settle();

    expect(text('.card-name')).toContain('Passenger 1');
    expect(fixture.nativeElement.textContent).not.toContain('Should Be Discarded');
    expect(q<HTMLInputElement>('#fullName-1')!.value).toBe(''); // blank form back in place
    expect(liveRegionText()).toContain('Edit cancelled.');
    expect(document.activeElement?.id).toBe('card-edit-0');
  });

  it('blocks Edit on other cards while an edit form is dirty (aria-disabled + reason suffix + click guard)', () => {
    addPassengers(2);
    click('#card-edit-0');

    // Pristine edit: the other card's Edit stays enabled
    expect(q('#card-edit-1')!.getAttribute('aria-disabled')).toBeNull();

    setInput('fullName-0', 'Dirty Now');
    expect(q('#card-edit-1')!.getAttribute('aria-disabled')).toBe('true');
    expect(q('#card-edit-1')!.getAttribute('aria-label')).toContain('(finish the current passenger first)');

    click('#card-edit-1'); // click guard no-ops — still editing card 0
    expect(q<HTMLInputElement>('#fullName-0')).toBeTruthy();
    expect(q<HTMLInputElement>('#fullName-0')!.value).toBe('Dirty Now');
  });

  it('parks a dirty new-passenger draft during an edit and restores it afterwards', async () => {
    addPassengers(1);
    setInput('fullName-1', 'Draft Person'); // in-progress passenger 2 draft

    click('#card-edit-0'); // dirty draft parked
    expect(q<HTMLInputElement>('#fullName-0')!.value).toBe('Passenger 1');

    submitActiveForm(); // save the (unchanged, valid) edit
    await settle();

    expect(q<HTMLInputElement>('#fullName-1')!.value).toBe('Draft Person'); // draft restored
    expect(liveRegionText()).toContain('Returning to passenger 2 details.');
    // `parkedDraft` is protected (state internal); test-only any-cast to assert cleanup.
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    expect((fixture.componentInstance as any).parkedDraft()).toBeNull();
  });

  // ── Remove, renumbering, minimum-1 ──────────────────────────────────────────

  it('removing a card renumbers the remaining passengers positionally and moves focus to the next Remove', async () => {
    addPassengers(3);

    click('#card-remove-0');
    await settle();

    const eyebrows = qa('.card-eyebrow').map((el) => el.textContent ?? '');
    expect(eyebrows.length).toBe(2);
    expect(eyebrows[0]).toContain('Passenger 1');
    expect(eyebrows[1]).toContain('Passenger 2');
    const names = qa('.card-name').map((el) => el.textContent ?? '');
    expect(names[0]).toContain('Passenger 2'); // former passenger 2 is now first
    expect(liveRegionText()).toContain('Passenger 1 removed. Remaining passengers renumbered.');
    expect(document.activeElement?.id).toBe('card-remove-0');
  });

  it('removing the last remaining card keeps the blank passenger-1 form open (minimum-1 by construction)', async () => {
    addPassengers(1);

    click('#card-remove-0');
    await settle();

    expect(qa('.passenger-card').length).toBe(0);
    expect(q<HTMLInputElement>('#fullName-0')).toBeTruthy();
    expect(text('legend')).toContain('Passenger 1');
    expect(liveRegionText()).toContain('Passenger removed. Add at least one passenger to continue.');
    expect(document.activeElement?.id).toBe('fullName-0');
  });

  // ── 9-passenger cap ─────────────────────────────────────────────────────────

  it('saving the 9th passenger removes the add path and the blank form, shows the cap note, and keeps Confirm', async () => {
    addPassengers(9);
    await settle();

    expect(qa('.passenger-card').length).toBe(9);
    expect(q('#add-another-btn')).toBeFalsy();
    expect(q('form')).toBeFalsy(); // nothing left to collect
    expect(text('.cap-note')).toContain("You've reached the maximum of 9 passengers for one booking.");
    expect(q('#confirm-booking-btn')).toBeTruthy();
    expect(liveRegionText()).toContain('Passenger 9 added. Maximum of 9 passengers reached.');
    expect(document.activeElement?.id).toBe('confirm-booking-btn');
  });

  it('Confirm at the cap submits all 9 saved passengers', async () => {
    fakeBookingState.submitBooking.mockResolvedValue('success');
    addPassengers(9);

    click('#confirm-booking-btn');
    await settle();

    expect(fakeBookingState.submitBooking).toHaveBeenCalledTimes(1);
    const request = fakeBookingState.submitBooking.mock.calls[0][0];
    expect(request.passengerCount).toBe(9);
    expect(request.passengers.length).toBe(9);
  });

  it('editing stays possible at the cap and returns to the cap state (no add path) afterwards', async () => {
    addPassengers(9);
    click('#card-edit-0');

    expect(q<HTMLInputElement>('#fullName-0')!.value).toBe('Passenger 1');
    setInput('fullName-0', 'Updated At Cap');
    submitActiveForm();
    await settle();

    expect(text('.card-name')).toContain('Updated At Cap');
    expect(q('form')).toBeFalsy();
    expect(q('#add-another-btn')).toBeFalsy();
    expect(q('#confirm-booking-btn')).toBeTruthy();
  });

  // ── Submit: loading lockdown, outcomes ──────────────────────────────────────

  it('locks every mutating control with aria-disabled while the POST is in flight', () => {
    addPassengers(1);

    loadingSignal.set(true);
    fixture.detectChanges();

    const confirm = q('#confirm-booking-btn')!;
    expect(confirm.textContent).toContain('Confirming…');
    expect(confirm.getAttribute('aria-disabled')).toBe('true');
    expect(confirm.hasAttribute('disabled')).toBe(false); // focus must survive
    expect(q('#card-edit-0')!.getAttribute('aria-disabled')).toBe('true');
    expect(q('#card-remove-0')!.getAttribute('aria-disabled')).toBe('true');
    expect(q('#add-another-btn')!.getAttribute('aria-disabled')).toBe('true');

    click('#card-remove-0'); // click guard: the saved list cannot mutate mid-flight
    expect(qa('.passenger-card').length).toBe(1);
    submitActiveForm(); // double-submit guard
    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
  });

  it('a generic submit failure renders the focused error banner', async () => {
    fakeBookingState.submitBooking.mockImplementation(async () => {
      errorMessageSignal.set("We couldn't complete your booking. Please try again.");
      return 'error';
    });
    addPassengers(1);

    submitActiveForm();
    await settle();

    const banner = q('#generic-error-banner')!;
    expect(banner.textContent).toContain("We couldn't complete your booking. Please try again.");
    expect(banner.getAttribute('role')).toBe('alert');
    expect(q('#confirm-booking-btn')).toBeTruthy(); // still confirmable after the failure
  });

  // ── Server field-error mapping ──────────────────────────────────────────────

  it('maps passengers[i].* errors to a summary banner, badges, and a chained repair flow reopening the right passengers', async () => {
    fakeBookingState.submitBooking.mockImplementation(async () => {
      fieldErrorsSignal.set({
        'passengers[0].fullName': ['Full name is required.'],
        'passengers[1].email': ['A valid email address is required.'],
      });
      return 'validation';
    });
    addPassengers(2);

    submitActiveForm();
    await settle();

    // Summary banner with one line per offending passenger, backend text verbatim
    const summary = q('#error-summary')!;
    expect(summary.getAttribute('role')).toBe('alert');
    expect(summary.textContent).toContain("We couldn't confirm your booking. Please correct the details below.");
    expect(summary.textContent).toContain('Passenger 1: Full name is required.');
    expect(summary.textContent).toContain('Passenger 2: A valid email address is required.');

    // Auto-opens the smallest offending index in the in-place form with its server errors shown
    expect(q<HTMLInputElement>('#fullName-0')!.value).toBe('Passenger 1');
    expect(text('#save-changes-btn')).toContain('Save changes');
    expect(qa('.error').some((el) => el.textContent?.includes('Full name is required.'))).toBe(true);

    // The other offending card is badged
    expect(text('.needs-correction-badge')).toContain('Needs correction');

    // Repairing passenger 1 chains to passenger 2
    setInput('fullName-0', 'Repaired Name');
    submitActiveForm();
    await settle();
    expect(q<HTMLInputElement>('#fullName-1')!.value).toBe('Passenger 2');
    expect(liveRegionText()).toContain('Editing passenger 2.');
    expect(qa('.error').some((el) => el.textContent?.includes('A valid email address is required.'))).toBe(true);

    // Repairing the last flagged passenger clears the banner and restores the blank form
    setInput('email-1', 'repaired@example.com');
    submitActiveForm();
    await settle();
    expect(q('#error-summary')).toBeFalsy();
    expect(q('.needs-correction-badge')).toBeFalsy();
    expect(q<HTMLInputElement>('#fullName-2')!.value).toBe('');
    expect(q('#confirm-booking-btn')).toBeTruthy();
    expect(liveRegionText()).toContain('All corrections saved.');
    expect(qa('.card-name').map((el) => el.textContent).join(' ')).toContain('Repaired Name');
  });

  it('a structural change (Remove) clears all indexed server errors and badges', async () => {
    fakeBookingState.submitBooking.mockImplementation(async () => {
      fieldErrorsSignal.set({ 'passengers[1].email': ['A valid email address is required.'] });
      return 'validation';
    });
    addPassengers(2);
    submitActiveForm();
    await settle();

    // Flagged passenger 2 auto-opened; cancel back to the blank form, then remove passenger 1
    click('#cancel-edit-btn');
    expect(q('#error-summary')).toBeTruthy();

    click('#card-remove-0');
    expect(q('#error-summary')).toBeFalsy();
    expect(q('.needs-correction-badge')).toBeFalsy();
  });

  // ── Re-submission guard (already confirmed) ─────────────────────────────────

  it('back-nav after confirmation renders read-only (FR-038): banner, aria-disabled Confirm, no form, no Edit/Remove', () => {
    bookingResponseSignal.set(buildBookingResponse());
    const confirmedFixture = TestBed.createComponent(BookingFormComponent);
    confirmedFixture.detectChanges();
    const root: HTMLElement = confirmedFixture.nativeElement;

    expect(root.querySelector('.banner-info')?.textContent).toContain(
      'This booking has already been confirmed. Start a new search to book again.',
    );
    const confirm = root.querySelector<HTMLButtonElement>('#confirm-booking-btn')!;
    expect(confirm.getAttribute('aria-disabled')).toBe('true');
    expect(root.querySelector('form')).toBeFalsy();
    expect(root.querySelector('#card-edit-0')).toBeFalsy();
    expect(root.querySelector('#add-another-btn')).toBeFalsy();

    confirm.click();
    expect(fakeBookingState.submitBooking).not.toHaveBeenCalled();
  });

  it('back-nav after confirmation shows the BOOKED total and count from the response (CR-003)', () => {
    const response = buildBookingResponse();
    response.totalPrice = 750;
    response.passengers = [
      { fullName: 'P One', age: 40 },
      { fullName: 'P Two', age: 35 },
      { fullName: 'P Three', age: 9 },
    ];
    bookingResponseSignal.set(response);

    const confirmedFixture = TestBed.createComponent(BookingFormComponent);
    confirmedFixture.detectChanges();
    const root: HTMLElement = confirmedFixture.nativeElement;

    // Breakdown comes from the response itself, not recomputed locally
    expect(root.querySelector('.total-price')?.textContent).toContain('USD 750.00 total');
    expect(root.querySelector('.per-person-price')?.textContent).toContain('× 3 passengers');
  });

  // ── Navigation guard ────────────────────────────────────────────────────────

  it('canLeave() allows leaving silently when nothing has been entered, and prompts once data exists', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

    expect(fixture.componentInstance.canLeave()).toBe(true);
    expect(confirmSpy).not.toHaveBeenCalled();

    addPassengers(1);
    expect(fixture.componentInstance.canLeave()).toBe(false);
    expect(confirmSpy).toHaveBeenCalledWith("Leave this page? Passenger details you've entered will be lost.");
  });
});

// ── CR-001: bookingLeaveGuard must be REGISTERED on the real /booking route ──────────────────
// These tests run against the actual `routes` from app.routes.ts, so they fail if the
// canDeactivate registration is ever dropped — canLeave() unit coverage alone cannot catch that.
describe('BookingFormComponent — bookingLeaveGuard route registration (CR-001)', () => {
  beforeEach(async () => {
    const fakeBookingState = {
      selectedFlight: signal<FlightResult | null>(buildFlight()),
      passengerCount: signal(1),
      loading: signal(false),
      errorMessage: signal<string | null>(null),
      fieldErrors: signal<FieldErrors | null>(null),
      bookingResponse: signal<BookingResponse | null>(null),
      submitBooking: vi.fn(),
    };
    // SearchFormComponent activates at /search after a permitted leave; fake its state
    // service so no HttpClient wiring is needed.
    const fakeSearchState = {
      loading: signal(false),
      fieldErrors: signal<FieldErrors | null>(null),
      errorMessage: signal<string | null>(null),
      search: vi.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        { provide: BookingStateService, useValue: fakeBookingState },
        { provide: SearchStateService, useValue: fakeSearchState },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  /** Types into the in-place form's first field so the leave guard arms (dirty active form). */
  function enterDraftData(harness: RouterTestingHarness): void {
    const input = harness.routeNativeElement!.querySelector<HTMLInputElement>('#fullName-0');
    if (!input) {
      throw new Error('No #fullName-0 input rendered at /booking');
    }
    input.value = 'Ada Lovelace';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();
  }

  it('aborts router navigation away from /booking when entered data exists and the user cancels', async () => {
    const harness = await RouterTestingHarness.create('/booking');
    enterDraftData(harness);

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const router = TestBed.inject(Router);

    await expect(router.navigateByUrl('/results')).resolves.toBe(false); // navigation blocked
    expect(confirmSpy).toHaveBeenCalledWith("Leave this page? Passenger details you've entered will be lost.");
    expect(router.url).toBe('/booking'); // still on the form, data intact
  });

  it('proceeds with router navigation when the user confirms leaving with unsaved data', async () => {
    const harness = await RouterTestingHarness.create('/booking');
    enterDraftData(harness);

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const router = TestBed.inject(Router);

    await expect(router.navigateByUrl('/search')).resolves.toBe(true);
    expect(confirmSpy).toHaveBeenCalledTimes(1);
    expect(router.url).toBe('/search');
  });

  it('leaves /booking silently (no confirm) when nothing has been entered', async () => {
    await RouterTestingHarness.create('/booking');

    const confirmSpy = vi.spyOn(window, 'confirm');
    const router = TestBed.inject(Router);

    await expect(router.navigateByUrl('/search')).resolves.toBe(true);
    expect(confirmSpy).not.toHaveBeenCalled();
  });
});
