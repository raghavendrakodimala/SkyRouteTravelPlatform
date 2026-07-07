# Risk Register — SkyRoute Travel Platform MVP

Version: 1.1
Date: 2026-07-07 (Phase 21 reconciliation; baseline 2026-07-03)
Author: Project Coordinator
Status: Active

---

## 1. Purpose

This register tracks delivery and technical risks for the SkyRoute Travel Platform MVP. Risks are assessed for probability and impact, assigned a severity, and assigned a mitigation strategy and owner.

The register is reviewed at each phase boundary and updated when new risks are identified or existing risk status changes.

---

## 2. Probability Scale

| Value | Meaning |
|---|---|
| Low | Unlikely to occur (less than 25% chance) |
| Medium | May occur (25–60% chance) |
| High | Likely to occur (more than 60% chance) |

---

## 3. Impact Scale

| Value | Meaning |
|---|---|
| Low | Minor inconvenience; no phase delay |
| Medium | Causes delay or rework but recoverable within sprint |
| High | Threatens sprint goal or delivery quality |
| Critical | Would prevent delivery or require Human PO scope decision |

---

## 4. Severity Matrix

| Probability \ Impact | Low | Medium | High | Critical |
|---|---|---|---|---|
| Low | Low | Low | Medium | High |
| Medium | Low | Medium | High | Critical |
| High | Medium | High | Critical | Critical |

---

## 5. Risk Status Values

| Status | Meaning |
|---|---|
| Open | Risk is active and being monitored |
| Mitigated | Mitigation actions applied; risk is reduced |
| Accepted | Risk accepted by owner (Low/Medium) or by Human PO (High/Critical) |
| Closed | Risk no longer applicable |
| Escalated | Risk escalated to Human Product Owner for decision |

---

## 6. Risk Register

