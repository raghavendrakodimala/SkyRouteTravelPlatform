# SDLC Delivery Operating Model — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
Author: Project Coordinator
Status: Approved

---

## 1. Purpose

This document defines the SDLC Delivery Operating Model for the SkyRoute Travel Platform MVP. It describes how the simulated IT delivery team operates, how specialist agents interact, how work is delegated and tracked, and how quality gates are enforced across all SDLC phases.

The Human Product Owner (Raghavendra Kodimala) remains the final approver at all human approval gates.

---

## 2. Project Context

| Item | Value |
|---|---|
| Project | SkyRoute Travel Platform — Flight Search and Booking |
| Delivery type | Hiring challenge MVP |
| Stack | Angular 17 (standalone components) + ASP.NET Core 8 |
| Team model | Simulated IT delivery team — specialist Claude agents |
| Deadline | EOD 2026-07-03 |
| Delivery mode | Phased SDLC Autopilot — `--auto-commit-merge --no-push` |
| Sprint model | Single compressed sprint covering all SDLC phases |
| Branch workflow | Direct merge, no PRs |

---

## 3. Operating Principles

The delivery team operates under the following principles.

### 3.1 Spec-Driven Delivery

No implementation begins before required specifications are complete. The sequence is: requirement > acceptance criteria > architecture direction > API contract (if applicable) > UI flow (if applicable) > test plan > implementation.

### 3.2 Phase-Based Delivery

The SDLC is executed one phase at a time. Each phase uses a dedicated branch, produces expected artefacts, and is committed and merged to `main` before the next phase begins.

### 3.3 Traceability

Every backlog item traces through: user story > feature spec > implementation task > test case > review finding > delivery tracking update.

### 3.4 Quality Gates

Quality is enforced through Definition of Ready (before implementation), Definition of Done (before merge), and review findings (post-implementation). Critical and High findings require explicit Human Product Owner acceptance before merge.

### 3.5 Human Approval Gates

The workflow pauses at defined gates requiring human approval. The SDLC Orchestrator never bypasses a human approval gate.

### 3.6 Controlled Delegation

Agents delegate within approved boundaries defined in `.claude/rules/delegation-rules.md`. All delegation is recorded in `docs/delivery/delegation-log.md`.

---

## 4. Agent Roles and Coordination

### 4.1 SDLC Orchestrator

The SDLC Orchestrator (`sdlc-orchestrator`) coordinates the full workflow. It:

- Identifies the responsible agent for each phase.
- Delegates work to specialist agents.
- Validates expected artefacts after each phase.
- Validates handoff notes.
- Updates workflow state.
- Enforces human approval gates.

The SDLC Orchestrator does not perform specialist work. It coordinates, validates, and continues or stops.

### 4.2 Specialist Agents

Each specialist agent is responsible for defined phases and artefacts. See `docs/delivery/roles-and-responsibilities.md` for the full RACI matrix.

| Agent | Role Focus |
|---|---|
| `product-owner` | Requirements, acceptance criteria, MVP scope, product decisions |
| `scrum-master` | Scrum ceremonies, readiness checks, impediment tracking, retrospectives |
| `project-coordinator` | Delivery tracking, dependencies, risks, decisions, task board |
| `solution-architect` | Architecture, NFRs, API contracts, technical governance |
| `lead-full-stack-engineer` | Primary implementation delivery and engineering task breakdown |
| `senior-full-stack-engineer` | Complex implementation, fixes, and technical problem-solving |
| `junior-developer` | Small scoped tasks under lead engineer supervision |
| `database-engineer` | Data models, repository layer, persistence strategy |
| `devops-engineer` | CI/CD, build configuration, environment setup |
| `ux-ui-designer` | UI flow, interaction design, accessibility expectations |
| `functional-tester` | Test strategy, test writing, test execution, QA findings |
| `code-reviewer` | Code review findings (CR-series) |
| `security-reviewer` | Security review findings (SEC-series) |
| `accessibility-tester` | Accessibility review findings (A11Y-series) |
| `performance-tester` | Performance review findings (PERF-series) |
| `technical-writer` | Documentation creation and updates |

---

## 5. Phase-by-Phase Delivery Model

### Phase Overview

