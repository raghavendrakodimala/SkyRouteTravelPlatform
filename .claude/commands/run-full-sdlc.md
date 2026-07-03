---
description: Run the full SDLC workflow in strict phased mode, committing and merging each phase before starting the next.
argument-hint: "[project/scope] [--phased] [--auto-commit-merge] [--no-push]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite, Task
---

Run the full SDLC workflow for:

$ARGUMENTS

You are the SDLC Orchestrator.

This command must run in strict phased mode.

Do not complete the whole SDLC in one branch.

Do not create one large uncommitted change set.

Each phase must be completed, committed, merged into `main`, and closed before the next phase starts.

---

## Required User Approval Interpretation

If `$ARGUMENTS` includes:

```text
--auto-commit-merge
```

treat that as explicit approval for the orchestrator to run these Git commands for this workflow only:

```bash
git status
git diff --stat
git log --oneline -n 10
git switch main
git switch -c <phase-branch>
git add .
git commit -m "<phase commit message>"
git merge --no-ff <phase-branch> -m "<phase merge message>"
git branch -d <phase-branch>
```

Do not push unless `$ARGUMENTS` includes:

```text
--push-approved
```

If `--auto-commit-merge` is not present, stop before every commit and merge and ask the human user for approval.

---

## Mandatory Rule

Before starting, read:

- `CLAUDE.md`
- `.claude/rules/phased-execution.md`
- `.claude/rules/delegation-rules.md`
- `.claude/rules/tool-safety.md`
- `.claude/rules/review-and-test-reporting.md`
- `docs/handoffs/workflow-state.md` if it exists
- `docs/handoffs/current-handoff.md` if it exists

---

## Preflight

Before Phase 01:

1. Run:

```bash
git status
```

2. If working tree is not clean, stop and report what must be committed/stashed/resolved.

3. Confirm current branch is `main`.

4. If not on `main`, switch to `main` only if safe:

```bash
git switch main
```

5. Do not run `git pull` unless the user explicitly requested remote sync.

---

## Scope Slug

Create a scope slug from `$ARGUMENTS`.

Examples:

- `SkyRoute MVP` -> `skyroute-mvp`
- `Trip Search MVP` -> `trip-search-mvp`

Use the slug in branch names.

---

## Phase Transaction Procedure

For each phase below:

1. Switch to `main`.

```bash
git switch main
```

2. Create the phase branch.

```bash
git switch -c sdlc/<phase-number>-<phase-name>-<scope-slug>
```

3. Run only the current phase.

4. Invoke the required specialist agent or agents.

5. Verify expected artifacts.

6. Create/update:

```text
docs/handoffs/current-handoff.md
docs/handoffs/workflow-state.md
docs/handoffs/handoff-index.md
```

7. If tasks were delegated, update:

```text
docs/delivery/delegation-log.md
```

8. Run safe validation if relevant.

9. Check status:

```bash
git status
git diff --stat
```

10. If no files changed, create/update a phase handoff/status note so the phase result is recorded.

11. Commit:

```bash
git add .
git commit -m "<phase commit message>"
```

12. Merge to main:

```bash
git switch main
git merge --no-ff sdlc/<phase-number>-<phase-name>-<scope-slug> -m "<phase merge message>"
```

13. Delete completed branch:

```bash
git branch -d sdlc/<phase-number>-<phase-name>-<scope-slug>
```

14. Continue to the next phase.

---

## Phase List

### Phase 01 — Scrum Operating Model

Branch:

```text
sdlc/01-scrum-operating-model-<scope-slug>
```

Primary agent:

- scrum-master

Create/update:

- `docs/delivery/scrum-operating-model.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 01 scrum operating model
```

Merge message:

```text
merge: complete phase 01 scrum operating model
```

---

### Phase 02 — SDLC Delivery Model

Branch:

```text
sdlc/02-delivery-model-<scope-slug>
```

Primary agent:

- project-coordinator

Create/update:

- `docs/delivery/sdlc-operating-model.md`
- `docs/delivery/roles-and-responsibilities.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/risk-register.md`
- `docs/delivery/decision-log.md`
- `docs/delivery/impediment-log.md`
- `docs/delivery/delegation-log.md`
- `docs/delivery/task-board.md`
- `docs/delivery/raci-matrix.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 02 delivery model
```

