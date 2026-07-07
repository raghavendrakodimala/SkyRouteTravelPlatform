import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { formatTime } from '../../../shared/utils/datetime-format.util';
import { formatUsd } from '../../../shared/utils/pricing.util';
import { BookingStateService } from '../../booking/booking-state.service';

/**
 * Displays the booking reference prominently, flight summary, total price, and passenger
 * names (BL-035, US-006 AC4–5), reading exclusively from BookingStateService — no new API
 * call. totalPrice is the server-supplied value from the booking response; this component
 * only formats it (formatUsd), it does not recompute it (DP-011 governs calculation, not
 * display formatting of an already-final server value).
 */
@Component({
  selector: 'app-confirmation',
  standalone: true,
  templateUrl: './confirmation.component.html',
  styleUrl: './confirmation.component.css',
})
export class ConfirmationComponent {
  private readonly router = inject(Router);
  protected readonly bookingState = inject(BookingStateService);

  protected readonly booking = this.bookingState.bookingResponse;

  formatFlightTime(isoDateTime: string): string {
    return formatTime(isoDateTime);
  }

  formatPrice(amount: number): string {
    return formatUsd(amount);
  }

  /** US-006 AC7/FR-038: the only path back into a new booking flow is a deliberate
   * navigation back to search — this screen has no control that re-submits the prior
   * BookingRequest. */
  startNewSearch(): void {
    void this.router.navigate(['/search']);
  }
}
