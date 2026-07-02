---
description: Run the complete SDLC workflow using orchestrated custom agents.
argument-hint: "[project/scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, TodoWrite, Task
---

Run the full SDLC workflow for:

$ARGUMENTS

Act as the SDLC Orchestrator.

Proceed through these phases:

1. Scrum operating model
2. SDLC delivery model
3. Requirements analysis
4. NFR specification
5. Test strategy
6. Architecture planning
7. Project backlog
8. Parallel delivery plan
9. Sprint planning
10. Feature specs
11. Spec readiness check
12. Implementation
13. Test writing
14. Test execution summary
15. Code review
16. Security review
17. Accessibility review if UI is involved
18. Performance review if relevant
19. Fix findings
20. Re-test/re-review
21. Delivery tracking update
22. Sprint review
23. Retrospective

For each phase:

- invoke the responsible agent
- verify artifacts
- require handoff
- update workflow state
- identify next responsible agent

Stop only for:

- missing requirements
- human approval needed
- dependency installation
- destructive action
- deployment
- unresolved Critical/High finding requiring acceptance
- merge approval

Do not commit, merge, push, or delete branches unless explicitly instructed.
