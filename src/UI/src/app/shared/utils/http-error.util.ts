import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, FieldErrors } from '../models/api-error.model';

/**
 * The single error-mapping point applied uniformly inside every Angular HTTP-calling
 * service (DP-010, docs/features/feature-error-handling-and-validation.md Section 4).
 * Maps a raw HttpErrorResponse to either a structured field-error object (400 validation
 * failures, rendered inline by the form) or a plain user-facing message (500 / network
 * failures, rendered as a generic banner). Never lets a raw status code or exception
 * message reach a component (FR-071).
 */
export function mapHttpError(
  error: HttpErrorResponse,
  genericServerMessage: string,
  networkMessage: string = 'Network error. Please check your connection and try again.',
): ApiError {
  if (error.status === 400 && error.error && typeof error.error === 'object' && 'errors' in error.error) {
    return { kind: 'validation', errors: error.error.errors as FieldErrors };
  }

  if (error.status === 0) {
    // No HTTP response at all — timeout, connection refused, CORS failure, etc.
    return { kind: 'message', message: networkMessage };
  }

  // 500 or any other unexpected status: generic message only, never backend internals.
  return { kind: 'message', message: genericServerMessage };
}