| Phase | Name | Primary Agent | Key Artefacts | Human Gate |
|---:|---|---|---|---|
| 01 | Scrum Operating Model | scrum-master | `docs/delivery/scrum-operating-model.md` | No |
| 02 | SDLC Delivery Operating Model | project-coordinator | Delivery model docs | No |
| 03 | Requirements Analysis | solution-architect + product-owner | `docs/requirements.md` | Yes — approve requirements |
| 04 | NFR Specification | solution-architect | `docs/specs/non-functional-requirements.md` | No |
| 05 | Test Strategy | functional-tester | `docs/testing/test-strategy.md` | No |
| 06 | Architecture Planning | solution-architect | `docs/architecture/` | No |
| 07 | Project Backlog | project-coordinator + product-owner | `docs/delivery/project-backlog.md` | Yes — approve backlog |
| 08 | Parallel Delivery Plan | project-coordinator | `docs/delivery/parallel-delivery-plan.md` | No |
| 09 | Sprint Planning | scrum-master | `docs/delivery/sprint-plan.md` | Yes — approve sprint scope |
| 10 | Feature Specifications | solution-architect | `docs/features/` | No |
| 11 | Spec Readiness Check | scrum-master | Readiness report | No |
| 12 | Implementation | lead-full-stack-engineer | Source code, `src/` | No |
| 13 | Test Writing | functional-tester | Test files | No |
| 14 | Test Execution Summary | functional-tester | `docs/testing/execution/` | No |
| 15 | Code Review | code-reviewer | `docs/reviews/code-review-*.md` | No |
| 16 | Security Review | security-reviewer | `docs/reviews/security-review-*.md` | No |
| 17 | Accessibility Review | accessibility-tester | `docs/reviews/accessibility-review-*.md` | No |
| 18 | Performance Review | performance-tester | `docs/reviews/performance-review-*.md` | No |
| 19 | Findings Fixes | lead-full-stack-engineer | Updated source code | No |
| 20 | Re-test and Re-review | functional-tester | Updated execution summaries | No |
| 21 | Delivery Tracking Update | project-coordinator | Updated delivery docs | No |
| 22 | Sprint Review | scrum-master | `docs/delivery/sprint-review.md` | Yes — accept increment |
| 23 | Retrospective | scrum-master | `docs/delivery/retrospective.md` | No |
| 24 | Final SDLC Summary | project-coordinator | Summary artefact | Yes — approve merge |

### Phase Entry Criteria

A phase may not start until:

- The previous phase branch is committed and merged to `main`.
- The previous phase handoff note is complete.
- The workflow state file reflects the previous phase as Complete.
- No blocker is recorded in the workflow state.

### Phase Exit Criteria

A phase is complete only when:

- All expected artefacts exist.
- The handoff note is complete with all required fields.
- The workflow state is updated.
- The delegation log is updated if delegation occurred.
- The phase branch is committed.
- The phase branch is merged to `main` with `--no-ff`.
- The phase branch is deleted.

---

## 6. Decision-Making Authority Matrix

| Decision Type | Who Decides | Who Approves | Who Is Informed |
|---|---|---|---|
| Product priority and MVP scope | product-owner | Human Product Owner | scrum-master, project-coordinator |
| Sprint scope commitment | scrum-master | Human Product Owner | All agents |
| Technical architecture | solution-architect | SDLC Orchestrator | lead-full-stack-engineer, project-coordinator |
| NFR targets | solution-architect | SDLC Orchestrator | functional-tester |
| API contract | solution-architect | SDLC Orchestrator | lead-full-stack-engineer |
| Dependency introduction | solution-architect | Human Product Owner | project-coordinator |
| Risk acceptance (Low/Medium) | project-coordinator | SDLC Orchestrator | scrum-master |
| Risk acceptance (High/Critical) | project-coordinator | Human Product Owner | All agents |
| Test strategy | functional-tester | SDLC Orchestrator | lead-full-stack-engineer |
| Review finding (Low/Medium) | Reviewer | lead-full-stack-engineer | project-coordinator |
| Review finding (High/Critical) | Reviewer | Human Product Owner | All agents |
| Implementation approach | lead-full-stack-engineer | solution-architect | SDLC Orchestrator |
| File deletion | Any | Human Product Owner | SDLC Orchestrator |
| Destructive commands | Any | Human Product Owner | SDLC Orchestrator |
| Merge to main | SDLC Orchestrator | Human Product Owner | All agents |
| Deployment | SDLC Orchestrator | Human Product Owner | All agents |

