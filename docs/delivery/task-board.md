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

Stories will be added here at Phase 07 — Project Backlog Creation.

The Product Owner and Solution Architect will define user stories with T-shirt size estimates and MoSCoW priority. The Project Coordinator will add story cards to this section at that time.

| ID | Story Title | Priority | Size | Owner | Status | Sprint | Notes |
|---|---|---|---|---|---|---|---|
| — | To be populated at Phase 07 | — | — | — | — | — | — |

---

## 5. Board Update Log

| Date | Updated By | Change |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial board created for Phase 02. Phase 01 and Phase 02 moved to Done. All remaining phases added to Backlog. |

---

## 6. Reference Documents

- `docs/delivery/project-backlog.md` (created at Phase 07)
- `docs/delivery/sdlc-operating-model.md`
- `docs/delivery/dependency-register.md`
- `docs/handoffs/workflow-state.md`
