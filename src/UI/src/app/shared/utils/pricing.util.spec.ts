import { describe, expect, it } from 'vitest';
import { calculateTotalPrice, formatTotalAndPerPersonLabel, formatUsd } from './pricing.util';

describe('calculateTotalPrice', () => {
  it('multiplies price per passenger by passenger count', () => {
    expect(calculateTotalPrice(115.0, 2)).toBe(230.0);
  });

  it('handles a larger passenger count', () => {
    expect(calculateTotalPrice(287.5, 3)).toBe(862.5);
  });

  it('handles a single passenger (identity)', () => {
    expect(calculateTotalPrice(199.99, 1)).toBe(199.99);
  });

  it('defensively rounds to 2 decimal places for floating point artefacts', () => {
    // 0.1 + 0.2 style floating point drift check via a value known to produce drift.
    expect(calculateTotalPrice(100.1, 3)).toBe(300.3);
  });

  it('rounds a value that would otherwise have more than 2 decimal places', () => {
    expect(calculateTotalPrice(33.333, 3)).toBe(100.0);
  });
});

describe('formatUsd', () => {
  it('formats a whole number to 2 decimal places with a USD prefix', () => {
    expect(formatUsd(320)).toBe('USD 320.00');
  });

  it('formats a value already at 1 decimal place to 2 decimal places', () => {
    expect(formatUsd(160.5)).toBe('USD 160.50');
  });

  it('formats a value already at 2 decimal places unchanged', () => {
    expect(formatUsd(99.99)).toBe('USD 99.99');
  });

  it('formats zero correctly', () => {
    expect(formatUsd(0)).toBe('USD 0.00');
  });
});

describe('formatTotalAndPerPersonLabel', () => {
  it('produces the exact verbatim format from FR-018 for (160, 2)', () => {
    expect(formatTotalAndPerPersonLabel(160, 2)).toBe('USD 320.00 total / USD 160.00 per person');
  });

  it('produces the correct label for a single passenger', () => {
    expect(formatTotalAndPerPersonLabel(287.5, 1)).toBe('USD 287.50 total / USD 287.50 per person');
  });

  it('produces the correct label for three passengers', () => {
    expect(formatTotalAndPerPersonLabel(287.5, 3)).toBe('USD 862.50 total / USD 287.50 per person');
  });
});
