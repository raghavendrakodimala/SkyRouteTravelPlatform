# Parallel Delivery Plan — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
Author: project-coordinator
Status: Active
Phase: Phase 08 — Parallel Delivery Plan

---

## 1. Purpose and Scope

This document builds the full parallel-track dependency graph for the 37 active backlog items in `docs/delivery/project-backlog.md` v1.1 (35 items from Phase 07, with BL-033 decomposed into BL-036/BL-037/BL-038 at this phase — see Section 6). It groups items into delivery tracks with minimal cross-track dependencies, identifies the critical path, states what can genuinely proceed without waiting on anything else, and gives a realistic execution order given the EOD 2026-07-03 delivery deadline.

No new scope, priority, or architecture decision is introduced here. Every track/sequencing call is derived directly from the "Dependencies" field already stated per item in `docs/delivery/project-backlog.md` Sections 4–6 and the component boundaries in `docs/architecture/architecture-plan.md` v1.0.

### 1.1 Delivery Model Assumption — No Fabricated Team Size

`docs/requirements.md` does not state a team size, and no prior SDLC phase (Scrum operating model, delivery model) committed to a specific number of contributors. This plan therefore does **not** assume N developers are available. "Parallel" in this document means: **which backlog items have no blocking dependency on each other and could be worked in any order, or picked up concurrently by additional contributors if and when they become available** — not a claim that concurrent building is actually happening. Section 5 gives the sequential-with-parallelizable-batches ordering that applies if, as the delivery model implies, there is effectively one primary implementer for Phase 12.

---

## 2. Carried-Forward Decisions (HO-007 Open Questions — Resolved by SDLC Orchestrator)

Both open questions raised in `docs/handoffs/07-project-coordinator-to-sdlc-orchestrator-backlog.md` (HO-007) were resolved by the SDLC Orchestrator before this phase began. They are applied directly below, not re-opened:

1. **BL-033 split (RISK-014) — Decision: Split.** `BookingFormComponent` (L-sized, the single largest backlog item) is decomposed into three smaller sub-tasks — BL-036 (S), BL-037 (M), BL-038 (M) — along the seams of read-only display, per-passenger form-array orchestration, and submit/loading/error/navigation wiring. Applied in `docs/delivery/project-backlog.md` v1.1 Section 5 and Section 6; `docs/delivery/task-board.md` and `docs/delivery/risk-register.md` (RISK-014) updated accordingly. This is a task-decomposition split only — all three sub-tasks still implement the single `booking-form.component.ts` file named in `docs/architecture/architecture-plan.md` Section 4.1; no new component/class was introduced and no Solution Architect approval was required.
2. **BL-003/BL-021 sequencing (RISK-015/DEP-025) — Decision: Parallel build accepted.** `docs/architecture/architecture-plan.md` Section 5 (API Contract Summary) is treated as frozen for Sprint 1. Backend contract models (BL-003, `SkyRoute.Application/Contracts/*`) and frontend shared TypeScript models (BL-021, `shared/models/*.model.ts`) can therefore be built independently against that frozen shape without either side waiting on the other. RISK-015 and DEP-025 are marked Mitigated/Resolved respectively in the registers, citing this decision.

---

## 3. Delivery Tracks

Five tracks, grouped to minimize cross-track blocking. Each track is internally sequenced into batches (Section 3.x sub-numbering) because items *within* a track are not all mutually independent — only cross-track independence is claimed.

### Track A — Backend Domain, Contracts & Providers (Search side)

Items: BL-001, BL-002, BL-003, BL-004, BL-005, BL-006, BL-007, BL-008, BL-009, BL-010

