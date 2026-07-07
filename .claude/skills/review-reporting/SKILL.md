---
name: review-reporting
description: Use when creating code, security, accessibility, or performance review reports.
---

# Review Reporting Skill

Store reports under `docs/reviews/`.

Finding fields, ID prefixes (`CR-*`, `SEC-*`, `A11Y-*`, `PERF-*`), and statuses: `.claude/rules/review-and-test-reporting.md` and `CLAUDE.md` §12.

Evidence requirements:

- All reviews: quote the offending code with file and line.
- UI reviews: rendered-app evidence — live walkthrough or screenshots at 360px, 768px, and 1280px, stating what was rendered and observed (`.claude/rules/ui-ux-quality-gates.md` §3). Code reading alone cannot file or resolve UI findings, including re-verification passes.
- Performance reviews: runtime measurements when the app or tests can run safely; no heavy load tests without approval.

Reports drive the Iterative Review-Fix Loop (`.claude/rules/phased-execution.md`): findings are filed `Open`, routed to developer agents per `.claude/rules/delegation-rules.md`, and re-verified by the same reviewer until zero `Open` remain. Only the reviewer updates finding statuses; developer agents never edit the report.
