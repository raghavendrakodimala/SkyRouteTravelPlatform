---
description: Create feature-level specs before implementation.
argument-hint: "[feature name/context]"
allowed-tools: Read, Write, Edit, MultiEdit, Grep, Glob, LS, WebFetch, WebSearch, TodoWrite, Task
---

Create feature-level specs for:

$ARGUMENTS

Use relevant specialist agents.

Create/update:

- `docs/features/<feature>/delivery-plan.md`
- `docs/features/<feature>/api-contract.md` if API involved
- `docs/features/<feature>/ui-flow.md` if UI involved
- `docs/design/<feature>-design-spec.md` if UI involved (ux-ui-designer; must reach Approved before implementation — `.claude/rules/ui-ux-quality-gates.md`)
- `docs/features/<feature>/test-plan.md`
- `docs/features/<feature>/accessibility-review.md` if UI involved
- `docs/handoffs/`

Do not implement code.
