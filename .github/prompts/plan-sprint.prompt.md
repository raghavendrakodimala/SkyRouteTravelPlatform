---
agent: "agent"
description: "Plan a SkyRoute sprint with sprint goal, selected backlog items, parallel work tracks, dependencies, and risks."
tools: ["search/codebase", "search", "search/changes", "read/problems","vscodeGeneral/problems"]
---

# Plan Sprint

Act as the Scrum Master and Project Coordinator for SkyRoute.

## Goal

Create a realistic sprint plan using the current requirements, architecture, backlog, dependency register, risk register, and current repository state.

## Rules

- Do not invent requirements.
- Do not select work that is not ready unless clearly marked as needing refinement.
- Ensure every selected story has acceptance criteria.
- Ensure testing, accessibility, security, DevOps, and documentation are planned from the start.
- Identify work that can proceed in parallel.
- Identify blockers before sprint commitment.
- Treat the sprint as production-intended.

## Planning Inputs

Review when available:

- `docs/requirements.md`
- `docs/architecture/architecture-overview.md`
- `docs/delivery/project-backlog.md`
- `docs/delivery/parallel-delivery-plan.md`
- `docs/delivery/dependency-register.md`
- `docs/delivery/risk-register.md`
- `docs/testing/acceptance-test-plan.md`

## Output Format

Return:

1. Sprint goal
2. Proposed sprint scope
3. Selected backlog items
4. Definition of Ready status
5. Parallel work tracks
6. Backend tasks
7. Frontend tasks
8. Testing and automation tasks
9. Accessibility tasks
10. Security review tasks
11. DevOps tasks
12. Documentation tasks
13. Dependencies and blockers
14. Risks and mitigations
15. Definition of Done checklist
16. Recommended first actions