| Batch | Items | Rationale |
|---|---|---|
| A1 | BL-001 | First item, no dependency. |
| A2 | BL-002 | Needs BL-001 only. |
| A3 | BL-003, BL-004, BL-006 | All three depend only on BL-002; mutually independent — genuinely parallelizable within the track. |
| A4 | BL-005 | Needs BL-004. |
| A5 | BL-007 | Needs BL-002, BL-003. |
| A6 | BL-008 | Needs BL-007. Contains the BR-001/BR-002 pricing formulas — moderate complexity, worked examples exist. |
| A7 | BL-009 | Needs BL-007, BL-008. Contains the AD-010 fault-isolation detail (per-task try/catch before `Task.WhenAll`) — a subtle correctness risk flagged explicitly in the architecture plan; do not rush this item. |
| A8 | BL-010 | Needs BL-003, BL-004. Could actually run as early as batch A4 in parallel with BL-005/BL-007, since its only prerequisites are BL-003/BL-004. Listed last in the track narrative only because it is not needed until BL-017 (Track C). |

### Track B — Backend Booking & Persistence

Items: BL-011, BL-012, BL-013, BL-014, BL-015

| Batch | Items | Rationale |
|---|---|---|
| B1 | BL-011, BL-012, BL-013 | BL-011 needs BL-002; BL-012 and BL-013 need only BL-001. All three are mutually independent and can start as soon as Track A's batch A2 (BL-002) lands — genuinely parallelizable within the track, and with Track A batch A3 across tracks. BL-013 (`BookingReferenceGenerator`, BR-004 cryptographic RNG suffix + collision-aware format) is flagged for early attention in Section 5 — it is standalone, cheap, and carries real correctness risk (must use `RandomNumberGenerator`, not `System.Random`) that is better validated early than left for last. |
| B2 | BL-014 | Needs BL-003 (Track A), BL-005 (Track A), BL-006 (Track A). This is the one point where Track B has a hard cross-track dependency on Track A — Track B cannot fully finish BL-014 until Track A batches A3–A4 land. |
| B3 | BL-015 | Needs BL-005 (Track A), BL-011, BL-012, BL-013, BL-014 (all Track B). The 7-step booking orchestration (architecture plan Section 3.3) — the most logic-dense backend item after BL-009. |

### Track C — Backend API / Cross-Cutting

Items: BL-016, BL-017, BL-018, BL-019

| Batch | Items | Rationale |
|---|---|---|
| C1 | BL-016 | Needs BL-001 only — can start immediately, fully independent of Tracks A/B beyond scaffolding. |
| C2 | BL-017 | Needs BL-009, BL-010 (Track A). |
| C3 | BL-018 | Needs BL-014, BL-015 (Track B). |
| C4 | BL-019 | Needs BL-008, BL-009, BL-011, BL-012, BL-013, BL-016, BL-017, BL-018 — the backend "join point." Cannot start until both Track A and Track B are fully complete. This is the last backend item; it marks a complete, runnable backend. |

### Track D — Frontend Foundation & Services

Items: BL-020, BL-021, BL-022, BL-023, BL-024, BL-025, BL-026, BL-027, BL-031

| Batch | Items | Rationale |
|---|---|---|
| D1 | BL-020 | First frontend item, no dependency — **can start on day one at the same time as BL-001** (Track A A1); there is no cross-track dependency between them. |
| D2 | BL-021, BL-022, BL-025 | BL-021 depends only on the frozen contract shape (Section 2, Decision 2) — not on Track A's BL-003 actually being coded. BL-022/BL-025 need only BL-020. Mutually independent. |
| D3 | BL-023, BL-024, BL-026, BL-031 | BL-023/BL-026/BL-031 need BL-021; BL-024 needs BL-006 (Track A) in principle but the pattern values are already fixed in `docs/requirements.md` US-005 AC6/DP-015, so BL-024 can realistically start as soon as BL-020 lands without waiting on Track A at all (soft dependency only, per the original backlog note). |
| D4 | BL-027 | Needs BL-026. |

### Track E — Frontend Feature Components

Items: BL-028, BL-029, BL-030, BL-032, BL-034, BL-036, BL-037, BL-038, BL-035

