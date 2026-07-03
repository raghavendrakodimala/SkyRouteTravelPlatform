# Roles and Responsibilities — SkyRoute Travel Platform MVP

Version: 1.0
Date: 2026-07-03
Author: Project Coordinator
Status: Approved

---

## 1. Purpose

This document defines the responsibilities of every agent role in the simulated IT delivery team. It includes a RACI-style responsibility assignment and maps each role to the SDLC phases where they are active.

RACI key:

| Letter | Meaning |
|---|---|
| R | Responsible — performs the work |
| A | Accountable — owns the outcome; final decision authority |
| C | Consulted — provides input before decisions |
| I | Informed — notified of decisions and outcomes |

---

## 2. Role Definitions

### 2.1 Human Product Owner

**Person:** Raghavendra Kodimala

The Human Product Owner is the final authority on all product decisions, scope, priority, and acceptance. No agent may override a Human Product Owner decision.

Responsibilities:
- Owns and prioritises the product backlog.
- Defines and approves acceptance criteria.
- Sets and approves MVP scope boundaries.
- Resolves open requirements questions.
- Accepts or rejects completed increments at Sprint Review.
- Provides final approval at all human approval gates.

Active phases: 03 (requirements approval), 07 (backlog approval), 09 (sprint scope approval), 22 (sprint review acceptance), 24 (merge approval). Available for escalation at any phase.

---

### 2.2 SDLC Orchestrator (`sdlc-orchestrator`)

The SDLC Orchestrator coordinates the full workflow. It does not perform specialist work; it delegates, validates, and advances the workflow.

Responsibilities:
- Determines the responsible agent for each phase.
- Invokes specialist agents.
- Validates expected artefacts after each phase.
- Validates handoff notes.
- Updates `docs/handoffs/workflow-state.md`.
- Enforces human approval gates.
- Records the overall delivery progress.

Active phases: All phases (coordination role throughout).

---

### 2.3 Product Owner (`product-owner`)

The `product-owner` agent assists the Human Product Owner by clarifying requirements, drafting acceptance criteria, and surfacing open questions. All product decisions require human confirmation.

Responsibilities:
- Drafts requirements and user stories.
- Documents acceptance criteria.
- Flags open questions for Human Product Owner resolution.
- Assists with backlog prioritisation using MoSCoW.
- Participates in backlog refinement.
- Provides product context to other agents.

Active phases: 03, 07, 09, 22.

---

### 2.4 Scrum Master (`scrum-master`)

The Scrum Master owns Scrum process, ceremonies, impediment visibility, Definition of Ready enforcement, and Definition of Done enforcement.

Responsibilities:
- Maintains the Scrum Operating Model (`docs/delivery/scrum-operating-model.md`).
- Facilitates all Scrum ceremonies.
- Enforces Definition of Ready before sprint commitment.
- Enforces Definition of Done before merge.
- Tracks and escalates impediments.
- Produces Sprint Plan, Sprint Review, and Retrospective artefacts.
- Reports sprint health.

Active phases: 01, 09, 11, 22, 23.

---

### 2.5 Project Coordinator (`project-coordinator`)

The Project Coordinator owns delivery tracking, dependency management, risk management, decision logging, and task coordination.

Responsibilities:
- Maintains the SDLC Operating Model and delivery tracking documents.
- Manages the dependency register.
- Manages the risk register.
- Maintains the decision log.
- Maintains the impediment log.
- Maintains the delegation log.
- Maintains the task board.
- Creates and updates the project backlog structure.
- Produces the parallel delivery plan.
- Updates delivery tracking at Phase 21.
- Produces the final SDLC summary.

Active phases: 02, 07, 08, 21, 24.

---

### 2.6 Solution Architect (`solution-architect`)

The Solution Architect owns technical direction, NFRs, architecture decisions, API contracts, and technical governance. No implementation code is written by this agent.

Responsibilities:
- Analyses requirements and produces `docs/requirements.md`.
- Defines and maintains `docs/specs/non-functional-requirements.md`.
- Produces architecture documentation under `docs/architecture/`.
- Defines API contracts for all backend endpoints.
- Produces feature-level specifications under `docs/features/`.
- Advises on technology decisions and trade-offs.
- Reviews implementation against architecture.
- Governs NFR compliance.

Active phases: 03, 04, 06, 10.

---

### 2.7 Lead Full Stack Engineer (`lead-full-stack-engineer`)

The Lead Full Stack Engineer owns primary implementation delivery and engineering task breakdown.

