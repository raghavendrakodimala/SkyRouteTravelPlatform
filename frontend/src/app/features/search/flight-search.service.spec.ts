import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { ApiError } from '../../shared/models/api-error.model';
import { FlightResult } from '../../shared/models/flight-result.model';
import { SearchRequest } from '../../shared/models/search-request.model';
import { FlightSearchService } from './flight-search.service';

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

describe('FlightSearchService', () => {
  let service: FlightSearchService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), FlightSearchService],
    });

    service = TestBed.inject(FlightSearchService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('issues exactly one POST to {apiBaseUrl}/search with the request body', () => {
    const request = buildRequest();

    service.search(request).subscribe();

    const requests = httpMock.match(`${environment.apiBaseUrl}/search`);
    expect(requests.length).toBe(1);
    const req = requests[0];
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);

    req.flush(buildResults());
  });

  it('resolves with the FlightResult[] returned by a successful response', () => {
    const request = buildRequest();
    const results = buildResults();
    let actual: FlightResult[] | undefined;

    service.search(request).subscribe((value) => (actual = value));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/search`);
    req.flush(results);

    expect(actual).toEqual(results);
  });

  it('maps a 400 validation error response to a validation ApiError via the catchError pipeline', () => {
    const request = buildRequest();
    let actualError: ApiError | undefined;

    service.search(request).subscribe({
      next: () => {
        throw new Error('expected an error, got a success value');
      },
      error: (err) => (actualError = err as ApiError),
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/search`);
    req.flush({ errors: { origin: ['Origin airport is required.'] } }, { status: 400, statusText: 'Bad Request' });

    expect(actualError).toEqual({ kind: 'validation', errors: { origin: ['Origin airport is required.'] } });
  });

  it('maps a 500 error response to a message ApiError with the service-specific generic message', () => {
    const request = buildRequest();
    let actualError: ApiError | undefined;

    service.search(request).subscribe({
      next: () => {
        throw new Error('expected an error, got a success value');
      },
      error: (err) => (actualError = err as ApiError),
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/search`);
    req.flush({ message: 'internal details' }, { status: 500, statusText: 'Internal Server Error' });

    expect(actualError).toEqual({ kind: 'message', message: "We couldn't complete your search. Please try again." });
  });
});
