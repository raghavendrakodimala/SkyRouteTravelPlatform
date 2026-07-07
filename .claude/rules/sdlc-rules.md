# SDLC Rules

Owner concept: this file owns the top-level SDLC operating rules — Scrum with spec-driven delivery gates. It does not own phase numbering, the Definition of Ready/Done, or spec content requirements; those are cited below.

The project follows Scrum with spec-driven delivery gates.

## Phase Order

The canonical phase numbering — Phase 00 (repository/tooling setup, one-time pre-phase) plus Phases 01–24 — is defined once in `CLAUDE.md` §7. Do not restate or renumber the phase list here or in any other rules file; always cite `CLAUDE.md` §7.

Key structural points (numbering per `CLAUDE.md` §7):

- Phases 15–18 (Code, Security, Accessibility, Performance Review) each run an Iterative Review-Fix Loop internally — findings are routed to a developer agent, fixed, and re-reviewed until zero `Open` remain, before that review phase is merged.
- Phase 19 (Fix review/test findings) is a consolidation step for `QA-*` findings and anything a loop could not close, not the default venue for review findings.
- See `.claude/rules/phased-execution.md` and `CLAUDE.md` §22 for the loop mechanics.

## Mandatory Gates

Implementation must not start unless the selected work satisfies:

- the Definition of Ready in `.claude/rules/definition-of-ready.md`, and
- the spec requirements in `.claude/rules/spec-driven-development.md`.

## Definition of Done

The canonical Definition of Done lives in `.claude/rules/definition-of-done.md`. Work is done only when it satisfies that definition.
