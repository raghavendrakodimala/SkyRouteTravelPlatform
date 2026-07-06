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
| PH-13 | Phase 13 — Test Writing | lead-full-stack-engineer + functional-tester | Critical | Phase 12 | **Done.** Backend complete (114/114 tests passing). Frontend unit/component suite written (145 tests, 16 files) and now **executed**: IMP-002 resolved per Human PO approval to install `vitest`/`jsdom`; final result **145/145 passing, 0 failed, 0 skipped** across all 16 files. See HO-013 (authoring) and HO-013E (execution + 5 spec-file fixes; `docs/testing/execution/frontend-unit-test-execution-summary.md`). E2E suite (Playwright, `frontend/e2e/`, 11 tests/6 files) written and executed against real backend+frontend per Human PO approval; first run 8 passed/3 failed due to new Critical finding QA-003 (booking form could not be submitted in a real browser — see HO-013C). **QA-003 fixed same-day per Human PO approval for an out-of-sequence fix** (`booking-form.component.html`/`.ts` — added missing `[formGroup]` binding); re-run confirmed **11/11 E2E tests passing**. See HO-013D. QA-001, QA-002, QA-004, QA-005 (Medium/Low) remain Open, deferred to Phase 19. |
| PH-14 | Phase 14 — Test Execution Summary | functional-tester | Critical | Phase 13 | **Done.** Formal, consolidated QA Test Execution Summary produced at `docs/testing/execution/phase-14-test-execution-summary.md`. All three automated test layers independently re-run and re-verified green in this phase (not just consumed from Phase 13 reports): backend xUnit **114/114** (`dotnet build`/`dotnet test SkyRoute.slnx`), frontend unit/component Vitest **145/145** (16/16 files, `npm test`), E2E Playwright **11/11** (6/6 files, real backend+frontend servers started/stopped cleanly, verified via `netstat`). No discrepancy vs. Phase 13's reports; no new QA finding raised. QA-003 confirmed Resolved with no regression. QA-001 (Medium), QA-002/QA-004/QA-005 (Low) remain Open, deferred to Phase 19 — none block Phase 15. Final QA recommendation: proceed to Phase 15 (Code Review). See HO-014. |
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

### 4.1 Backend Items (Done)

All 19 backend items (BL-001–BL-019) were implemented in Phase 12 (backend half) on branch `sdlc/12-implementation-skyroute-mvp`. Evidence: `docs/handoffs/12a-lead-full-stack-engineer-to-sdlc-orchestrator-backend-implementation.md` (HO-012A), `dotnet build` succeeded with 0 warnings/0 errors. Test writing/execution (Phase 13/14), code/security review (Phase 15/16), and re-test/re-review remain outstanding per the SDLC phase sequence — "Done" here means Phase 12 implementation is complete per architecture-plan.md, not full Definition of Done (Section 10 of CLAUDE.md) for the sprint as a whole.

| ID | Task | User Story | Size | Priority | Status |
|---|---|---|---|---|---|
| BL-001 | Solution and Project Scaffolding | US-007 (enabling) | XS | Must | Done |
| BL-002 | Domain Models | US-002, 005, 006, 007, 008 | S | Must | Done |
| BL-003 | API Contract Models | US-001, 006 | S | Must | Done |
| BL-004 | Airport Static Data (`AirportDataService`) | US-008 | XS | Must | Done |
| BL-005 | `RouteTypeResolver` | US-005 | XS | Must | Done |
| BL-006 | Document Validation Patterns | US-005 | XS | Must | Done |
| BL-007 | `IFlightProvider` Interface | US-007 | XS | Must | Done |
| BL-008 | `GlobalAirProvider`/`BudgetWingsProvider` | US-002, 007 | M | Must | Done |
| BL-009 | `IFlightAggregatorService` | US-007, 002 | M | Must | Done |
| BL-010 | `SearchRequestValidator` | US-001 | S | Must | Done |
| BL-011 | `IBookingStore`/`InMemoryBookingStore` | US-006 | S | Must | Done |
| BL-012 | `ITenantContext`/`DefaultTenantContext` | US-006 (seam) | XS | Must | Done |
| BL-013 | `BookingReferenceGenerator` | US-006 | S | Must | Done |
| BL-014 | `BookingRequestValidator` | US-005, 006 | S | Must | Done |
| BL-015 | `IBookingService`/`BookingService` | US-006 | M | Must | Done |
| BL-016 | `ApiExceptionMiddleware` | All (cross-cutting) | XS | Must | Done |
| BL-017 | `SearchController` | US-001 | XS | Must | Done |
| BL-018 | `BookingController` | US-006 | XS | Must | Done |
| BL-019 | DI Composition Root/CORS/Config | US-007 (+all) | S | Must | Done |

