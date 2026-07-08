import { Component, ElementRef, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AIRPORTS } from '../../../shared/constants/airports.constants';
import { CabinClass, SearchRequest } from '../../../shared/models/search-request.model';
import { SearchStateService } from '../search-state.service';

const CABIN_CLASSES: readonly CabinClass[] = ['Economy', 'Business', 'First Class'];

type SearchFieldName = 'origin' | 'destination' | 'departureDate' | 'cabinClass';

/**
 * AUD-002: today's date derived from LOCAL calendar components, never `toISOString()` (which
 * is UTC and rolls over a day early/late for non-UTC users, wrongly blocking or allowing
 * "today"). Returns an ISO `YYYY-MM-DD` string that sorts lexicographically, so plain string
 * comparison against another `YYYY-MM-DD` value is a correct date comparison.
 */
function localTodayIso(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * AUD-010: reactive past-date validator. The native `min` attribute only marks the input
 * out-of-range visually — the Angular control stays valid, so a typed past date passed
 * client validation and only failed on a server round-trip. This makes the inline
 * "cannot be in the past" message fire client-side. Empty values are left to `required`.
 */
const notInPastValidator: ValidatorFn = (control): ValidationErrors | null => {
  const value = control.value as string | null;
  if (!value) {
    return null;
  }
  return value < localTodayIso() ? { pastDate: true } : null;
};

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
  /** AUD-002: local-date floor for the native picker (see localTodayIso). */
  protected readonly today = localTodayIso();

  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group(
    {
      origin: this.fb.nonNullable.control('', { validators: [Validators.required] }),
      destination: this.fb.nonNullable.control('', { validators: [Validators.required] }),
      // AUD-010: reactive past-date validator in addition to the required rule.
      departureDate: this.fb.nonNullable.control('', { validators: [Validators.required, notInPastValidator] }),
      cabinClass: this.fb.nonNullable.control<CabinClass>('Economy'),
    },
    { validators: [differentAirportsValidator] },
  );

  protected readonly loading = this.searchState.loading;
  protected readonly fieldErrors = this.searchState.fieldErrors;
  protected readonly errorMessage = this.searchState.errorMessage;

  /** QA fix (PO-reported 2026-07-07): raw FormControl values are NOT signals, so a
   * computed() reading them directly has zero signal dependencies — it evaluates once and
   * caches forever. After a same-airport submit, the cached `true` never invalidated, so
   * the error (and the onSubmit guard) stuck even after the user corrected the destination.
   * Bridge valueChanges into the signal graph so the computed re-evaluates on every edit. */
  private readonly formValues = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly sameAirportSelected = computed(() => {
    const { origin, destination } = this.formValues();
    return !!origin && !!destination && origin === destination;
  });

  constructor() {
    // AUD-008: "Modify search" (and the header brand link) re-enter /search — pre-fill the
    // form from the last successful search criteria so the user edits their existing query
    // instead of re-entering all four fields. lastCriteria is only ever set on a successful
    // search (AUD-003), so this reflects the query that produced the results being modified.
    const criteria = this.searchState.lastCriteria();
    if (criteria) {
      this.form.patchValue({
        origin: criteria.origin,
        destination: criteria.destination,
        departureDate: criteria.departureDate,
        cabinClass: criteria.cabinClass,
      });
    }
  }

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

  /** AUD-018 (WCAG 4.1.2): expose the invalid state so a control re-announces as invalid, not
   * just via its red border. Mirrors the exact conditions of the visible inline messages. */
  ariaInvalid(field: SearchFieldName): 'true' | null {
    const clientError = this.submitted() && this.form.controls[field].invalid;
    const sameAirport = field === 'destination' && this.submitted() && this.sameAirportSelected();
    return clientError || sameAirport || !!this.fieldError(field) ? 'true' : null;
  }

  /** AUD-018 (WCAG 3.3.1): associate each visible error message with its control via id, so a
   * screen reader reads the error text whenever the field regains focus. */
  describedBy(field: SearchFieldName): string | null {
    const ids: string[] = [];
    if (this.submitted() && this.form.controls[field].invalid) {
      ids.push(`${field}-error`);
    }
    if (field === 'destination' && this.submitted() && this.sameAirportSelected()) {
      ids.push('destination-sameairport-error');
    }
    if (this.fieldError(field)) {
      ids.push(`${field}-server-error`);
    }
    return ids.length > 0 ? ids.join(' ') : null;
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
