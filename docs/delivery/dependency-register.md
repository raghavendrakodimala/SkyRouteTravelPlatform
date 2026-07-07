# Dependency Register — SkyRoute Travel Platform MVP

Version: 1.1
Date: 2026-07-07 (Phase 21 reconciliation; baseline 2026-07-03)
Author: Project Coordinator
Status: Active

---

## 1. Purpose

This register tracks all delivery dependencies for the SkyRoute MVP. Dependencies are relationships where one item or phase must be complete before another can begin or proceed.

Dependencies are reviewed at each phase boundary and updated when new dependencies are identified.

---

## 2. Dependency Status Values

| Status | Meaning |
|---|---|
| Open | Dependency is active and unresolved |
| In Progress | Dependent work has started but the prerequisite is not yet complete |
| Resolved | Prerequisite is complete; dependency is satisfied |
| Blocked | Prerequisite is blocked — downstream work cannot proceed |
| Deferred | Dependency acknowledged; downstream work deferred by decision |
| Not Applicable | Dependency does not apply to this sprint/scope |

---

## 3. Dependency Types

| Type | Meaning |
|---|---|
| Phase | One SDLC phase must complete before another can begin |
| Spec | A specification artefact must exist before implementation |
| Technical | A technical component must exist before a dependent component can be built |
| External | An external system, library, or approval is required |
| Human | A human decision or approval is required |
| Review | A review must complete before the next action |

---

## 4. Dependency Register

| ID | Dependency Description | From (Prerequisite) | To (Dependent) | Type | Status | Risk if Blocked | Notes |
|---|---|---|---|---|---|---|---|
| DEP-001 | Scrum Operating Model must be complete before SDLC Delivery Model work begins | Phase 01 — Scrum Operating Model | Phase 02 — SDLC Delivery Model | Phase | Resolved | Low | Phase 01 complete per HO-001 |
| DEP-002 | SDLC Delivery Model must be complete before Requirements Analysis begins | Phase 02 — SDLC Delivery Model | Phase 03 — Requirements Analysis | Phase | Resolved | Low | Standard phase sequencing |
| DEP-003 | Requirements Analysis must be complete and approved before NFR Specification begins | Phase 03 — Requirements Analysis | Phase 04 — NFR Specification | Phase + Human | Resolved | High | Human PO approval required before NFRs can be defined against requirements |
| DEP-004 | Requirements Analysis must be complete before Architecture Planning begins | Phase 03 — Requirements Analysis | Phase 06 — Architecture Planning | Phase | Resolved | High | Architecture depends on understood requirements |
| DEP-005 | NFR Specification must be complete before Architecture Planning completes | Phase 04 — NFR Specification | Phase 06 — Architecture Planning | Spec | Resolved | High | Architecture decisions must be validated against NFR targets |
| DEP-006 | Test Strategy must be complete before Feature Specifications are finalised | Phase 05 — Test Strategy | Phase 10 — Feature Specifications | Spec | Resolved | Medium | Feature specs should reference test approach |
| DEP-007 | Architecture Planning must be complete before Feature Specifications are written | Phase 06 — Architecture Planning | Phase 10 — Feature Specifications | Phase | Resolved | High | Feature specs depend on API contracts and component boundaries from architecture |
| DEP-008 | API contracts must be defined before backend implementation begins | Phase 06 / Phase 10 — API contracts | Phase 12 — Implementation (backend) | Spec | Resolved | Critical | Backend must implement against approved contracts; no improvised API shape |
| DEP-009 | UI flow specifications must be defined before frontend implementation begins | Phase 06 / Phase 10 — UI flows | Phase 12 — Implementation (frontend) | Spec | Resolved | Critical | Frontend must implement against approved UI flows |
| DEP-010 | Feature Specifications must be complete and readiness-checked before implementation | Phase 10 + Phase 11 — Feature Specs + Readiness | Phase 12 — Implementation | Phase | Resolved | Critical | Spec-driven development rule — no implementation without approved specs |
| DEP-011 | Sprint Planning must be approved before implementation begins | Phase 09 — Sprint Planning | Phase 12 — Implementation | Human | Resolved | Critical | Human PO sprint approval required |
| DEP-012 | Backend API endpoints must be implemented before frontend integration tests can run | Phase 12 — Backend implementation | Phase 13–14 — Integration test writing and execution | Technical | Resolved | High | Frontend integration depends on working backend |
| DEP-013 | Implementation must be complete before code review begins | Phase 12 — Implementation | Phase 15 — Code Review | Phase | Resolved | Low | Standard review dependency |
| DEP-014 | Implementation must be complete before security review begins | Phase 12 — Implementation | Phase 16 — Security Review | Phase | Resolved | Low | Standard review dependency |
| DEP-015 | Implementation must be complete before accessibility review begins | Phase 12 — Implementation | Phase 17 — Accessibility Review | Phase | Resolved | Low | Standard review dependency |
| DEP-016 | Implementation must be complete before performance review begins | Phase 12 — Implementation | Phase 18 — Performance Review | Phase | Resolved | Low | Standard review dependency |
| DEP-017 | All review reports (Phases 15–18) must be complete before fixes begin | Phases 15–18 — Reviews | Phase 19 — Findings Fixes | Review | Resolved | Medium | Developers need full findings list before fixing; avoids partial fix cycles |
| DEP-018 | Findings fixes (Phase 19) must be complete before re-test and re-review | Phase 19 — Fixes | Phase 20 — Re-test and Re-review | Phase | Resolved | Low | Standard re-test dependency |
| DEP-019 | Test execution summary must exist before Sprint Review | Phase 14 / Phase 20 — Test Execution | Phase 22 — Sprint Review | Phase | Resolved | High | Sprint Review cannot confirm DoD without test evidence |
| DEP-020 | All Critical and High findings must be resolved or accepted before merge | Phases 19–20 — Fix and Re-review | Phase 24 — Merge | Human | Resolved | Critical | Human PO acceptance required for any unresolved Critical/High findings |
| DEP-021 | ASP.NET Core 8 backend data layer must be implemented before Angular service integration | Backend — FlightRepository / in-memory store | Frontend — Angular flight search service | Technical | Resolved | High | Angular services consume backend HTTP endpoints; in-memory store feeds those endpoints |
| DEP-022 | Angular 17 component tree must be designed before individual components are implemented | UX/UI flow specification | Phase 12 — Frontend component implementation | Spec | Resolved | Medium | Prevents rework from component boundary changes during implementation |
| DEP-023 | npm packages and dotnet SDK must be available in the environment for test execution | External — runtime environment | Phase 14 — Test Execution | External | Resolved | High | Test execution requires user approval to run npm install / dotnet restore; tracked as IMP-001 |
| DEP-024 | Human Product Owner must approve requirements before architecture and backlog proceed | Phase 03 approval gate | Phase 06, Phase 07 | Human | Resolved | Critical | Architecture and backlog are built on approved requirements only |
| DEP-025 | Backend API contract models (BL-001, project-backlog.md) and frontend shared TypeScript models (BL-021, project-backlog.md) both implement the same contract shape defined in `docs/architecture/architecture-plan.md` Section 5, independently and potentially in parallel | BL-003 — `SkyRoute.Application/Contracts/*` | BL-021 — `shared/models/*.model.ts` | Technical | Resolved | Low | Architecture-plan.md Section 5 is treated as frozen for Sprint 1; if the contract shape changes during Phase 12 implementation, both sides must be updated together — raise as a decision-log entry before either side proceeds. See also RISK-015. **Resolved at Phase 08:** SDLC Orchestrator confirmed the frozen contract accepts genuinely parallel, unsequenced build of BL-003 and BL-021 — no forced ordering is required; see `docs/delivery/parallel-delivery-plan.md` v1.0 |

