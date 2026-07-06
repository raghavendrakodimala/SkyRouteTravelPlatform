import { describe, expect, it } from 'vitest';
import { formatDuration, formatTime } from './datetime-format.util';

describe('formatTime', () => {
  it('extracts the literal HH:mm substring from a Z-suffixed ISO datetime', () => {
    expect(formatTime('2026-08-01T09:00:00Z')).toBe('09:00');
  });

  it('extracts HH:mm without performing any timezone conversion', () => {
    expect(formatTime('2026-08-01T23:45:00Z')).toBe('23:45');
  });

  it('returns an empty string when no time component is present', () => {
    expect(formatTime('not-a-date')).toBe('');
  });

  it('returns an empty string for an empty input', () => {
    expect(formatTime('')).toBe('');
  });
});

describe('formatDuration', () => {
  it('formats 510 minutes as "8h 30m"', () => {
    expect(formatDuration(510)).toBe('8h 30m');
  });

  it('formats an exact-hour duration with a zero-minute component shown ("3h 0m")', () => {
    expect(formatDuration(180)).toBe('3h 0m');
  });

  it('formats 65 minutes as "1h 5m"', () => {
    expect(formatDuration(65)).toBe('1h 5m');
  });

  it('formats a sub-hour duration with a zero-hour component shown ("0h 45m")', () => {
    expect(formatDuration(45)).toBe('0h 45m');
  });

  it('formats zero minutes as "0h 0m"', () => {
    expect(formatDuration(0)).toBe('0h 0m');
  });
});
