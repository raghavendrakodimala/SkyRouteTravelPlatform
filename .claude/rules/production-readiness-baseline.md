# Production-Readiness Baseline

Owner concept: this file owns the standing production-readiness baseline ("requirements are floors, not ceilings"), the waiver mechanism for baseline items, and the Red-Team Product Review gate.

Requirements are floors, not ceilings.

Written requirements and specs define the minimum a delivery must do — they never define the maximum a professional team ships. The baseline below applies to EVERY delivery, regardless of whether any requirement, spec, or backlog item mentions it.

Every baseline item must be either:

- IMPLEMENTED, or
- EXPLICITLY WAIVED as a dated entry in `docs/delivery/decision-log.md` (what is waived, why, who approved).

Silent omission of a baseline item is a defect, with the same standing as a failed acceptance criterion.

---

## API/Service Baseline

- OpenAPI specification (latest format) plus an interactive reference UI.
- Health-check endpoint performing real dependency checks — not a bare static 200.
- API versioning strategy, with a default version mapped.
- Global routing conventions — no per-controller prefix duplication.
- Consistent error contract across all endpoints.
- Security headers on responses.

## Data Baseline

- Relational data model designed BEFORE entities/DTOs exist (database-engineer, Phase 06 per `CLAUDE.md` §7).
- Traceability from every DTO and domain entity back to that model.

## Frontend/UX Baseline

- Production visual bar: the full Production Layout Checklist in `ui-ux-quality-gates.md`, including long-content stress and reflow.
- Format hints on every format-validated field — the user must never have to guess an expected format.
- Empty, error, and loading states demonstrably reachable through real usage, not only asserted in unit tests.
- Input-domain breadth: the product works for the whole valid input space, not just fixture-known paths.

## Engineering Hygiene Baseline

- Coherent workspace layout.
- Every tsconfig/lint/build config actually runs clean standalone.
- No stray artifacts or duplicate trees in the repository.
- Dependency vulnerability warnings (e.g. NU1903) treated as findings, not noise.

---

## Red-Team Product Review

Before any PO demo checkpoint, and before phase closure of any user-facing work, one agent runs a red-team pass — role-playing a demanding client/senior engineer:

- Reviewer by subject: code-reviewer for API/service subjects; ux-ui-designer for UI subjects.
- Uses the RUNNING product with arbitrary and hostile inputs — not the happy path.
- Walks every screen (for UI) or exercises every endpoint (for API).
- Applies this baseline plus the Production Layout Checklist (`ui-ux-quality-gates.md`).
- Files findings routed through the Iterative Review-Fix Loop (`phased-execution.md`, `CLAUDE.md` §22) with the normal finding-ID scheme.

Evidence = what was exercised, with the real inputs used and the real outputs observed. A pass without exercised inputs/outputs is not a red-team pass.

A demo without a preceding red-team pass is incomplete.

---

## Closing Principle

"Before any handoff, every agent asks: what would a demanding senior human engineer point out here? If the answer is known, fix it before handing off — the human PO must never be the one to find it."