### 4.2 Frontend Items (Done)

All 18 active frontend items (BL-020–BL-032, BL-034–BL-038) were implemented in Phase 12 (frontend half) on branch `sdlc/12-implementation-skyroute-mvp`. Evidence: `docs/handoffs/12b-lead-full-stack-engineer-to-sdlc-orchestrator-frontend-implementation.md` (HO-012B), `npm run build` succeeded with 0 errors/0 warnings. Test writing/execution (Phase 13/14), code/security/accessibility review (Phase 15/16/17), and re-test/re-review remain outstanding per the SDLC phase sequence — "Done" here means Phase 12 implementation is complete per architecture-plan.md, not full Definition of Done (Section 10 of CLAUDE.md) for the sprint as a whole.

| ID | Task | User Story | Size | Priority | Status |
|---|---|---|---|---|---|
| BL-020 | Angular Workspace/Routing Shell | US-001, 004 (+all) | S | Must/Should | Done |
| BL-021 | Shared Models (TS) | US-001, 002, 005, 006 | XS | Must | Done |
| BL-022 | `airports.constants.ts` | US-008 | XS | Must | Done |
| BL-023 | `pricing.util.ts` | US-002, 004 | XS | Must | Done |
| BL-024 | `document-number.validators.ts` | US-005 | XS | Must | Done |
| BL-025 | `AuthService` (no-op) | (seam) | XS | Must | Done |
| BL-026 | `FlightSearchService` (Angular) | US-001 | XS | Must | Done |
| BL-027 | `SearchStateService` | US-001, 002, 003 | S | Must | Done |
| BL-028 | `SearchFormComponent` | US-001, 008 | M | Must | Done |
| BL-029 | `ResultsListComponent` | US-002, 004 | M | Must | Done |
| BL-030 | `SortControlComponent` | US-003 | S | Must | Done |
| BL-031 | `BookingService` (Angular) | US-006 | XS | Must | Done |
| BL-032 | `BookingStateService` | US-004, 005, 006 | S | Must | Done |
| BL-034 | `PassengerFormSectionComponent` | US-005 | M | Must | Done |
| BL-035 | `ConfirmationComponent` | US-006 | S | Must | Done |
| BL-036 | `BookingFormComponent`: Summary & Price Display *(split of BL-033)* | US-004 | S | Must | Done |
| BL-037 | `BookingFormComponent`: Passenger Form Array Orchestration *(split of BL-033)* | US-004, 005 | M | Must | Done |
| BL-038 | `BookingFormComponent`: Submit/Loading/Error/Re-submission *(split of BL-033)* | US-006 | M | Must | Done |

**BL-033 removed from this board at Phase 08** — decomposed into BL-036/BL-037/BL-038 above per SDLC Orchestrator decision on HO-007 (RISK-014). No scope change; see `docs/delivery/project-backlog.md` v1.1 and `docs/delivery/parallel-delivery-plan.md` v1.0.

Source: `docs/delivery/project-backlog.md` v1.1 (Phase 07, updated Phase 08). Parallel-track assignment across agents/roles is recorded in `docs/delivery/parallel-delivery-plan.md` v1.0 (Phase 08); sprint commitment is Phase 09 scope.

---

## 5. Board Update Log

