/**
 * Shared time/duration formatting rules (FR-016, docs/features/feature-search-results-and-
 * sorting.md Section 2.2), reused by the results, booking, and confirmation screens so the
 * formatting logic exists in exactly one place (DRY — not individually mandated by a DP-* ID
 * the way pricing/airport data are, but following the same single-source principle).
 */

/** Literal HH:mm substring of an ISO datetime string — no timezone conversion is performed
 * (Gap-fill SR-02). */
export function formatTime(isoDateTime: string): string {
  const match = /T(\d{2}):(\d{2})/.exec(isoDateTime);
  return match ? `${match[1]}:${match[2]}` : '';
}

/** "Xh Ym" — both components always shown, even when zero (Gap-fill SR-01). */
export function formatDuration(durationMinutes: number): string {
  const hours = Math.floor(durationMinutes / 60);
  const minutes = durationMinutes % 60;
  return `${hours}h ${minutes}m`;
}
