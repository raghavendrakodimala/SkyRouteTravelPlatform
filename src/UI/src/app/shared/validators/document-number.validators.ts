import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Named validation pattern constants (DP-015), mirroring the backend's DocumentPatterns
 * (SkyRoute.Application/Validation/DocumentPatterns.cs) exactly. Backend validation remains
 * authoritative (DP-014) — these are a convenience layer for immediate client-side feedback
 * only. Never duplicated inline in a component.
 */
export const PASSPORT_PATTERN = /^[A-Z0-9]{6,9}$/;
export const NATIONAL_ID_PATTERN = /^[A-Za-z0-9-]{5,20}$/;
export const EMAIL_PATTERN = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;
export const FULL_NAME_PATTERN = /^(?=.*[A-Za-z]).{2,100}$/;

/** Literal documentType enum values (FR-040) — note the space in "National ID". */
export const PASSPORT_DOCUMENT_TYPE = 'Passport' as const;
export const NATIONAL_ID_DOCUMENT_TYPE = 'National ID' as const;

/** Passenger age sanity bounds (PO age feature 2026-07-08; DEC-022: age is pure data
 * capture — no business rule is bound to it), mirroring the backend's
 * BookingRequestValidator MinAge/MaxAge constants exactly (DP-015 spirit). */
export const AGE_MIN = 0;
export const AGE_MAX = 120;

/**
 * Frontend-only equivalent of the backend's authoritative RouteTypeResolver (DP-016) — used
 * solely for immediate label/validation feedback on the booking screen. The backend
 * re-resolves and authoritatively enforces route type independently at booking time
 * (BR-003, NFR-DATA-004); this frontend copy is never trusted as the final answer.
 */
export type RouteType = 'International' | 'Domestic';

export function resolveRouteType(originCountry: string | undefined, destinationCountry: string | undefined): RouteType {
  return originCountry === destinationCountry ? 'Domestic' : 'International';
}

export function documentLabelForRouteType(routeType: RouteType): string {
  return routeType === 'International' ? 'Passport Number' : 'National ID';
}

export function documentTypeForRouteType(routeType: RouteType): 'Passport' | 'National ID' {
  return routeType === 'International' ? PASSPORT_DOCUMENT_TYPE : NATIONAL_ID_DOCUMENT_TYPE;
}

export function documentPatternForRouteType(routeType: RouteType): RegExp {
  return routeType === 'International' ? PASSPORT_PATTERN : NATIONAL_ID_PATTERN;
}

/** Human-readable format hint shown under the document field (WCAG 3.3.2, PO 2026-07-08) —
 * must stay in sync with the pattern constants above. */
export function documentHintForRouteType(routeType: RouteType): string {
  return routeType === 'International'
    ? '6–9 characters, uppercase letters (A–Z) and digits only — e.g. A1234567'
    : '5–20 characters, letters, digits and hyphens — e.g. ID-102-3345';
}

export function documentNumberValidator(routeType: RouteType): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string | null;
    if (!value) {
      return { required: true };
    }
    return documentPatternForRouteType(routeType).test(value) ? null : { documentFormat: true };
  };
}

export function fullNameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string | null;
    if (!value) {
      return { required: true };
    }
    return FULL_NAME_PATTERN.test(value) ? null : { fullNameFormat: true };
  };
}

export function emailFormatValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string | null;
    if (!value) {
      return { required: true };
    }
    return EMAIL_PATTERN.test(value) ? null : { emailFormat: true };
  };
}

/**
 * Age validation (PO age feature 2026-07-08): required whole number 0–120 for every
 * passenger — sanity bounds only. DEC-022 (PO 2026-07-08): age is pure data capture; no
 * business rule (pricing, eligibility, lead-adult) is bound to it. Backend validation
 * remains authoritative (DP-014) — the backend's BookingRequestValidator enforces the same
 * required/range rules on `passengers[i].age`.
 */
export function ageValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as number | string | null;
    if (value === null || value === undefined || value === '') {
      return { required: true };
    }
    const age = typeof value === 'number' ? value : Number(value);
    if (!Number.isInteger(age) || age < AGE_MIN || age > AGE_MAX) {
      return { ageRange: true };
    }
    return null;
  };
}
