# Corrective Actions — PO Process Review, 2026-07-07

Source: harsh, specific Product Owner review of the multi-agent SDLC system after project closure. Four confirmed failures. Each is recorded here with its root cause and the structural fix that makes it impossible to repeat. All fixes are process-level edits to `.claude/agents/`, `.claude/rules/`, and `.claude/commands/` — no source code was changed by this corrective-action pass (the one shipped defect had already been fixed separately; see "Immediate Defect Fix" below).

---

## Finding 1 — No designed data model; database-engineer never invoked early

**PO finding (substance):** The database-engineer agent was never invoked in the early phases to design a relational data model. Entities and DTOs were written ad hoc by implementing engineers and were never derived from a designed schema.

**Root cause:** Phase 06 (Architecture Planning) listed the database-engineer only as an optional supporting agent ("if needed"), and no phase artifact list contained a data-model deliverable. Nothing in spec readiness or the engineers' quality bars required entities/DTOs to trace to a designed model, so the gap was invisible to every downstream gate.

**Structural fix:**

| File | Change |
|---|---|
| `.claude/agents/database-engineer.md` | New Phase 06 duty: design the relational data model FIRST as `docs/architecture/data-model.md` (entities, attributes, keys, relationships, mermaid ERD, in-memory persistence mapping note; designed as if a relational DB will back it). Quality bar: every API DTO and domain entity must be derivable from this model. |
| `.claude/agents/lead-full-stack-engineer.md` | Quality bar: entities and DTOs must trace to the approved data model; implementing a data-bearing story without one is a spec-readiness violation — stop and route to database-engineer/solution-architect. |
| `.claude/commands/create-architecture-plan.md` | `docs/architecture/data-model.md` added to the Phase 06 artifact list (owner: database-engineer, delegated by solution-architect); architecture is not complete without it. |
| `.claude/commands/spec-readiness-check.md` | Readiness for any data-bearing story now requires the approved data model — missing or unapproved model means not Ready. |
| `.claude/commands/run-full-sdlc.md` | Phase 06 artifact list now names the database-engineer data-model deliverable and states Phase 06 is not complete without it. |

**Retroactive deliverable:** `docs/architecture/data-model.md` is being produced retroactively (in parallel with this corrective-action pass) by the database-engineer, documenting the model the shipped entities/DTOs imply and reconciling any drift.

---

## Finding 2 — First-pass UI quality far below production standard

**PO finding (substance):** The ux-ui-designer's first-pass UI was far below production quality. Acceptable quality only arrived after repeated PO complaints, burning review cycles that a real team would not tolerate.

**Root cause:** The designer's quality bar defined coverage (layout, states, responsiveness) but no absolute quality benchmark for the FIRST deliverable, so "meets the checklist minimally" passed as done and the PO feedback loop became the de facto quality mechanism.

**Structural fix:**

| File | Change |
|---|---|
| `.claude/agents/ux-ui-designer.md` | Explicit production benchmark on the FIRST design spec: it must pass for a real airline/travel product next to Kayak/Skyscanner — "if it would embarrass in a stakeholder demo, it is a defect." Every control/spec must define long-content/overflow behavior for the longest realistic option/label/value. The designer must render-verify the implementation of their own spec before any handoff; a handoff without this is incomplete. |

---

## Finding 3 — Shipped select-overflow defect survived every review

**PO finding (substance):** A real UI defect shipped and survived every review: native `<select>` elements overflowed their card container (CSS grid `min-width: auto` default plus long option text). The accessibility-tester never tested zoom, reflow, or multiple screen sizes — exactly the conditions where this class of defect is caught.

**Root cause:** The accessibility review protocol required rendered-app evidence only at 360/768/1280 px and never mandated 320 px, zoom equivalence, text resizing, or any overflow detection. Long-content stress was in nobody's checklist, so a defect that only manifests with long option text under constrained width was structurally invisible.

**Structural fix:**