| Batch | Items | Rationale |
|---|---|---|
| E1 | BL-032 | Needs BL-031 (Track D). |
| E2 | BL-028, BL-030 | BL-028 needs BL-022, BL-026, BL-027 (Track D); BL-030 needs BL-027 (Track D). Mutually independent of each other. |
| E3 | BL-034 | Needs BL-024 (Track D). |
| E4 | BL-029 | Needs BL-027 (Track D), BL-023 (Track D), BL-030 (this track). |
| E5 | BL-036, BL-037 | BL-036 needs BL-023 (Track D), BL-032 (this track). BL-037 needs BL-032 (this track), BL-034 (this track). Mutually independent — genuinely parallelizable with each other. |
| E6 | BL-038 | Needs BL-031 (Track D), BL-036, BL-037 (this track, both). Integration point of the three BL-033 sub-tasks. |
| E7 | BL-035 | Needs BL-032 (this track) only — could actually run as early as batch E1+1, listed last in the track narrative only because it completes the user-facing flow last. |

---

## 4. Critical Path

The critical path is the longest chain of hard (not soft) dependencies — it is the floor on delivery time regardless of how many contributors are available, and it is what matters most against the EOD deadline.

### 4.1 Backend critical path (8 items, tied at two branches)

Both of the following chains are 8 items long and both must complete before BL-019 (the backend join point):

- **Search-side chain:** BL-001 → BL-002 → BL-003 → BL-007 → BL-008 → BL-009 → BL-017 → BL-019
- **Booking-side chain:** BL-001 → BL-002 → BL-004 → BL-005 → BL-014 → BL-015 → BL-018 → BL-019

Because BL-019 needs outputs from *both* chains (it needs BL-017 **and** BL-018, among others), the backend critical path length is **8 items** (the length of either chain, since they run to the same joint node) — not the sum of both chains. Both chains share BL-001 → BL-002 as a common root, then diverge at Track A/Track B and rejoin at BL-019.

### 4.2 Frontend critical path (8 items)

- **Booking-flow chain:** BL-020 → BL-021 → BL-031 → BL-032 → BL-034 → BL-037 → BL-038 (7 items) → *(BL-035 and BL-036 are shorter side-branches that do not extend this chain, since BL-038 only needs BL-036 and BL-037, and the longer of the two — through BL-034/BL-037 — dominates)*

