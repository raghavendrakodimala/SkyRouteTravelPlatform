# Decision Log — SkyRoute Travel Platform MVP

Version: 1.1
Date: 2026-07-07 (Phase 21 reconciliation; baseline 2026-07-03)
Author: Project Coordinator
Status: Active

---

## 1. Purpose

This log records all significant decisions made during the SkyRoute Travel Platform MVP delivery. Decisions are logged to maintain traceability, avoid repeated discussions, and provide context for future phases.

Decisions made in earlier phases carry forward unless explicitly superseded.

---

## 2. Decision Status Values

| Status | Meaning |
|---|---|
| Approved | Decision is approved and in effect |
| Pending | Decision is raised but awaiting approval |
| Superseded | Decision has been replaced by a later decision |
| Rejected | Decision was considered but rejected |
| Deferred | Decision deferred to a future phase |

---

## 3. Decision Impact Categories

| Category | Meaning |
|---|---|
| Scope | Affects what is built or excluded from the sprint |
| Architecture | Affects system design, patterns, or technical approach |
| Process | Affects how the team operates or delivers |
| Technology | Affects the technology stack or framework choice |
| Risk | Affects risk posture or acceptance |
| Quality | Affects quality standards or review criteria |

---

## 4. Decision Log

| ID | Date | Decision | Rationale | Made By | Status | Impact |
|---|---|---|---|---|---|---|
| DEC-SA-001 | 2026-07-03 | Design Principles Review completed; Section 3.10 added to requirements; FR-046 expanded to enumerate required interfaces; FR-051 clarified for SRP; FR-055 clarified for DRY; BR-008 updated for DIP; BR-011/012/013 added; FR-001 and FR-005b updated for trip type seam | See detailed rationale in decision log entry below | solution-architect | Approved (pending PO review) | Architecture — SOLID compliance, interface contracts, extensibility seams |
| DEC-001 | 2026-07-03 | Sprint length is compressed to one working day (EOD 2026-07-03) covering all SDLC phases sequentially as a single sprint | Hiring challenge has a fixed EOD deadline; all delivery must complete within this window | scrum-master (approved by Human PO context) | Approved | Process — all phases must complete within the day |
| DEC-002 | 2026-07-03 | Daily Standup is adapted as a phase boundary check recorded in handoff files — no separate standup artefact | Simulated team does not operate on a time-boxed daily schedule; phase transitions serve the same purpose | scrum-master | Approved | Process — no separate standup artefact required |
| DEC-003 | 2026-07-03 | Estimation uses T-shirt sizes (XS/S/M/L/XL) rather than Fibonacci story points | Single-sprint compressed delivery; T-shirt sizing is sufficient for relative complexity tracking without overhead of story point calibration | scrum-master | Approved | Process — estimation approach |
| DEC-004 | 2026-07-03 | Human Product Owner participates in Sprint Review (Phase 22) but not in automated phases — SDLC Orchestrator stops at Phase 22 for human input | Human oversight at the increment acceptance gate is required; automated phases do not require synchronous human involvement | scrum-master | Approved | Process — human gate at Phase 22 |
| DEC-005 | 2026-07-03 | Critical and High findings may only be accepted (not fixed) with explicit Human Product Owner approval, recorded in the review report as Accepted Risk | Ensures quality governance is not bypassed; Human PO bears accountability for accepted risks | scrum-master | Approved | Quality — finding acceptance protocol |
| DEC-006 | 2026-07-03 | Backlog refinement occurs as a single session at Phase 07 and Phase 10 — no rolling calendar refinement | Single compressed sprint; no time for rolling refinement sessions | scrum-master | Approved | Process — backlog refinement cadence |
| DEC-007 | 2026-07-03 | Phased SDLC Autopilot is used with `--auto-commit-merge --no-push` — SDLC Orchestrator may commit and merge phase branches autonomously | Explicit user approval was provided by initiating phased autopilot run; push is not approved | Human PO (via run command) | Approved | Process — Git automation boundary |
| DEC-008 | 2026-07-03 | In-memory data store is used for flight data — no database or external persistence in the MVP | Confirmed by architecture-plan.md (Phase 06): `IBookingStore`/`InMemoryBookingStore`, thread-safe, per BR-008 | project-coordinator; confirmed solution-architect Phase 06 | Approved | Architecture — persistence strategy |
| DEC-009 | 2026-07-03 | Angular standalone components are used (no NgModule-based architecture) | Confirmed by architecture-plan.md (Phase 06); requirements.md v1.4 targets Angular 22 (superseding the earlier Angular 17 reference in this row's original wording) | project-coordinator; confirmed solution-architect Phase 06 | Approved | Architecture — Angular component model |
| DEC-010 | 2026-07-03 | No external flight data provider API is integrated — flight data is seeded via two mock providers (`GlobalAirProvider`, `BudgetWingsProvider`) | Confirmed in requirements.md v1.4 (ASM-007) and architecture-plan.md Phase 06 | project-coordinator; confirmed solution-architect Phase 03/06 | Approved | Scope — external dependency exclusion |
| DEC-011 | 2026-07-03 | Review phases 15–18 may be partially overlapped (Code Review + Security Review concurrently; Accessibility + Performance concurrently) if timeline pressure warrants | Parallel delivery to protect the EOD deadline while maintaining independent review quality | project-coordinator | Pending — revisit at Phase 09 Sprint Planning | Process — parallel review execution |
| DEC-012 | 2026-07-03 | No PR-based review comments are used — all review findings are stored as markdown files under `docs/reviews/` | Project operating model decision from CLAUDE.md; ensures findings are persistent and traceable without PR tooling | Human PO (via CLAUDE.md) | Approved | Process — review artefact storage |
| DEC-013 | 2026-07-03 | `BookingFormComponent` (BL-033, L-sized) split into BL-036/037/038 (S/M/M) at Phase 08 rather than implemented as one item | Reduces single-item concentration risk (RISK-014) ahead of a same-day delivery window; all three still implement one component file per architecture-plan.md — task decomposition only, no architecture change | sdlc-orchestrator (resolving HO-007 open question), executed by project-coordinator Phase 08 | Approved | Process — task decomposition |
| DEC-014 | 2026-07-03 | Backend contract models (BL-003) and frontend shared models (BL-021) are built in parallel against the frozen architecture-plan.md §5 API contract, rather than strictly sequenced | Contract is already frozen at Phase 06; strict sequencing would cost time against the EOD deadline for no accuracy benefit | sdlc-orchestrator (resolving HO-007 open question), executed by project-coordinator Phase 08 | Approved | Process — parallel build sequencing |
| DEC-015 | 2026-07-07 | Passenger count is captured at booking (one-passenger-at-a-time in-place add), not on the search form; the search passenger field is removed and `SearchRequest.passengerCount` always submits `1`. Deliberate deviation from the challenge PDF, documented in README | PO UX correction during the booking UI redesign — supersedes the original search passenger-count selector (US-001/BL-028) and DESIGN-FLOW-001 Part B's prompt/review wizard. See HO-032, HO-034 | Human PO | Approved | Scope — challenge-PDF deviation |
| DEC-016 | 2026-07-07 | Canonical phase model adopted system-wide: Phase 00 (repository/tooling pre-phase) + Phases 01–24; Iterative Review-Fix Loops run inside Phases 15–18; Phase 19 is QA-* consolidation; no separate merge phase (each phase merges its own branch). Defined once in CLAUDE.md §7; all other files cite it | Resolved three mutually incompatible phase-numbering schemes (defect D-1, `docs/delivery/autopilot-efficiency-review-2026-07-07.md`); the delivered-history numbering was made canonical | Human PO (directive), executed by sdlc-orchestrator (DEL-025, HO-035) | Approved | Process — SDLC phase model |
| DEC-017 | 2026-07-07 | Handoff loop-log economy: numbered handoff files at phase boundaries only; inside a review-fix loop all participants append to a single per-phase loop log (`docs/handoffs/<phase>-loop-log.md`); `current-handoff.md` mirrors latest state; the index lists each loop log once. Required handoff content fields unchanged | Loop iterations were minting per-iteration numbered handoff files (HO-027–HO-031 accumulated as unindexed noise — defect D-2, autopilot efficiency review) | Human PO (directive), executed by sdlc-orchestrator (DEL-025, HO-035) | Approved | Process — handoff economy |
| DEC-018 | 2026-07-07 | Transient dev-server runs are pre-approved for rendered-UI verification, PO demos, and E2E execution (servers must be stopped after evidence capture); safe validation commands (existing test suites, builds, lint, type-check, read-only git) are pre-approved for every agent whose role requires validation and never require a human stop. Installs/upgrades and destructive operations remain gated; CLAUDE.md §21 gates intact | Resolved the dev-server-vs-§14 contradiction and autopilot friction (defect D-3, autopilot efficiency review); no safety rule weakened | Human PO (directive), executed by sdlc-orchestrator (DEL-025, HO-035) | Approved | Process — validation/dev-server pre-approval |

---

## 4b. DEC-SA-001 — Design Principles Review: Detailed Rationale

**Decision ID:** DEC-SA-001
**Date:** 2026-07-03
**Made By:** solution-architect
**Status:** Approved (pending PO review)
**Impact:** Architecture

### What was reviewed

The full `docs/requirements.md` v1.0 was evaluated against SOLID, YAGNI, KISS, DRY, and Separation of Concerns. The review focused on seven areas: provider architecture, booking service abstractions, airport data abstraction, notification extension point, frontend design principles, validation single source of truth, and auth/extensibility seams.

### Decisions and rationale

**1. Required backend interfaces explicitly enumerated (FR-046 updated)**

The original FR-046 named only `IFlightProvider`. Three further interfaces are required for genuine DIP compliance and unit-testability: `IFlightAggregatorService`, `IBookingService`, and `IBookingStore`. Without naming these, the implementation risk is that controllers contain business logic and concrete classes are referenced directly — both SOLID violations. These are not YAGNI: all four interfaces serve concrete MVP needs (testability, swappability of persistence, thin controllers).

**2. Pricing logic isolation within providers (FR-051 updated)**

SRP analysis: a provider class has two distinct responsibilities — constructing flight data and computing the price. If BudgetWings changes its pricing algorithm, the data construction code should not need to be read or touched. The requirement now mandates that pricing logic is implemented as a named, isolated method within the provider, not inlined. A separate `IPricingStrategy` interface is explicitly excluded (YAGNI-003) — the complexity does not justify extraction for two fixed rules. The SRP benefit is achieved at zero additional abstraction cost.

**3. Airport data abstraction clarified (FR-055 updated)**

The original FR-054/FR-055 pair allowed "either API or hardcoded" without constraint, creating a DRY risk: airport data could silently proliferate into component files, service files, and validation logic. The requirement now mandates a single authoritative source per layer. YAGNI-001 explicitly excludes `IAirportRepository` — a static list does not warrant a repository interface.

**4. Notification extension point — excluded as YAGNI (YAGNI-002)**

Email confirmation is out of scope. Adding `INotificationService` as a placeholder interface adds indirection (KISS violation) with no current benefit. The seam is preserved architecturally by the absence of any notification call in the booking service — no interface is needed to maintain that seam.

**5. Centralised error handling mandated (BR-011, DP-007)**

Without an explicit requirement, each controller is at risk of independently implementing stack-trace suppression and error-body formatting. This is a DRY violation and a SRP violation (controllers would have two reasons to change). BR-011 and DP-007 mandate a single middleware as the authority for 500 error responses.

**6. Auth middleware seam preserved (BR-012, DP-008)**

BR-010 correctly excludes auth from MVP. However, without a constraint, developers may add `if (user == null) return Unauthorized()` checks inside service methods — a pattern that makes auth a distributed concern scattered through business logic rather than a middleware layer. BR-012 explicitly prohibits this and explains why: OCP compliance requires auth to be addable as middleware without modifying existing service code.

**7. Trip type field added to search model (BR-013, FR-001, FR-005b)**

Round-trip flights are explicitly out of scope (Section 7, item 12) but are a near-certain near-future feature. If the search API omits the `tripType` field and assumes one-way, adding return-trip support later requires a breaking API change and a breaking frontend model change. Adding the field now with a single valid value (`OneWay`) preserves OCP at zero implementation cost. This is the textbook OCP case: extend without modifying. This is not a YAGNI violation — the requirement for extensibility is explicit in Section 1.2 ("extensible provider architecture") and the out-of-scope list confirms round-trips are a known future item.

**8. Frontend SRP and DRY requirements (DP-009 through DP-016)**

No existing requirement explicitly prevented components from containing HTTP calls, duplicating total price calculation, or mixing reactive patterns. These gaps produce predictable maintainability problems. The new requirements are minimal and targeted: components delegate to services (SRP), total price is computed once (DRY), airport constants live in one file (DRY), reactive patterns are consistent (KISS).

**9. YAGNI/KISS guard table (YAGNI-001 through YAGNI-006)**

The explicit exclusion list prevents well-intentioned over-engineering during implementation. Without named exclusions, a developer may add event sourcing, a caching layer, or a repository-on-top-of-store pattern believing they are following best practices. The explicit list makes the MVP boundary concrete and reviewable.

---

## 5. Decision Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial log created for Phase 02; DEC-001 through DEC-006 sourced from HO-001 (scrum-master Phase 01 decisions); DEC-007 through DEC-012 raised from Phase 02 analysis |
| 2026-07-07 | project-coordinator | Phase 21 — recorded DEC-015 (passenger-count-at-booking PDF deviation, PO-approved), DEC-016 (canonical phase model 01–24), DEC-017 (handoff loop-log economy), DEC-018 (transient dev-server + validation-command pre-approval), sourced from HO-032/HO-034/HO-035 and `docs/delivery/autopilot-efficiency-review-2026-07-07.md`. Note: DEC-011 (overlapped reviews) was never invoked — reviews ran sequentially as Phases 15–18; effectively lapsed without impact |

---

## 6. Reference Documents

- `docs/delivery/risk-register.md`
- `docs/handoffs/workflow-state.md`
- `docs/handoffs/01-scrum-master-to-sdlc-orchestrator-scrum-model.md`
- `CLAUDE.md`
