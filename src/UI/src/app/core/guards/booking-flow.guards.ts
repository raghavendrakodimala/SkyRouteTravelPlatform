import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { BookingStateService } from '../../features/booking/booking-state.service';
import { SearchStateService } from '../../features/search/search-state.service';

/**
 * Should Have (architecture-plan.md Section 4.4) — usability robustness guards preventing a
 * broken UI on direct URL entry to /booking or /confirmation. Not FR-mandated; redirects to
 * /search when the required upstream state is absent, since neither screen may perform a new
 * API call to reconstruct it (FR-025, US-004 AC4).
 */
export const hasSelectedFlightGuard: CanActivateFn = () => {
  const bookingState = inject(BookingStateService);
  const router = inject(Router);
  return bookingState.selectedFlight() !== null ? true : router.createUrlTree(['/search']);
};

/**
 * AUD-005: refreshing or deep-linking /results with no search in state left the user on a
 * blank, contentless page (only the heading and "Modify search"). Mirror the /booking and
 * /confirmation guards: redirect to /search when no search has been run, since /results may
 * not perform a new API call to reconstruct the result set (US-002 AC1/AC8). A legitimate
 * empty-result search sets hasSearched, so the styled "No flights found" state is preserved.
 */
export const hasSearchedGuard: CanActivateFn = () => {
  const searchState = inject(SearchStateService);
  const router = inject(Router);
  return searchState.hasSearched() ? true : router.createUrlTree(['/search']);
};

export const hasBookingResponseGuard: CanActivateFn = () => {
  const bookingState = inject(BookingStateService);
  const router = inject(Router);
  return bookingState.bookingResponse() !== null ? true : router.createUrlTree(['/search']);
};
