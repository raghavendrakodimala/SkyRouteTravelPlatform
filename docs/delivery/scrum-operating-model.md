# Scrum Operating Model — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
Author: Scrum Master
Status: Approved

---

## 1. Purpose

This document defines the Scrum operating model for the SkyRoute Travel Platform MVP delivery. It governs how the simulated IT delivery team — represented by specialist Claude agents and coordinated by the SDLC Orchestrator — applies Scrum principles to deliver the Flight Search and Booking module on time, with quality, and with full traceability.

The Human Product Owner remains the final approver for scope, priority, and acceptance decisions throughout.

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

---

## 3. Scrum Roles

### 3.1 Product Owner

**Accountable:** Human user (Raghavendra Kodimala)

Responsibilities:
- Owns and prioritises the product backlog.
- Defines and approves acceptance criteria.
- Approves MVP scope boundaries.
- Resolves open questions on business requirements.
- Accepts or rejects completed increments at Sprint Review.
- Provides final approval at all human approval gates defined in `CLAUDE.md`.

The `product-owner` agent assists the Human Product Owner by clarifying requirements, drafting acceptance criteria, and surfacing open questions — but all Product Owner decisions require human confirmation before the workflow proceeds.

### 3.2 Scrum Master

**Agent:** `scrum-master`

Responsibilities:
- Maintains this Scrum operating model.
- Runs and facilitates all Scrum ceremonies (in their adapted simulated form).
- Enforces Definition of Ready and Definition of Done.
- Tracks and escalates impediments.
- Validates sprint readiness before commitment.
- Produces sprint plan, sprint review, and retrospective artefacts.
- Ensures no backlog item enters a sprint without meeting the Definition of Ready.
- Reports sprint health to the Human Product Owner.

### 3.3 Development Team

**Coordinator:** `sdlc-orchestrator`

The Development Team is composed of the following specialist agents acting under SDLC Orchestrator coordination:

| Agent | Specialty |
|---|---|
| solution-architect | Requirements, NFRs, architecture, API contracts |
| lead-full-stack-engineer | Primary implementation delivery |
| senior-full-stack-engineer | Complex implementation and fixes |
| junior-developer | Small scoped tasks |
| database-engineer | Data model and persistence |
| devops-engineer | CI/CD, build, environment |
| ux-ui-designer | UI flow and interaction design |
| functional-tester | Test strategy, test writing, execution |
| code-reviewer | Code review findings |
| security-reviewer | Security review findings |
| accessibility-tester | Accessibility review findings |
| performance-tester | Performance review findings |
| technical-writer | Documentation |
| project-coordinator | Delivery tracking, risk, dependencies |

All agents operate within the delegation boundaries defined in `.claude/rules/delegation-rules.md`.

### 3.4 Stakeholders

For the purposes of this hiring challenge:
- The Human Product Owner is the sole business stakeholder.
- No external stakeholder review is required.
- The Sprint Review is a demonstration of completed work to the Human Product Owner.

---

## 4. Sprint Structure

Given the single-day delivery deadline (EOD 2026-07-03), the sprint is structured as a compressed single sprint covering all SDLC phases.

| Item | Value |
|---|---|
| Sprint number | Sprint 1 |
| Sprint goal | Deliver a working, reviewed SkyRoute Flight Search and Booking MVP |
| Sprint start | 2026-07-03 |
| Sprint end | EOD 2026-07-03 |
| Sprint length | 1 working day (compressed for hiring challenge) |
| Sprint model | Phased SDLC Autopilot with sequential phase gates |

Each SDLC phase within the sprint is treated as a discrete delivery transaction with its own branch, commit, and merge. See `docs/handoffs/workflow-state.md` for the full phase list.

---

## 5. Ceremonies

All ceremonies are adapted for the simulated team model. Each ceremony produces a persistent artefact. Ceremonies are run at the appropriate SDLC phase rather than on a fixed calendar schedule.

### 5.1 Sprint Planning

**Phase:** Phase 09 — Sprint Planning
**Responsible agent:** `scrum-master`
**Artefact:** `docs/delivery/sprint-plan.md`

Purpose:
- Confirm sprint goal with the Human Product Owner.
- Select backlog items that meet the Definition of Ready.
- Define the sprint scope as an ordered list of work items.
- Confirm capacity, dependencies, and risks.
- Record any items that are not ready and why.

Inputs required:
- Prioritised product backlog (`docs/delivery/` backlog artefacts).
- Definition of Ready confirmation for each selected item.
- Architecture direction from solution-architect.
- Risk and dependency register from project-coordinator.

Sprint Planning gate: no implementation may begin until the sprint plan is approved and all selected items meet the Definition of Ready.

### 5.2 Daily Standup (Adapted)

**Frequency:** Once per SDLC phase boundary
**Responsible agent:** `scrum-master` (facilitated through SDLC Orchestrator phase transitions)

