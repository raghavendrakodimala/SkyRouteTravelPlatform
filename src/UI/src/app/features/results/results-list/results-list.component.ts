import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { getAirportByCode } from '../../../shared/constants/airports.constants';
import { FlightResult } from '../../../shared/models/flight-result.model';
import { formatDuration, formatTime } from '../../../shared/utils/datetime-format.util';
import { formatUsd } from '../../../shared/utils/pricing.util';
import { DEFAULT_SORT_OPTION, SORT_OPTION_LABELS, SortOption, sortFlights } from '../../../shared/utils/sort-flights.util';
import { BookingStateService } from '../../booking/booking-state.service';
import { SearchStateService } from '../../search/search-state.service';
import { SortControlComponent } from '../sort-control/sort-control.component';

/**
 * Renders the current search result set (BL-029). Reads exclusively from SearchStateService
 * signals — no new HTTP call (US-002 AC1, AC8). Sorting is a pure computed() recombination
 * of the existing results signal (FR-021, matching architecture-plan.md Section 4.3's
 * `sortedResults = computed(() => sortFlights(...))` example literally).
 */
@Component({
  selector: 'app-results-list',
  standalone: true,
  imports: [SortControlComponent, RouterLink],
  templateUrl: './results-list.component.html',
  styleUrl: './results-list.component.css',
})
export class ResultsListComponent {
  private readonly router = inject(Router);
  protected readonly searchState = inject(SearchStateService);
  private readonly bookingState = inject(BookingStateService);

  protected readonly sortOption = signal<SortOption>(DEFAULT_SORT_OPTION);

  protected readonly sortedResults = computed(() => sortFlights(this.searchState.results(), this.sortOption()));

  /** AUD-019/AUD-022 (WCAG 4.1.3): a single always-present polite live region. It announces
   * "Searching…" on the first search (a conditionally-inserted region drops that first
   * announcement) and, crucially, announces that the list reordered after a sort — the sort
   * label is part of the text, so activating a new sort changes it and triggers an SR read.
   * Errors are left to the assertive role="alert" banner, so they are not duplicated here. */
  protected readonly liveStatus = computed(() => {
    if (this.searchState.loading()) {
      return 'Searching for flights…';
    }
    if (this.searchState.errorMessage()) {
      return '';
    }
    if (this.searchState.isEmpty()) {
      return 'No flights found for your search.';
    }
    const count = this.sortedResults().length;
    if (count > 0) {
      const flightWord = count === 1 ? 'flight' : 'flights';
      return `Showing ${count} ${flightWord}, sorted by ${SORT_OPTION_LABELS[this.sortOption()]}.`;
    }
    return '';
  });

  /** Recap line under the heading — route, date, and cabin of the search being shown. */
  protected readonly searchRecap = computed(() => {
    const criteria = this.searchState.lastCriteria();
    if (!criteria) {
      return null;
    }
    const date = new Date(`${criteria.departureDate}T00:00:00`);
    const dateLabel = Number.isNaN(date.getTime())
      ? criteria.departureDate
      : date.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short' });
    return `${this.cityLabel(criteria.origin)} → ${this.cityLabel(criteria.destination)} · ${dateLabel} · ${criteria.cabinClass}`;
  });

  onSortChange(option: SortOption): void {
    this.sortOption.set(option);
  }

  selectFlight(result: FlightResult): void {
    const criteria = this.searchState.lastCriteria();
    const passengerCount = criteria?.passengerCount ?? 1;
    this.bookingState.selectFlight(result, passengerCount, result.cabinClass);
    void this.router.navigate(['/booking']);
  }

  cityLabel(code: string): string {
    const airport = getAirportByCode(code);
    return airport ? `${airport.city} (${code})` : code;
  }

  /** Provider identity badge — initials from the provider name (e.g. "GlobalAir" → "GA"). */
  providerInitials(provider: string): string {
    const capitals = provider.match(/[A-Z]/g);
    return (capitals && capitals.length >= 2 ? capitals.slice(0, 2).join('') : provider.slice(0, 2)).toUpperCase();
  }

  /** Per-provider badge color class; unknown providers fall back to the brand style. */
  providerClass(provider: string): string {
    switch (provider) {
      case 'GlobalAir':
        return 'badge-ga';
      case 'BudgetWings':
        return 'badge-bw';
      default:
        return 'badge-default';
    }
  }

  formatFlightTime(isoDateTime: string): string {
    return formatTime(isoDateTime);
  }

  formatFlightDuration(durationMinutes: number): string {
    return formatDuration(durationMinutes);
  }

  /** AUD-009: passenger count is chosen later, at booking — so the results figure is a
   * per-person fare, never a "total". Showing it as "$X total" (which always equalled the
   * per-person figure here) misled shoppers who then saw a real multi-passenger total at
   * booking. Results now shows the single per-person fare; "total" is reserved for booking,
   * where the passenger count is known. */
  farePriceText(result: FlightResult): string {
    return formatUsd(result.pricePerPassenger);
  }

  /** A11Y-002 (Phase 17 accessibility review, WCAG 4.1.2/2.4.6): every "Select" button must have
   * a unique, self-describing accessible name — otherwise a screen reader announces
   * "Select, Select, Select..." with no way to tell rows apart. Reuses the same formatting
   * helpers already driving the row's visible text so the accessible name and visual content
   * never drift apart. */
  selectButtonLabel(result: FlightResult): string {
    return (
      `Select ${result.provider} flight ${result.flightNumber}, ` +
      `${this.cityLabel(result.origin)} to ${this.cityLabel(result.destination)}, ` +
      `departing ${this.formatFlightTime(result.departureDateTime)}, ${this.farePriceText(result)} per person`
    );
  }
}
