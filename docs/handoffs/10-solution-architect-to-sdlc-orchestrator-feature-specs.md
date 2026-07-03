# Handoff: HO-010

| Field | Value |
|---|---|
| Handoff ID | HO-010 |
| Date | 2026-07-03 |
| Branch | sdlc/10-feature-specifications-skyroute-mvp |
| Phase | Phase 10 — Feature Specifications |
| From agent | solution-architect |
| To agent | sdlc-orchestrator |
| Status | Complete |

---

## Work Completed

Produced five implementation-ready feature specification documents under `docs/features/`, forming the last concretization layer before Phase 11 (Spec Readiness Check) and Phase 12 (Implementation). Each document turns the architecture plan's component shapes and the requirements' business rules into exact request/response JSON payloads, exact validation error shapes, exact regex/pattern strings, exact UI-state rules, and exact worked examples — so Phase 12 implementers do not need to re-derive or improvise any decision already settled by `docs/requirements.md` v1.4 or `docs/architecture/architecture-plan.md` v1.0.

Read in full before drafting: `docs/requirements.md` v1.4, `docs/specs/non-functional-requirements.md` v1.0, `docs/architecture/architecture-plan.md` v1.0, `docs/testing/test-strategy.md` v1.0, `docs/delivery/project-backlog.md` v1.1, `docs/delivery/sprint-1-plan.md` v1.0, `docs/handoffs/workflow-state.md`, `docs/handoffs/current-handoff.md`.

No new scope, endpoint, or field was introduced beyond what `docs/requirements.md` and `docs/architecture/architecture-plan.md` already establish. Every document includes an explicit "Compliance Statement" confirming it does not contradict either source, and cites the exact backlog items (`BL-*`) it makes implementation-ready.

---

## Artifacts Created or Updated

- `docs/features/feature-flight-search.md` (new, v1.0) — US-001, US-008. Search form fields, exact `SearchRequest` JSON shape, exact 400 validation error messages per field, airport dropdown data shape, loading/empty/error UI-state rules.
- `docs/features/feature-search-results-and-sorting.md` (new, v1.0) — US-002, US-003. Exact `FlightResult` JSON shape, price display/formatting rules (total vs per-person), time/duration formatting, sort behavior and default sort, stable-sort tie-breaking rule.
- `docs/features/feature-provider-aggregation.md` (new, v1.0) — US-007. `IFlightProvider` contract detail, a concrete fixed mock flight dataset for GlobalAir (4 flights) and BudgetWings (4 flights) with routes/times/durations/base fares, cabin-class fare multipliers, worked BR-001/BR-002 pricing examples against that dataset, and a fully worked BR-007 fault-isolation example (provider throws → exact degraded 200 response body).
- `docs/features/feature-booking-flow.md` (new, v1.0) — US-004, US-005, US-006. Booking-screen data-carry rules, per-passenger field validation (full name, email, document number) with exact regex patterns, a worked BR-003 document-type-routing example against two concrete airport pairs, exact `BookingRequest`/`BookingResponse` JSON shapes, the exact booking-reference generation algorithm (cryptographic RNG + bounded collision-retry), exact confirmation-screen fields.
- `docs/features/feature-error-handling-and-validation.md` (new, v1.0) — cross-cutting. Canonical 400/404/500 response shapes used by every endpoint, centralized exception-middleware behavior (BR-011), and a complete enumerated list of every 400/404/500 scenario across search and booking with example bodies.
- `docs/handoffs/10-solution-architect-to-sdlc-orchestrator-feature-specs.md` (this file, new)
- `docs/handoffs/current-handoff.md` (updated to point to HO-010)
- `docs/handoffs/handoff-index.md` (HO-010 row added)
- `docs/handoffs/workflow-state.md` (Phase 10 marked Complete; Next phase set to Phase 11 — Spec Readiness Check, owner scrum-master)

No code files (`.ts`/`.cs`) were created. No command was run. No file was deleted.

---

## Decisions Made — Gap-Fill Decisions (Full List)

The following are implementation-detail decisions made within already-approved flexibility (not scope changes, per the task brief governing this phase). Each is also recorded inline, with rationale, in its source document.

