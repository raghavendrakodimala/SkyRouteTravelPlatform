import { describe, expect, it } from 'vitest';
import { FlightResult } from '../models/flight-result.model';
import { DEFAULT_SORT_OPTION, sortFlights } from './sort-flights.util';

function buildFlight(overrides: Partial<FlightResult>): FlightResult {
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

describe('sortFlights', () => {
  const flightA = buildFlight({
    flightNumber: 'A',
    pricePerPassenger: 300,
    durationMinutes: 600,
    departureDateTime: '2026-08-01T14:00:00Z',
  });
  const flightB = buildFlight({
    flightNumber: 'B',
    pricePerPassenger: 150,
    durationMinutes: 300,
    departureDateTime: '2026-08-01T06:00:00Z',
  });
  const flightC = buildFlight({
    flightNumber: 'C',
    pricePerPassenger: 220,
    durationMinutes: 450,
    departureDateTime: '2026-08-01T10:00:00Z',
  });
  const flightD = buildFlight({
    flightNumber: 'D',
    pricePerPassenger: 220,
    durationMinutes: 720,
    departureDateTime: '2026-08-01T20:00:00Z',
  });

  const results: readonly FlightResult[] = [flightA, flightB, flightC, flightD];

  it('DEFAULT_SORT_OPTION is priceAsc', () => {
    expect(DEFAULT_SORT_OPTION).toBe('priceAsc');
  });

  it('priceAsc orders by pricePerPassenger ascending', () => {
    const sorted = sortFlights(results, 'priceAsc');
    expect(sorted.map((f) => f.flightNumber)).toEqual(['B', 'C', 'D', 'A']);
  });

  it('priceDesc orders by pricePerPassenger descending', () => {
    const sorted = sortFlights(results, 'priceDesc');
    expect(sorted.map((f) => f.flightNumber)).toEqual(['A', 'C', 'D', 'B']);
  });

  it('durationAsc orders by durationMinutes ascending', () => {
    const sorted = sortFlights(results, 'durationAsc');
    expect(sorted.map((f) => f.flightNumber)).toEqual(['B', 'C', 'A', 'D']);
  });

  it('departureAsc orders by departureDateTime ascending', () => {
    const sorted = sortFlights(results, 'departureAsc');
    expect(sorted.map((f) => f.flightNumber)).toEqual(['B', 'C', 'A', 'D']);
  });

  it('does not mutate the input array', () => {
    const originalOrder = results.map((f) => f.flightNumber);
    sortFlights(results, 'priceAsc');
    expect(results.map((f) => f.flightNumber)).toEqual(originalOrder);
  });

  it('returns a new array instance, not the same reference', () => {
    const sorted = sortFlights(results, 'priceAsc');
    expect(sorted).not.toBe(results);
  });

  it('is stable: elements with equal pricePerPassenger retain relative input order under priceAsc', () => {
    // flightC and flightD both have pricePerPassenger 220; C appears before D in the input.
    const sorted = sortFlights(results, 'priceAsc');
    const cIndex = sorted.findIndex((f) => f.flightNumber === 'C');
    const dIndex = sorted.findIndex((f) => f.flightNumber === 'D');
    expect(cIndex).toBeLessThan(dIndex);
  });
});