Counting BL-020 through BL-038 inclusive: **8 items** on the frontend critical path (020, 021, 031, 032, 034, 037, 038 — 7 nodes — the chain effectively ties the backend's 8-item length once BL-020 is counted as node 1).

### 4.3 What this means for the EOD deadline

Both the backend and frontend critical paths are approximately the same length (~8 sequential items each) and — critically — **neither chain depends on the other being finished**, only on the frozen contract shape (Section 2, Decision 2) and, later, on actual HTTP wiring for true end-to-end integration testing (Phase 13–14, per DEP-012/DEP-021 — those remain genuinely sequential: a live backend endpoint is needed before an integration test can call it, even though the Angular service code itself can be written and unit-tested earlier).

If more than one contributor were available, the backend and frontend critical-path chains could run concurrently, and total wall-clock time would be governed by whichever chain is longer (roughly 8 sequential steps), plus final integration. **Because this delivery model assumes effectively one implementer** (Section 1.1), the two chains cannot literally run at the same time — the practical implication is covered in Section 5: order the single implementer's work so that the highest-risk, most-blocking items are front-loaded, and so that non-blocking foundational work from both chains is interleaved rather than deferring all frontend work until the entire backend chain is done.

---

## 5. What Can Genuinely Run in Parallel (Given Additional Contributors, or Reordered for One)

The following pairs/groups have **zero dependency on each other** and are the concrete answer to "what could two contributors split, or one contributor freely reorder without penalty":

- **BL-001 (backend scaffolding) and BL-020 (frontend workspace scaffolding)** — no shared dependency at all; both are the respective track's first item.
- **BL-021 (frontend shared models) and the entire rest of Track A/B backend logic** — BL-021 depends only on the frozen `architecture-plan.md` Section 5 contract shape being treated as final, per Decision 2 (Section 2 above), not on BL-003 actually being coded. This is the concrete unblocking mechanism named in the task brief: because the contract is frozen, frontend model authoring can proceed the moment Phase 08 confirms the freeze, without waiting for backend Track A to reach BL-003.
- **BL-016 (`ApiExceptionMiddleware`) and BL-025 (no-op `AuthService`)** — both are named cross-cutting seams with a single dependency each (BL-001 and BL-020 respectively) and no downstream consumer until BL-019/composition-root time; either can be done whenever convenient, including as a "filler" item between higher-priority work.
- **BL-013 (`BookingReferenceGenerator`) and BL-012 (`ITenantContext`)** — both depend only on BL-001, are standalone (no constructor dependencies beyond trivial ones), and do not block or get blocked by anything else in their batch.
- **BL-036 and BL-037** (the two non-terminal BL-033 splits) — both depend on BL-032/BL-034 but not on each other; a second contributor picking up booking-feature work could take one while the primary implementer takes the other.
- **BL-028 (`SearchFormComponent`) and BL-030 (`SortControlComponent`)** — both depend only on BL-027, not on each other.

**What does *not* genuinely parallelize** (common mistake to flag): BL-017/BL-018 (controllers) cannot start before their respective service-layer chains (BL-009/BL-010 and BL-014/BL-015) are done — these are hard technical dependencies, not sequencing conveniences. Likewise BL-019 cannot start before *all* of BL-008/009/011/012/013/016/017/018 land — it is a true join point, not a batch that can be partially started.

---

## 6. Recommended Execution Order (Solo-Implementer, Sequential-with-Parallelizable-Batches)

Given the EOD deadline and the single-primary-implementer assumption (Section 1.1), this is a concrete, dependency-respecting order that (a) front-loads the highest-risk logic rather than leaving it for last, and (b) interleaves cheap, non-blocking frontend foundation work into gaps in the backend chain so nothing sits idle. Batches within a step have no dependency on each other and can be done in either order.

| Step | Items | Why here |
|---|---|---|
| 1 | BL-001, BL-020 | Both scaffolding items, zero mutual dependency, unlock everything else. Do both before anything else. |
| 2 | BL-002 | Unlocks the majority of Track A/B batch A3/B1. Highest-leverage single item after scaffolding. |
| 3 | BL-021 | Frontend shared models — safe to do now given the frozen contract (Decision 2); unlocks almost all of Track D. |
| 4 | BL-003, BL-004, BL-006 | Track A batch A3 — all depend only on BL-002, mutually independent. |
| 5 | **BL-013** | **Front-loaded deliberately.** `BookingReferenceGenerator` is standalone (needs only BL-001), cheap (S), and carries the one piece of backend logic most likely to be under-tested or gotten subtly wrong if rushed at the end of the day (BR-004's `RandomNumberGenerator`-not-`System.Random` requirement, the `SKY-[INT/DOM]-[XXXXXX]` format, and collision-regeneration behavior). Doing it now — while it is fresh and there is no time pressure yet — directly addresses the task brief's warning not to leave this for last. |
| 6 | BL-012, BL-011 | Track B batch B1 remainder — cheap, standalone-ish, needs only BL-001/BL-002. |
| 7 | BL-022, BL-023, BL-024, BL-025, BL-026, BL-031 | Track D batches D2–D3 — all cheap (XS/S), unlock the rest of Track D/E and cost little time relative to the backend logic-heavy items still ahead. Clearing these now means the remaining frontend work later is pure component-building, not service/util scaffolding. |
| 8 | BL-005 | Needs BL-004 (step 4). Unlocks BL-014. |
| 9 | BL-007 | Needs BL-002, BL-003. |
| 10 | BL-008 | Needs BL-007. Pricing formulas (BR-001/BR-002) — has worked examples in the requirements/architecture docs, moderate risk, but well-specified; do before BL-009 which depends on it. |
| 11 | BL-009 | Needs BL-007, BL-008. The AD-010 fault-isolation detail is the second-highest correctness risk in the backend (after BL-013) — do this with full attention, not rushed. |
| 12 | BL-010 | Needs BL-003, BL-004 (already done). Cheap, unlocks BL-017. |
| 13 | BL-014 | Needs BL-003, BL-005, BL-006 (all done). Unlocks BL-015. |
| 14 | BL-015 | Needs BL-005, BL-011, BL-012, BL-013, BL-014 (all done). The most logic-dense single backend item (7-step orchestration) — do it once all its inputs exist and BL-013's RNG logic (step 5) is already proven correct. |
| 15 | BL-016 | Needs BL-001 only — could have gone anywhere; placed here as a short, low-risk item to do right before the controllers. |
| 16 | BL-017, BL-018 | BL-017 needs BL-009, BL-010; BL-018 needs BL-014, BL-015. Both now unlocked; thin controllers, low risk. |
| 17 | BL-019 | Needs everything above from Tracks A–C. **Backend is now feature-complete and runnable.** |
| 18 | BL-027, BL-032 | Track D/E state services — needs BL-026/BL-031 (step 7, already done). |
| 19 | BL-028, BL-030 | Both need only BL-027 (step 18); mutually independent, do either order. |
| 20 | BL-034 | Needs BL-024 (step 7, already done). |
| 21 | BL-029 | Needs BL-027, BL-023 (done), BL-030 (step 19). |
| 22 | BL-036, BL-037 | Both need BL-032 (step 18); BL-036 also needs BL-023 (done), BL-037 also needs BL-034 (step 20). Mutually independent — do either order, or split across a second contributor if one becomes available. |
| 23 | BL-038 | Needs BL-031 (done), BL-036, BL-037 (step 22). Completes the booking flow end-to-end. |
| 24 | BL-035 | Needs only BL-032 (step 18) — could have gone as early as step 19, placed last here since it is the final screen in the user journey and there is no benefit to building it earlier than the flow that feeds it. |

**End state after step 24:** both the backend (step 17) and frontend (step 24) are feature-complete per the approved backlog, in an order that never leaves BL-013's cryptographic reference-generation logic, BL-009's fault-isolation logic, or BL-015's booking orchestration logic to a rushed end-of-day pass — the three items most likely to hide a subtle correctness defect are all done in the first half of the backend sequence (steps 5, 11, 14), not the last.

Phase 12 (Implementation) is where this order is actually executed; Phase 09 (Sprint Planning) is where the human Product Owner/Scrum Master confirm this order — or a re-prioritized variant — against the day's actual remaining time budget.

---

## 7. Backend/Frontend Integration Point (Restated from Backlog Section 8)

Per `docs/delivery/project-backlog.md` Section 8 and DEP-021: frontend services (BL-026, BL-031) can be built and unit-tested against contract models (BL-021) before the backend controllers (BL-017, BL-018) are live. **True end-to-end integration testing** (Phase 13–14) still requires both a running backend and a built frontend, consistent with DEP-012 — that dependency is not removed by this plan, only the independent-build phase before it.

---

## 8. Risk and Dependency Register Updates Applied by This Phase

- `docs/delivery/risk-register.md` — RISK-014 and RISK-015 marked **Mitigated**, each citing the decision applied in Section 2 above.
- `docs/delivery/dependency-register.md` — DEP-025 marked **Resolved**, citing the accepted-parallel-build decision.
- `docs/delivery/project-backlog.md` — bumped to v1.1: BL-033 marked Decomposed/Superseded; BL-036, BL-037, BL-038 added; summary table, coverage map, and sequencing notes (Section 8) updated accordingly.
- `docs/delivery/task-board.md` — Section 4.2 updated: BL-033 row removed, BL-036/BL-037/BL-038 rows added; Board Update Log entry added.

No new risk or dependency was identified during this phase beyond what Phase 07 already surfaced (RISK-014/RISK-015/DEP-025) and this phase resolved.

---

## 9. Reference Documents

- `docs/delivery/project-backlog.md` (v1.1)
- `docs/architecture/architecture-plan.md` (v1.0)
- `docs/delivery/risk-register.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/task-board.md`
- `docs/handoffs/07-project-coordinator-to-sdlc-orchestrator-backlog.md` (HO-007)
- `.claude/rules/delegation-rules.md`

---

## 10. Plan Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial parallel delivery plan created for Phase 08 — 5 delivery tracks defined across 37 active backlog items; critical path identified (~8 items each side); BL-033 split into BL-036/BL-037/BL-038 applied to project-backlog.md; RISK-014/RISK-015/DEP-025 closed out per SDLC Orchestrator decisions on HO-007. |

---

*End of Parallel Delivery Plan v1.0.*
