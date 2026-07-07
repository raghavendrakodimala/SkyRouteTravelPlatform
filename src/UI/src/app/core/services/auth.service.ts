import { Injectable } from '@angular/core';

/**
 * No-op AuthService (DP-AUTH-003) — the single authoritative point of all authentication-
 * related behaviour on the frontend. In the MVP it does nothing: no token storage, no
 * header injection, no login/logout state (BR-010: no authentication required for MVP). No
 * other component or feature service (FlightSearchService, BookingService) may reference an
 * auth library or embed token-handling logic. When a real auth provider is introduced
 * (OIDC, OAuth 2.0, SAML, SSO — DP-AUTH-005), only this class's implementation changes.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  /** Always true in the MVP — every endpoint is publicly accessible (BR-010). */
  isAuthenticated(): boolean {
    return true;
  }
}
