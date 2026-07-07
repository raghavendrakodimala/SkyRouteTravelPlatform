---
description: Create accessibility review report.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, Bash, WebFetch, WebSearch, TodoWrite
---

Use the accessibility-tester agent.

Review accessibility for:

$ARGUMENTS

If no UI is involved, create a Not Applicable review note and handoff, and stop.

Create/update:

- `docs/reviews/accessibility-review-<scope>.md` — `A11Y-*` findings per `.claude/rules/review-and-test-reporting.md`
- `docs/handoffs/<phase>-loop-log.md` and `docs/handoffs/current-handoff.md`

Check WCAG 2.2 AA, semantic HTML, labels, keyboard navigation, focus states, validation messages, screen reader behavior, and responsive behavior.

Evidence rules (`.claude/rules/ui-ux-quality-gates.md` §3):

- Quote the offending code with file and line.
- Include rendered-app evidence — a live walkthrough or screenshots of the running app at 360px, 768px, and 1280px, stating what was rendered and observed. Code reading alone cannot file or resolve UI findings; this applies to re-verification passes too.

This is not a findings-only pass. Drive the Iterative Review-Fix Loop (`.claude/rules/phased-execution.md`):

1. File findings, all `Open`.
2. Orchestrator routes each `Open` finding to a developer agent per the routing table in `.claude/rules/delegation-rules.md`.
3. Developer fixes source and tests; the developer never edits the review report.
4. The same accessibility-tester re-verifies against the rendered app and updates statuses.
5. Repeat until the report shows zero `Open` findings.

The PO visual demo checkpoint (`.claude/rules/ui-ux-quality-gates.md` §4) must be complete before UI review phases close.

Do not modify application code — fixes go through the loop.