Merge message:

```text
merge: complete phase 02 delivery model
```

---

### Phase 03 — Requirements Analysis

Branch:

```text
sdlc/03-requirements-analysis-<scope-slug>
```

Primary agents:

- solution-architect
- product-owner if clarification is needed

Create/update:

- `docs/requirements.md`
- `docs/handoffs/`

Do not invent requirements.

If source requirements are insufficient, stop and ask the human user for clarification.

Commit message:

```text
docs: complete phase 03 requirements analysis
```

Merge message:

```text
merge: complete phase 03 requirements analysis
```

---

### Phase 04 — Non-Functional Requirements

Branch:

```text
sdlc/04-nfr-specification-<scope-slug>
```

Primary agent:

- solution-architect

Create/update:

- `docs/specs/non-functional-requirements.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 04 non-functional requirements
```

Merge message:

```text
merge: complete phase 04 non-functional requirements
```

---

### Phase 05 — Test Strategy

Branch:

```text
sdlc/05-test-strategy-<scope-slug>
```

Primary agent:

- functional-tester

Create/update:

- `docs/testing/test-strategy.md`
- `docs/testing/acceptance-test-plan.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 05 test strategy
```

Merge message:

```text
merge: complete phase 05 test strategy
```

---

### Phase 06 — Architecture Planning

Branch:

```text
sdlc/06-architecture-planning-<scope-slug>
```

Primary agent:

- solution-architect

Supporting agents if needed:

- database-engineer
- devops-engineer
- ux-ui-designer
- security-reviewer
- accessibility-tester
- performance-tester
- lead-full-stack-engineer
- functional-tester

Create/update:

- `docs/architecture/architecture-overview.md`
- `docs/architecture/api-strategy.md`
- `docs/architecture/backend-architecture.md`
- `docs/architecture/frontend-architecture.md`
- `docs/architecture/persistence-strategy.md`
- `docs/architecture/testing-strategy.md`
- `docs/delivery/decision-log.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 06 architecture planning
```

Merge message:

```text
merge: complete phase 06 architecture planning
```

---

### Phase 07 — Project Backlog

Branch:

```text
sdlc/07-project-backlog-<scope-slug>
```

Primary agents:

- project-coordinator
- product-owner if priority clarification is needed

Create/update:

- `docs/delivery/project-backlog.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/risk-register.md`
- `docs/delivery/task-board.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 07 project backlog
```

Merge message:

```text
merge: complete phase 07 project backlog
```

---

### Phase 08 — Parallel Delivery Plan

Branch:

```text
sdlc/08-parallel-delivery-plan-<scope-slug>
```

Primary agent:

- project-coordinator

Create/update:

- `docs/delivery/parallel-delivery-plan.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/delegation-log.md`
- `docs/delivery/task-board.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 08 parallel delivery plan
```

Merge message:

```text
merge: complete phase 08 parallel delivery plan
```

---

### Phase 09 — Sprint Planning

Branch:

```text
sdlc/09-sprint-planning-<scope-slug>
```

Primary agent:

- scrum-master

Supporting agents if needed:

- project-coordinator
- product-owner
- lead-full-stack-engineer
- functional-tester
- solution-architect

Create/update:

- `docs/delivery/sprint-plan.md`
- `docs/delivery/impediment-log.md`
- `docs/delivery/task-board.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 09 sprint planning
```

Merge message:

```text
merge: complete phase 09 sprint planning
```

---

### Phase 10 — Feature-Level Specifications

Branch:

```text
sdlc/10-feature-specifications-<scope-slug>
```

Primary agents:

- solution-architect
- ux-ui-designer if UI is involved
- functional-tester

Create/update as applicable:

- `docs/features/<feature>/delivery-plan.md`
- `docs/features/<feature>/api-contract.md`
- `docs/features/<feature>/ui-flow.md`
- `docs/features/<feature>/test-plan.md`
- `docs/features/<feature>/accessibility-review.md`
- `docs/handoffs/`

Do not implement code.

Commit message:

```text
docs: complete phase 10 feature specifications
```

