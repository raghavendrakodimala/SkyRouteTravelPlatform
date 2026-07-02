---
description: Create accessibility review report.
argument-hint: "[scope]"
allowed-tools: Read, Write, Edit, Grep, Glob, LS, WebFetch, WebSearch, TodoWrite
---

Use the accessibility-tester agent.

Review accessibility for:

$ARGUMENTS

Create/update:

- `docs/reviews/accessibility-review-<scope>.md`
- `docs/handoffs/`

Check WCAG 2.2 AA, semantic HTML, labels, keyboard navigation, focus states, validation messages, screen reader behavior, and responsive behavior.

Do not modify application code unless explicitly instructed.
