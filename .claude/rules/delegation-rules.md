# Delegation Rules

Delegation simulates real IT team task distribution while keeping ownership, traceability, and quality gates clear.

Only agents with `Task` permission may actively invoke other agents.

Agents without `Task` may still create handoff notes, findings, reports, or requests.

---

## Delegation Authority

### SDLC Orchestrator

May delegate to any agent to move the SDLC workflow forward.

### Product Owner

May request impact analysis from:

- Project Coordinator
- Solution Architect
- Scrum Master
- Functional Tester

Must not directly assign implementation work.

### Scrum Master

May request:

- status updates
- readiness checks
- blocker updates
- ceremony inputs
- Definition of Ready evidence
- Definition of Done evidence

Must not directly assign technical implementation scope.

### Project Coordinator

May request:

- delivery updates
- dependency updates
- risk updates
- estimates
- task tracking updates
- action ownership

Must not change product priority without Product Owner approval.

Must not change architecture without Solution Architect approval.

### Solution Architect

May request specialist input from:

- Database Engineer
- DevOps Engineer
- UX/UI Designer
- Accessibility Tester
- Security Reviewer
- Performance Tester
- Lead Full Stack Engineer
- Functional Tester

Must not directly implement production code.

### Functional Tester

May request:

- accessibility validation
- security validation
- performance validation
- developer fixes through QA findings
- re-test support

Must not change expected behavior without Product Owner/spec update.

### Lead Full Stack Engineer

May delegate implementation subtasks to:

- Senior Full Stack Engineer
- Junior Developer
- Database Engineer
- DevOps Engineer
- Functional Tester
- Technical Writer

Must not change scope, architecture, dependencies, or delete files without approval.

### Review Finding → Developer Agent Routing

During the Iterative Review-Fix Loop (Code/Security/Accessibility/Performance Review, see `.claude/rules/phased-execution.md` and `CLAUDE.md` §22), the orchestrator routes each `Open` finding to a developer agent as follows:

- **Lead Full Stack Engineer** — architectural/design gaps, production-integrity issues, and any Critical or High severity finding regardless of apparent size.
- **Senior Full Stack Engineer** — complex, cross-file, or concurrency-sensitive work, and changes to key business logic (pricing, booking, aggregation).
- **Junior Developer** — trivial, mechanical fixes that mirror an existing proven pattern elsewhere in the codebase (e.g. adding a validation rule identical in shape to a sibling validator).

A developer agent fixes the finding (source + tests) and records evidence in a handoff note. It never edits the review report itself, and never sets a finding to `Accepted Risk`, `Deferred`, or `Rejected` — those statuses require human approval recorded by the reviewer, except when a Low/informational finding's own original review text already stated no fix was required (the reviewer may mark that `Accepted Risk` directly, without a fresh human round-trip).

### Senior Full Stack Engineer

May delegate small, clearly scoped tasks to:

- Junior Developer

Must review junior output before handoff.

### DevOps Engineer

May request support from:

- Lead Full Stack Engineer
- Functional Tester
- Technical Writer

Must not deploy, publish, change secrets, or delete files without approval.

### UX/UI Designer

May request:

- accessibility review
- Product Owner feedback
- Lead Engineer feasibility input

Must not implement application code unless explicitly instructed.

---

## Agents That Should Not Actively Delegate

These agents generally produce findings, reports, handoffs, or requests instead of invoking other agents:

- Code Reviewer
- Security Reviewer
- Accessibility Tester
- Performance Tester
- Junior Developer
- Database Engineer
- Technical Writer

---

## Required Records

Every delegated task must be recorded in:

- `docs/delivery/delegation-log.md`

Every delegated task must produce or update:

- `docs/handoffs/`

---

## Delegation Brief Format

Every delegation must include:

- Delegation ID
- From agent
- To agent
- Objective
- Context files
- Expected artifacts
- Acceptance criteria
- Constraints
- Target phase/date
- Required handoff file

---

## No Circular Delegation

An agent must not delegate the same task back to the delegating agent unless clarification is required.

If circular dependency appears, escalate to SDLC Orchestrator.

---

## Human Approval Gates

Delegation must stop for human approval before:

- scope change
- priority change with delivery impact
- new dependency
- destructive command
- file deletion
- deployment
- secret changes
- unresolved Critical/High risk acceptance
