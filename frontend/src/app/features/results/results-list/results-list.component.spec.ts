import { Component, computed, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FlightResult } from '../../../shared/models/flight-result.model';
import { SearchRequest } from '../../../shared/models/search-request.model';
import { BookingStateService } from '../../booking/booking-state.service';
import { SearchStateService } from '../../search/search-state.service';
import { ResultsListComponent } from './results-list.component';

// Trivial stand-in for the real `/booking` route target. `selectFlight()` calls
// `router.navigate(['/booking'])`, so the test router config needs a matching route registered
// or that navigation rejects with NG04002 ("Cannot match any routes"). This component renders
// nothing and is never asserted against.
@Component({ standalone: true, template: '' })
class BookingStubComponent {}

function buildFlight(overrides: Partial<FlightResult> = {}): FlightResult {
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
    ...overrides,
  };
}

describe('ResultsListComponent', () => {
  let fixture: ComponentFixture<ResultsListComponent>;
  let resultsSignal: ReturnType<typeof signal<FlightResult[]>>;
  let loadingSignal: ReturnType<typeof signal<boolean>>;
  let errorMessageSignal: ReturnType<typeof signal<string | null>>;
  let hasSearchedSignal: ReturnType<typeof signal<boolean>>;
  let lastCriteriaSignal: ReturnType<typeof signal<SearchRequest | null>>;
  let fakeSearchState: {
    results: typeof resultsSignal;
    loading: typeof loadingSignal;
    errorMessage: typeof errorMessageSignal;
    hasSearched: typeof hasSearchedSignal;
    lastCriteria: typeof lastCriteriaSignal;
    isEmpty: ReturnType<typeof computed<boolean>>;
    search: ReturnType<typeof vi.fn>;
  };
  let fakeBookingState: { selectFlight: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    resultsSignal = signal<FlightResult[]>([]);
    loadingSignal = signal(false);
    errorMessageSignal = signal<string | null>(null);
    hasSearchedSignal = signal(false);
    lastCriteriaSignal = signal<SearchRequest | null>(null);

    fakeSearchState = {
      results: resultsSignal,
      loading: loadingSignal,
      errorMessage: errorMessageSignal,
      hasSearched: hasSearchedSignal,
      lastCriteria: lastCriteriaSignal,
      isEmpty: computed(() => hasSearchedSignal() && !loadingSignal() && !errorMessageSignal() && resultsSignal().length === 0),
      search: vi.fn(),
    };

    fakeBookingState = { selectFlight: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ResultsListComponent],
      providers: [
        provideRouter([{ path: 'booking', component: BookingStubComponent }]),
        { provide: SearchStateService, useValue: fakeSearchState },
        { provide: BookingStateService, useValue: fakeBookingState },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResultsListComponent);
  });

  it('renders a loading indicator while loading, and no result rows', () => {
    loadingSignal.set(true);
    resultsSignal.set([buildFlight()]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.loading')).toBeTruthy();
    expect(fixture.nativeElement.querySelectorAll('.result-card').length).toBe(0);
  });

  it('renders the exact empty-state message when isEmpty() is true', () => {
    hasSearchedSignal.set(true);
    resultsSignal.set([]);
    fixture.detectChanges();

    const emptyEl = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyEl).toBeTruthy();
    expect(emptyEl.textContent.trim()).toBe('No flights found for your search. Please try different criteria.');
  });

  it('renders one result-card per result in a non-empty result set', () => {
    hasSearchedSignal.set(true);
    resultsSignal.set([buildFlight({ flightNumber: 'GA100' }), buildFlight({ flightNumber: 'GA200' })]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.result-card').length).toBe(2);
  });

  it('reorders displayed rows on sort-option change without calling SearchStateService.search again', () => {
    hasSearchedSignal.set(true);
    const cheap = buildFlight({ flightNumber: 'CHEAP', pricePerPassenger: 100 });
    const pricey = buildFlight({ flightNumber: 'PRICEY', pricePerPassenger: 900 });
    resultsSignal.set([pricey, cheap]);
    fixture.detectChanges();

    let numbers: string[] = Array.from<Element>(fixture.nativeElement.querySelectorAll('.provider')).map(
      (el: Element) => el.textContent ?? '',
    );
    expect(numbers[0]).toContain('CHEAP');

    fixture.componentInstance.onSortChange('priceDesc');
    fixture.detectChanges();

    numbers = Array.from<Element>(fixture.nativeElement.querySelectorAll('.provider')).map((el: Element) => el.textContent ?? '');
    expect(numbers[0]).toContain('PRICEY');

    expect(fakeSearchState.search).not.toHaveBeenCalled();
  });

  it('gives each Select button a unique, descriptive accessible name (A11Y-002)', () => {
    hasSearchedSignal.set(true);
    resultsSignal.set([
      buildFlight({ flightNumber: 'GA100', provider: 'GlobalAir' }),
      buildFlight({ flightNumber: 'GA200', provider: 'SkyJet' }),
    ]);
    fixture.detectChanges();

    const buttons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('.result-card button'));
    expect(buttons.length).toBe(2);

    const labels = buttons.map((button) => button.getAttribute('aria-label'));
    expect(labels[0]).toBeTruthy();
    expect(labels[1]).toBeTruthy();
    // Names must be unique so a screen reader does not announce "Select, Select" identically.
    expect(labels[0]).not.toBe(labels[1]);
    expect(labels[0]).toContain('GA100');
    expect(labels[0]).toContain('GlobalAir');
    expect(labels[1]).toContain('GA200');
    expect(labels[1]).toContain('SkyJet');
    // Visible label text must remain unchanged.
    expect(buttons[0].textContent?.trim()).toBe('Select');
  });

  it('selectFlight delegates to BookingStateService.selectFlight without an additional HTTP call', () => {
    lastCriteriaSignal.set({
      origin: 'LHR',
      destination: 'JFK',
      departureDate: '2026-08-01',
      passengerCount: 3,
      cabinClass: 'Economy',
      tripType: 'OneWay',
    });
    const flight = buildFlight();

    fixture.componentInstance.selectFlight(flight);

    expect(fakeBookingState.selectFlight).toHaveBeenCalledWith(flight, 3, flight.cabinClass);
    expect(fakeSearchState.search).not.toHaveBeenCalled();
  });
});
