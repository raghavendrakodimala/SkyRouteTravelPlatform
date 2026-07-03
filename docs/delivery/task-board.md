# Task Board — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
Author: Project Coordinator
Status: Active

---

## 1. Purpose

This task board provides a visual overview of the delivery state of all sprint work items. It is updated by the Project Coordinator at each phase boundary and refreshed after significant state changes.

User stories and implementation tasks will be added at Phase 07 — Project Backlog Creation, once the product backlog is established.

---

## 2. Board Column Definitions

| Column | Meaning |
|---|---|
| Backlog | Work identified but not yet started |
| In Progress | Work actively being executed in the current phase |
| In Review | Work complete; awaiting validation, review, or human approval |
| Done | Work complete per Definition of Done; committed and merged |

---

## 3. SDLC Phase Board

### Done

| ID | Task | Owner | Evidence |
|---|---|---|---|
| PH-01 | Phase 01 — Scrum Operating Model | scrum-master | `docs/delivery/scrum-operating-model.md`, HO-001 |
| PH-02 | Phase 02 — SDLC Delivery Operating Model | project-coordinator | Delivery model docs, HO-002 |

### In Review

| ID | Task | Owner | Reviewer | Findings |
|---|---|---|---|---|
| — | None currently | — | — | — |

### In Progress

| ID | Task | Owner | Started | Blockers |
|---|---|---|---|---|
| — | None currently | — | — | — |

### Backlog

| ID | Task | Owner | Priority | Source | Notes |
|---|---|---|---|---|---|
| PH-03 | Phase 03 — Requirements Analysis | solution-architect + product-owner | Critical | DEP-002 | Human PO approval gate at end |
| PH-04 | Phase 04 — NFR Specification | solution-architect | Critical | DEP-003 | Depends on Phase 03 approval |
| PH-05 | Phase 05 — Test Strategy | functional-tester | High | DEP-002 | May run after Phase 03 |
| PH-06 | Phase 06 — Architecture Planning | solution-architect | Critical | DEP-004, DEP-005 | Depends on Phases 03 and 04 |
| PH-07 | Phase 07 — Project Backlog | project-coordinator + product-owner | Critical | DEP-024 | Human PO approval gate at end |
| PH-08 | Phase 08 — Parallel Delivery Plan | project-coordinator | High | Phase 07 | Follows Phase 07 |
| PH-09 | Phase 09 — Sprint Planning | scrum-master | Critical | Phase 07, 08 | Human PO approval gate |
| PH-10 | Phase 10 — Feature Specifications | solution-architect | Critical | DEP-007, DEP-006 | Depends on Phases 06, 05 |
| PH-11 | Phase 11 — Spec Readiness Check | scrum-master | Critical | Phase 10 | Definition of Ready gate |
| PH-12 | Phase 12 — Implementation | lead-full-stack-engineer | Critical | DEP-010, DEP-011 | Stories added at Phase 07 |
| PH-13 | Phase 13 — Test Writing | functional-tester | Critical | Phase 12 | — |
| PH-14 | Phase 14 — Test Execution Summary | functional-tester | Critical | Phase 13 | IMP-001 — human approval for test commands |
| PH-15 | Phase 15 — Code Review | code-reviewer | High | Phase 12 | CR-series findings |
| PH-16 | Phase 16 — Security Review | security-reviewer | High | Phase 12 | SEC-series findings |
| PH-17 | Phase 17 — Accessibility Review | accessibility-tester | High | Phase 12 | A11Y-series findings |
| PH-18 | Phase 18 — Performance Review | performance-tester | Medium | Phase 12 | PERF-series findings |
| PH-19 | Phase 19 — Findings Fixes | lead-full-stack-engineer | Critical | DEP-017 | Fix by finding ID |
| PH-20 | Phase 20 — Re-test and Re-review | functional-tester | Critical | Phase 19 | — |
| PH-21 | Phase 21 — Delivery Tracking Update | project-coordinator | High | Phase 20 | — |
| PH-22 | Phase 22 — Sprint Review | scrum-master | Critical | Phase 21 | Human PO participation required |
| PH-23 | Phase 23 — Retrospective | scrum-master | Medium | Phase 22 | — |
| PH-24 | Phase 24 — Final SDLC Summary + Merge | project-coordinator | Critical | Phase 23 | Human PO merge approval required |

---

## 4. Product Story Board

Populated at Phase 07 — Project Backlog Creation; updated at Phase 08 for the BL-033 decomposition. Full item detail (description, architecture-component mapping, sizing rationale, dependencies, DoR check) lives in `docs/delivery/project-backlog.md` v1.1. This board tracks only current Kanban state — all 37 active items start in **To Do**.

### 4.1 Backend Items (To Do)

