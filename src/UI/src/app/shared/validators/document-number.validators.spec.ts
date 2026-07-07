import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import {
  AGE_MAX,
  AGE_MIN,
  EMAIL_PATTERN,
  FULL_NAME_PATTERN,
  NATIONAL_ID_PATTERN,
  PASSPORT_PATTERN,
  ageValidator,
  documentLabelForRouteType,
  documentNumberValidator,
  documentPatternForRouteType,
  documentTypeForRouteType,
  emailFormatValidator,
  fullNameValidator,
  resolveRouteType,
} from './document-number.validators';

describe('resolveRouteType', () => {
  it('resolves to Domestic when origin and destination countries match', () => {
    expect(resolveRouteType('United Kingdom', 'United Kingdom')).toBe('Domestic');
  });

  it('resolves to International when origin and destination countries differ', () => {
    expect(resolveRouteType('United Kingdom', 'United States')).toBe('International');
  });

  it('resolves to International when either country is undefined', () => {
    expect(resolveRouteType(undefined, 'United States')).toBe('International');
    expect(resolveRouteType('United Kingdom', undefined)).toBe('International');
    expect(resolveRouteType(undefined, undefined)).toBe('Domestic');
  });
});

describe('documentLabelForRouteType', () => {
  it('returns "Passport Number" for International', () => {
    expect(documentLabelForRouteType('International')).toBe('Passport Number');
  });

  it('returns "National ID" for Domestic', () => {
    expect(documentLabelForRouteType('Domestic')).toBe('National ID');
  });
});

describe('documentTypeForRouteType', () => {
  it('returns "Passport" for International', () => {
    expect(documentTypeForRouteType('International')).toBe('Passport');
  });

  it('returns "National ID" for Domestic', () => {
    expect(documentTypeForRouteType('Domestic')).toBe('National ID');
  });
});

describe('documentPatternForRouteType', () => {
  it('returns the PASSPORT_PATTERN for International', () => {
    expect(documentPatternForRouteType('International')).toBe(PASSPORT_PATTERN);
  });

  it('returns the NATIONAL_ID_PATTERN for Domestic', () => {
    expect(documentPatternForRouteType('Domestic')).toBe(NATIONAL_ID_PATTERN);
  });
});

describe('PASSPORT_PATTERN (mirrors backend DocumentPatterns.cs ^[A-Z0-9]{6,9}$)', () => {
  it('accepts a 6-character uppercase-alphanumeric value', () => {
    expect(PASSPORT_PATTERN.test('AB1234')).toBe(true);
  });

  it('accepts a 9-character uppercase-alphanumeric value', () => {
    expect(PASSPORT_PATTERN.test('AB1234567')).toBe(true);
  });

  it('rejects a 5-character value (too short)', () => {
    expect(PASSPORT_PATTERN.test('AB123')).toBe(false);
  });

  it('rejects a 10-character value (too long)', () => {
    expect(PASSPORT_PATTERN.test('AB12345678')).toBe(false);
  });

  it('rejects lowercase characters', () => {
    expect(PASSPORT_PATTERN.test('ab1234')).toBe(false);
  });

  it('rejects a value with an embedded space', () => {
    expect(PASSPORT_PATTERN.test('AB 234')).toBe(false);
  });
});

describe('NATIONAL_ID_PATTERN (mirrors backend DocumentPatterns.cs ^[A-Za-z0-9-]{5,20}$)', () => {
  it('accepts a 5-character value', () => {
    expect(NATIONAL_ID_PATTERN.test('AB123')).toBe(true);
  });

  it('accepts a 20-character value', () => {
    expect(NATIONAL_ID_PATTERN.test('A'.repeat(20))).toBe(true);
  });

  it('accepts a value containing a hyphen', () => {
    expect(NATIONAL_ID_PATTERN.test('AB-123')).toBe(true);
  });

  it('accepts mixed-case values', () => {
    expect(NATIONAL_ID_PATTERN.test('AbC12')).toBe(true);
  });

  it('rejects a 4-character value (too short)', () => {
    expect(NATIONAL_ID_PATTERN.test('AB12')).toBe(false);
  });

  it('rejects a 21-character value (too long)', () => {
    expect(NATIONAL_ID_PATTERN.test('A'.repeat(21))).toBe(false);
  });

  it('rejects a value with an embedded space', () => {
    expect(NATIONAL_ID_PATTERN.test('AB 12')).toBe(false);
  });
});

describe('EMAIL_PATTERN', () => {
  it('accepts a well-formed email address', () => {
    expect(EMAIL_PATTERN.test('jane@example.com')).toBe(true);
  });

  it('rejects an address missing the @ symbol', () => {
    expect(EMAIL_PATTERN.test('janeexample.com')).toBe(false);
  });

  it('rejects an address missing a domain', () => {
    expect(EMAIL_PATTERN.test('jane@')).toBe(false);
  });

  it('rejects an address with no top-level domain', () => {
    expect(EMAIL_PATTERN.test('jane@example')).toBe(false);
  });
});