Because this is a compressed simulated sprint, the Daily Standup is adapted as a phase boundary check:

At each phase boundary the SDLC Orchestrator confirms:
- What was completed in the previous phase.
- What is starting in the current phase.
- Any impediments that have emerged.

This check is recorded in `docs/handoffs/workflow-state.md` and `docs/handoffs/current-handoff.md`.

There is no separate Daily Standup artefact file; the handoff files serve as the phase boundary record.

### 5.3 Sprint Review

**Phase:** Phase 22 — Sprint Review
**Responsible agent:** `scrum-master`
**Artefact:** `docs/delivery/sprint-review.md`

Purpose:
- Present the completed increment to the Human Product Owner.
- Confirm which backlog items are done per the Definition of Done.
- Identify any items not completed and carry-forward decisions.
- Capture Human Product Owner acceptance or rejection of the increment.

The Sprint Review requires human participation. The SDLC Orchestrator will stop and await Human Product Owner input before the Sprint Review is marked complete.

### 5.4 Retrospective

**Phase:** Phase 23 — Retrospective
**Responsible agent:** `scrum-master`
**Artefact:** `docs/delivery/retrospective.md`

Purpose:
- Reflect on the delivery process.
- Identify what went well.
- Identify what could be improved.
- Capture action items for future sprints or projects.

Given the single-sprint context, retrospective findings will serve as improvement notes for future hiring challenge or project runs.

### 5.5 Backlog Refinement

**Phase:** Phase 07 — Project Backlog and Phase 10 — Feature Specifications
**Responsible agents:** `product-owner`, `solution-architect`, `project-coordinator`

Purpose:
- Review and refine backlog items before sprint commitment.
- Confirm user stories are well-formed (title, business value, acceptance criteria).
- Confirm dependencies and risks are identified.
- Confirm architecture guidance exists for complex items.
- Confirm test approach is defined.
- Assign Definition of Ready status to each item.

Refinement is complete when every item intended for Sprint 1 has been reviewed and meets the Definition of Ready.

---

## 6. Definition of Ready

A backlog item may not be committed to a sprint unless all of the following criteria are met.

| # | Criterion | Notes |
|---|---|---|
| 1 | Business value is clear | Documented in user story or requirement |
| 2 | User story is documented | Format: As a [role] I want [capability] so that [benefit] |
| 3 | Acceptance criteria are documented | Specific, testable, unambiguous |
| 4 | Scope boundaries are clear | In/out of scope explicitly stated |
| 5 | Dependencies are identified | Technical and team dependencies listed |
| 6 | Risks are identified | Likelihood and impact noted |
| 7 | Architecture guidance exists | For any item with non-trivial technical design |
| 8 | API contract exists | For any item involving API endpoints |
| 9 | UI flow exists | For any item involving UI screens or components |
| 10 | NFR impact is understood | Performance, security, accessibility implications noted |
| 11 | Test approach is defined | At least one test scenario documented |
| 12 | Human Product Owner has no blocking questions | No outstanding open questions that could change scope |

**Definition of Ready statuses:**

| Status | Meaning |
|---|---|
| Ready | All criteria met — item may enter sprint |
| Conditionally Ready | Minor gaps acceptable with explicit exception approved |
| Blocked | One or more criteria not met — item must not enter sprint |

When a Blocked item is identified, the Scrum Master raises an impediment (see Section 8) and the item cannot enter the sprint until the block is resolved.

---

## 7. Definition of Done

Work is considered done only when all of the following criteria are satisfied.

| # | Criterion | Applies To |
|---|---|---|
| 1 | Implementation matches approved specs | All implementation work |
| 2 | Tests are added or updated | All implementation work |
| 3 | Test execution summary exists | All implementation work |
| 4 | Code review report exists | All implementation work |
| 5 | Security review report exists | API and sensitive data work |
| 6 | Accessibility review exists | All UI work |
| 7 | Performance review exists | Performance-sensitive paths |
| 8 | Critical and High findings resolved or accepted by Human Product Owner | All review-flagged work |
| 9 | Documentation is updated | All work with user-facing or system impact |
| 10 | Delivery tracking is updated | All work items |
| 11 | Handoff notes are complete | All agent transitions |
| 12 | Working tree is clean before merge | All branches pending merge |

Review reports are stored under `docs/reviews/`. Test execution summaries are stored under `docs/testing/execution/`. Handoff files are stored under `docs/handoffs/`.

Critical and High findings may only be accepted as risk — without a fix — by explicit Human Product Owner approval. The acceptance must be recorded in the relevant review report with the status `Accepted Risk`.

---

## 8. Backlog Management

### 8.1 Backlog Structure

