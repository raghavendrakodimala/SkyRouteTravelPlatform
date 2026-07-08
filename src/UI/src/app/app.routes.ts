import { Routes } from '@angular/router';
import { hasBookingResponseGuard, hasSearchedGuard, hasSelectedFlightGuard } from './core/guards/booking-flow.guards';
import { bookingLeaveGuard } from './features/booking/booking-leave.guard';
import { BookingFormComponent } from './features/booking/booking-form/booking-form.component';
import { ConfirmationComponent } from './features/confirmation/confirmation/confirmation.component';
import { ResultsListComponent } from './features/results/results-list/results-list.component';
import { SearchFormComponent } from './features/search/search-form/search-form.component';

/**
 * Four-route shell (architecture-plan.md Section 4.4, BL-020). /booking and /confirmation
 * carry a functional CanActivate guard (Should Have) redirecting to /search when the
 * required upstream state is absent, avoiding a broken UI on direct URL entry.
 *
 * A11Y-003 fix (Phase 17 accessibility review, docs/reviews/accessibility-review-phase-17.md;
 * WCAG 2.1 SC 2.4.2 Page Titled): each route declares a distinct, descriptive `title`. Angular's
 * Router applies this via its built-in `DefaultTitleStrategy`, which calls the `Title` service
 * (`@angular/platform-browser`, `providedIn: 'root'`) automatically on every successful
 * navigation — no extra provider/service is required beyond `provideRouter(routes)` already
 * registered in `app.config.ts`.
 */
export const routes: Routes = [
  { path: '', redirectTo: 'search', pathMatch: 'full' },
  { path: 'search', component: SearchFormComponent, title: 'Search Flights — SkyRoute' },
  {
    path: 'results',
    component: ResultsListComponent,
    // AUD-005: redirect to /search on refresh/deep-link when no search is in state.
    canActivate: [hasSearchedGuard],
    title: 'Flight Results — SkyRoute',
  },
  {
    path: 'booking',
    component: BookingFormComponent,
    canActivate: [hasSelectedFlightGuard],
    // CR-001 (DESIGN-FLOW-001 §B.11): router leg of the leave guard — in-app navigation
    // away from an armed wizard must confirm before passenger data is destroyed.
    canDeactivate: [bookingLeaveGuard],
    title: 'Booking Details — SkyRoute',
  },
  {
    path: 'confirmation',
    component: ConfirmationComponent,
    canActivate: [hasBookingResponseGuard],
    title: 'Booking Confirmed — SkyRoute',
  },
  { path: '**', redirectTo: 'search' },
];
