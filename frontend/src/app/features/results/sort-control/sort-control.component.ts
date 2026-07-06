import { Component, input, output } from '@angular/core';
import { SORT_OPTION_LABELS, SortOption } from '../../../shared/utils/sort-flights.util';

const OPTIONS: readonly SortOption[] = ['priceAsc', 'priceDesc', 'durationAsc', 'departureAsc'];

/**
 * Sort control offering the 4 options (US-003, FR-020). Purely presentational — re-ordering
 * itself is a pure computed() recombination performed by the parent ResultsListComponent
 * (FR-021); this component only emits the selected option and highlights the active one
 * (FR-023).
 */
@Component({
  selector: 'app-sort-control',
  standalone: true,
  templateUrl: './sort-control.component.html',
  styleUrl: './sort-control.component.css',
})
export class SortControlComponent {
  readonly active = input.required<SortOption>();
  readonly optionSelected = output<SortOption>();

  protected readonly options = OPTIONS;
  protected readonly labels = SORT_OPTION_LABELS;

  select(option: SortOption): void {
    this.optionSelected.emit(option);
  }
}