---

## 7. Communication and Escalation Paths

### 7.1 Agent-to-Agent Communication

All agent communication is mediated through:

1. The active SDLC Orchestrator session.
2. Persistent handoff files under `docs/handoffs/`.

Agents do not communicate directly with each other. All delegation and coordination flows through the SDLC Orchestrator or through approved delegation chains defined in `.claude/rules/delegation-rules.md`.

### 7.2 Handoff Protocol

Every agent must create or update `docs/handoffs/current-handoff.md` after completing work. Additionally, each handoff produces a numbered file:

```
docs/handoffs/<sequence>-<from-agent>-to-<to-agent>-<scope>.md
```

The SDLC Orchestrator reads the handoff note before invoking the next agent. Agents must not assume another agent has completed work unless the artefact exists and the handoff confirms completion.

### 7.3 Escalation Path

| Situation | Escalation Target | Action |
|---|---|---|
| Missing requirement | product-owner | Stop; request clarification |
| Missing architecture direction | solution-architect | Stop; request architecture guidance |
| Blocking impediment | scrum-master + SDLC Orchestrator | Log impediment; stop if human decision required |
| High/Critical finding | Human Product Owner | Stop; present finding; await decision |
| Scope change request | Human Product Owner | Stop; present impact; await decision |
| Dependency introduction | Human Product Owner | Stop; present dependency; await approval |
| Destructive action needed | Human Product Owner | Stop; present risk; await explicit approval |
| Merge to main | Human Product Owner | Stop; confirm merge readiness; await approval |

### 7.4 Blocking Communication

When the workflow is blocked, the SDLC Orchestrator must:

1. Record the blocker in `docs/handoffs/workflow-state.md`.
2. Record the blocker in `docs/handoffs/current-handoff.md`.
3. Record the impediment in `docs/delivery/impediment-log.md`.
4. Present a clear decision request to the Human Product Owner in the session.
5. Not proceed until explicit approval or resolution is received.

---

## 8. Quality Gate Checkpoints

### 8.1 Definition of Ready Gate (Phase 11 — Spec Readiness Check)

Before any backlog item enters implementation:

- User story documented.
- Acceptance criteria documented.
- Architecture guidance exists.
- API contract exists (if applicable).
- UI flow exists (if applicable).
- NFR impact understood.
- Test approach defined.
- Dependencies identified.
- Risks identified.
- Human Product Owner has no blocking questions.

Items that do not meet the Definition of Ready are marked Blocked and cannot proceed to implementation.

### 8.2 Implementation Gate (Phase 12 entry)

Before implementation begins:

- Spec Readiness Check (Phase 11) is complete.
- Sprint plan is approved.
- All selected items have Definition of Ready status: Ready or Conditionally Ready (with approved exception).

### 8.3 Review Gate (Phases 15–18 entry)

Before reviews begin:

- Implementation is complete and committed.
- Test execution summary exists.
- Test results do not indicate a build-blocking failure.

### 8.4 Fix Gate (Phase 19 entry)

Before fixes begin:

- All review reports are complete.
- Findings are documented with IDs.
- Lead engineer has received the full findings list.

### 8.5 Merge Gate (Phase 24 — before merge to main)

Before merging to main:

- Working tree is clean.
- All Critical and High findings are resolved or explicitly accepted by Human Product Owner.
- Test execution summary exists.
- Delivery tracking is updated.
- Documentation is updated.
- Handoff notes are complete.
- Human Product Owner has approved the merge.

---

## 9. Artifact Traceability Model

Every backlog item must be traceable through the full delivery chain:

```
Requirement / User Story
    |
    v
Acceptance Criteria
    |
    v
Feature Specification
    |
    v
Architecture Direction / API Contract
    |
    v
Implementation Task (backlog item)
    |
    v
Test Case (test strategy + test writing)
    |
    v
Test Execution Result
    |
    v
Review Finding (if applicable)
    |
    v
Fix Commit (if required)
    |
    v
Delivery Tracking Update
    |
    v
Sprint Review Acceptance
```

