import { Component, ElementRef, inject, input, output } from '@angular/core';
import { SORT_OPTION_LABELS, SortOption } from '../../../shared/utils/sort-flights.util';

const OPTIONS: readonly SortOption[] = ['priceAsc', 'priceDesc', 'durationAsc', 'departureAsc'];

/**
 * Sort control offering the 4 options (US-003, FR-020). Purely presentational — re-ordering
 * itself is a pure computed() recombination performed by the parent ResultsListComponent
 * (FR-021); this component only emits the selected option and highlights the active one
 * (FR-023).
 *
 * AUD-022 (WCAG 4.1.2): these four mutually-exclusive options are a single-select, modelled as
 * an ARIA radiogroup (role="radiogroup" + role="radio"/aria-checked) rather than four
 * independent aria-pressed toggles. It uses the standard radio-group keyboard pattern — roving
 * tabindex (only the checked option is tabbable) with Arrow/Home/End moving and selecting.
 * The re-order itself is announced by the parent's polite live region (liveStatus).
 */
@Component({
  selector: 'app-sort-control',
  standalone: true,
  templateUrl: './sort-control.component.html',
  styleUrl: './sort-control.component.css',
})
export class SortControlComponent {
  private readonly host = inject(ElementRef);

  readonly active = input.required<SortOption>();
  readonly optionSelected = output<SortOption>();

  protected readonly options = OPTIONS;
  protected readonly labels = SORT_OPTION_LABELS;

  select(option: SortOption): void {
    this.optionSelected.emit(option);
  }

  /** Radio-group keyboard navigation (AUD-022): Arrow keys / Home / End move focus AND select,
   * matching the WAI-ARIA radio pattern. */
  protected onKeydown(event: KeyboardEvent, index: number): void {
    let nextIndex: number | null = null;
    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        nextIndex = (index + 1) % this.options.length;
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        nextIndex = (index - 1 + this.options.length) % this.options.length;
        break;
      case 'Home':
        nextIndex = 0;
        break;
      case 'End':
        nextIndex = this.options.length - 1;
        break;
      default:
        return;
    }
    event.preventDefault();
    this.select(this.options[nextIndex]);
    const buttons = (this.host.nativeElement as HTMLElement).querySelectorAll<HTMLElement>('.sort-option');
    buttons[nextIndex]?.focus();
  }
}
