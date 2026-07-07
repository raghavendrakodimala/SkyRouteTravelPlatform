# SDLC Rules

The project follows Scrum with spec-driven delivery gates.

## Phase Order

1. Scrum setup
2. SDLC operating model
3. Requirements analysis
4. NFR specification
5. Test strategy
6. Architecture
7. Backlog
8. Parallel delivery plan
9. Sprint planning
10. Feature specs
11. Spec readiness check
12. Implementation
13. Test writing
14. Test execution
15. Reviews
16. Fixes
17. Re-test/re-review
18. Delivery tracking
19. Sprint review
20. Retrospective
21. Merge

Reviews (step 15) run an Iterative Review-Fix Loop internally — findings are routed to a developer agent, fixed, and re-reviewed until zero `Open` remain, before that review is merged. Fixes (step 16) is a consolidation step for `QA-*` findings and anything a loop could not close, not the default venue for review findings. See `.claude/rules/phased-execution.md` and `CLAUDE.md` §22.

## Mandatory Gates

Implementation must not start unless selected work has:

- requirement/user story
- acceptance criteria
- required specs
- test scenarios
- dependencies and risks tracked
- Definition of Ready status

## Definition of Done

Work is done only when:

- implementation matches approved specs
- tests are added/updated
- test execution summary exists
- reviews are completed
- Critical/High findings are resolved or accepted by the human user
- documentation is updated
- delivery tracking is updated
- handoff notes are complete