### 9.1 Traceability IDs

| Artefact Type | ID Format | Location |
|---|---|---|
| Backlog item | US-001, TASK-001 | `docs/delivery/project-backlog.md` |
| Requirement | REQ-001 | `docs/requirements.md` |
| Feature spec | SPEC-001 | `docs/features/` |
| Test case | TC-001 | `docs/testing/test-strategy.md` |
| Code review finding | CR-001 | `docs/reviews/` |
| Security finding | SEC-001 | `docs/reviews/` |
| Accessibility finding | A11Y-001 | `docs/reviews/` |
| Performance finding | PERF-001 | `docs/reviews/` |
| QA finding | QA-001 | `docs/testing/execution/` |
| Impediment | IMP-001 | `docs/delivery/impediment-log.md` |
| Risk | RISK-001 | `docs/delivery/risk-register.md` |
| Dependency | DEP-001 | `docs/delivery/dependency-register.md` |
| Decision | DEC-001 | `docs/delivery/decision-log.md` |
| Delegation | DEL-001 | `docs/delivery/delegation-log.md` |

---

## 10. Artefact Registry

| Artefact | Path | Owner | Phase |
|---|---|---|---|
| Scrum Operating Model | `docs/delivery/scrum-operating-model.md` | scrum-master | 01 |
| SDLC Operating Model | `docs/delivery/sdlc-operating-model.md` | project-coordinator | 02 |
| Roles and Responsibilities | `docs/delivery/roles-and-responsibilities.md` | project-coordinator | 02 |
| Dependency Register | `docs/delivery/dependency-register.md` | project-coordinator | 02 |
| Risk Register | `docs/delivery/risk-register.md` | project-coordinator | 02 |
| Decision Log | `docs/delivery/decision-log.md` | project-coordinator | 02 |
| Impediment Log | `docs/delivery/impediment-log.md` | project-coordinator | 02 |
| Delegation Log | `docs/delivery/delegation-log.md` | project-coordinator | 02 |
| Task Board | `docs/delivery/task-board.md` | project-coordinator | 02 |
| RACI Matrix | `docs/delivery/raci-matrix.md` | project-coordinator | 02 |
| Requirements | `docs/requirements.md` | solution-architect + product-owner | 03 |
| NFR Specification | `docs/specs/non-functional-requirements.md` | solution-architect | 04 |
| Test Strategy | `docs/testing/test-strategy.md` | functional-tester | 05 |
| Architecture Docs | `docs/architecture/` | solution-architect | 06 |
| Project Backlog | `docs/delivery/project-backlog.md` | project-coordinator | 07 |
| Parallel Delivery Plan | `docs/delivery/parallel-delivery-plan.md` | project-coordinator | 08 |
| Sprint Plan | `docs/delivery/sprint-plan.md` | scrum-master | 09 |
| Feature Specifications | `docs/features/` | solution-architect | 10 |
| Test Execution Summaries | `docs/testing/execution/` | functional-tester | 14, 20 |
| Review Reports | `docs/reviews/` | Reviewers | 15–18, 20 |
| Sprint Review | `docs/delivery/sprint-review.md` | scrum-master | 22 |
| Retrospective | `docs/delivery/retrospective.md` | scrum-master | 23 |
| Workflow State | `docs/handoffs/workflow-state.md` | SDLC Orchestrator | Ongoing |
| Handoff Index | `docs/handoffs/handoff-index.md` | SDLC Orchestrator | Ongoing |
| Current Handoff | `docs/handoffs/current-handoff.md` | Active agent | Ongoing |

---

## 11. Reference Documents

- `CLAUDE.md`
- `.claude/rules/sdlc-rules.md`
- `.claude/rules/phased-execution.md`
- `.claude/rules/delegation-rules.md`
- `.claude/rules/agent-communication.md`
- `.claude/rules/definition-of-ready.md`
- `.claude/rules/definition-of-done.md`
- `.claude/rules/spec-driven-development.md`
- `.claude/rules/nfr-governance.md`
- `.claude/rules/review-and-test-reporting.md`
- `.claude/rules/tool-safety.md`
- `.claude/rules/git-workflow.md`
- `docs/delivery/roles-and-responsibilities.md`
- `docs/delivery/scrum-operating-model.md`
