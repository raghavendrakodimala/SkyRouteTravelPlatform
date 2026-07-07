import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { RouteFocusService } from './core/services/route-focus.service';

/** The four stops of the booking journey, in order — drives the header stepper. */
const JOURNEY_STEPS = [
  { path: '/search', label: 'Search' },
  { path: '/results', label: 'Results' },
  { path: '/booking', label: 'Booking' },
  { path: '/confirmation', label: 'Confirmation' },
] as const;

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  // A11Y-001 fix (docs/reviews/accessibility-review-phase-17.md, NFR-A11Y-006): starts the
  // centralized route-focus-management subscription for the lifetime of the app shell.
  private readonly routeFocusService = inject(RouteFocusService);
  private readonly router = inject(Router);

  protected readonly steps = JOURNEY_STEPS;

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );

  /** Index of the journey step matching the current route (unknown routes read as Search). */
  protected readonly currentStepIndex = computed(() => {
    const url = this.currentUrl();
    const index = JOURNEY_STEPS.findIndex((step) => url.startsWith(step.path));
    return index === -1 ? 0 : index;
  });

  constructor() {
    this.routeFocusService.start();
  }
}