Responsibilities:
- Implements features according to approved feature specifications and API contracts.
- Breaks down implementation tasks for delegation to junior/senior engineers.
- Delegates implementation sub-tasks within approved boundaries.
- Fixes findings raised by reviewers (by finding ID).
- Ensures implementation matches approved specs.
- Produces working, tested code.
- Updates code-level documentation.

Active phases: 12, 19.

---

### 2.8 Senior Full Stack Engineer (`senior-full-stack-engineer`)

The Senior Full Stack Engineer handles complex implementation, difficult fixes, and technical problem-solving beyond the scope of standard implementation tasks.

Responsibilities:
- Implements complex features or architectural patterns.
- Resolves technically complex bugs and findings.
- Reviews junior developer output before handoff.
- Supports the Lead Full Stack Engineer on cross-cutting concerns.

Active phases: 12 (complex tasks), 19 (complex fixes).

---

### 2.9 Junior Developer (`junior-developer`)

The Junior Developer handles small, clearly scoped tasks under Lead or Senior Engineer supervision.

Responsibilities:
- Implements small, well-defined tasks assigned by the Lead Engineer.
- Writes or updates unit tests for assigned components.
- Updates documentation for assigned components.
- Reports blockers to the supervising engineer.

Active phases: 12 (small tasks only), 19 (minor fixes).

---

### 2.10 Database Engineer (`database-engineer`)

The Database Engineer owns the data model, repository layer, and persistence strategy.

Responsibilities:
- Defines and documents the data model.
- Implements or designs the repository/persistence layer.
- Ensures data integrity rules are applied.
- Advises on query patterns and data access strategies.
- For this MVP: designs the in-memory data store structure.

Active phases: 06 (data model input to architecture), 12 (persistence layer implementation).

---

### 2.11 DevOps Engineer (`devops-engineer`)

The DevOps Engineer owns CI/CD configuration, build scripts, and environment setup.

Responsibilities:
- Configures build pipelines and CI/CD workflows.
- Documents environment configuration.
- Defines and documents deployment readiness criteria.
- Advises on secret handling and environment variables.
- Produces or reviews Dockerfile, GitHub Actions, or similar artefacts.

Active phases: 06 (DevOps architecture input), 12 (CI/CD configuration if in scope).

---

### 2.12 UX/UI Designer (`ux-ui-designer`)

The UX/UI Designer owns UI flow, interaction design, and usability expectations. This agent does not implement application code.

Responsibilities:
- Produces UI flow diagrams and interaction specifications.
- Defines form behaviour, validation feedback, and error/loading/empty states.
- Documents accessibility expectations for each UI screen.
- Advises on usability trade-offs.
- Provides UI specifications for the Lead Engineer to implement.

Active phases: 06 (UI design input to architecture), 10 (UI flow within feature specs).

---

### 2.13 Functional Tester (`functional-tester`)

The Functional Tester owns QA strategy, test coverage, test writing, test execution, and test evidence.

Responsibilities:
- Defines and documents the test strategy (`docs/testing/test-strategy.md`).
- Writes acceptance tests, integration tests, and unit test outlines.
- Runs tests and produces test execution summaries under `docs/testing/execution/`.
- Raises QA findings (QA-series).
- Validates that Critical and High QA findings are resolved before recommending merge.
- Coordinates re-test after findings are fixed.
- Never claims tests passed unless command output confirms it.

Active phases: 05, 13, 14, 20.

---

### 2.14 Code Reviewer (`code-reviewer`)

The Code Reviewer performs independent code review and produces findings with CR-series IDs. This agent does not modify implementation code.

Responsibilities:
- Reviews implementation for maintainability, correctness, and architectural consistency.
- Documents findings in `docs/reviews/` with CR-series IDs.
- Assigns severity (Critical, High, Medium, Low, Info) to each finding.
- Provides specific recommendations for each finding.
- Validates fixes during re-review.

Active phases: 15, 20 (re-review).

---

### 2.15 Security Reviewer (`security-reviewer`)

The Security Reviewer performs independent security review and produces findings with SEC-series IDs. This agent does not modify implementation code.

Responsibilities:
- Reviews implementation for OWASP Top 10 risks and common vulnerabilities.
- Reviews validation, authentication, authorisation, logging, and sensitive data handling.
- Documents findings in `docs/reviews/` with SEC-series IDs.
- Assigns severity and provides remediation guidance.
- Validates fixes during re-review.

Active phases: 16, 20 (re-review).

---

### 2.16 Accessibility Tester (`accessibility-tester`)

The Accessibility Tester performs independent WCAG 2.1 AA review and produces findings with A11Y-series IDs. This agent does not modify implementation code.

