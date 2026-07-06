/**
 * Structured error shape produced by the single HTTP-error-mapping point in each Angular
 * HTTP-calling service (DP-010, docs/features/feature-error-handling-and-validation.md
 * Section 4). Components never parse HttpErrorResponse directly (DP-009) — they only ever
 * see this already-mapped shape.
 */
export interface FieldErrors {
  [field: string]: string[];
}

export interface ValidationApiError {
  kind: 'validation';
  errors: FieldErrors;
}

export interface MessageApiError {
  kind: 'message';
  message: string;
}

export type ApiError = ValidationApiError | MessageApiError;