| File | Change |
|---|---|
| `.claude/agents/accessibility-tester.md` | Mandatory viewport/zoom protocol: test at 320/360/768/1024/1280 px widths, AND 400% zoom equivalence (WCAG 1.4.10 reflow, ~320 px CSS viewport), AND 200% text size (WCAG 1.4.4); detect element overflow/clipping at each condition; an automated overflow sweep (elements extending past their parent's box) is required evidence. A review without this protocol is incomplete and cannot resolve findings. |
| `.claude/commands/accessibility-review.md` | The same protocol added to the command's evidence requirements. |
| `.claude/rules/ui-ux-quality-gates.md` | Production Layout Checklist gains two items: (1) "Long-content stress: every control, card, and label verified with the longest realistic data — nothing crosses its container boundary"; (2) "Reflow: no two-dimensional scrolling or clipped/overlapping content at 320px width and 400% zoom (WCAG 1.4.10) and text resized to 200% (WCAG 1.4.4)". |

**Immediate defect fix (already applied to source, separate from this pass):** The select overflow was fixed in `src/UI/src/app/features/search/search-form/search-form.component.css` via `min-width: 0` on the grid items and `width: 100%` on the form controls. Verified in the rendered app at 1280 px and 320 px widths with zero container overflow.

---

## Finding 4 — PO demo gate rubber-stamped a defective UI

**PO finding (substance):** The product-owner agent's demo-gate approval rubber-stamped a defective UI. The gate existed but produced approval without genuine per-screen scrutiny, so it caught nothing.

**Root cause:** The demo gate required that a checkpoint *happen* and be *recorded*, but not that the PO agent apply any concrete acceptance rubric. With no required verdict artifact, a pass-through approval was indistinguishable from a rigorous one.

**Structural fix:**

| File | Change |
|---|---|
| `.claude/agents/product-owner.md` | Demo-gate rigor: the PO agent walks EVERY screen against the full Production Layout Checklist (including the two new long-content-stress and reflow items) and records a per-screen verdict table (screen × checklist item → pass/fail) in the demo checkpoint. It must reject work that fails any checklist item — "approve only what you would ship to real passengers." A PO approval without the recorded per-screen table is invalid. |

---

## Finding 5 — Solution-architect delivered an incomplete architecture and never engaged its specialists

**PO finding (substance):** "The solution-architect should have given a real architecture of the system." The Phase 06 architecture covered code structure, interfaces, and DI seams in depth, but omitted an entire architectural view — the data architecture. No relational model, schema, keys, relationships, or ERD was ever designed; the omission was rationalized as YAGNI (`architecture-plan.md` YAGNI-006/007, "no real database in MVP"). Entities and DTOs were consequently shaped ad hoc from API needs instead of being derived from a designed schema (Finding 1 is the downstream symptom; this is the upstream cause).

**Root cause:** Two-fold. (a) The solution-architect's quality bar defined excellence per-artifact (NFRs, API contracts, decision records) but never defined what a *complete* architecture must cover, so a missing view didn't register as a defect. (b) Delegation authority over the database-engineer existed (`delegation-rules.md` lists it first among the architect's specialists) but was treated as optional — the specialist whose domain was being skipped was simply never invoked.

**Structural fix:**

| File | Change |
|---|---|
| `.claude/agents/solution-architect.md` | Quality bar now defines a real architecture as covering every view — data (relational model via database-engineer, designed BEFORE entities/DTOs exist), runtime/deployment, integration, security, cross-cutting. YAGNI is confined to implementation choices (e.g. no ORM yet) and may never defer design work without PO sign-off. Phase 06 is not complete until the data model exists and every planned DTO/entity is derivable from it. Delegation reframed as a duty: failing to invoke an available specialist whose domain the architecture touches is itself an architecture-phase defect. |

---

## Finding 6 — UI glitches are nobody's explicit QA responsibility

**PO finding (substance):** "All the UI glitches should have been reported by functional-tester." Reported symptom: large whitespace below the footer on the booking screen. Diagnosis: DOM measurement at 360/800 and 1280/900 shows the footer flush with the page bottom on every screen (gap ≤ 0.6px, sub-pixel) — the whitespace in the report was the IDE Launch-preview panel letterboxing a fixed synthetic viewport (set during earlier testing), not a page defect; the preview viewport has been reset to native. The finding's principle stands regardless: had the gap been real, no agent's contract obligated catching it.

**Root cause:** Layout integrity (footer pinning, dead whitespace, overlap) sat between roles — the designer specs it, the accessibility-tester now tests reflow/overflow, but the functional-tester's execution passes had no layout-verification duty at all.

**Structural fix:**

| File | Change |
|---|---|
| `.claude/agents/functional-tester.md` | New quality-bar duty: every test-execution pass over UI work includes a DOM-measured layout-integrity sweep per screen at 360/768/1280px — footer flush with page bottom, no element crossing its container, no horizontal scroll, no overlap — with measured numbers in the execution summary. Visual glitches are `QA-*` defects to file even when design/accessibility reviews also missed them. |

---

## Finding 7 — Same-airport validation error sticks after the user corrects the field

**PO finding (reproduced 1:1):** Select the same origin and destination, fill the other fields, click Search → the "Origin and destination airports must be different." error appears (correct). Change the destination to a different airport, click Search → the error persists and Search stays blocked forever.

**Root cause:** `sameAirportSelected` in `search-form.component.ts` was an Angular `computed()` reading **raw FormControl values**, which are not signals. A computed with zero signal dependencies evaluates once and caches permanently — after the first same-airport submit it cached `true`, and no later form edit could invalidate it, wedging both the template error and the `onSubmit()` guard. The unit suite tested that the error *appears* but never tested the *recovery* path, so the wedge shipped.

**Fixes:**

| File | Change |
|---|---|
| `src/UI/src/app/features/search/search-form/search-form.component.ts` | Bridged form edits into the signal graph: `formValues = toSignal(form.valueChanges, {initialValue: getRawValue()})`; `sameAirportSelected` now derives from it and re-evaluates on every edit. Verified live: error clears the moment the destination is corrected; submit proceeds to /results. |
| `src/UI/src/app/features/search/search-form/search-form.component.spec.ts` | New regression test covering the full recovery path (same-airport submit blocked → correct destination → error gone → submit calls the API exactly once). Suite: 182/182. |
| `.claude/agents/code-reviewer.md` | New quality-bar rule: any `computed()` reading non-signal state (raw FormControl values, DOM, plain fields) is a High finding by default — stale-cache-forever defect class; reactive-form state must be bridged via `toSignal(valueChanges)` or read imperatively in methods. Swept the codebase: this was the only instance. |

---

## Finding 8 — Most origin/destination combinations returned the empty state

**PO finding (2026-07-08, reproduced):** Searching most airport combinations shows "No flights found." Only ~6 of the 30 ordered pairs of the 6 airports had fixture flights after route filtering was introduced — technically correct per route, but violating the challenge PDF's own requirement that each mock return "a realistic set of flight results for **any given search**", and useless for anyone exploring the app.

**Root cause (product + process):** Route filtering was bolted onto fixed 4-flight fixtures without extending coverage to the full input domain — and QA never exercised routes beyond the fixture-known happy paths, so the coverage hole shipped invisible.

**Fixes:**

| File | Change |
|---|---|
| `src/Service/SkyRoute.Infrastructure/Providers/RouteScheduleGenerator.cs` (new) | Pure deterministic generator (no randomness, no clock): a 15-pair symmetric duration table over the 6 airports; for every ordered route a provider's fixtures do not cover, generates 2 flights per provider (GA 07:30/16:45, BW 10:15/21:30; flight numbers 500–559, direction-offset so no number repeats across directions; duration-derived base fares) feeding the existing per-provider pricing pipeline unchanged. Fixture routes byte-identical (LHR→JFK still exactly GA101/BW210). |
| `GlobalAirProvider.cs` / `BudgetWingsProvider.cs` | Compose fixtures + generated schedule; `TryResolveFare` resolves generated flights (SEC-001 booking re-derivation intact). |
| Provider unit tests | Obsolete "unmatched route → empty" tests replaced by: exact deterministic generated output, determinism-across-calls, fixture non-augmentation, all-30-ordered-pairs coverage loop, unknown-code → empty. Application suite 157 → 169. |
| `src/UI/e2e/error-states.spec.ts` | Empty state is now structurally unreachable via any real search — test restored to interception (`page.route` → 200 `[]`). |
| Docs | `feature-provider-aggregation.md` v1.2 (fixtures + generation model), README mock-data trade-off bullet, `data-model.md` Flights-entity source note. |
| `.claude/agents/functional-tester.md` | New duty: input-domain sweep — exercise representative combinations across the whole valid input space (all airport-pair classes, passenger min/max, every cabin), not just fixture-known happy paths. |

**Verification:** live API sweep — 8 representative routes all return 2–4 flights (fixture routes unchanged at 2); repeated identical search returns byte-identical responses; UI walk SYD→DXB shows 4 realistic flights (14h, overnight arrival). Backend suites 169 + 15 green, build clean.

---

## Status

| # | Corrective action | Status |
|---|---|---|
| 1 | Data-model-first process edits (5 files) | Complete (this change set) |
| 1a | Retroactive `docs/architecture/data-model.md` | Complete — `docs/architecture/data-model.md` (DATA-MODEL-001) |
| 2 | Designer first-pass production benchmark | Complete (this change set) |
| 3 | Accessibility viewport/zoom/overflow protocol + checklist items | Complete (this change set) |
| 3a | Select-overflow source fix (`search-form.component.css`) | Complete (prior change, verified at 1280/320 px) |
| 4 | PO demo-gate per-screen verdict table | Complete (this change set) |
| 5 | Solution-architect completeness bar + delegation-as-duty | Complete (this change set) |
| 6 | Functional-tester layout-integrity sweep duty; footer-whitespace report diagnosed as preview-panel letterboxing (page measured flush ≤0.6px at 360/1280) | Complete (this change set) |
| 7 | Same-airport stale-validation fix (`toSignal` bridge) + recovery-path regression test (182/182) + code-reviewer stale-computed sweep rule | Complete (this change set) |
| 8 | Full route coverage via deterministic RouteScheduleGenerator + input-domain QA sweep duty (backend 184/184) | Complete (this change set) |
