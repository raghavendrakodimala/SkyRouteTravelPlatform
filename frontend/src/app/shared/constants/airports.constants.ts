import { Airport } from '../models/airport.model';

/**
 * Single authoritative frontend source of airport data (AD-002, DP-012, FR-055). Must not be
 * duplicated in any component or service file. The backend independently maintains its own
 * equivalent static list (SkyRoute.Application/Data/AirportDataService.cs) for validation —
 * each copy is the single source for its own layer (FR-055); the two lists are kept
 * consistent for a coherent demo but are not the same object reference.
 *
 * Minimum data set per FR-056–059 (6 airports, 2+ countries, 2+ airports sharing a country).
 */
export const AIRPORTS: readonly Airport[] = [
  { code: 'LHR', city: 'London', country: 'United Kingdom', displayName: 'London Heathrow (LHR)' },
  { code: 'MAN', city: 'Manchester', country: 'United Kingdom', displayName: 'Manchester (MAN)' },
  { code: 'JFK', city: 'New York', country: 'United States', displayName: 'New York JFK (JFK)' },
  { code: 'LAX', city: 'Los Angeles', country: 'United States', displayName: 'Los Angeles (LAX)' },
  { code: 'DXB', city: 'Dubai', country: 'United Arab Emirates', displayName: 'Dubai (DXB)' },
  { code: 'SYD', city: 'Sydney', country: 'Australia', displayName: 'Sydney (SYD)' },
];

export function getAirportByCode(code: string | null | undefined): Airport | undefined {
  if (!code) {
    return undefined;
  }
  return AIRPORTS.find((airport) => airport.code === code);
}

export function getCountryForCode(code: string | null | undefined): string | undefined {
  return getAirportByCode(code)?.country;
}
