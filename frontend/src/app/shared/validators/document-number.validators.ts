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