The product backlog is maintained in `docs/delivery/` and includes:
- Epics (high-level feature groups)
- User stories (specific deliverable capabilities)
- Technical tasks (infrastructure, architecture, DevOps)
- Bug/fix items (raised from review findings)

### 8.2 Prioritisation

The `product-owner` agent prioritises the backlog in consultation with the Human Product Owner using MoSCoW:

| Priority | Label | Meaning |
|---|---|---|
| Must Have | M | MVP critical — sprint fails without this |
| Should Have | S | High value — include if capacity allows |
| Could Have | C | Low risk addition if time permits |
| Won't Have | W | Out of scope for this sprint |

The Human Product Owner has final authority on all priority decisions. The SDLC Orchestrator must stop and seek human approval before changing Must Have scope.

### 8.3 Estimation

For this sprint, story complexity is tracked using T-shirt sizes:

| Size | Complexity |
|---|---|
| XS | Trivial — configuration, minor copy |
| S | Small — single component or endpoint |
| M | Medium — one feature with standard patterns |
| L | Large — cross-cutting feature, multiple layers |
| XL | Extra large — complex, high risk, multi-agent |

Estimation is performed by the `lead-full-stack-engineer` and `solution-architect` during Sprint Planning. The Human Product Owner does not estimate but may challenge estimates that impact MVP scope.

### 8.4 Backlog Refinement Cadence

In the simulated model, backlog refinement occurs as a single session at Phase 07 and is revisited at Phase 10 (Feature Specifications). There is no rolling calendar-based refinement in this compressed sprint.

---

## 9. Velocity Tracking

### 9.1 Approach

Velocity is tracked as the number of story points or T-shirt size units completed per sprint. For this single-sprint MVP delivery:

- Initial capacity is treated as the full available sprint capacity (compressed single day).
- There is no historical velocity baseline — this is sprint 1.
- Velocity will be recorded at Sprint Review as actual delivered story points.
- The retrospective will note velocity as a reference for future runs.

### 9.2 Velocity Record

The sprint velocity record will be added to `docs/delivery/sprint-review.md` at the end of Sprint 1.

Because this is a single-sprint delivery, velocity tracking is primarily informational. Any items not completed within the sprint window are recorded in the Sprint Review as carry-forward items with explicit Human Product Owner decision on acceptance or deferral.

---

## 10. Impediment Management

### 10.1 What Constitutes an Impediment

An impediment is any condition that prevents a backlog item from progressing as planned. Examples:

- Missing spec, architecture guidance, or API contract.
- Unresolved open question from the Human Product Owner.
- Missing dependency (environment, tool, library approval).
- Blocking finding from a review that requires human acceptance.
- Conflicting requirements or architectural direction.
- Build or test failure that should not be merged.
- A phase blocking another phase from starting.

### 10.2 Impediment Lifecycle

| Step | Action | Owner |
|---|---|---|
| Raise | Agent or SDLC Orchestrator identifies a block | Any agent |
| Log | Impediment recorded in `docs/delivery/impediment-log.md` | Scrum Master |
| Escalate | If human decision is needed, SDLC Orchestrator stops and presents to Human Product Owner | SDLC Orchestrator |
| Resolve | Human Product Owner provides decision or agent resolves technical block | Human PO or agent |
| Close | Impediment marked resolved in log | Scrum Master |

### 10.3 Impediment Log Format

Each impediment in `docs/delivery/impediment-log.md` must include:

| Field | Description |
|---|---|
| ID | IMP-001, IMP-002, ... |
| Date raised | ISO date |
| Raised by | Agent or human |
| Affected item | Backlog item or phase |
| Description | Clear description of the block |
| Impact | What cannot proceed |
| Owner | Who must resolve |
| Status | Open / In Progress / Resolved / Accepted |
| Resolution | How it was resolved |
| Date closed | ISO date when resolved |

### 10.4 Impediment Escalation Rules

The SDLC Orchestrator must stop for human approval when an impediment:
- Changes MVP scope.
- Requires accepting an unresolved Critical or High finding.
- Requires a destructive command or file deletion.
- Involves an unresolved dependency on an external system or library.
- Blocks the sprint goal.

The Scrum Master reports open impediments at Sprint Review regardless of resolution status.

---

## 11. Team Working Agreements

The following working agreements apply to all agent interactions within this simulated team.

### 11.1 Communication

- All agent-to-agent communication is mediated through the SDLC Orchestrator session and persistent handoff files.
- Every agent must create or update `docs/handoffs/current-handoff.md` after completing work.
- Agents must not assume another agent has completed work unless the artefact exists and the handoff confirms completion.
- Open questions must be documented — agents must not make assumptions about business requirements.

### 11.2 Availability and Focus

- Agents operate sequentially within each SDLC phase — no concurrent implementation work unless explicitly parallelised by the SDLC Orchestrator.
- No agent performs work outside their defined role or phase scope.
- No future-phase work is performed in a current-phase branch.