| ID | Document | Decision |
|---|---|---|
| GAP-FS-01 | feature-flight-search.md | `tripType` is not a visible/editable form control; frontend always sends the fixed literal `"OneWay"`. |
| GAP-FS-02 | feature-flight-search.md | Passenger count uses a `<select>` dropdown with exactly the 9 options 1–9, not a free numeric input. |
| GAP-FS-03 | feature-flight-search.md | Exact validation error message strings for every search field-error rule (origin/destination identical, unknown code, past date, passenger count range, cabin class enum, trip type). |
| GAP-FS-04 | feature-flight-search.md | Exact server-error (`"We couldn't complete your search. Please try again."`) and network-failure (`"Network error. Please check your connection and try again."`) message strings. |
| GAP-FS-05 | feature-flight-search.md | Field-error JSON keys use camelCase matching request body property names. |
| GAP-SR-01 | feature-search-results-and-sorting.md | Duration always renders both hour and minute components (e.g., `"3h 0m"`, never `"3h"`). |
| GAP-SR-02 | feature-search-results-and-sorting.md | Departure/arrival times render as the literal `HH:mm` substring of the ISO datetime, with no timezone conversion. |
| GAP-SR-03 | feature-search-results-and-sorting.md | Sort is a stable sort; equal-key results retain original relative order — no secondary sort key introduced. |
| GAP-SR-04 | feature-search-results-and-sorting.md | `baseFare` is present in the response payload but not required to be rendered in the UI. |
| GAP-PA-01 | feature-provider-aggregation.md | Concrete fixed mock datasets: GlobalAir 4 flights (GA101/GA204/GA309/GA412), BudgetWings 4 flights (BW210/BW225/BW238/BW241), with specific routes/times/durations/Economy base fares. |
| GAP-PA-02 | feature-provider-aggregation.md | Cabin class fare multipliers applied to Economy base fare before provider pricing rules: Business ×2.0, First Class ×3.5. |
| GAP-PA-03 | feature-provider-aggregation.md | `departureDateTime`/`arrivalDateTime` combine the fixed local time-of-day with the requester's `SearchRequest.departureDate`; the flight set/time-of-day itself remains fixed (ASM-006 preserved). |
| GAP-PA-04 | feature-provider-aggregation.md | `ProviderName` literal values are exactly `"GlobalAir"` and `"BudgetWings"`. |
| GAP-BF-01 | feature-booking-flow.md | Email validation regex: `^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$`, named once per layer. |
| GAP-BF-02 | feature-booking-flow.md | Full name maximum length: 100 characters (in addition to the existing FR-029/FR-064 minimum/letter rules). |
| GAP-BF-03 | feature-booking-flow.md | Booking-reference collision-retry loop capped at 10 attempts; exhaustion throws, handled as a 500 by the centralized middleware. |
| GAP-BF-04 | feature-booking-flow.md | National ID pattern permits both letter cases: `^[A-Za-z0-9-]{5,20}$` (Passport remains explicitly uppercase-only per BR-003/US-005 AC6). |
| GAP-BF-05 | feature-booking-flow.md | Booking creation returns `StatusCode(201, response)` with no populated `Location` header (no GET-by-reference endpoint exists in MVP to reference). |
| GAP-EH-01 | feature-error-handling-and-validation.md | Exact 500-response title text: `"An unexpected error occurred. Please try again later."` |
| GAP-EH-02 | feature-error-handling-and-validation.md | FR-068's 404 scenario is not reachable by any MVP endpoint (no GET-by-identifier endpoint exists for flights or bookings). The 404 shape is documented for completeness/future-readiness only. |
| GAP-EH-03 | feature-error-handling-and-validation.md | Consumers must rely only on `status`/`title`/`errors` in the error envelope — `type`/`traceId` are ASP.NET Core framework defaults, not custom fields, and must not be asserted against fixed values in tests. |

None of the 21 Gap-fill decisions above change any approved requirement, business rule, NFR target, or architecture decision — each fills a deliberate gap that `docs/requirements.md`/`docs/architecture/architecture-plan.md` left open for implementation-time judgment (e.g., ASM-006 explicitly leaves the specific mock flight data unspecified; FR-063 requires field-level detail but not exact wording). None requires a new Product Owner approval gate.