Merge message:

```text
merge: complete phase 10 feature specifications
```

---

### Phase 11 — Spec Readiness Check

Branch:

```text
sdlc/11-spec-readiness-check-<scope-slug>
```

Primary agent:

- scrum-master

Supporting agents if needed:

- solution-architect
- functional-tester
- project-coordinator
- lead-full-stack-engineer

Create/update:

- `docs/delivery/spec-readiness-check.md`
- `docs/delivery/impediment-log.md`
- `docs/handoffs/`

Implementation is allowed to start only if readiness is Approved or explicitly approved by human user as Conditionally Approved.

Commit message:

```text
docs: complete phase 11 spec readiness check
```

Merge message:

```text
merge: complete phase 11 spec readiness check
```

---

### Phase 12 — Implementation

Branch:

```text
sdlc/12-implementation-<scope-slug>
```

Primary agent:

- lead-full-stack-engineer

Delegation allowed to:

- senior-full-stack-engineer
- junior-developer
- database-engineer
- devops-engineer
- functional-tester
- technical-writer

Create/update:

- source files
- implementation-supporting docs if needed
- `docs/handoffs/`
- `docs/delivery/delegation-log.md` if delegated

Before committing implementation:

- run safe build/type-check/lint/test commands if available and appropriate.
- if build fails due implementation issue, fix within this phase before merge.
- if dependency installation is needed, stop for approval.

Commit message:

```text
feat: complete phase 12 implementation
```

Merge message:

```text
merge: complete phase 12 implementation
```

---

### Phase 13 — Test Writing

Branch:

```text
sdlc/13-test-writing-<scope-slug>
```

Primary agent:

- functional-tester

Supporting agents if needed:

- lead-full-stack-engineer
- senior-full-stack-engineer

Create/update:

- automated test files
- test data files
- `docs/features/<feature>/test-plan.md` if needed
- `docs/handoffs/`

Commit message:

```text
test: complete phase 13 test writing
```

Merge message:

```text
merge: complete phase 13 test writing
```

---

### Phase 14 — Test Execution Summary

Branch:

```text
sdlc/14-test-execution-<scope-slug>
```

Primary agent:

- functional-tester

Create/update:

- `docs/testing/execution/<scope-slug>-test-execution-summary.md`
- `docs/handoffs/`

Run safe test commands if possible.

Never claim tests passed unless command output confirms it.

If tests fail, document QA findings and route next work to fixes.

Commit message:

```text
docs: complete phase 14 test execution summary
```

Merge message:

```text
merge: complete phase 14 test execution summary
```

---

### Phase 15 — Code Review

Branch:

```text
sdlc/15-code-review-<scope-slug>
```

Primary agent:

- code-reviewer

Create/update:

- `docs/reviews/code-review-<scope-slug>.md`
- `docs/handoffs/`

Do not fix code in this phase.

Commit message:

```text
docs: complete phase 15 code review
```

Merge message:

```text
merge: complete phase 15 code review
```

---

### Phase 16 — Security Review

Branch:

```text
sdlc/16-security-review-<scope-slug>
```

Primary agent:

- security-reviewer

Create/update:

- `docs/reviews/security-review-<scope-slug>.md`
- `docs/handoffs/`

Do not fix code in this phase.

Commit message:

```text
docs: complete phase 16 security review
```

Merge message:

```text
merge: complete phase 16 security review
```

---

### Phase 17 — Accessibility Review

Branch:

```text
sdlc/17-accessibility-review-<scope-slug>
```

Primary agent:

- accessibility-tester

Create/update:

- `docs/reviews/accessibility-review-<scope-slug>.md`
- `docs/handoffs/`

If no UI is involved, create a Not Applicable review note and handoff.

Do not fix code in this phase.

Commit message:

```text
docs: complete phase 17 accessibility review
```

Merge message:

```text
merge: complete phase 17 accessibility review
```

---

### Phase 18 — Performance Review

Branch:

```text
sdlc/18-performance-review-<scope-slug>
```

Primary agent:

- performance-tester

Create/update:

- `docs/reviews/performance-review-<scope-slug>.md`
- `docs/testing/performance/` if needed
- `docs/handoffs/`