### 11.3 Quality

- No implementation begins before the Definition of Ready is confirmed.
- No branch is merged before the Definition of Done is confirmed.
- All review findings are documented with IDs. Developers fix findings by ID.
- Critical and High findings are never silently deferred — they require explicit Human Product Owner acceptance.

### 11.4 Traceability

- Every backlog item must trace to: user story > feature spec > implementation > test case > review result.
- Every commit and merge must reference the SDLC phase.
- Handoff files must reference all relevant artefacts.

### 11.5 Tool Safety

- No agent may delete files or run destructive commands without explicit human approval.
- Forbidden commands are listed in `.claude/rules/tool-safety.md`.
- Agents report all commands run and their results.

---

## 12. Adapted Scrum for Simulated / AI Team Context

This section explains how standard Scrum ceremonies and artefacts map to the simulated agent-based delivery model.

| Standard Scrum Concept | Adapted Form in This Project |
|---|---|
| Sprint | Single compressed sprint — one working day, all phases sequential |
| Sprint Planning meeting | Phase 09 — Sprint Planning artefact produced by `scrum-master` |
| Daily Standup | Phase boundary check — SDLC Orchestrator workflow state update |
| Sprint Review meeting | Phase 22 — Sprint Review artefact, Human PO participation required |
| Retrospective meeting | Phase 23 — Retrospective artefact produced by `scrum-master` |
| Backlog refinement session | Phase 07 + Phase 10 — backlog and feature spec artefacts |
| Physical task board | `docs/delivery/task-board.md` — managed by `project-coordinator` |
| Team velocity chart | Sprint Review artefact — recorded after sprint completion |
| Impediment in Standup | `docs/delivery/impediment-log.md` — raised by any agent, owned by Scrum Master |
| Definition of Ready gate | Checked before sprint commitment and before each phase starts |
| Definition of Done gate | Checked before branch merge and at Sprint Review |
| Product Owner acceptance | Human user approval — SDLC Orchestrator stops and presents |
| Sprint Goal | Defined in `docs/delivery/sprint-plan.md` |

### 12.1 Phase Boundary as Sprint Event Equivalent

Because the sprint is compressed into a single day of sequential SDLC phases, each phase transition serves the function of a sprint event boundary:

- Start of Phase 09 (Sprint Planning) = Sprint Planning ceremony
- Each phase boundary = Daily Standup check
- Phase 22 (Sprint Review) = Sprint Review ceremony
- Phase 23 (Retrospective) = Retrospective ceremony

The SDLC Orchestrator coordinates all phase transitions and ensures no phase starts without meeting the entry criteria defined in `docs/handoffs/workflow-state.md`.

### 12.2 Human Approval Gates

The following events always require Human Product Owner input regardless of workflow automation:

- Final requirements acceptance
- MVP scope changes
- Sprint scope changes after planning
- Accepting unresolved Critical or High findings
- Merging to main
- Deployment

The SDLC Orchestrator stops the automated workflow at these gates and presents a clear decision request to the human user.

---

## 13. Artefact Registry

| Artefact | Path | Owner | Phase |
|---|---|---|---|
| Scrum Operating Model | `docs/delivery/scrum-operating-model.md` | Scrum Master | Phase 01 |
| SDLC Delivery Model | `docs/delivery/` (Phase 02 artefacts) | Project Coordinator | Phase 02 |
| Sprint Plan | `docs/delivery/sprint-plan.md` | Scrum Master | Phase 09 |
| Impediment Log | `docs/delivery/impediment-log.md` | Scrum Master | Ongoing |
| Sprint Review | `docs/delivery/sprint-review.md` | Scrum Master | Phase 22 |
| Retrospective | `docs/delivery/retrospective.md` | Scrum Master | Phase 23 |
| Workflow State | `docs/handoffs/workflow-state.md` | SDLC Orchestrator | Ongoing |
| Handoff Index | `docs/handoffs/handoff-index.md` | SDLC Orchestrator | Ongoing |
| Current Handoff | `docs/handoffs/current-handoff.md` | Active agent | Ongoing |
| Task Board | `docs/delivery/task-board.md` | Project Coordinator | Ongoing |
| Delegation Log | `docs/delivery/delegation-log.md` | Project Coordinator | Ongoing |
| RACI Matrix | `docs/delivery/raci-matrix.md` | Project Coordinator | Phase 02 |

---

## 14. Reference Documents

- `.claude/rules/definition-of-ready.md`
- `.claude/rules/definition-of-done.md`
- `.claude/rules/delegation-rules.md`
- `.claude/rules/agent-communication.md`
- `.claude/rules/sdlc-rules.md`
- `.claude/rules/phased-execution.md`
- `.claude/rules/tool-safety.md`
- `CLAUDE.md`
