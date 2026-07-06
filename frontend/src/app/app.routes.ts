import { Routes } from '@angular/router';
import { hasBookingResponseGuard, hasSelectedFlightGuard } from './core/guards/booking-flow.guards';
import { BookingFormComponent } from './features/booking/booking-form/booking-form.component';
import { ConfirmationComponent } from './features/confirmation/confirmation/confirmation.component';
import { ResultsListComponent } from './features/results/results-list/results-list.component';
import { SearchFormComponent } from './features/search/search-form/search-form.component';

/**
 * Four-route shell (architecture-plan.md Section 4.4, BL-020). /booking and /confirmation
 * carry a functional CanActivate guard (Should Have) redirecting to /search when the
 * required upstream state is absent, avoiding a broken UI on direct URL entry.
 */
export const routes: Routes = [
  { path: '', redirectTo: 'search', pathMatch: 'full' },
  { path: 'search', component: SearchFormComponent },
  { path: 'results', component: ResultsListComponent },
  { path: 'booking', component: BookingFormComponent, canActivate: [hasSelectedFlightGuard] },
  { path: 'confirmation', component: ConfirmationComponent, canActivate: [hasBookingResponseGuard] },
  { path: '**', redirectTo: 'search' },
];
