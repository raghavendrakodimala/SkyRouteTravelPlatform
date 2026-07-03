# Decision Log — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
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
| DEC-001 | 2026-07-03 | Sprint length is compressed to one working day (EOD 2026-07-03) covering all SDLC phases sequentially as a single sprint | Hiring challenge has a fixed EOD deadline; all delivery must complete within this window | scrum-master (approved by Human PO context) | Approved | Process — all phases must complete within the day |
| DEC-002 | 2026-07-03 | Daily Standup is adapted as a phase boundary check recorded in handoff files — no separate standup artefact | Simulated team does not operate on a time-boxed daily schedule; phase transitions serve the same purpose | scrum-master | Approved | Process — no separate standup artefact required |
| DEC-003 | 2026-07-03 | Estimation uses T-shirt sizes (XS/S/M/L/XL) rather than Fibonacci story points | Single-sprint compressed delivery; T-shirt sizing is sufficient for relative complexity tracking without overhead of story point calibration | scrum-master | Approved | Process — estimation approach |
| DEC-004 | 2026-07-03 | Human Product Owner participates in Sprint Review (Phase 22) but not in automated phases — SDLC Orchestrator stops at Phase 22 for human input | Human oversight at the increment acceptance gate is required; automated phases do not require synchronous human involvement | scrum-master | Approved | Process — human gate at Phase 22 |
| DEC-005 | 2026-07-03 | Critical and High findings may only be accepted (not fixed) with explicit Human Product Owner approval, recorded in the review report as Accepted Risk | Ensures quality governance is not bypassed; Human PO bears accountability for accepted risks | scrum-master | Approved | Quality — finding acceptance protocol |
| DEC-006 | 2026-07-03 | Backlog refinement occurs as a single session at Phase 07 and Phase 10 — no rolling calendar refinement | Single compressed sprint; no time for rolling refinement sessions | scrum-master | Approved | Process — backlog refinement cadence |
| DEC-007 | 2026-07-03 | Phased SDLC Autopilot is used with `--auto-commit-merge --no-push` — SDLC Orchestrator may commit and merge phase branches autonomously | Explicit user approval was provided by initiating phased autopilot run; push is not approved | Human PO (via run command) | Approved | Process — Git automation boundary |
| DEC-008 | 2026-07-03 | In-memory data store is used for flight data — no database or external persistence in the MVP | Hiring challenge context; external database would add setup complexity and is out of scope for a demonstration MVP | project-coordinator (pending architecture confirmation in Phase 06) | Pending | Architecture — persistence strategy |
| DEC-009 | 2026-07-03 | Angular 17 standalone components are used (no NgModule-based architecture) | Angular 17 canonical pattern; standalone components are the modern recommended approach; reduces boilerplate | project-coordinator (pending architecture confirmation in Phase 06) | Pending | Architecture — Angular component model |
| DEC-010 | 2026-07-03 | No external flight data provider API is integrated — flight data is seeded in the in-memory store | Hiring challenge scope; external API integration would require credentials, network access, and error handling beyond the MVP scope | project-coordinator (pending requirements confirmation in Phase 03) | Pending | Scope — external dependency exclusion |
| DEC-011 | 2026-07-03 | Review phases 15–18 may be partially overlapped (Code Review + Security Review concurrently; Accessibility + Performance concurrently) if timeline pressure warrants | Parallel delivery to protect the EOD deadline while maintaining independent review quality | project-coordinator | Pending | Process — parallel review execution |
| DEC-012 | 2026-07-03 | No PR-based review comments are used — all review findings are stored as markdown files under `docs/reviews/` | Project operating model decision from CLAUDE.md; ensures findings are persistent and traceable without PR tooling | Human PO (via CLAUDE.md) | Approved | Process — review artefact storage |

---

## 5. Decision Review Log

| Date | Reviewer | Action |
|---|---|---|
| 2026-07-03 | project-coordinator | Initial log created for Phase 02; DEC-001 through DEC-006 sourced from HO-001 (scrum-master Phase 01 decisions); DEC-007 through DEC-012 raised from Phase 02 analysis |

---

## 6. Reference Documents

- `docs/delivery/risk-register.md`
- `docs/handoffs/workflow-state.md`
- `docs/handoffs/01-scrum-master-to-sdlc-orchestrator-scrum-model.md`
- `CLAUDE.md`