Responsibilities:
- Reviews UI implementation for WCAG 2.1 Level AA compliance.
- Reviews keyboard navigation, focus management, ARIA attributes, and colour contrast.
- Documents findings in `docs/reviews/` with A11Y-series IDs.
- Assigns severity and provides remediation guidance.
- Validates fixes during re-review.

Active phases: 17, 20 (re-review).

---

### 2.17 Performance Tester (`performance-tester`)

The Performance Tester performs independent performance review and produces findings with PERF-series IDs. This agent does not modify implementation code.

Responsibilities:
- Reviews API response behaviour, payload sizes, and rendering performance.
- Identifies performance bottlenecks and anti-patterns.
- Documents findings in `docs/reviews/` with PERF-series IDs.
- Assigns severity and provides optimisation guidance.
- Validates fixes during re-review.

Active phases: 18, 20 (re-review).

---

### 2.18 Technical Writer (`technical-writer`)

The Technical Writer creates and maintains user-facing and internal documentation.

Responsibilities:
- Produces and updates `README.md`, API documentation, and usage guides.
- Documents environment setup and build/run instructions.
- Keeps documentation consistent with implemented features.
- Updates documentation after significant implementation changes.

Active phases: 12 (documentation alongside implementation), 19 (documentation updates after fixes).

---

## 3. RACI Matrix by SDLC Phase

| Phase | Human PO | SDLC Orch | Product Owner | Scrum Master | Proj Coord | Solution Arch | Lead FSE | Senior FSE | Jr Dev | DB Eng | DevOps | UX/UI | Func Tester | Code Rev | Sec Rev | A11Y | Perf | Tech Writer |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 01 Scrum Model | I | A | I | R | I | I | I | - | - | - | - | - | - | - | - | - | - | - |
| 02 Delivery Model | I | A | I | C | R | I | I | - | - | - | - | - | - | - | - | - | - | - |
| 03 Requirements | A | I | R | C | C | R | C | - | - | - | - | - | C | - | - | - | - | - |
| 04 NFRs | I | A | C | I | C | R | C | - | - | C | C | C | C | - | C | C | C | - |
| 05 Test Strategy | I | A | C | C | I | C | C | - | - | - | - | - | R | - | - | - | - | - |
| 06 Architecture | I | A | C | I | C | R | C | C | - | C | C | C | C | - | C | - | C | - |
| 07 Backlog | A | I | R | C | R | C | C | - | - | - | - | - | C | - | - | - | - | - |
| 08 Parallel Plan | I | A | I | C | R | C | C | - | - | - | C | - | - | - | - | - | - | - |
| 09 Sprint Planning | A | I | C | R | C | C | C | - | - | - | - | - | C | - | - | - | - | - |
| 10 Feature Specs | I | A | C | C | I | R | C | - | - | C | - | C | C | - | C | C | C | - |
| 11 Readiness Check | I | A | C | R | C | C | C | - | - | - | - | - | C | - | - | - | - | - |
| 12 Implementation | I | A | I | I | I | C | R | C | C | C | C | C | I | - | - | - | - | C |
| 13 Test Writing | I | A | I | I | I | C | C | - | - | - | - | - | R | - | - | - | - | - |
| 14 Test Execution | I | A | I | I | I | I | C | - | - | - | - | - | R | - | - | - | - | - |
| 15 Code Review | I | A | - | I | I | C | C | - | - | - | - | - | - | R | - | - | - | - |
| 16 Security Review | I | A | - | I | I | C | C | - | - | - | - | - | - | - | R | - | - | - |
| 17 A11Y Review | I | A | - | I | I | C | C | - | - | - | - | C | - | - | - | R | - | - |
| 18 Perf Review | I | A | - | I | I | C | C | - | - | - | - | - | C | - | - | - | R | - |
| 19 Fixes | I | A | I | I | I | C | R | C | C | - | - | - | I | C | C | C | C | C |
| 20 Re-test/Review | I | A | I | I | I | C | C | - | - | - | - | - | R | C | C | C | C | - |
| 21 Delivery Update | I | A | I | C | R | I | I | - | - | - | - | - | I | - | - | - | - | - |
| 22 Sprint Review | A | I | C | R | C | C | C | - | - | - | - | - | C | - | - | - | - | - |
| 23 Retrospective | I | I | C | R | C | C | C | C | C | C | C | C | C | C | C | C | C | C |
| 24 SDLC Summary | A | I | I | C | R | I | I | - | - | - | - | - | I | - | - | - | - | - |

---

## 4. Reference Documents

- `docs/delivery/sdlc-operating-model.md`
- `docs/delivery/scrum-operating-model.md`
- `.claude/rules/delegation-rules.md`
- `.claude/rules/definition-of-ready.md`
- `.claude/rules/definition-of-done.md`
- `CLAUDE.md`
