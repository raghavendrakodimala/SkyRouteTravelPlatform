import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { getCountryForCode } from '../../../shared/constants/airports.constants';
import { BookingFlightSnapshot, BookingRequest } from '../../../shared/models/booking-request.model';
import { PassengerDetail } from '../../../shared/models/passenger-detail.model';
import { formatDuration, formatTime } from '../../../shared/utils/datetime-format.util';
import { calculateTotalPrice, formatTotalAndPerPersonLabel, formatUsd } from '../../../shared/utils/pricing.util';
import {
  RouteType,
  documentLabelForRouteType,
  documentNumberValidator,
  documentTypeForRouteType,
  emailFormatValidator,
  fullNameValidator,
  resolveRouteType,
} from '../../../shared/validators/document-number.validators';
import { BookingStateService } from '../booking-state.service';
import {
  PassengerFormSectionComponent,
  PassengerServerErrors,
} from '../passenger-form-section/passenger-form-section.component';

/**
 * Combines the three BL-033 split sub-tasks into the single booking-form.component.ts named
 * in architecture-plan.md Section 4.1 (task-decomposition only — no new component/file per
 * the backlog's decomposition note):
 * - BL-036: flight summary + price breakdown display (read-only, from BookingStateService).
 * - BL-037: per-passenger FormArray orchestration + aggregate validity gating.
 * - BL-038: submit/loading/error/re-submission-guard wiring.
 */
@Component({
  selector: 'app-booking-form',
  standalone: true,
  imports: [ReactiveFormsModule, PassengerFormSectionComponent],
  templateUrl: './booking-form.component.html',
  styleUrl: './booking-form.component.css',
})
export class BookingFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  protected readonly bookingState = inject(BookingStateService);

  protected readonly flight = this.bookingState.selectedFlight;
  protected readonly passengerCount = this.bookingState.passengerCount;
  protected readonly loading = this.bookingState.loading;

  /** DP-016: frontend equivalent of the backend's authoritative RouteTypeResolver, used only
   * for immediate label/validation feedback — the backend re-resolves and authoritatively
   * enforces route type independently at booking time (BR-003, NFR-DATA-004). */
  protected readonly routeType = computed<RouteType>(() => {
    const flight = this.flight();
    if (!flight) {
      return 'International';
    }
    return resolveRouteType(getCountryForCode(flight.origin), getCountryForCode(flight.destination));
  });

  protected readonly documentLabel = computed(() => documentLabelForRouteType(this.routeType()));

  /** BL-036 — price breakdown via the single shared calculation point (DP-011). */
  protected readonly priceBreakdown = computed(() => {
    const flight = this.flight();
    const count = this.passengerCount();
    if (!flight) {
      return null;
    }
    return {
      perPerson: flight.pricePerPassenger,
      count,
      total: calculateTotalPrice(flight.pricePerPassenger, count),
      label: formatTotalAndPerPersonLabel(flight.pricePerPassenger, count),
    };
  });

  /** BL-037 — one FormGroup per passenger, sized from the selected flight's passenger count.
   * Built as a field initializer (not inside an explicit constructor) so that field
   * declaration order — fb/bookingState/passengerCount/routeType first, then
   * passengersForm, then formStatus — governs initialization order deterministically. */
  protected readonly passengersForm: FormArray<FormGroup> = this.fb.array(
    Array.from({ length: Math.max(this.passengerCount(), 1) }, () => this.buildPassengerGroup(this.routeType())),
  );

  /** QA-003 fix: the native `<form>` needs a real `[formGroup]` binding so Angular's
   * FormGroupDirective intercepts `ngSubmit` (otherwise the browser performs a native form
   * submit/page reload instead of calling onSubmit()). `passengersForm` above is a FormArray
   * (required by `formGroup`'s FormGroup-typed input), so it is wrapped in this thin FormGroup
   * purely to satisfy the directive's binding — no passenger validation/structure changes. */
  protected readonly bookingForm: FormGroup = this.fb.group({ passengers: this.passengersForm });

  protected readonly submitted = signal(false);

  private readonly formStatus = toSignal(this.passengersForm.statusChanges, {
    initialValue: this.passengersForm.status,
  });

  /** Aggregate "all passengers valid" gate consumed by the submit button (US-005 AC10, FR-031). */
  protected readonly allPassengersValid = computed(() => this.formStatus() === 'VALID');

  protected readonly alreadyConfirmed = computed(() => this.bookingState.bookingResponse() !== null);

  protected readonly genericServerError = computed(() => {
    const message = this.bookingState.errorMessage();
    if (message) {
      return message;
    }
    const errors = this.bookingState.fieldErrors();
    return errors?.['flight']?.[0] ?? errors?.['passengerCount']?.[0] ?? null;
  });

  private buildPassengerGroup(routeType: RouteType): FormGroup {
    return this.fb.nonNullable.group({
      fullName: this.fb.nonNullable.control('', { validators: [fullNameValidator()] }),
      email: this.fb.nonNullable.control('', { validators: [emailFormatValidator()] }),
      documentNumber: this.fb.nonNullable.control('', { validators: [documentNumberValidator(routeType)] }),
    });
  }

  protected passengerGroups(): FormGroup[] {
    return this.passengersForm.controls;
  }

  protected passengerServerErrors(index: number): PassengerServerErrors | null {
    const errors = this.bookingState.fieldErrors();
    if (!errors) {
      return null;
    }
    const prefix = `passengers[${index}].`;
    const result: PassengerServerErrors = {
      fullName: errors[`${prefix}fullName`]?.[0],
      email: errors[`${prefix}email`]?.[0],
      documentNumber: errors[`${prefix}documentNumber`]?.[0],
      documentType: errors[`${prefix}documentType`]?.[0],
    };
    return Object.values(result).some((v) => v !== undefined) ? result : null;
  }

  protected formatFlightTime(isoDateTime: string): string {
    return formatTime(isoDateTime);
  }

  protected formatFlightDuration(durationMinutes: number): string {
    return formatDuration(durationMinutes);
  }

  protected formatPrice(amount: number): string {
    return formatUsd(amount);
  }

  async onSubmit(): Promise<void> {
    this.submitted.set(true);
    this.passengersForm.markAllAsTouched();

    const flight = this.flight();
    if (!flight || !this.allPassengersValid() || this.bookingState.loading() || this.alreadyConfirmed()) {
      return;
    }

    const routeType = this.routeType();
    const documentType = documentTypeForRouteType(routeType);

    const passengers: PassengerDetail[] = this.passengersForm.controls.map((group) => ({
      fullName: (group.value.fullName as string).trim(),
      email: (group.value.email as string).trim(),
      documentType,
      documentNumber: (group.value.documentNumber as string).trim(),
    }));

    const flightSnapshot: BookingFlightSnapshot = {
      provider: flight.provider,
      flightNumber: flight.flightNumber,
      origin: flight.origin,
      destination: flight.destination,
      departureDateTime: flight.departureDateTime,
      arrivalDateTime: flight.arrivalDateTime,
      durationMinutes: flight.durationMinutes,
      cabinClass: flight.cabinClass,
      baseFare: flight.baseFare,
      pricePerPassenger: flight.pricePerPassenger,
    };

    const request: BookingRequest = {
      flight: flightSnapshot,
      passengerCount: passengers.length,
      passengers,
    };

    const outcome = await this.bookingState.submitBooking(request);
    if (outcome === 'success') {
      await this.router.navigate(['/confirmation']);
    }
  }
}
