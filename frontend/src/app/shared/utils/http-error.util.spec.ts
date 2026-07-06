import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { mapHttpError } from './http-error.util';

const GENERIC_MESSAGE = "We couldn't complete your request. Please try again.";
const CUSTOM_NETWORK_MESSAGE = 'Custom network problem message.';

describe('mapHttpError', () => {
  it('maps a 400 response with a structured errors body to a validation ApiError', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { origin: ['Origin airport is required.'] } },
    });

    const result = mapHttpError(error, GENERIC_MESSAGE);

    expect(result).toEqual({
      kind: 'validation',
      errors: { origin: ['Origin airport is required.'] },
    });
  });

  it('maps a status-0 (network) error to a message ApiError using the provided networkMessage', () => {
    const error = new HttpErrorResponse({ status: 0 });

    const result = mapHttpError(error, GENERIC_MESSAGE, CUSTOM_NETWORK_MESSAGE);

    expect(result).toEqual({ kind: 'message', message: CUSTOM_NETWORK_MESSAGE });
  });

  it('maps a status-0 (network) error to the default networkMessage when none is supplied', () => {
    const error = new HttpErrorResponse({ status: 0 });

    const result = mapHttpError(error, GENERIC_MESSAGE);

    expect(result).toEqual({
      kind: 'message',
      message: 'Network error. Please check your connection and try again.',
    });
  });

  it('maps a 500 response to a message ApiError using the genericServerMessage, never backend internals', () => {
    const error = new HttpErrorResponse({
      status: 500,
      error: { message: 'NullReferenceException at line 42' },
    });

    const result = mapHttpError(error, GENERIC_MESSAGE);

    expect(result).toEqual({ kind: 'message', message: GENERIC_MESSAGE });
  });

  it('maps a 400 response WITHOUT a structured errors body to a generic message ApiError', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: 'Bad Request',
    });

    const result = mapHttpError(error, GENERIC_MESSAGE);

    expect(result).toEqual({ kind: 'message', message: GENERIC_MESSAGE });
  });

  it('maps an unexpected status (e.g. 404) to a generic message ApiError', () => {
    const error = new HttpErrorResponse({ status: 404 });

    const result = mapHttpError(error, GENERIC_MESSAGE);

    expect(result).toEqual({ kind: 'message', message: GENERIC_MESSAGE });
  });
});
