import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../shared/models/api-error.model';
import { FlightResult } from '../../shared/models/flight-result.model';
import { SearchRequest } from '../../shared/models/search-request.model';
import { FlightSearchService } from './flight-search.service';
import { SearchStateService } from './search-state.service';

function buildRequest(): SearchRequest {
  return {
    origin: 'LHR',
    destination: 'JFK',
    departureDate: '2026-08-01',
    passengerCount: 2,
    cabinClass: 'Economy',
    tripType: 'OneWay',
  };
}

function buildResults(): FlightResult[] {
  return [
    {
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
  ];
}

describe('SearchStateService', () => {
  let fakeFlightSearchService: { search: ReturnType<typeof vi.fn> };
  let service: SearchStateService;

  beforeEach(() => {
    fakeFlightSearchService = { search: vi.fn() };

    TestBed.configureTestingModule({
      providers: [SearchStateService, { provide: FlightSearchService, useValue: fakeFlightSearchService }],
    });

    service = TestBed.inject(SearchStateService);
  });

  it('starts with empty state and isEmpty false before any search', () => {
    expect(service.results()).toEqual([]);
    expect(service.hasSearched()).toBe(false);
    expect(service.isEmpty()).toBe(false);
  });

  it('on success, sets results, sets hasSearched, clears loading, and returns "success"', async () => {
    const results = buildResults();
    fakeFlightSearchService.search.mockReturnValue(of(results));

    const outcome = await service.search(buildRequest());

    expect(outcome).toBe('success');
    expect(service.results()).toEqual(results);
    expect(service.hasSearched()).toBe(true);
    expect(service.loading()).toBe(false);
    expect(service.errorMessage()).toBeNull();
    expect(service.fieldErrors()).toBeNull();
  });

  it('records the last search criteria', async () => {
    fakeFlightSearchService.search.mockReturnValue(of(buildResults()));
    const request = buildRequest();

    await service.search(request);

    expect(service.lastCriteria()).toEqual(request);
  });

  it('AUD-003: does NOT record lastCriteria for a failed search (criteria and results move together)', async () => {
    // First, a successful A→B search records its criteria.
    const firstRequest = buildRequest();
    fakeFlightSearchService.search.mockReturnValue(of(buildResults()));
    await service.search(firstRequest);
    expect(service.lastCriteria()).toEqual(firstRequest);

    // A subsequent failed re-search must NOT overwrite lastCriteria — otherwise the /results
    // recap would describe the failed query above the still-visible previous results.
    const failedRequest: SearchRequest = { ...firstRequest, destination: 'LAX' };
    fakeFlightSearchService.search.mockReturnValue(throwError(() => ({ kind: 'message', message: 'Server error' }) as ApiError));
    await service.search(failedRequest);

    expect(service.lastCriteria()).toEqual(firstRequest); // unchanged
    expect(service.results()).toEqual(buildResults()); // previous results intact
  });

  it('on a validation ApiError, sets fieldErrors and returns "validation"', async () => {
    const apiError: ApiError = { kind: 'validation', errors: { origin: ['Origin airport is required.'] } };
    fakeFlightSearchService.search.mockReturnValue(throwError(() => apiError));

    const outcome = await service.search(buildRequest());

    expect(outcome).toBe('validation');
    expect(service.fieldErrors()).toEqual({ origin: ['Origin airport is required.'] });
    expect(service.errorMessage()).toBeNull();
    expect(service.loading()).toBe(false);
  });

  it('on a message ApiError, sets errorMessage and returns "error"', async () => {
    const apiError: ApiError = { kind: 'message', message: "We couldn't complete your search. Please try again." };
    fakeFlightSearchService.search.mockReturnValue(throwError(() => apiError));

    const outcome = await service.search(buildRequest());

    expect(outcome).toBe('error');
    expect(service.errorMessage()).toBe("We couldn't complete your search. Please try again.");
    expect(service.fieldErrors()).toBeNull();
    expect(service.loading()).toBe(false);
  });

  it('isEmpty is true only after a successful search that returned zero results', async () => {
    fakeFlightSearchService.search.mockReturnValue(of([]));

    await service.search(buildRequest());

    expect(service.isEmpty()).toBe(true);
  });

  it('isEmpty is false when a successful search returned results', async () => {
    fakeFlightSearchService.search.mockReturnValue(of(buildResults()));

    await service.search(buildRequest());

    expect(service.isEmpty()).toBe(false);
  });

  it('isEmpty is false when the search resulted in an error', async () => {
    const apiError: ApiError = { kind: 'message', message: 'Server error' };
    fakeFlightSearchService.search.mockReturnValue(throwError(() => apiError));

    await service.search(buildRequest());

    expect(service.isEmpty()).toBe(false);
  });

  it('isEmpty is false while a search is still loading (before the observable emits)', async () => {
    const subject = new Subject<FlightResult[]>();
    fakeFlightSearchService.search.mockReturnValue(subject.asObservable());

    const outcome = service.search(buildRequest());

    expect(service.loading()).toBe(true);
    expect(service.isEmpty()).toBe(false);

    subject.next([]);
    subject.complete();
    await outcome;

    expect(service.loading()).toBe(false);
    expect(service.isEmpty()).toBe(true);
  });

  it('passes the request through to FlightSearchService.search unchanged', async () => {
    fakeFlightSearchService.search.mockReturnValue(of(buildResults()));
    const request = buildRequest();

    await service.search(request);

    expect(fakeFlightSearchService.search).toHaveBeenCalledTimes(1);
    expect(fakeFlightSearchService.search).toHaveBeenCalledWith(request);
  });
});