describe('FULL_NAME_PATTERN', () => {
  it('accepts a 2-character value containing a letter', () => {
    expect(FULL_NAME_PATTERN.test('Jo')).toBe(true);
  });

  it('rejects a numeric-only value', () => {
    expect(FULL_NAME_PATTERN.test('12345')).toBe(false);
  });

  it('rejects an empty value', () => {
    expect(FULL_NAME_PATTERN.test('')).toBe(false);
  });

  it('accepts a typical full name', () => {
    expect(FULL_NAME_PATTERN.test('Jane Doe')).toBe(true);
  });
});

describe('documentNumberValidator', () => {
  it('returns {required: true} for an empty control value', () => {
    const control = new FormControl('');
    expect(documentNumberValidator('International')(control)).toEqual({ required: true });
  });

  it('returns null for a valid passport number under International', () => {
    const control = new FormControl('AB123456');
    expect(documentNumberValidator('International')(control)).toBeNull();
  });

  it('returns {documentFormat: true} for an invalid passport number under International', () => {
    const control = new FormControl('short');
    expect(documentNumberValidator('International')(control)).toEqual({ documentFormat: true });
  });

  it('returns null for a valid national ID under Domestic', () => {
    const control = new FormControl('AB-12345');
    expect(documentNumberValidator('Domestic')(control)).toBeNull();
  });

  it('returns {documentFormat: true} for an invalid national ID under Domestic', () => {
    const control = new FormControl('a');
    expect(documentNumberValidator('Domestic')(control)).toEqual({ documentFormat: true });
  });
});

describe('fullNameValidator', () => {
  it('returns {required: true} for an empty control value', () => {
    const control = new FormControl('');
    expect(fullNameValidator()(control)).toEqual({ required: true });
  });

  it('returns null for a valid full name', () => {
    const control = new FormControl('Jane Doe');
    expect(fullNameValidator()(control)).toBeNull();
  });

  it('returns {fullNameFormat: true} for a numeric-only value', () => {
    const control = new FormControl('12345');
    expect(fullNameValidator()(control)).toEqual({ fullNameFormat: true });
  });
});

describe('ageValidator (PO age feature 2026-07-08; DEC-022: sanity bounds only, no business rules bound to age)', () => {
  it('exports the backend-mirrored constants (0, 120)', () => {
    expect(AGE_MIN).toBe(0);
    expect(AGE_MAX).toBe(120);
  });

  it('returns {required: true} for an empty control value', () => {
    expect(ageValidator()(new FormControl<number | null>(null))).toEqual({ required: true });
    expect(ageValidator()(new FormControl(''))).toEqual({ required: true });
  });

  it('returns null for the 0 and 120 boundaries', () => {
    expect(ageValidator()(new FormControl(0))).toBeNull();
    expect(ageValidator()(new FormControl(120))).toBeNull();
  });

  it('returns {ageRange: true} for a negative age', () => {
    expect(ageValidator()(new FormControl(-1))).toEqual({ ageRange: true });
  });

  it('returns {ageRange: true} for an age above 120', () => {
    expect(ageValidator()(new FormControl(121))).toEqual({ ageRange: true });
  });

  it('returns {ageRange: true} for a non-whole-number age', () => {
    expect(ageValidator()(new FormControl(12.5))).toEqual({ ageRange: true });
    expect(ageValidator()(new FormControl('abc'))).toEqual({ ageRange: true });
  });

  it('returns null for a minor at any position — the former AGE-LEAD-18 lead-adult rule was removed (DEC-022, PO 2026-07-08)', () => {
    expect(ageValidator()(new FormControl(15))).toBeNull();
    expect(ageValidator()(new FormControl(5))).toBeNull();
    expect(ageValidator()(new FormControl(17))).toBeNull();
  });

  it('accepts a numeric string value (parsed, not string-compared)', () => {
    expect(ageValidator()(new FormControl('34'))).toBeNull();
    expect(ageValidator()(new FormControl('121'))).toEqual({ ageRange: true });
  });
});

describe('emailFormatValidator', () => {
  it('returns {required: true} for an empty control value', () => {
    const control = new FormControl('');
    expect(emailFormatValidator()(control)).toEqual({ required: true });
  });

  it('returns null for a valid email address', () => {
    const control = new FormControl('jane@example.com');
    expect(emailFormatValidator()(control)).toBeNull();
  });

  it('returns {emailFormat: true} for an invalid email address', () => {
    const control = new FormControl('not-an-email');
    expect(emailFormatValidator()(control)).toEqual({ emailFormat: true });
  });
});
