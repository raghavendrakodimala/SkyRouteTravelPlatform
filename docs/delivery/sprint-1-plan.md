# Sprint 1 Plan — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
Author: scrum-master
Status: Approved by Human Product Owner (2026-07-03) — see Section 9
Phase: Phase 09 — Sprint Planning

---

## 1. Sprint Goal

**Deliver the full SkyRoute flight search and booking MVP — one-way flight search across two mock providers, sortable results, and a complete passenger-details-to-confirmation booking flow — per the approved requirements (`docs/requirements.md` v1.4), architecture (`docs/architecture/architecture-plan.md` v1.0), and backlog (`docs/delivery/project-backlog.md` v1.1), within the EOD 2026-07-03 delivery window.**

This is Sprint 1 — the only sprint of this engagement (ASM-008, single-sprint MVP). There is no Sprint 2 to defer incomplete work into. Per `docs/delivery/scrum-operating-model.md` Section 4, the entire SDLC — requirements through retrospective — is compressed into this single sprint.

---

## 2. Sprint Scope

### 2.1 All 37 active backlog items are in scope for Sprint 1

Per `docs/delivery/project-backlog.md` v1.1, there are 37 active backlog items (19 backend, 18 frontend; BL-033 decomposed into BL-036/BL-037/BL-038 at Phase 08, so it is not separately counted). All 37 are Must Have or contain a Must Have core (BL-004 and BL-020 each carry one Should Have sub-element — see Section 2.3).

**Because this is a single-sprint delivery with no future sprint to carry work into, all 37 items are committed to Sprint 1.** No item is descoped, deprioritized, or silently dropped by this plan. Scrum Master does not have authority to remove Must Have scope (`.claude/rules/delegation-rules.md` — Scrum Master "must not directly assign technical implementation scope," and CLAUDE.md Section 21 requires Human PO approval before "changing committed sprint scope").

### 2.2 At-risk items (flagged, not descoped)

Given the EOD deadline (RISK-001, Critical) and the absence of a velocity baseline (RISK-009, High — see Section 6), the following are flagged as **at-risk of not completing within the window**, based on size and position in the critical path (`docs/delivery/parallel-delivery-plan.md` Section 4). These are flagged for visibility only — Scrum Master is not unilaterally cutting any of them:

| Item | Why at-risk | Recommendation if time runs short |
|---|---|---|
| BL-009 (`IFlightAggregatorService`) | M-sized, on backend critical path, carries the AD-010 fault-isolation correctness risk explicitly called out in the architecture plan | Do not rush; if the day is running behind, this is a candidate for Human PO discussion before accepting a shortcut, not a candidate for silent simplification |
| BL-015 (`IBookingService`) | M-sized, most logic-dense single backend item (7-step orchestration), last item on the Track B chain before BL-018/BL-019 | Same as above — core booking logic, not a candidate for descoping |
| BL-037 (Passenger Form Array Orchestration) | M-sized, most structurally complex of the three BL-033 splits, gates BL-038 | If behind schedule, BL-038 (submit wiring) cannot start until this is done — this is a genuine schedule risk, not just a size risk |
| BL-038 (Submit/Loading/Error/Re-submission) | M-sized, last item on the frontend critical path, integration point of BL-036+BL-037 | Same — sits at the end of the frontend chain; any upstream delay compounds here |
| Reviews 15–18 + fixes (19–20) | Not backlog items, but Phase-level risk (RISK-010): Critical/High findings surfacing late in the day could consume remaining time before merge | If a Critical/High finding cannot be fixed in the remaining window, it requires explicit Human PO Accepted Risk decision (DEC-005) — not a default pass |

**No backlog item is recommended for removal from MVP scope by this plan.** If, during Phase 12 (Implementation), it becomes clear that the full 37-item scope genuinely cannot complete by EOD, that is a scope-change decision requiring immediate escalation to the Human Product Owner (CLAUDE.md Section 21; `docs/delivery/scrum-operating-model.md` Section 10.4) — not a decision Scrum Master, SDLC Orchestrator, or any implementation agent may make alone.