| ID | Task | User Story | Size | Priority | Blocked By |
|---|---|---|---|---|---|
| BL-001 | Solution and Project Scaffolding | US-007 (enabling) | XS | Must | — |
| BL-002 | Domain Models | US-002, 005, 006, 007, 008 | S | Must | BL-001 |
| BL-003 | API Contract Models | US-001, 006 | S | Must | BL-002 |
| BL-004 | Airport Static Data (`AirportDataService`) | US-008 | XS | Must | BL-002 |
| BL-005 | `RouteTypeResolver` | US-005 | XS | Must | BL-004 |
| BL-006 | Document Validation Patterns | US-005 | XS | Must | BL-002 |
| BL-007 | `IFlightProvider` Interface | US-007 | XS | Must | BL-002, BL-003 |
| BL-008 | `GlobalAirProvider`/`BudgetWingsProvider` | US-002, 007 | M | Must | BL-007 |
| BL-009 | `IFlightAggregatorService` | US-007, 002 | M | Must | BL-007, BL-008 |
| BL-010 | `SearchRequestValidator` | US-001 | S | Must | BL-003, BL-004 |
| BL-011 | `IBookingStore`/`InMemoryBookingStore` | US-006 | S | Must | BL-002 |
| BL-012 | `ITenantContext`/`DefaultTenantContext` | US-006 (seam) | XS | Must | BL-001 |
| BL-013 | `BookingReferenceGenerator` | US-006 | S | Must | BL-001 |
| BL-014 | `BookingRequestValidator` | US-005, 006 | S | Must | BL-003, BL-005, BL-006 |
| BL-015 | `IBookingService`/`BookingService` | US-006 | M | Must | BL-005, BL-011, BL-012, BL-013, BL-014 |
| BL-016 | `ApiExceptionMiddleware` | All (cross-cutting) | XS | Must | BL-001 |
| BL-017 | `SearchController` | US-001 | XS | Must | BL-009, BL-010 |
| BL-018 | `BookingController` | US-006 | XS | Must | BL-015, BL-014 |
| BL-019 | DI Composition Root/CORS/Config | US-007 (+all) | S | Must | BL-008, 009, 011, 012, 013, 016, 017, 018 |

### 4.2 Frontend Items (To Do)

| ID | Task | User Story | Size | Priority | Blocked By |
|---|---|---|---|---|---|
| BL-020 | Angular Workspace/Routing Shell | US-001, 004 (+all) | S | Must/Should | — |
| BL-021 | Shared Models (TS) | US-001, 002, 005, 006 | XS | Must | BL-003 |
| BL-022 | `airports.constants.ts` | US-008 | XS | Must | BL-020 |
| BL-023 | `pricing.util.ts` | US-002, 004 | XS | Must | BL-021 |
| BL-024 | `document-number.validators.ts` | US-005 | XS | Must | BL-006 |
| BL-025 | `AuthService` (no-op) | (seam) | XS | Must | BL-020 |
| BL-026 | `FlightSearchService` (Angular) | US-001 | XS | Must | BL-021 |
| BL-027 | `SearchStateService` | US-001, 002, 003 | S | Must | BL-026 |
| BL-028 | `SearchFormComponent` | US-001, 008 | M | Must | BL-022, BL-026, BL-027 |
| BL-029 | `ResultsListComponent` | US-002, 004 | M | Must | BL-027, BL-023, BL-030 |
| BL-030 | `SortControlComponent` | US-003 | S | Must | BL-027 |
| BL-031 | `BookingService` (Angular) | US-006 | XS | Must | BL-021 |
| BL-032 | `BookingStateService` | US-004, 005, 006 | S | Must | BL-031 |
| BL-034 | `PassengerFormSectionComponent` | US-005 | M | Must | BL-024 |
| BL-035 | `ConfirmationComponent` | US-006 | S | Must | BL-032 |
| BL-036 | `BookingFormComponent`: Summary & Price Display *(split of BL-033)* | US-004 | S | Must | BL-023, BL-032 |
| BL-037 | `BookingFormComponent`: Passenger Form Array Orchestration *(split of BL-033)* | US-004, 005 | M | Must | BL-032, BL-034 |
| BL-038 | `BookingFormComponent`: Submit/Loading/Error/Re-submission *(split of BL-033)* | US-006 | M | Must | BL-031, BL-036, BL-037 |

**BL-033 removed from this board at Phase 08** — decomposed into BL-036/BL-037/BL-038 above per SDLC Orchestrator decision on HO-007 (RISK-014). No scope change; see `docs/delivery/project-backlog.md` v1.1 and `docs/delivery/parallel-delivery-plan.md` v1.0.

Source: `docs/delivery/project-backlog.md` v1.1 (Phase 07, updated Phase 08). Parallel-track assignment across agents/roles is recorded in `docs/delivery/parallel-delivery-plan.md` v1.0 (Phase 08); sprint commitment is Phase 09 scope.

---

## 5. Board Update Log

| Date | Updated By | Change |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial board created for Phase 02. Phase 01 and Phase 02 moved to Done. All remaining phases added to Backlog. |
| 2026-07-03 | project-coordinator | Phase 07 — seeded Section 4 Product Story Board with 35 backlog items (BL-001–BL-035) from `docs/delivery/project-backlog.md` v1.0, all in To Do state, split into Backend Items (4.1, 19 items) and Frontend Items (4.2, 16 items). |
| 2026-07-03 | project-coordinator | Phase 08 — removed BL-033 (`BookingFormComponent`, L) from Section 4.2 and replaced with its three decomposition sub-tasks BL-036 (S), BL-037 (M), BL-038 (M), per SDLC Orchestrator decision on HO-007 (RISK-014). Frontend item count: 16 → 18; total active items: 35 → 37. Parallel-track assignment recorded in new `docs/delivery/parallel-delivery-plan.md` v1.0. |

---

## 6. Reference Documents

- `docs/delivery/project-backlog.md` (created at Phase 07)
- `docs/delivery/sdlc-operating-model.md`
- `docs/delivery/dependency-register.md`
- `docs/handoffs/workflow-state.md`