---

## Open Questions

One item is flagged for explicit Phase 11 (Spec Readiness Check) acknowledgement — not a blocker, and not a new scope question:

- **GAP-EH-02**: FR-068 (404 Not Found) is written generically but is not reachable by either of the MVP's two endpoints (`POST /api/search`, `POST /api/bookings`) — there is no GET-by-identifier lookup anywhere in this MVP's surface (booking retrieval is explicitly Out of Scope, item 3; no airports GET endpoint per AD-002; no flight-by-ID lookup per AD-004). `docs/features/feature-error-handling-and-validation.md` documents the standard 404 shape for completeness/future-readiness only. Recommend Phase 11 explicitly note this as "FR-068: satisfied by shape-documentation only, no code path required in MVP" rather than leaving it ambiguous whether a dead-code 404 handler is expected.

No other open question was raised. All prior open questions in `docs/requirements.md` Section 8 (OQ-001–OQ-006) remain Resolved and were not reopened.

---

## Risks and Impediments

No new risk or impediment introduced. Pre-existing risks (RISK-001 EOD deadline, IMP-001 test-execution approval gate, RISK-004/005/009/010, and the sprint-plan's at-risk items BL-009/BL-015/BL-037/BL-038) are unaffected by this phase and remain tracked in `docs/handoffs/workflow-state.md`. This phase consumed no implementation time from the sprint's critical path — it is a specification-only phase, as required by CLAUDE.md Section 20/`.claude/rules/phased-execution.md` ("During a phase, do not perform work belonging to future phases").

---

## Required Next Agent Action

1. Orchestrator to review the five `docs/features/*.md` documents for completeness against the Phase 10 task brief.
2. Orchestrator to invoke `scrum-master` for Phase 11 — Spec Readiness Check, using `docs/features/feature-flight-search.md`, `docs/features/feature-search-results-and-sorting.md`, `docs/features/feature-provider-aggregation.md`, `docs/features/feature-booking-flow.md`, `docs/features/feature-error-handling-and-validation.md`, plus `docs/delivery/project-backlog.md` v1.1 and `docs/delivery/sprint-1-plan.md` v1.0, as inputs. Phase 11 must perform a feature-spec-level Definition of Ready re-check (distinct from the Phase 07/09 backlog-level DoR already confirmed) per `docs/delivery/sprint-1-plan.md` Section 8, and should explicitly acknowledge the GAP-EH-02 open item above.
3. Orchestrator to commit and merge `sdlc/10-feature-specifications-skyroute-mvp` to `main` (with human approval per the phased-execution workflow) before Phase 11 begins on a fresh branch.

---

## Completion Criteria for Next Step

Phase 11 (Spec Readiness Check) is complete when scrum-master has re-verified Definition of Ready against each of the five feature specs and the backlog items they make ready (BL-003, BL-004, BL-010, BL-017, BL-020–BL-032, BL-034–BL-038, and supporting items BL-001, BL-002, BL-005–BL-007, BL-011–BL-013, BL-016, BL-019), records the outcome in a Phase 11 handoff, and either confirms readiness for Phase 12 (Implementation) or raises any blocking gap to the Human Product Owner.

---

## Relevant Files

- `docs/features/feature-flight-search.md` (new)
- `docs/features/feature-search-results-and-sorting.md` (new)
- `docs/features/feature-provider-aggregation.md` (new)
- `docs/features/feature-booking-flow.md` (new)
- `docs/features/feature-error-handling-and-validation.md` (new)
- `docs/requirements.md` (read, not modified)
- `docs/specs/non-functional-requirements.md` (read, not modified)
- `docs/architecture/architecture-plan.md` (read, not modified)
- `docs/testing/test-strategy.md` (read, not modified)
- `docs/delivery/project-backlog.md` (read, not modified)
- `docs/delivery/sprint-1-plan.md` (read, not modified)
- `docs/handoffs/workflow-state.md` (updated)
- `docs/handoffs/handoff-index.md` (updated)
- `docs/handoffs/current-handoff.md` (updated)
