import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { getAirportByCode } from '../../../shared/constants/airports.constants';
import { FlightResult } from '../../../shared/models/flight-result.model';
import { formatDuration, formatTime } from '../../../shared/utils/datetime-format.util';
import { calculateTotalPrice, formatUsd } from '../../../shared/utils/pricing.util';
import { DEFAULT_SORT_OPTION, SortOption, sortFlights } from '../../../shared/utils/sort-flights.util';
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
  imports: [SortControlComponent],
  templateUrl: './results-list.component.html',
  styleUrl: './results-list.component.css',
})
export class ResultsListComponent {
  private readonly router = inject(Router);
  protected readonly searchState = inject(SearchStateService);
  private readonly bookingState = inject(BookingStateService);

  protected readonly sortOption = signal<SortOption>(DEFAULT_SORT_OPTION);

  protected readonly sortedResults = computed(() => sortFlights(this.searchState.results(), this.sortOption()));

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

  formatFlightTime(isoDateTime: string): string {
    return formatTime(isoDateTime);
  }

  formatFlightDuration(durationMinutes: number): string {
    return formatDuration(durationMinutes);
  }

  /** FR-018/US-002 AC4: total is the primary, visually dominant figure — rendered separately
   * (larger/bolder) from the secondary per-person figure via two distinct template elements,
   * both sourced from the single shared pricing calculation (DP-011). */
  totalPriceText(result: FlightResult): string {
    const passengerCount = this.searchState.lastCriteria()?.passengerCount ?? 1;
    return `${formatUsd(calculateTotalPrice(result.pricePerPassenger, passengerCount))} total`;
  }

  perPersonPriceText(result: FlightResult): string {
    return `/ ${formatUsd(result.pricePerPassenger)} per person`;
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
      `departing ${this.formatFlightTime(result.departureDateTime)}, ${this.totalPriceText(result)}`
    );
  }
}
