import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/**
 * A11Y-001 fix (Phase 17 accessibility review, docs/reviews/accessibility-review-phase-17.md;
 * NFR-A11Y-006, WCAG 2.1 SC 2.4.3 Focus Order): moves keyboard/screen-reader focus to the new
 * screen's top-level heading after every route transition.
 *
 * Without this, the element focused at the moment of navigation (e.g. the "Select" button on a
 * result card) is torn down along with the outgoing screen's component, and default browser
 * behaviour silently drops focus back to `<body>` — the exact non-compliant outcome the finding
 * describes on all 4 primary transitions (`/search`→`/results`→`/booking`→`/confirmation`→
 * `/search`).
 *
 * Implementation notes:
 * - A single, centralized `NavigationEnd` subscriber is used (per the finding's own
 *   recommendation) instead of four one-off `.focus()` calls scattered across
 *   `SearchFormComponent`/`ResultsListComponent`/`BookingFormComponent`/`ConfirmationComponent`.
 * - The target element is resolved generically (first `<h1>`, else first `<h2>`, else first
 *   `[role="heading"]`, else the routed view's own root element inside `<main>`) rather than
 *   hard-coded per screen, so this keeps working regardless of which heading level each screen
 *   ends up using (heading-hierarchy consistency itself is tracked separately as A11Y-006 and is
 *   intentionally not changed here).
 * - The very first navigation (initial app bootstrap, `''` → `/search`) is intentionally not
 *   refocused — the finding is scoped to the 4 *transitions* between already-loaded screens, and
 *   stealing focus away from the browser's own initial-load state is not the documented failure
 *   mode.
 */
@Injectable({ providedIn: 'root' })
export class RouteFocusService {
  private static readonly HEADING_SELECTOR = 'main h1, main h2, main [role="heading"]';

  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);

  private started = false;
  private isFirstNavigation = true;

  /** Called once from the app root component so the subscription lives for the app's lifetime. */
  start(): void {
    if (this.started) {
      return;
    }
    this.started = true;

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.onNavigationEnd());
  }

  private onNavigationEnd(): void {
    if (this.isFirstNavigation) {
      this.isFirstNavigation = false;
      return;
    }

    // Deferred so the routed component has finished rendering into <router-outlet> before the
    // DOM is searched for its heading.
    setTimeout(() => this.focusMainHeading());
  }

  private focusMainHeading(): void {
    const target = this.document.querySelector<HTMLElement>(RouteFocusService.HEADING_SELECTOR) ?? this.findFallbackTarget();
    if (!target) {
      return;
    }

    if (!target.hasAttribute('tabindex')) {
      target.setAttribute('tabindex', '-1');
    }
    target.focus();
  }

  /** Used only when a screen has no `<h1>`/`<h2>`/`[role="heading"]` at all (none of the 4
   * current screens hits this path, but it keeps the mechanism robust for future screens): the
   * routed component's own root element, i.e. the first child of `<main>` that is not the
   * `<router-outlet>` anchor itself. */
  private findFallbackTarget(): HTMLElement | null {
    const main = this.document.querySelector<HTMLElement>('main');
    if (!main) {
      return null;
    }
    const child = Array.from(main.children).find((el) => el.tagName.toLowerCase() !== 'router-outlet');
    return (child as HTMLElement) ?? null;
  }
}