| ID | Risk Description | Category | Probability | Impact | Severity | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|---|
| RISK-001 | EOD 2026-07-03 deadline cannot be met due to compressed delivery timeline — insufficient time to complete all SDLC phases with full quality gates | Timeline | High | Critical | Critical | Enforce strict phase sequencing with no scope creep; prioritise Must Have items; invoke parallel delivery plan; raise scope trade-off decision with Human PO immediately if delays emerge | project-coordinator | Closed — 2026-07-07: Phases 01–20 delivered, 365/365 tests, zero Open findings in all four numbered reviews (HO-038). The original EOD 2026-07-03 gate was superseded by PO-directed continuation through 2026-07-07 |
| RISK-002 | In-memory data store for flight data is lost on application restart — no persistence across sessions | Technical | High | Medium | High | Accepted by design: in-memory store is a deliberate MVP trade-off; document limitation in README and architecture docs; ensure evaluators are aware this is intentional | solution-architect | Accepted — deliberate MVP trade-off (DEC-008); limitation documented in README/architecture docs |
| RISK-003 | Flight provider integration is tightly coupled — adding a second flight provider requires significant backend rework | Technical | Medium | High | High | Design the provider layer with a provider abstraction (interface/strategy pattern) from the start; document extensibility in architecture; validate in code review | solution-architect | Closed — `IFlightProvider`/`FlightAggregatorService` abstraction implemented (Phase 12) and verified at Phase 15 code review (zero Open) |
| RISK-004 | Test execution is blocked because npm install / dotnet test / dotnet restore cannot run autonomously — requires human approval | Process | High | High | Critical | Logged as IMP-001; test execution commands to be presented to Human PO at Phase 14; test execution summary will document "tests not run — pending approval" until resolved | functional-tester | Closed — approvals granted per-command (IMP-001 Resolved); all suites executed fresh through Phase 20: 365/365 (HO-038). Validation commands now pre-approved per DEC-018 |
| RISK-005 | Hiring challenge evaluation criteria are not fully specified — implemented features may not align with evaluator expectations | Scope | Medium | High | High | Implement the most visible, demonstrable capabilities: search, results, booking summary; document all trade-offs; ensure README clearly explains what was built and why | product-owner | Accepted — mitigation complete: README documents scope, trade-offs, and the PO-approved PDF deviation (DEC-015); evaluation outcome is outside project control |
| RISK-006 | Angular 17 standalone component model requires specific patterns — team agent may produce incompatible component declarations | Technical | Medium | Medium | Medium | Enforce standalone component pattern in architecture guidance; code reviewer to check for NgModule anti-patterns; lead engineer to use Angular 17 canonical examples | lead-full-stack-engineer | Closed — standalone-component workspace (Angular 22) delivered and code-reviewed with no NgModule anti-pattern findings (Phase 15) |
| RISK-007 | ASP.NET Core 8 minimal API vs controller pattern decision not finalised — inconsistent implementation if not decided early | Technical | Medium | Medium | Medium | Architecture planning phase (Phase 06) to make explicit decision and record in decision log; enforce pattern in feature specs | solution-architect | Closed — controller pattern decided at Phase 06 (architecture-plan.md) and implemented consistently (thin controllers, Phase 15 verified) |
| RISK-008 | Multi-passenger booking support scope is ambiguous — incorrect assumption could cause significant rework | Scope | Low | High | Medium | Requirements analysis (Phase 03) to clarify and document multi-passenger scope; Human PO to approve before sprint planning | product-owner | Closed — multi-passenger scope clarified and approved at Phase 03 (requirements v1.4); flow subsequently refined by DEC-015 (passenger count captured at booking, PO 2026-07-07) |
| RISK-009 | No historical velocity baseline — sprint capacity estimate may be incorrect, leading to over-commitment | Delivery | High | Medium | High | Treat Phase 01–11 (planning phases) as the primary capacity consumer for the day; implementation phases are the core delivery block; use T-shirt sizing to right-size commitment | scrum-master | Closed — sprint delivered in full; no further capacity planning in scope for this engagement |
| RISK-010 | Review phases (15–18) may surface Critical/High findings that require significant fix effort, risking the merge deadline | Quality | Medium | High | High | Run reviews concurrently where possible (Code + Security can overlap; A11Y + Perf can overlap); address High findings promptly; Critical findings require immediate Human PO decision | project-coordinator | Closed — all four review loops closed to zero Open (CR-001–005, SEC-001–004, A11Y-001–006, PERF-001); SEC-001 (High) resolved by fix, not acceptance (HO-016E) |
| RISK-011 | CORS configuration between Angular (port 4200) and ASP.NET Core (port 5000/5001) may not be configured correctly, blocking frontend-backend integration | Technical | Medium | Medium | Medium | DevOps/architecture phase to include CORS policy definition; lead engineer to configure CORS explicitly; functional tester to include CORS in integration test scope | lead-full-stack-engineer | Closed — CORS restricted to `http://localhost:4200` (Phase 12); proven end-to-end by 12/12 E2E runs against live servers (Phase 20) |
| RISK-012 | Accessibility review may not be executable in full without a running browser environment — static review only | Process | Medium | Medium | Medium | Accessibility tester to perform static code review against WCAG 2.1 AA criteria and document findings; note limitation in review report | accessibility-tester | Closed — rendered-app/browser verification was performed (HO-026, E2E runs); rendered-UI evidence is now mandatory per `.claude/rules/ui-ux-quality-gates.md` |
| RISK-013 | Single agent context window limitations may cause loss of context across phases if handoff files are not read correctly | Process | Low | High | Medium | Enforce strict handoff file protocol; all agents read current-handoff.md and workflow-state.md before starting; SDLC Orchestrator validates handoff completeness | sdlc-orchestrator | Closed — handoff protocol held through Phase 20 (HO-001–HO-038); loop-log economy adopted (DEC-017) to remove residual per-iteration handoff noise |
| RISK-014 | BL-033 (`BookingFormComponent`, project-backlog.md) is the single largest/most complex backlog item — it combines flight-summary display, price-breakdown display, per-passenger form orchestration, submit/loading/error state, and re-submission guarding (US-004, US-006). If not split further, it risks becoming a single-item bottleneck during the compressed one-day sprint | Delivery | Medium | Medium | Medium | Phase 08 (Parallel Delivery Plan) and Phase 09 (Sprint Planning) to consider splitting BL-033 into smaller implementation sub-tasks (e.g., summary/breakdown render vs. submit orchestration) if a single owner cannot absorb an L-sized item within the available window; flag at sprint planning if this proves to be a critical-path item | project-coordinator | Mitigated — Phase 08: BL-033 decomposed into BL-036/BL-037/BL-038 (S/M/M) per SDLC Orchestrator decision on HO-007; see `docs/delivery/parallel-delivery-plan.md` v1.0 |
| RISK-015 | BL-002 (backend Domain Models) and BL-021 (frontend Shared Models) are both built independently and in parallel against the same frozen contract shape in `docs/architecture/architecture-plan.md` Section 5. If the backend contract shape needs adjustment after frontend implementation has started against it, both sides require synchronized rework | Technical | Low | Medium | Low | Treat architecture-plan.md Section 5 as frozen for Sprint 1; any contract-shape change during implementation (Phase 12) must be raised as a decision-log entry and communicated to both backend and frontend owners before either side proceeds further; Phase 08 to sequence BL-003 (backend contract models) confirmation ahead of BL-021 (frontend shared models) start where parallel tracks allow | project-coordinator | Mitigated — Phase 08: SDLC Orchestrator confirmed parallel build is acceptable given the frozen Section 5 contract (no forced BL-003-before-BL-021 sequencing); see `docs/delivery/parallel-delivery-plan.md` v1.0 |
| RISK-016 | Nested duplicate folder `SkyRouteTravelPlatform/` remains in the working tree (gitignored) — risk of confusion/accidental edits against the stale copy | Process | Low | Low | Low | Folder is gitignored and flagged as a hazard in the git-flow command. Next action: obtain explicit Human PO deletion approval at Phase 24 closure (deletion requires PO approval per tool-safety rules) | project-coordinator | Open |
| RISK-017 | `main` is ~59 commits ahead of `origin/main` — all delivered work exists only on the local machine until push is approved | Delivery | Low | High | Medium | Standing `--no-push` rule is deliberate (DEC-007). Next action: request explicit PO push approval at Phase 24 (final merge/closure gate); no destructive git operations meanwhile | sdlc-orchestrator | Open |
| RISK-018 | 6 Low advisory, report-only findings remain `Open` in the ad-hoc `docs/reviews/booking-ui-redesign-review-2026-07-07.md` (working labels A11Y-009..012, CR-004, VIS-014) — closure-hygiene residue, not quality blockers per the Phase 20 sweep (HO-038) | Quality | High | Low | Medium | None blocks merge per the report's own design (0 unresolved Critical/High). Next action: PO to disposition each (Accepted Risk / Deferred) via the reviewing agents before project closure, per the Phase 20 recommendation | product-owner | Open |
| RISK-019 | NFR-TEST-005 coverage percentage remains unmeasured — pass/fail health is green (365/365) but the numeric coverage gate has never been collected (collection command never approved/run; unchanged since Phase 14) | Quality | High | Low | Medium | Next action: PO decides at Phase 22/24 whether to approve the coverage-collection run before closure or accept the gap; functional-tester to execute if approved | functional-tester | Open |

