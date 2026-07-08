import { Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

export interface PassengerServerErrors {
  fullName?: string;
  age?: string;
  email?: string;
  documentNumber?: string;
  documentType?: string;
}

/**
 * One instance per passenger (BL-034, FR-028). Purely presentational — collects full name,
 * age (PO age feature 2026-07-08), email, and document number for a single passenger's
 * FormGroup, supplied by the parent BookingFormComponent (BL-037). No HTTP, no business
 * logic (DP-009).
 */
@Component({
  selector: 'app-passenger-form-section',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './passenger-form-section.component.html',
  styleUrl: './passenger-form-section.component.css',
})
export class PassengerFormSectionComponent {
  readonly group = input.required<FormGroup>();
  /** Zero-based passenger index — rendered as "Passenger {index + 1}". */
  readonly index = input.required<number>();
  /** Passenger 1 is the lead passenger; their email is also the primary contact (BR-005). */
  readonly isLead = input<boolean>(false);
  readonly documentLabel = input.required<string>();
  /** Route-type-specific format hint (WCAG 3.3.2, PO 2026-07-08) — rendered persistently
   * under the document field and referenced by the input's aria-describedby. */
  readonly documentHint = input.required<string>();
  readonly serverErrors = input<PassengerServerErrors | null>(null);

  /** AUD-018 (WCAG 4.1.2): a control's client-side error is shown once it is touched + invalid;
   * this drives both the inline message and the input's aria-invalid state. */
  protected isInvalid(field: string): boolean {
    const control = this.group().get(field);
    return !!control && control.touched && control.invalid;
  }

  protected hasServerError(field: keyof PassengerServerErrors): boolean {
    return !!this.serverErrors()?.[field];
  }

  protected ariaInvalid(field: keyof PassengerServerErrors): 'true' | null {
    return this.isInvalid(field) || this.hasServerError(field) ? 'true' : null;
  }

  /** AUD-018 (WCAG 3.3.1): associate the control with its persistent hint (document field only)
   * and any currently-shown error message(s) via id, so a screen reader announces the error
   * text whenever the field regains focus — not just its label. */
  protected describedBy(field: keyof PassengerServerErrors): string | null {
    const ids: string[] = [];
    if (field === 'documentNumber') {
      ids.push(`documentHint-${this.index()}`);
    }
    if (this.isInvalid(field)) {
      ids.push(`${field}-error-${this.index()}`);
    }
    if (this.hasServerError(field)) {
      ids.push(`${field}-server-error-${this.index()}`);
    }
    // The document field also renders a server documentType error under it — associate it too.
    if (field === 'documentNumber' && this.hasServerError('documentType')) {
      ids.push(`documentType-server-error-${this.index()}`);
    }
    return ids.length > 0 ? ids.join(' ') : null;
  }
}