| Date | Updated By | Change |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial board created for Phase 02. Phase 01 and Phase 02 moved to Done. All remaining phases added to Backlog. |
| 2026-07-03 | project-coordinator | Phase 07 — seeded Section 4 Product Story Board with 35 backlog items (BL-001–BL-035) from `docs/delivery/project-backlog.md` v1.0, all in To Do state, split into Backend Items (4.1, 19 items) and Frontend Items (4.2, 16 items). |
| 2026-07-03 | project-coordinator | Phase 08 — removed BL-033 (`BookingFormComponent`, L) from Section 4.2 and replaced with its three decomposition sub-tasks BL-036 (S), BL-037 (M), BL-038 (M), per SDLC Orchestrator decision on HO-007 (RISK-014). Frontend item count: 16 → 18; total active items: 35 → 37. Parallel-track assignment recorded in new `docs/delivery/parallel-delivery-plan.md` v1.0. |
| 2026-07-03 | lead-full-stack-engineer | Phase 12 (backend half) — moved all 19 backend items (BL-001–BL-019) from Section 4.1 To Do to Done. Solution/project scaffolding, domain models, contracts, interfaces, providers, aggregation service, booking service, validators, in-memory store, tenancy seam, exception middleware, controllers, and DI composition root all implemented per `docs/architecture/architecture-plan.md` v1.0. `dotnet build` on `SkyRoute.slnx` succeeded: 0 Warning(s), 0 Error(s). Frontend items (BL-020–BL-038) remain in To Do pending the frontend half of Phase 12. See HO-012A. |
| 2026-07-03 | lead-full-stack-engineer | Phase 12 (frontend half) — moved all 18 active frontend items (BL-020–BL-032, BL-034–BL-038) from Section 4.2 To Do to Done. Angular 22 standalone-component workspace scaffolded at `frontend/`; shared models/constants/utils/validators, search/results/booking/confirmation feature folders, Signal-based state services, and the 4-route shell with guards all implemented per `docs/architecture/architecture-plan.md` v1.0 Section 4 and the Phase 10 feature specs. `npm run build` succeeded: 0 errors, 0 warnings. Phase 12 is now complete in full (backend + frontend). See HO-012B. |
| 2026-07-06 | lead-full-stack-engineer | Phase 13 (Test Writing) — added the backend xUnit suite (`tests/SkyRoute.Application.Tests/`, `tests/SkyRoute.Api.IntegrationTests/`; 114 tests covering providers, aggregation/fault-isolation, validators, booking orchestration, in-memory persistence, exception middleware, and both controllers via `WebApplicationFactory`). `dotnet build`/`dotnet test` both green: 114/114 passing, independently re-verified. Added the frontend Vitest/Angular-TestBed suite (16 spec files, 145 test cases covering utils/validators/state services/HTTP services/guards/6 key components) — written and reviewed but **not executed**: the Angular workspace's test-runtime packages (`vitest`, `jsdom`) were never installed (Phase 12 used `ng new --skip-tests`), and installing them requires human approval (tracked as **IMP-002**). Two QA findings recorded, not fixed: QA-001 (`BookingRequestValidator` null-handling inconsistency, Medium), QA-002 (`ApiExceptionMiddleware` Content-Type mismatch, Low) — both deferred to Phase 19. See HO-013. |
| 2026-07-06 | functional-tester | Phase 13 extension — automated E2E via Playwright, per explicit Human PO approval superseding the manual-only E2E posture (`docs/testing/test-strategy.md` v1.0 §1.4, `QA-STRAT-OQ-002`). Installed `@playwright/test@1.61.1` + Chromium in `frontend/`; authored `frontend/playwright.config.ts` and 6 spec files (11 tests) at `frontend/e2e/` covering all 8 user stories' "Manual E2E"-tagged acceptance criteria. Ran the suite against the real backend (`dotnet run`, :5094) and real frontend (`ng serve`, :4200): **8 passed, 3 failed**. The 3 failures are not test defects — they surface a new **Critical** finding, **QA-003**: `BookingFormComponent`'s `<form>` has no `[formGroup]`, so `(ngSubmit)` never intercepts the native `submit` event, and clicking "Confirm Booking" triggers a full page reload instead of an API call — no booking can currently be completed through the real app. Two Low-severity findings also recorded: QA-004 (dead-code same-airport error paragraph), QA-005 (`passengerCount` sent as a JSON string, not a number). `docs/testing/test-strategy.md` bumped to v1.1. Both dev servers stopped after the run. See HO-013C and `docs/testing/execution/e2e-playwright-test-execution-summary.md`. |
| 2026-07-06 | lead-full-stack-engineer | Phase 13 — QA-003 fixed out-of-sequence per explicit Human PO approval (Critical finding, blocked the entire booking flow). Root cause: `booking-form.component.html`'s `<form>` had no `[formGroup]` directive, so `(ngSubmit)` never intercepted the native submit event. Fix: added `[formGroup]="bookingForm"` binding and a wrapping `FormGroup` in `booking-form.component.ts` (the existing form model was a bare `FormArray`, which `FormGroupDirective` cannot bind to directly). `npm run build`/`dotnet build` both clean (0/0). Re-ran the full Playwright suite against the real app: **11/11 E2E tests now passing** (up from 8/11). QA-001, QA-002, QA-004, QA-005 untouched, remain Open, deferred to Phase 19. Phase 13 (Test Writing) is now **Done** in full. See HO-013D. |
| 2026-07-06 | lead-full-stack-engineer | Phase 13 — IMP-002 resolved per explicit Human PO approval: installed `vitest@4.1.10` and `jsdom@29.1.1` as devDependencies in `frontend/` and executed the 145-test/16-file Vitest unit/component suite authored earlier in Phase 13 (HO-013) for the first time. First run failed to compile (4x `TS2345` across 3 spec files — `Array.from(...)` inferred `unknown[]` instead of `Element[]`); fixed by adding an explicit `Array.from<Element>(...)` type argument in each. Second run: 144/145 passed, 1 failed + 1 unhandled rejection, both `NG04002: Cannot match any routes` — two spec files (`search-form.component.spec.ts`, `results-list.component.spec.ts`) registered an empty test route table (`provideRouter([])`) while intentionally exercising success paths that call `router.navigate(['/results'])`/`['/booking'])`; fixed by registering trivial stub routes in each spec's `TestBed` config. All 5 fixes were test-authoring corrections only — no `frontend/src/app/**` application file was touched, and no new QA finding was warranted (the app's real route table already registers both routes, independently corroborated by the 11/11-passing Playwright E2E suite). **Final result: 145/145 passing, 0 failed, 0 skipped, across all 16 files.** `npm run build` re-verified clean (0 errors, 0 warnings) after the fixes. See HO-013E and `docs/testing/execution/frontend-unit-test-execution-summary.md`. |
| 2026-07-06 | functional-tester | Phase 14 (Test Execution Summary) — independently re-ran all three automated test layers written/executed in Phase 13 rather than relying solely on the prior reports, per this role's "never claim tests passed unless command output confirms it" mandate. Backend: `dotnet build SkyRoute.slnx --no-incremental` (0/0) then `dotnet test SkyRoute.slnx` → **114/114 passing** (103 + 11). Frontend unit/component: `npm test` → **145/145 passing**, 16/16 files. E2E: started backend (`dotnet run --no-build --launch-profile http`, :5094) and frontend (`npm start`, :4200), confirmed both responsive, ran `npx playwright test` → **11/11 passing**, then stopped both servers (`taskkill //F //PID ... //T`) and confirmed via `netstat` that no LISTENING socket remained on either port. All three results matched Phase 13's reported counts exactly — no discrepancy, no flakiness, no new QA finding. Produced the formal consolidated Phase 14 report at `docs/testing/execution/phase-14-test-execution-summary.md`, spot-checked test-strategy.md v1.1's traceability matrix (6 representative rows, all confirmed), and reconfirmed QA-003 Resolved with no regression; QA-001/QA-002/QA-004/QA-005 remain Open, deferred to Phase 19. **Final QA recommendation: proceed to Phase 15 (Code Review).** See HO-014. |

---

## 6. Reference Documents

- `docs/delivery/project-backlog.md` (created at Phase 07)
- `docs/delivery/sdlc-operating-model.md`
- `docs/delivery/dependency-register.md`
- `docs/handoffs/workflow-state.md`
