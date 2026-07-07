# Review and Test Reporting Rules

Owner concept: this file owns the finding-ID scheme and the required content of review reports and test execution summaries.

Review reports are stored under:

- `docs/reviews/`

Test execution summaries are stored under:

- `docs/testing/execution/`

Finding IDs:

- `CR-001` for code review
- `SEC-001` for security review
- `A11Y-001` for accessibility review
- `PERF-001` for performance review
- `QA-001` for testing

Review findings must include:

- ID
- Severity
- File or area
- Evidence
- Impact
- Recommendation
- Required fix
- Status

Test execution summaries must include:

- Branch
- Commit hash if available
- Test environment
- Commands executed
- Result by test area
- Failed tests
- Evidence/output summary
- Defects
- Risks
- Final QA recommendation

Developers must fix findings by ID.

Reviewers must not modify implementation code unless explicitly instructed.