If not applicable, create a Not Applicable review note and handoff.

Do not run heavy load tests without approval.

Commit message:

```text
docs: complete phase 18 performance review
```

Merge message:

```text
merge: complete phase 18 performance review
```

---

### Phase 19 — Findings Fixes

Branch:

```text
sdlc/19-findings-fixes-<scope-slug>
```

Primary agent:

- lead-full-stack-engineer

Supporting agents as needed:

- senior-full-stack-engineer
- junior-developer
- database-engineer
- devops-engineer
- functional-tester
- technical-writer

Create/update:

- source files
- tests if needed
- review report statuses if needed
- `docs/handoffs/`
- `docs/delivery/delegation-log.md` if delegated

Fix findings by ID:

- `QA-*`
- `CR-*`
- `SEC-*`
- `A11Y-*`
- `PERF-*`

Do not introduce unrelated changes.

Commit message:

```text
fix: complete phase 19 findings fixes
```

Merge message:

```text
merge: complete phase 19 findings fixes
```

---

### Phase 20 — Re-test and Re-review

Branch:

```text
sdlc/20-retest-rereview-<scope-slug>
```

Primary agents:

- functional-tester
- code-reviewer if code findings were fixed
- security-reviewer if security findings were fixed
- accessibility-tester if accessibility findings were fixed
- performance-tester if performance findings were fixed

Create/update:

- `docs/testing/execution/<scope-slug>-retest-summary.md`
- relevant review reports
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 20 retest and rereview
```

Merge message:

```text
merge: complete phase 20 retest and rereview
```

---

### Phase 21 — Delivery Tracking Update

Branch:

```text
sdlc/21-delivery-tracking-<scope-slug>
```

Primary agent:

- project-coordinator

Create/update:

- `docs/delivery/project-backlog.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/risk-register.md`
- `docs/delivery/decision-log.md`
- `docs/delivery/impediment-log.md`
- `docs/delivery/delegation-log.md`
- `docs/delivery/task-board.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 21 delivery tracking update
```

Merge message:

```text
merge: complete phase 21 delivery tracking update
```

---

### Phase 22 — Sprint Review

Branch:

```text
sdlc/22-sprint-review-<scope-slug>
```

Primary agent:

- scrum-master

Create/update:

- `docs/delivery/sprint-review.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 22 sprint review
```

Merge message:

```text
merge: complete phase 22 sprint review
```

---

### Phase 23 — Retrospective

Branch:

```text
sdlc/23-retrospective-<scope-slug>
```

Primary agent:

- scrum-master

Create/update:

- `docs/delivery/retrospective.md`
- `docs/delivery/impediment-log.md`
- `docs/delivery/task-board.md`
- `docs/handoffs/`

Commit message:

```text
docs: complete phase 23 retrospective
```

Merge message:

```text
merge: complete phase 23 retrospective
```

---

### Phase 24 — Final SDLC Completion Summary

Branch:

```text
sdlc/24-final-summary-<scope-slug>
```

Primary agents:

- project-coordinator
- technical-writer

Create/update:

- `docs/delivery/final-sdlc-summary-<scope-slug>.md`
- `docs/delivery/merge-summary-<scope-slug>.md`
- `docs/handoffs/workflow-state.md`
- `docs/handoffs/current-handoff.md`
- `docs/handoffs/handoff-index.md`

Commit message:

```text
docs: complete phase 24 final SDLC summary
```

Merge message:

```text
merge: complete phase 24 final SDLC summary
```

---

## Dynamic Routing

If Phase 14 test execution produces failing QA findings, route to Phase 19 fixes before final completion.

If Phase 15 to Phase 18 reviews produce findings, route to Phase 19 fixes.

If Phase 19 fixes occur, Phase 20 re-test/re-review is mandatory.

If no findings exist, Phase 19 may be recorded as Not Applicable and Phase 20 may be recorded as Not Applicable.

---

## End Condition

The workflow is complete only when:

- all applicable phases are completed,
- every completed phase was committed,
- every completed phase was merged into `main`,
- no phase branch remains,
- workflow state says `Completed`,
- final summary exists.

Do not push unless explicitly approved.
