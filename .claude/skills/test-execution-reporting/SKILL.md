---
name: test-execution-reporting
description: Use when running tests or creating QA execution summaries.
---

# Test Execution Reporting Skill

Store summaries under `docs/testing/execution/`.

Required summary content: `.claude/rules/review-and-test-reporting.md` (Branch, Commit hash, Environment, Commands executed, Results by test area, Failed tests, Evidence, Defects, Risks, QA recommendation). QA finding IDs: `QA-*`.

Rules:

- Run tests in non-watch/single-run mode only.
- Never claim tests passed without command output.
- Report exact counts (passed/failed/skipped) taken from the output — no estimates or "all passing" without numbers.
- If tests cannot run, state: tests not run, the reason, and the recommended command.
