# Impediment Log — SkyRoute Travel Platform MVP

Version: 1.1
Date: 2026-07-07 (Phase 21 reconciliation; baseline 2026-07-03)
Author: Project Coordinator
Status: Active

---

## 1. Purpose

This log tracks impediments that block or threaten to block sprint progress. Impediments are raised by any agent, owned by the Scrum Master for escalation, and resolved through Human Product Owner decisions or agent actions.

---

## 2. Impediment Status Values

| Status | Meaning |
|---|---|
| Open | Impediment is active; blocking or threatening progress |
| In Progress | Resolution is being actively worked |
| Resolved | Impediment is removed; downstream work can proceed |
| Accepted | Impediment cannot be removed; team has accepted and adapted |
| Escalated | Impediment escalated to Human Product Owner for decision |
| Deferred | Impediment acknowledged; not blocking the current phase but may affect later phases |

---

## 3. Severity Values

| Severity | Meaning |
|---|---|
| Critical | Completely blocks sprint delivery or a Must Have feature |
| High | Blocks a significant work item; threatens sprint goal |
| Medium | Slows progress; workaround exists but is costly |
| Low | Minor nuisance; workaround is easy or impact is minimal |

---

## 4. Impediment Log

| ID | Date Raised | Raised By | Severity | Affected Phase / Item | Description | Impact | Owner | Status | Resolution | Date Resolved |
|---|---|---|---|---|---|---|---|---|---|---|
| IMP-001 | 2026-07-03 | project-coordinator | High | Phase 14 — Test Execution Summary; Phase 13 — Test Writing | Test execution requires commands that cannot run autonomously: `npm install`, `npm test`, `dotnet restore`, `dotnet test`. These commands require explicit human approval per `CLAUDE.md` tool safety rules before they can be executed. | Test execution summaries for Phase 14 and Phase 20 (Re-test) cannot confirm actual test pass/fail status until the Human Product Owner approves command execution. The functional tester must document "tests not run — awaiting human approval" in the execution summary until this impediment is resolved. | scrum-master | Resolved | Human PO granted per-command approvals throughout Phases 13–14; all suites executed and re-executed green through Phase 20 (365/365, HO-038). Running existing test suites/builds/lint/type-check is now standing pre-approved per DEC-018 (autopilot efficiency review), so this impediment cannot recur. | 2026-07-06 |
| IMP-002 | 2026-07-06 | lead-full-stack-engineer | High | Phase 13 — Test Writing (frontend suite execution) | Frontend unit test runner (`vitest`/`jsdom`) was not installed (Phase 12 used `ng new --skip-tests`); installing devDependencies requires explicit human approval. | The authored 145-test Vitest suite could not be executed until installation was approved. | lead-full-stack-engineer | Resolved | Human PO approved installation of `vitest@4.1.10` and `jsdom@29.1.1`; suite executed — 145/145 passing after 5 test-authoring fixes. See HO-013E. | 2026-07-06 |

---

## 5. Impediment Escalation Rules

Per the Scrum Operating Model (`docs/delivery/scrum-operating-model.md`), the SDLC Orchestrator must stop for Human Product Owner input when an impediment:

- Changes MVP scope.
- Requires accepting an unresolved Critical or High finding.
- Requires a destructive command or file deletion.
- Involves an unresolved dependency on an external system or library.
- Blocks the sprint goal.

IMP-001 qualifies for escalation at Phase 14 as it blocks test execution confirmation. The SDLC Orchestrator will present the required commands to the Human Product Owner at that phase.

---

## 6. Impediment Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial log created for Phase 02; IMP-001 raised from tool safety analysis |
| 2026-07-07 | project-coordinator | Phase 21 — reconciliation: IMP-001 marked Resolved (per-command approvals granted from Phase 13 onward; superseded by DEC-018 standing pre-approval) and IMP-002 backfilled (raised and resolved 2026-07-06 per HO-013E — this log had never been updated despite `workflow-state.md` tracking both). No impediment is currently open. |

---

## 7. Reference Documents

- `docs/delivery/risk-register.md` — see RISK-004
- `docs/delivery/dependency-register.md` — see DEP-023
- `docs/delivery/scrum-operating-model.md` — Section 10 (Impediment Management)
- `.claude/rules/tool-safety.md`
- `CLAUDE.md` — Section 14 (Tool Safety Rules)
