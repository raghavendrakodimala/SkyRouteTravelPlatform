import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { BookingStateService } from '../../features/booking/booking-state.service';

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

export const hasBookingResponseGuard: CanActivateFn = () => {
  const bookingState = inject(BookingStateService);
  const router = inject(Router);
  return bookingState.bookingResponse() !== null ? true : router.createUrlTree(['/search']);
};