---

## 7. Risk Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial register created for Phase 02 |
| 2026-07-03 | project-coordinator | Phase 07 — added RISK-014 (BL-033 complexity concentration) and RISK-015 (backend/frontend contract-model parallel-build synchronization risk), surfaced during project-backlog.md decomposition |
| 2026-07-03 | project-coordinator | Phase 08 — RISK-014 and RISK-015 marked Mitigated per SDLC Orchestrator decisions on HO-007's two open questions (BL-033 split; BL-003/BL-021 parallel build accepted). See `docs/delivery/parallel-delivery-plan.md` v1.0 |
| 2026-07-07 | project-coordinator | Phase 21 — full reconciliation against Phases 12–20 outcomes (HO-032–HO-038, `docs/testing/execution/phase-20-retest-summary.md`): RISK-001/003/004/006/007/008/009/010/011/012/013 Closed; RISK-002/005 Accepted with mitigation evidence. Added carry-forwards RISK-016 (nested duplicate folder awaiting deletion approval), RISK-017 (~59 unpushed commits, push pending PO word), RISK-018 (6 Low advisory findings awaiting PO disposition), RISK-019 (NFR-TEST-005 coverage % unmeasured) |

---

## 8. Reference Documents

- `docs/delivery/dependency-register.md`
- `docs/delivery/impediment-log.md`
- `docs/handoffs/workflow-state.md`
- `docs/delivery/sdlc-operating-model.md`
- `.claude/rules/sdlc-rules.md`
