import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AIRPORTS } from '../../../shared/constants/airports.constants';
import { CabinClass, SearchRequest } from '../../../shared/models/search-request.model';
import { SearchStateService } from '../search-state.service';

const CABIN_CLASSES: readonly CabinClass[] = ['Economy', 'Business', 'First Class'];
const PASSENGER_COUNTS = Array.from({ length: 9 }, (_, i) => i + 1);

/** Group-level validator for US-001 AC8 / US-008 AC4 — origin and destination must differ. */
const differentAirportsValidator: ValidatorFn = (group): ValidationErrors | null => {
  const origin = group.get('origin')?.value;
  const destination = group.get('destination')?.value;
  if (origin && destination && origin === destination) {
    return { sameAirport: true };
  }
  return null;
};

/**
 * Standalone search form (BL-028, DP-009: presentation only — no HTTP requests constructed
 * here; delegates entirely to SearchStateService/FlightSearchService). Stays on /search for
 * the full request lifecycle so 400 field errors can be rendered inline next to the
 * offending fields (feature-flight-search.md Section 5.3); navigates to /results only after
 * a successful (2xx) response, including a legitimate empty array.
 */
@Component({
  selector: 'app-search-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './search-form.component.html',
  styleUrl: './search-form.component.css',
})
export class SearchFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  protected readonly searchState = inject(SearchStateService);

  protected readonly airports = AIRPORTS;
  protected readonly cabinClasses = CABIN_CLASSES;
  protected readonly passengerCounts = PASSENGER_COUNTS;
  protected readonly today = new Date().toISOString().slice(0, 10);

  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group(
    {
      origin: this.fb.nonNullable.control('', { validators: [Validators.required] }),
      destination: this.fb.nonNullable.control('', { validators: [Validators.required] }),
      departureDate: this.fb.nonNullable.control('', { validators: [Validators.required] }),
      passengerCount: this.fb.nonNullable.control<number>(1),
      cabinClass: this.fb.nonNullable.control<CabinClass>('Economy'),
    },
    { validators: [differentAirportsValidator] },
  );

  protected readonly loading = this.searchState.loading;
  protected readonly fieldErrors = this.searchState.fieldErrors;
  protected readonly errorMessage = this.searchState.errorMessage;

  protected readonly sameAirportSelected = computed(() => {
    const origin = this.form.controls.origin.value;
    const destination = this.form.controls.destination.value;
    return !!origin && !!destination && origin === destination;
  });

  async onSubmit(): Promise<void> {
    this.submitted.set(true);
    if (this.form.invalid || this.sameAirportSelected() || this.searchState.loading()) {
      return;
    }

    const value = this.form.getRawValue();
    const request: SearchRequest = {
      origin: value.origin,
      destination: value.destination,
      departureDate: value.departureDate,
      passengerCount: value.passengerCount,
      cabinClass: value.cabinClass,
      tripType: 'OneWay',
    };

    const outcome = await this.searchState.search(request);
    if (outcome === 'success') {
      await this.router.navigate(['/results']);
    }
  }

  fieldError(field: string): string | null {
    const errors = this.fieldErrors();
    return errors?.[field]?.[0] ?? null;
  }
}