### 2.3 Should Have sub-elements noted (not separately scoped out)

- BL-004: the FR-054 "GET `/api/airports`" endpoint alternative is Should Have and, per AD-002, is explicitly **not built** — this was an architecture decision at Phase 06, not a Sprint 09 descope. BL-004's Must Have core (static `AirportDataService`) remains fully in scope.
- BL-020: the functional `CanActivate` route guard is Should Have (usability robustness beyond the routing shell itself). It remains in scope as part of BL-020; if time pressure emerges late in the day, this specific sub-element (not the whole item) is the most defensible candidate for a Human-PO-approved trim, precisely because it was already distinguished as Should Have at Phase 07.

No other item carries a Should Have/Must Have split.

---

## 3. Sprint Backlog (Commitment Order)

The Sprint Backlog adopts, verbatim, the 24-step recommended execution order from `docs/delivery/parallel-delivery-plan.md` v1.0 Section 6 — that ordering already reflects the dependency graph, the critical-path analysis, and the deliberate front-loading of the two highest-correctness-risk items (BL-013, BL-009). This plan does not re-derive or alter that order; it restates it as the Sprint Backlog artifact and adds Scrum-specific grouping for progress tracking through the day.

### 3.1 Sprint Backlog Increments

For daily-progress visibility only (per DEC-002's phase-boundary-check model — this grouping is not a new ceremony), the 24 steps are grouped into three increments:

**Increment 1 — Foundation and Backend Core (Steps 1–11)**

| Step | Items | Track(s) |
|---:|---|---|
| 1 | BL-001, BL-020 | A, D (scaffolding) |
| 2 | BL-002 | A |
| 3 | BL-021 | D |
| 4 | BL-003, BL-004, BL-006 | A |
| 5 | BL-013 | B (front-loaded — BR-004 RNG correctness risk) |
| 6 | BL-012, BL-011 | B |
| 7 | BL-022, BL-023, BL-024, BL-025, BL-026, BL-031 | D |
| 8 | BL-005 | A |
| 9 | BL-007 | A |
| 10 | BL-008 | A |
| 11 | BL-009 | A (front-loaded — AD-010 fault-isolation correctness risk) |

**Increment 2 — Backend Completion (Steps 12–17)**

| Step | Items | Track(s) |
|---:|---|---|
| 12 | BL-010 | A |
| 13 | BL-014 | B |
| 14 | BL-015 | B |
| 15 | BL-016 | C |
| 16 | BL-017, BL-018 | C |
| 17 | BL-019 | C — backend feature-complete and runnable |

**Increment 3 — Frontend Feature Completion (Steps 18–24)**

| Step | Items | Track(s) |
|---:|---|---|
| 18 | BL-027, BL-032 | D, E |
| 19 | BL-028, BL-030 | E |
| 20 | BL-034 | E |
| 21 | BL-029 | E |
| 22 | BL-036, BL-037 | E |
| 23 | BL-038 | E — booking flow complete end-to-end |
| 24 | BL-035 | E — frontend feature-complete |

**End state after Increment 3 (Step 24):** both backend and frontend are feature-complete per the approved backlog (`parallel-delivery-plan.md` Section 6), ready for Phase 13 (Test Writing) onward.

### 3.2 No re-sequencing performed at this phase

This plan does not change the order, split further, merge steps, or re-prioritize within the 24 steps. If actual implementation progress (Phase 12) requires deviation from this order — e.g., picking up a second contributor for a genuinely-parallel pair per `parallel-delivery-plan.md` Section 5 — that is a Phase 12 execution decision to be recorded in `docs/delivery/task-board.md` and `docs/handoffs/`, not a Sprint Planning scope change.

---

## 4. Definition of Ready Confirmation

Per `docs/delivery/project-backlog.md` v1.1 Section 9, all 37 active backlog items were checked against `.claude/rules/definition-of-ready.md` at **Phase 07 (Project Backlog Creation)** and confirmed:

> "Result: all 37 active items are Ready. No item is Blocked or Conditionally Ready." (BL-036/BL-037/BL-038 — the BL-033 splits — were individually DoR-checked at Phase 08 and are also Ready.)

Sprint Planning (Phase 09) does not re-perform this analysis. This section is a confirmation, not new work:

- Business value: clear — inherited from the 8 approved, Human-PO-approved user stories (`docs/requirements.md` v1.4).
- User story/acceptance criteria: documented per item, traced to specific `US-*`/`FR-*`/`BR-*`/`DP-*` IDs (`project-backlog.md` Sections 4–5).
- Scope boundaries: clear — each item names exactly one architecture-plan component.
- Dependencies: identified — `project-backlog.md` Section 6 ("Blocked By" column) and `parallel-delivery-plan.md` Sections 3–4 (full dependency graph and critical path).
- Risks: identified — carried at register level (`docs/delivery/risk-register.md`); no new item-level risk emerged requiring a distinct entry.
- Architecture guidance: exists — 100% of items cite an architecture-plan Section 2–5 location.
- API/UI/test specs: API contracts (architecture plan Section 5), test approach (`docs/testing/test-strategy.md` v1.0), UI flow (architecture plan Section 4) all exist at the strategy level; item-level test-case authoring is Phase 13 scope, correctly not required for DoR at Phase 09.
- NFR impact: understood — governed at `docs/specs/non-functional-requirements.md` v1.0 level; no item introduces a new NFR concern.
- Human Product Owner blocking questions: none outstanding — all open questions in `docs/requirements.md` Section 8 are Resolved.

**Confirmation: all 37 items pulled into Sprint 1 are Ready. No item requires a Conditionally Ready exception.**

---

## 5. Definition of Done for This Sprint

Per `.claude/rules/definition-of-done.md` and `docs/delivery/scrum-operating-model.md` Section 7, Sprint 1 — and every backlog item within it — is done only when all of the following are satisfied before the final merge to `main`:

| # | Criterion | Applies to Sprint 1 as |
|---|---|---|
| 1 | Implementation matches approved specs | Every item traced back to `requirements.md` v1.4 / `architecture-plan.md` v1.0 / `project-backlog.md` v1.1 — no deviation without a recorded decision |
| 2 | Tests are added or updated | Phase 13 (Test Writing) — unit/integration/component/E2E per `docs/testing/test-strategy.md` |
| 3 | Test execution summary exists | Phase 14, stored under `docs/testing/execution/`, subject to RISK-004/IMP-001 (human approval needed for `dotnet test`/`npm test` commands) |
| 4 | Code review report exists | Phase 15, stored under `docs/reviews/`, findings IDed `CR-001` etc. |
| 5 | Security review report exists | Phase 16, applies to all API/sensitive-data work (all backend booking/search endpoints) |
| 6 | Accessibility review exists | Phase 17, applies — this MVP has a full UI (search, results, booking, confirmation screens) |
| 7 | Performance review exists | Phase 18, applies where relevant per NFR spec — aggregation fan-out (BL-009) and results rendering are the most performance-sensitive paths |
| 8 | Critical/High findings resolved or Human-PO-accepted | Phases 19–20; any Accepted Risk must be recorded per DEC-005 |
| 9 | Documentation updated | Ongoing + Phase 24 final summary |
| 10 | Delivery tracking updated | Phase 21, `docs/delivery/task-board.md` and related registers |
| 11 | Handoff notes complete | Every phase transition, per `.claude/rules/agent-communication.md` |
| 12 | Working tree clean before merge | Every phase branch, and the final merge to `main` |

**Sprint 1 is only "Done" when every one of the 37 backlog items satisfies all 12 criteria, or any exception is explicitly Human-PO-approved and recorded (Accepted Risk / carry-forward decision at Sprint Review, Phase 22).** There is no Sprint 2 to carry incomplete work into by default — any item not meeting Done by EOD must be presented to the Human Product Owner at Sprint Review (Phase 22) as a carry-forward or accepted-gap decision, per `docs/delivery/scrum-operating-model.md` Section 9.1/9.2.

---

## 6. Capacity and Risk Framing

**No velocity baseline exists (RISK-009, High, Open).** This is Sprint 1 — there is no prior sprint to derive velocity from, and `docs/delivery/scrum-operating-model.md` Section 9.1 confirms: "There is no historical velocity baseline — this is sprint 1." T-shirt sizes (XS/S/M/L — per DEC-003) describe *relative complexity*, not time commitments, and no story-point or hour conversion is fabricated by this plan.

**Sprint capacity is stated honestly as: best-effort within the EOD 2026-07-03 window, with effectively one primary implementer** (per `docs/delivery/parallel-delivery-plan.md` Section 1.1: "This plan therefore does not assume N developers are available"). No numeric capacity figure (story points, hours, item count per hour) is asserted, because none can be derived without fabrication.

This directly engages two tracked risks:

- **RISK-001 (Critical, Open)** — "EOD 2026-07-03 deadline cannot be met due to compressed delivery timeline." The mitigation on record ("enforce strict phase sequencing... prioritise Must Have items... invoke parallel delivery plan... raise scope trade-off decision with Human PO immediately if delays emerge") is the operating assumption for this sprint. This plan does not resolve RISK-001 — it remains Open and is the primary reason Section 2.2's at-risk items are flagged rather than silently absorbed.
- **RISK-009 (High, Open)** — "No historical velocity baseline — sprint capacity estimate may be incorrect, leading to over-commitment." The mitigation on record ("Treat Phase 01–11 (planning phases) as the primary capacity consumer for the day; implementation phases are the core delivery block; use T-shirt sizing to right-size commitment") is applied directly in Section 3 above — the Sprint Backlog uses T-shirt sizes only, no fabricated estimate.

**If the day's actual pace diverges materially from the 24-step order's assumptions**, the correct response is an impediment raised per `docs/delivery/scrum-operating-model.md` Section 10 (not a silent scope cut), escalated to the Human Product Owner if it would change MVP scope, per Section 10.4 and CLAUDE.md Section 21.

---

## 7. Sprint Ceremonies — Adaptation for This Sprint

This section confirms, rather than re-derives, the ceremony adaptations already approved in `docs/delivery/scrum-operating-model.md` Section 5 and the decisions in `docs/delivery/decision-log.md`:

- **Daily Scrum → Phase boundary check (DEC-002).** No separate standup artifact exists or is created by this plan. Per `scrum-operating-model.md` Section 5.2 and Section 12.1, each SDLC phase transition (Phase 09 → 10 → 11 → 12 → …) serves as the Daily Scrum equivalent: at each boundary, the SDLC Orchestrator confirms what was completed, what is starting, and any new impediment, recorded in `docs/handoffs/workflow-state.md` and `docs/handoffs/current-handoff.md`. This sprint plan does not introduce a competing artifact.
- **Sprint Review → Phase 22, Human PO participates (DEC-004).** Per `scrum-operating-model.md` Section 5.3 and DEC-004, the Human Product Owner joins synchronously only at Sprint Review (Phase 22), not at every automated phase. The Sprint Review will present the completed increment against this Sprint Backlog (Section 3), confirm Definition of Done status per item (Section 5), and record acceptance, rejection, or carry-forward decisions for any incomplete item. The SDLC Orchestrator will stop and await Human PO input at that gate — this plan does not shortcut it.
- **Retrospective → Phase 23.** Per `scrum-operating-model.md` Section 5.4, the retrospective at Phase 23 will produce `docs/delivery/retrospective.md`, capturing process learnings — including, notably, whether the 24-step order and the at-risk flags in Section 2.2 above proved accurate, for the benefit of any future run.
- **Backlog Refinement → already complete (Phase 07 + Phase 08).** Per `scrum-operating-model.md` Section 5.5, refinement occurred at Phase 07 (initial DoR pass) and was revisited at Phase 08 (BL-033 decomposition). This Sprint Plan does not re-open refinement; Section 4 above confirms the outcome.

No new ceremony, artifact, or adaptation is introduced by this plan beyond what `docs/delivery/scrum-operating-model.md` already establishes.

---

## 8. Readiness Gate for Phase 10 / Phase 11 — Spec-Driven Development Is Not Skipped

**Sprint Planning (Phase 09) does not authorize implementation.** Per CLAUDE.md Section 7 (Required SDLC Phases) and Section 8 (Spec-Driven Development Rule), and per `docs/delivery/task-board.md` (PH-10, PH-11), the following phases remain mandatory gates between this Sprint Plan and Phase 12 (Implementation):

- **Phase 10 — Feature Specifications** (owner: `solution-architect`). Feature-level specs must be produced for the backlog items in this Sprint Backlog before implementation begins, per CLAUDE.md Section 7 item 11 and Section 8.
- **Phase 11 — Spec Readiness Check** (owner: `scrum-master`). A dedicated Definition of Ready re-check against the Phase 10 feature specs (not a repeat of the Phase 07/09 backlog-level DoR confirmed in Section 4 above) must confirm readiness before Phase 12 starts, per `docs/delivery/task-board.md` PH-11 ("Definition of Ready gate").

This plan explicitly does **not** treat the Section 4 DoR confirmation (backlog-level, from Phase 07) as a substitute for the Phase 11 Spec Readiness Check (feature-spec-level, from Phase 10 output). They are distinct gates. No backlog item in this Sprint Backlog may enter Phase 12 (Implementation) until both Phase 10 and Phase 11 are complete for that item.

---

## 9. Human Product Owner Approval Gate

Per `docs/delivery/task-board.md` (PH-09: "Phase 09 — Sprint Planning ... Human PO approval gate") and CLAUDE.md Section 21 ("changing committed sprint scope" requires human approval before proceeding), this Sprint Plan — sprint goal (Section 1), full-scope commitment with at-risk flags (Section 2), and capacity framing (Section 6) — was presented to and **approved by the Human Product Owner on 2026-07-03**, with no descoping requested.

No scope, priority, or architecture decision in this document requires a *change* to anything already approved (all 37 items were already Must-Have-approved at Phase 03/07); this gate exists to confirm the Human Product Owner has no blocking question about the sprint commitment as assembled, consistent with Definition of Ready criterion 12.

---

## 10. Reference Documents

- `docs/delivery/scrum-operating-model.md` (v1.0, Phase 01)
- `docs/delivery/project-backlog.md` (v1.1, Phase 07/08)
- `docs/delivery/parallel-delivery-plan.md` (v1.0, Phase 08)
- `docs/delivery/decision-log.md` (DEC-001–DEC-014)
- `docs/delivery/risk-register.md` (RISK-001, RISK-009, RISK-010, RISK-014, RISK-015)
- `docs/delivery/task-board.md` (PH-09, PH-10, PH-11)
- `.claude/rules/definition-of-ready.md`
- `.claude/rules/definition-of-done.md`
- `.claude/rules/delegation-rules.md`
- `CLAUDE.md` Sections 7, 8, 21

---

## 11. Plan Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | scrum-master | Initial Sprint 1 Plan created for Phase 09 — sprint goal set; all 37 active backlog items committed to Sprint 1 (single-sprint delivery, no descoping); 5 items flagged at-risk (not removed); parallel-delivery-plan.md Section 6's 24-step order adopted verbatim as Sprint Backlog, grouped into 3 increments for progress visibility; DoR/DoD confirmed against existing Phase 07 analysis; capacity stated honestly with no fabricated velocity; ceremony adaptations confirmed against existing scrum-operating-model.md; Phase 10/11 gates reaffirmed as still required before implementation; Human PO approval gate (PH-09) flagged as outstanding. |

---

*End of Sprint 1 Plan v1.0.*