---

## 5. Dependency Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial register created for Phase 02 |
| 2026-07-03 | project-coordinator | Phase 07 — added DEP-025 (backend/frontend contract-model parallel-build dependency), surfaced during project-backlog.md decomposition |
| 2026-07-03 | project-coordinator | Phase 08 — DEP-025 marked Resolved per SDLC Orchestrator decision on HO-007 (parallel build of BL-003/BL-021 accepted against the frozen Section 5 contract). See `docs/delivery/parallel-delivery-plan.md` v1.0 |
| 2026-07-07 | project-coordinator | Phase 21 — register had not been updated since Phase 08 despite Phases 03–20 completing (stale-status gap). DEP-002–DEP-024 all marked Resolved: every prerequisite phase/spec/approval is complete per `docs/handoffs/workflow-state.md` phase table (01–20 Complete) and HO-003–HO-038. Notably: DEP-011/DEP-024 resolved by explicit Human PO approvals (requirements v1.4, sprint plan); DEP-020 resolved by *fixing* all Critical/High findings (zero Open across Phases 15–18, HO-038) — no unresolved Critical/High acceptance was needed; DEP-023 resolved via per-command approvals (IMP-001/IMP-002) and now standing validation pre-approval (DEC-018) |

---

## 6. Reference Documents

- `docs/delivery/risk-register.md`
- `docs/delivery/impediment-log.md`
- `docs/handoffs/workflow-state.md`
- `docs/delivery/sdlc-operating-model.md`
