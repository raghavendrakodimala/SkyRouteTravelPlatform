import { Component, ElementRef, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AIRPORTS } from '../../../shared/constants/airports.constants';
import { CabinClass, SearchRequest } from '../../../shared/models/search-request.model';
import { SearchStateService } from '../search-state.service';

const CABIN_CLASSES: readonly CabinClass[] = ['Economy', 'Business', 'First Class'];

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
  private readonly host = inject(ElementRef);
  protected readonly searchState = inject(SearchStateService);

  protected readonly airports = AIRPORTS;
  protected readonly cabinClasses = CABIN_CLASSES;
  protected readonly today = new Date().toISOString().slice(0, 10);

  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group(
    {
      origin: this.fb.nonNullable.control('', { validators: [Validators.required] }),
      destination: this.fb.nonNullable.control('', { validators: [Validators.required] }),
      departureDate: this.fb.nonNullable.control('', { validators: [Validators.required] }),
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
    if (this.searchState.loading()) {
      return; // A11Y-007: re-entrant submit while in flight — the aria-disabled button stays focusable
    }
    this.submitted.set(true);
    if (this.form.invalid || this.sameAirportSelected()) {
      // A11Y-008: the button is never natively disabled, so an invalid submit lands here —
      // the submitted() flag surfaces the inline role="alert" messages; markAllAsTouched
      // triggers the .ng-invalid.ng-touched border cue; focus lands on the first problem.
      this.form.markAllAsTouched();
      this.focusFirstInvalidControl();
      return;
    }

    const value = this.form.getRawValue();
    const request: SearchRequest = {
      origin: value.origin,
      destination: value.destination,
      departureDate: value.departureDate,
      passengerCount: 1, // Passenger count is determined during booking (PO decision 2026-07-07); the API contract still requires 1–9.
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

  /** A11Y-008: on a rejected submit, move focus to the first offending control (all controls
   * are statically rendered, so a direct synchronous focus is safe — mirrors the booking
   * wizard's focusFirstInvalidControl). The same-airport rule lives on the form group, so
   * neither select is individually invalid — focus the destination select for that case. */
  private focusFirstInvalidControl(): void {
    const order = ['origin', 'destination', 'departureDate'] as const;
    const root = this.host.nativeElement as HTMLElement;
    for (const name of order) {
      if (this.form.controls[name].invalid) {
        root.querySelector<HTMLElement>(`#${name}`)?.focus();
        return;
      }
    }
    if (this.sameAirportSelected()) {
      root.querySelector<HTMLElement>('#destination')?.focus();
    }
  }
}
