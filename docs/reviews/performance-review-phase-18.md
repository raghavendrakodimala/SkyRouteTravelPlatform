# Performance Review — Phase 18 — SkyRoute MVP

| Field | Value |
|---|---|
| Review ID | PERF-REVIEW-P18 |
| Date | 2026-07-07 |
| Branch | `sdlc/18-performance-review-skyroute-mvp` |
| Commit basis | `3d44d99` (merge: autopilot efficiency review) |
| Reviewer | performance-tester |
| NFR baseline | `docs/specs/non-functional-requirements.md` v1.0 (Approved), Section 3 (Performance) |
| Raw data | `docs/testing/performance/phase-18-measurements.md` |
| Open findings | **0** |

---

## 1. Environment and Methodology

- **Machine:** Windows 11 Enterprise 10.0.26200, single developer machine, localhost, no artificial network latency — exactly the load condition NFR-PERF-001 specifies.
- **Backend:** `dotnet run --project src/SkyRoute.Api --urls http://localhost:5094` (.NET SDK 10.0.301, Development environment). Both mock providers registered; neither contains any artificial delay (grep for `Task.Delay|Thread.Sleep` in `src/` returned no matches).
- **Latency measurement:** `curl -w "%{time_total}"`, 20 sequential warm iterations per endpoint (NFR-PERF-001 mandates ≥20 sequential requests, single-user). Cold latency measured as the first HTTP request after a fresh process start, with readiness detected from the "Now listening" log line so the timed request was genuinely the first.
- **Payloads:** representative valid requests — search LHR→JFK / Economy / 1 pax / OneWay (a 2-result route: GA101 + BW210); bookings with 1 and 9 passengers (BR-005 boundary values) using the GlobalAir GA101 flight snapshot (BaseFare 250.00 → PricePerPassenger 287.50).
- **Frontend:** `npm run build` (production configuration) in `frontend/`; bundle sizes read from the Angular CLI build report and compared to `angular.json` budgets.
- **No load testing performed** — explicitly out of scope per NFR-SCALE-001 and tool-safety rules.
- All server processes started for this review were stopped after evidence capture; port 5094 confirmed free.

---

## 2. Measured Results vs NFR Targets

| NFR | Target | Measured | Verdict |
|---|---|---|---|
| NFR-PERF-001 — search API latency | p50 < 800 ms; p95 < 2000 ms (≥20 sequential local requests) | p50 = **3.2 ms**, p95 = **8.8 ms**, max = 9.9 ms (n=20 warm); cold first request = **253.3 ms** | **Pass** — warm p95 is ~0.4% of budget; even the cold first request is well under the p50 target |
| NFR-PERF-002 — booking API latency | p50 < 400 ms; p95 < 1000 ms | 1 pax: p50 = **3.0 ms**, p95 = 18.2 ms, max = 80.1 ms; 9 pax: p50 = **2.5 ms**, p95 = 3.6 ms, max = 11.0 ms; cold first request = **83.0 ms** | **Pass** — both boundary passenger counts (1 and 9, BR-005) far inside targets; 9-pax not measurably slower than 1-pax |
| NFR-PERF-003 — sort re-render < 100 ms (≤50 flights) | < 100 ms | Not numerically re-measured this phase (browser DevTools Performance panel); see PERF-001 below. Indirect evidence: sort is a pure client-side array re-order (`sort-flights.util`), result sets are ≤2 flights per route with mock data, and Playwright e2e sort-flip scenarios complete inside 2.0 s total including page load | **Pass (indirect)** — no evidence of breach; numeric DevTools trace never recorded, noted as PERF-001 (Low, Accepted Risk) |
| NFR-PERF-004 — airport dropdown population < 50 ms | < 50 ms | Not numerically re-measured; dropdown is populated from a static in-bundle constant (DP-012) with no HTTP call — structurally incapable of a data-fetch delay. E2e dropdown scenarios pass in 1.2 s including page load | **Pass (indirect)** — covered by PERF-001 note |
| NFR-PERF-005 — aggregation overhead < 100 ms above slowest provider; concurrent invocation | `Task.WhenAll` per FR-049 | **Concurrent invocation confirmed in code:** `src/SkyRoute.Application/Services/FlightAggregatorService.cs:27-28` — `_providers.Select(provider => SafeInvokeAsync(...))` then `await Task.WhenAll(tasks)`; per-provider try/catch (`SafeInvokeAsync`) preserves fault isolation without serializing calls. Runtime corroboration: total warm search p50 of 3.2 ms with 2 providers bounds total aggregation overhead at < 100 ms trivially | **Pass** |
| NFR-SCALE-001 — no concurrency/load testing required | N/A boundary statement | No load testing performed | **Confirmed: Not Applicable — local single-user MVP**, recorded here as the NFR itself requires |
| Frontend initial bundle vs `angular.json` budgets | warning 500 kB / error 1 MB (raw initial) | Initial total **368.15 kB raw / 92.00 kB estimated transfer** (main 365.31 kB + styles 2.84 kB); build completed with no budget warning | **Pass** — 26% headroom below the warning threshold |
| Search response payload (2-result route) | No explicit NFR budget | **517 bytes** (`application/json`) for LHR→JFK 2-result response | **Informational** — negligible |

---

## 3. Findings

### PERF-001 — NFR-PERF-003/004 have no recorded numeric browser measurement

| Field | Value |
|---|---|
| ID | PERF-001 |
| Severity | Low (informational) |
| File or area | Frontend — results sort re-render (US-003) and airport dropdown (US-008); `docs/testing/execution/` record gap |
| Evidence | NFR-PERF-003/004 specify validation via "Manual check using browser DevTools Performance panel during functional/UI testing (Phase 05/14)". Grep of `docs/testing/execution/` shows sort and dropdown behaviour verified functionally (unit + e2e green, e.g. `full-journey-domestic.spec.ts` sort flip passes in 2.0 s total, `search-form.spec.ts` dropdown in 1.2 s total) but no DevTools frame-time number was ever recorded against the 100 ms / 50 ms thresholds. Structural evidence makes a breach implausible: sort is a pure in-memory re-order of ≤2 mock results (target allows up to 50), and the dropdown is a static in-bundle constant (DP-012) with zero I/O. |
| Impact | None observable at MVP data volumes. This is a test-evidence completeness note, not a performance defect — every measured runtime number in this review is 1–2 orders of magnitude inside its target, and the initial bundle (368 kB raw / 92 kB transfer) leaves rendering-path headroom. |
| Recommendation | If a future sprint grows result sets toward the 50-flight assumption, record one DevTools Performance trace of a sort re-order at that volume. |
| Required fix | **None required.** The functional and structural evidence is sufficient for MVP sign-off at current data volumes. |
| Status | **Accepted Risk** — Low/informational finding whose review text states no fix is required; marked directly by the reviewer per the Iterative Review-Fix Loop carve-out (`.claude/rules/phased-execution.md`). |

No other findings. No Critical, High, or Medium findings were identified — all measured numbers are far inside their approved NFR targets.

---

## 4. Verdict

**All performance NFRs pass.** Search and booking API latencies are two or more orders of magnitude inside their p50/p95 targets (warm and cold); provider aggregation is concurrent (`Task.WhenAll` with per-provider fault isolation, FR-049/BR-007); the production bundle is within its `angular.json` budgets with 26% headroom; payload sizes are negligible. Zero `Open` findings — Phase 18 meets the review-phase merge criterion.

---

*End of Performance Review — Phase 18.*
