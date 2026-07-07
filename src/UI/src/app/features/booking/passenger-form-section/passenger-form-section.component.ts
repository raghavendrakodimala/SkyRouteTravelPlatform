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
}
