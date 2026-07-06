# Test Strategy and Acceptance Test Plan — SkyRoute Travel Platform MVP

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | TEST-STRAT-001 |
| Version | 1.1 |
| Date | 2026-07-06 |
| Status | Approved — v1.1 updates automated E2E approach (Section 1.4), superseding manual-only E2E per Human PO approval; see Section 12 Changelog |
| Owner | functional-tester |
| Source | `docs/requirements.md` v1.4 (Approved), `docs/specs/non-functional-requirements.md` v1.0 |
| Phase | Phase 05 — Test Strategy and Acceptance Planning |
| Governance | `.claude/rules/review-and-test-reporting.md`, `.claude/rules/definition-of-ready.md`, `.claude/rules/definition-of-done.md`, CLAUDE.md Section 13 |

This document defines **how** SkyRoute MVP will be tested. It does not reopen, alter, or reinterpret any approved requirement, business rule, or NFR target. Where a numeric target in `docs/specs/non-functional-requirements.md` is marked `[PROPOSED — requires PO confirmation]`, this strategy references it as-is and does not treat it as a hard gate until confirmed (per NFR spec Section 17). NFR-TEST-005 (80% backend service-layer coverage) is treated per the task brief as **proposed and confirmed by Human PO** for planning purposes in this document; the orchestrator should verify this confirmation is recorded before Phase 14 treats it as a gate.

No test code, `.spec.ts` files, or `.cs` test files are created in this phase. This is Phase 05 (strategy); test authoring occurs in Phase 13 (Test Writing).

---

## 1. Test Levels and Scope

### 1.1 Unit Tests (Backend — Service-Layer Logic)

Scope: pure business logic, isolated from HTTP context, persistence technology, and cross-cutting concerns, consistent with DP-017–DP-020 (testability requirements).

| Area | Target Classes/Methods | Business Rule / Requirement |
|---|---|---|
| Pricing — GlobalAir | `GlobalAirProvider` pricing method (base fare × 1.15, round to 2dp) | BR-001, FR-011, FR-051, DP-006, DP-019 |
| Pricing — BudgetWings | `BudgetWingsProvider` pricing method (max(base fare × 0.90, 29.99), round to 2dp) | BR-002, FR-011, FR-051, DP-006, DP-019 |
| Booking reference generation | `GenerateBookingReference(routeType)` — format `SKY-[INT/DOM]-XXXXXX`, uniqueness/collision regeneration | BR-004, DP-018, NFR-DATA-001 |
| Aggregation / fault isolation | `IFlightAggregatorService` implementation — concurrent invocation (`Task.WhenAll`), exception containment per provider, merge of surviving results | BR-007, FR-007–FR-009, FR-049, FR-050, DP-001, DP-004, NFR-AVAIL-002, NFR-TEST-006 |
| Route-type / document-type determination | Route-type utility/service method (domestic vs international by airport country equality) determining document type (Passport vs National ID) | BR-003, DP-016, NFR-DATA-004 |
| Total price recomputation (booking time) | `IBookingService` implementation — server-side total price = per-passenger price × passenger count, independent of client-submitted total | BR-006, FR-044, NFR-DATA-002 |
| Passenger record integrity | `IBookingService` / `IBookingStore` implementation — n passengers produce n passenger records | BR-005, FR-040, FR-043, NFR-DATA-003 |

Unit tests must substitute dependencies with mocks/stubs/fakes via constructor injection (DP-017) — no HTTP context, no real persistence, no real provider call.

### 1.2 Integration Tests (Backend — API Endpoint Contract)

Scope: ASP.NET Core endpoint behavior including model binding, validation pipeline, controller-to-service wiring, and error shaping — verifying the contract, not re-verifying unit-level pricing math.

| Area | Endpoint | Requirement |
|---|---|---|
| Search — happy path | `POST /search` (or equivalent) | FR-001, FR-007–FR-010 |
| Search — validation errors | `POST /search` | FR-002–FR-006, FR-061, FR-063 |
| Search — provider fault isolation (end-to-end through controller) | `POST /search` | BR-007, FR-009, FR-050, FR-070, NFR-AVAIL-002 |
| Booking — happy path | `POST /booking` | FR-039, FR-042–FR-044 |
| Booking — validation errors | `POST /booking` | FR-041, FR-062–FR-065 |
| Airports (if implemented as endpoint) | `GET /airports` | FR-054, FR-056–FR-059 |
| Error shaping | All endpoints | FR-066–FR-069, BR-011, NFR-SEC-002 |

### 1.3 Frontend Component/Service Tests (Angular TestBed)

Scope: Angular services (HTTP-calling) tested via `TestBed` with `HttpClientTestingModule` per DP-020; components tested for presentation logic, state transitions (loading/empty/error), and delegation to services (not direct `HttpClient` usage, per DP-010/DP-PROTOCOL-006).

| Area | Target | Requirement |
|---|---|---|
| Flight search service | `FlightSearchService` — HTTP call via `HttpClientTestingModule`, no direct `HttpClient` in components | DP-020, DP-010, NFR-TEST-004 |
| Booking service | `BookingService` (Angular) — HTTP call via `HttpClientTestingModule` | DP-020, DP-010, NFR-TEST-004 |
| Total price calculation utility | Single-source total price function/service (DP-011) | DP-011, US-002 AC2–4 |
| Search form component | Validation states, submit-disabled states, airport selection guard (same-airport rejection) | US-001, FR-002, FR-004–FR-006 |
| Results component | Loading/empty/error states, sort re-ordering without API call | US-002, US-003, FR-013–FR-024 |
| Booking form component | Per-passenger sections, document label/validation switch by route type, submit-disabled on invalid state | US-005, FR-028–FR-031 |
| Confirmation component | Booking reference display, passenger list, non-resubmittable state | US-006, FR-034–FR-038 |

### 1.4 Automated E2E Acceptance Testing (Playwright) — Primary Mechanism as of v1.1

**Superseded (v1.1, 2026-07-06):** the original v1.0 posture below described E2E coverage as manual/exploratory only, specifically to avoid introducing an automated E2E tool without Product Owner approval (tracked as open question `QA-STRAT-OQ-002`, Section 10). **The Human Product Owner explicitly approved introducing Playwright for automated E2E testing on 2026-07-06**, including approval to run the associated `npm install`/`npx playwright install` commands and to run the real backend (`dotnet run`) and real frontend dev server (`ng serve`) locally for the sole purpose of executing the suite. This resolves `QA-STRAT-OQ-002` (see Section 10) and makes automated E2E via Playwright the **primary** E2E mechanism for MVP, superseding the manual-only approach for all coverage that can be automated.

Scope: full user-journey walkthroughs against each of the 8 user stories' acceptance criteria, executed against the real running application (real ASP.NET Core backend, real Angular dev server, no mocking of the application itself). This is the acceptance-level test pass that confirms the assembled system — not just individual units/integrations — satisfies each AC as written. Implemented at `frontend/playwright.config.ts` and `frontend/e2e/*.spec.ts` (Phase 13 extension, 2026-07-06); execution results are recorded in `docs/testing/execution/`.

Primary automated journeys (implemented):
1. Search → Results → Sort → Select → Book → Passenger Details → Confirm → Confirmation (happy path, single passenger, domestic route) — `frontend/e2e/full-journey-domestic.spec.ts`.
2. Same journey, multi-passenger (3 passengers), international route — `frontend/e2e/full-journey-international.spec.ts`.
3. Zero-result search → empty state; search API failure → generic error; booking API failure → generic error — `frontend/e2e/error-states.spec.ts`. **Note:** the fixed mock-provider schedule (ASM-006) means no valid search request can structurally produce a zero-result response or a real backend 500 through the UI (this document's own Section 5.2 language in the source feature spec already acknowledges the empty-array case is "never in practice" under the current fixed-mock-data design) — these three scenarios use Playwright's `page.route(...).fulfill(...)` to substitute the HTTP *response* for exactly the call under test, exercising the real, already-implemented frontend handling of that response shape without disabling or bypassing any frontend code. This is a deliberate, documented, narrowly-scoped exception to "no mocking" and is called out again inline in that spec file.
4. Validation-blocked submission attempts (search form same-airport guard, booking form passenger-section gating) — `frontend/e2e/search-form.spec.ts`, `frontend/e2e/booking-validation.spec.ts`.
5. Results persistence across in-app navigation without a new search — `frontend/e2e/results-persistence.spec.ts`.

Manual/exploratory E2E walkthroughs (Section 1.4 as originally written) remain a **documented fallback/supplement** — useful for ad hoc exploratory checks and for any future scenario not yet automated — but are no longer the primary acceptance-level mechanism now that the automated suite exists and has been executed at least once (see `docs/testing/execution/`).

---

## 2. Traceability Matrix — User Stories to Test Levels and Scenarios

| User Story | Test Level(s) | Representative Test Scenarios |
|---|---|---|
| US-001 — Search for Available Flights | Frontend component (search form), Integration (search validation), Manual E2E | "US-001-AC3: passenger count input rejects 0 and 10, accepts 1 and 9"; "US-001-AC5: submit button disabled while any required field invalid"; "US-001-AC8: same origin/destination selection blocked at form level"; "US-001-AC9: past departure date blocked at form level"; "US-001-AC6: loading indicator shown during search call (manual E2E)" |
| US-002 — View Flight Search Results | Unit (pricing feeding display data), Frontend component (results display), Manual E2E | "US-002-AC2: total price = per-passenger price × passenger count, displayed to 2 decimal places"; "US-002-AC6: search returns zero results shows empty state message"; "US-002-AC7: search API failure shows user-facing error without backend detail"; "US-002-AC8: results remain displayed until new search initiated (manual E2E)" |
| US-003 — Sort Flight Results | Frontend component (sort logic), Manual E2E | "US-003-AC2: selecting a sort option re-orders results without a new HTTP call (spy/mock asserts zero additional requests)"; "US-003-AC5: default sort on initial display is price low-to-high"; "US-003-AC3: active sort option visually indicated (manual E2E)" |
| US-004 — Select a Flight and Initiate Booking | Frontend component (navigation/state carry), Manual E2E | "US-004-AC4: booking screen renders selected flight summary without an additional API call (route state/service carries data)"; "US-004-AC3: price breakdown shows per-passenger price, passenger count, and total consistently" |
| US-005 — Enter Passenger Details | Unit (route-type/document-type determination), Frontend component (booking form validation), Manual E2E | "US-005-AC5: document field label is 'Passport Number' for international route, 'National ID' for domestic route"; "US-005-AC6: passport validation accepts 6–9 uppercase alphanumeric, rejects 5 and 10 chars"; "US-005-AC6: national ID validation accepts 5–20 alphanumeric with hyphens, rejects 4 and 21 chars"; "US-005-AC10: form submission blocked when any passenger section is invalid" |
| US-006 — Confirm Booking and Receive Reference | Unit (booking reference generation, total price recomputation), Integration (booking endpoint), Manual E2E | "US-006-AC4: booking reference format matches `SKY-[INT\|DOM]-XXXXXX`, 14 characters"; "US-006-AC6: booking API failure shows user-facing error without internal detail"; "US-006-AC7: confirmation screen cannot be resubmitted without navigating back (manual E2E)" |
| US-007 — Provider Extensibility | Unit (aggregation/fault isolation), Integration (search endpoint under provider failure), Manual E2E (not typically exercisable manually — covered primarily by automated levels) | "US-007-AC3: aggregation layer queries all registered providers concurrently (assert via timing/mock call count)"; "US-007-AC4: one provider throws, search still returns 200 with the other provider's results and no 500"; "US-007-AC5: each result carries a provider field identifying its source" |
| US-008 — View Airport Selection | Frontend component (airport dropdown), Manual E2E | "US-008-AC1: each airport entry displays code, city, and country"; "US-008-AC2: at least 6 airports across at least 2 countries are present in the static list"; "US-008-AC4: same airport cannot be selected as both origin and destination" |

---

## 3. Test Data Strategy

- **Mock provider data (GlobalAir, BudgetWings):** per ASM-006, both providers return a **fixed internal schedule** regardless of the requested date/route — the mock data is not filtered by date or route. Test scenarios must therefore assert against the known, fixed set of flights each provider returns for a given cabin class, rather than asserting date-specific or route-specific result content. Base fares used by each provider's hardcoded flight set are treated as known constants for the purposes of pricing assertions (e.g., a documented base fare of $100.00 for a given GlobalAir flight is expected to yield $115.00 per BR-001).
- **Deterministic pricing assertions:** unit tests for BR-001/BR-002 use fixed, hand-picked base fare inputs (not the full mock flight set) to test the pricing method in isolation — including the BudgetWings floor boundary cases explicitly called out in the NFR spec (base fare $25.00 → $29.99; base fare $30.00 → $29.99) and the GlobalAir rounding example ($87.50 → $100.63).
- **Static airport list:** the airport list (minimum 6 airports, at least 2 countries, at least 2 airports sharing a country per FR-056–FR-059) is treated as fixed test fixture data. Test scenarios reference specific known airport pairs to exercise both routing branches:
  - Domestic pair (e.g., LHR–MAN, both United Kingdom) — exercises National ID document-type branch (BR-003).
  - International pair (e.g., LHR–JFK, United Kingdom vs United States) — exercises Passport document-type branch (BR-003).
- **Provider fault injection:** for BR-007/FR-009/FR-050 scenarios, a test double (stub/fake) implementing `IFlightProvider` is substituted for one real provider and configured to throw; the other real (or another stubbed) provider returns its normal fixed data. No modification to the actual `GlobalAirProvider`/`BudgetWingsProvider` mock data is required to exercise this scenario.
- **Passenger record test data:** boundary passenger counts (1 and 9) are used to test array-length integrity (NFR-DATA-003) without requiring 9 distinct realistic identities — synthetic but valid-format names/emails/document numbers are sufficient.
- **No external test data source:** since ASM-007 confirms providers make no external HTTP calls, no network mocking library (e.g., WireMock) is required — provider substitution is done via the `IFlightProvider` interface (DP-001) directly, which is the seam the architecture already provides for this purpose.

---

## 4. Coverage Targets

- **Reference target:** NFR-TEST-005 — 80% line coverage for backend service-layer classes, proposed by the NFR spec and confirmed by the Human PO for planning purposes.
- **What "service-layer" means concretely** for this coverage target (in scope):
  - `IFlightProvider` implementations (`GlobalAirProvider`, `BudgetWingsProvider`), including their pricing methods.
  - `IFlightAggregatorService` implementation (aggregation/orchestration, concurrent invocation, fault isolation).
  - `IBookingService` implementation (booking creation orchestration, total price recomputation, route-type re-evaluation at booking time).
  - `IBookingStore` implementation (`InMemoryBookingStore`) — persistence operations, uniqueness/collision handling for booking references.
  - Supporting named business-logic methods called out in DP-005/DP-006/DP-016/DP-018/DP-019 (route-type determination, booking reference generation) regardless of which class they physically reside in.
- **Explicitly out of scope for the line-coverage percentage** (validated by other means instead):
  - Controllers — validated primarily through **integration tests** (Section 1.2) confirming request/response contract and status codes, not raw coverage percentage.
  - DTOs / domain model POCOs — no behavior to cover; validated by contract/serialization checks incidental to integration tests.
  - Angular components and services — validated primarily through **component/service tests** (Section 1.3) using `TestBed`, not a backend-style coverage percentage. If an Angular coverage tool is run (`ng test --code-coverage`), it is informational, not gated in this MVP.
  - `Program.cs` / startup/DI wiring — not business logic; excluded per NFR-TEST-005's own exclusion note.
- **Measurement mechanism (Phase 14):** `dotnet test --collect:"XPlat Code Coverage"` (or equivalent), scoped to the service-layer classes above, reviewed in the Phase 14 Test Execution Summary. Running this command requires human approval per Section 8 below — if approval is not granted before the Phase 14 deadline, the summary will record "tests not run" with reason and recommended command per `.claude/rules/review-and-test-reporting.md`.

---

## 5. Validation-Rule Test Scenarios (Boundary/Edge Cases)

Covers FR-002–FR-006 (search validation) and FR-061–FR-065 (validation rule detail), all authoritative on the backend (FR-060, DP-014) with frontend mirroring as a convenience layer.

| Rule | Requirement | Boundary/Edge Cases to Test |
|---|---|---|
| Origin ≠ Destination | FR-002, FR-061 | Identical origin/destination code → 400; distinct valid codes → accepted |
| Departure date not in past | FR-003, FR-061 | Yesterday's date → 400; today's date → accepted; future date → accepted |
| Passenger count 1–9 | FR-004, FR-061 | 0 → 400; 10 → 400; 1 → accepted; 9 → accepted; non-integer/negative → 400 |
| Cabin class enum | FR-005, FR-061 | Unrecognised string (e.g., "PremiumEconomy") → 400; "Economy"/"Business"/"First Class" → accepted |
| Trip type = OneWay | FR-005b | Absent value → 400; "RoundTrip" or other value → 400; "OneWay" → accepted |
| Known airport codes | FR-006, FR-061 | Unknown 3-letter code not in static list → 400; known code → accepted; malformed code (not 3 uppercase alpha) → 400 |
| Full name required, min 2 chars, letter required | FR-029, FR-062, FR-064 | Empty string → 400; 1 char → 400; 2 chars → accepted; numeric-only name (e.g., "12") → 400; name with at least one letter → accepted |
| Email format | FR-029, FR-062, FR-065 | Missing "@" → 400; missing domain → 400; well-formed address → accepted |
| Passport document number (international) | US-005 AC6, FR-062 | 5 chars → 400 (below 6 min); 6 chars uppercase alphanumeric → accepted; 9 chars → accepted; 10 chars → 400 (above 9 max); lowercase letters → 400; embedded space → 400 |
| National ID document number (domestic) | US-005 AC6, FR-062 | 4 chars → 400 (below 5 min); 5 chars → accepted; 20 chars → accepted; 21 chars → 400 (above 20 max); hyphen included → accepted; embedded space → 400 |
| Passenger count matches array length | FR-062 | Declared count 2 but 3 passenger records submitted → 400; matching count → accepted |
| Structured validation error body | FR-063 | Any of the above 400 cases → response body identifies failing field(s) and reason |

---

## 6. Provider Fault Isolation Test Scenario (BR-007 / FR-009 / FR-050)

Explicit scenario to prove provider fault isolation, at both the unit level (aggregation service in isolation) and the integration level (through the controller):

**Scenario: "US-007-AC4 / BR-007: one provider throws, the other's results are still returned, no 500 error"**

1. Register two `IFlightProvider` implementations with the aggregation service under test: a normal stub/fake returning a fixed, known set of flight results, and a second stub/fake configured to throw an exception when invoked (simulating a `GlobalAirProvider`- or `BudgetWingsProvider`-style failure).
2. Invoke the aggregation method (`IFlightAggregatorService` implementation) directly (unit level) with a valid search request.
3. Assert: the returned aggregate result set contains only the surviving provider's flights (throwing provider's contribution is treated as an empty collection, per FR-050); no exception propagates out of the aggregation call; a log entry is recorded containing the failing provider's name and exception message/type (NFR-AVAIL-003, NFR-OBS-001).
4. Repeat at the integration level: call `POST /search` through the full pipeline with the same provider configuration wired via DI substitution; assert HTTP 200 (not 500), a non-empty results array from the surviving provider, and a `provider` field present on each result (FR-008, FR-052).
5. Negative check: assert the response body and status code contain no indication that a provider failed (FR-070 — provider failures are silently degraded, not surfaced to the end user as an error).

This scenario is required automated coverage per NFR-TEST-006 (not left to manual verification only) and is authored in Phase 13.

---

## 7. Non-Functional Validation Approach

The table below covers each NFR category in `docs/specs/non-functional-requirements.md` whose assigned Validation Method is **test** (unit test, integration test, or manual timing check functioning as a lightweight test), as distinct from **code review** or **manual/structural check** categories, which are out of scope for this document and owned by the relevant review phase (15/16/17/18).

| NFR Category | Representative NFR IDs | How/When Exercised |
|---|---|---|
| Performance | NFR-PERF-001, NFR-PERF-002 | Lightweight local timing check during Phase 14 execution — browser DevTools Network tab timing or a `Stopwatch`-based integration test assertion, run against local mock-data conditions (single developer machine, ~20 sequential requests). **No dedicated load-testing tool is used** — this is an MVP; heavy load testing tooling requires explicit human approval per `.claude/rules/tool-safety.md` and is not planned for this delivery. |
| Performance | NFR-PERF-003 (sort re-render), NFR-PERF-004 (airport dropdown) | Manual check using browser DevTools Performance panel during functional/UI testing in Phase 14; optionally a bounded-assertion Angular component test (e.g., asserting sort completes within a fixed number of change-detection cycles) authored in Phase 13. |
| Performance | NFR-PERF-005 (aggregation overhead bounded by slowest provider) | Primarily a code-review check (confirming `Task.WhenAll` usage per FR-049); supplemented by a manual timing comparison only if a third mock provider is introduced during testing — not a dedicated benchmark. |
| Reliability/Availability | NFR-AVAIL-002, NFR-AVAIL-003 | Automated unit/integration test — the provider fault isolation scenario in Section 6 above, authored in Phase 13, executed in Phase 14. |
| Security | NFR-SEC-001 (input validation completeness) | Automated unit tests — one test per validation rule enumerated in Section 5 above, authored in Phase 13. |
| Testability | NFR-TEST-001 through NFR-TEST-004, NFR-TEST-006 | Automated unit/component tests as described in Sections 1.1–1.3; confirmed present at Phase 13/14 test-suite review (not a separate NFR-specific test run — satisfied as a byproduct of the unit/integration/component test suite itself). |
| Testability | NFR-TEST-005 (80% coverage) | Coverage report (`dotnet test --collect:"XPlat Code Coverage"`) generated and reviewed at Phase 14 — see Section 4 above. |
| Data Integrity | NFR-DATA-001 through NFR-DATA-004 | Automated unit/integration tests — booking reference uniqueness/regeneration, server-side total price recomputation, passenger-array length integrity, server-side route-type re-evaluation — authored in Phase 13, executed in Phase 14. |

NFR categories whose validation method is code review, manual/structural check, or "reviewable" only (Scalability, most of Security's "must not preclude later" group, Privacy structural checks, Accessibility [owned by Phase 17], Maintainability, Observability, Compatibility, Deployability, On-Premise/Cloud Readiness) are intentionally **not** re-planned as test scenarios in this document — they remain owned by their designated review phase, consistent with `docs/specs/non-functional-requirements.md` Section 19 (Validation Method Summary).

---

## 8. Test Execution Environment

- Running actual `npm`/`ng test`/`dotnet test`/`dotnet build` commands requires **explicit human approval** per `.claude/rules/tool-safety.md` and `.claude/rules/common-agent-rules.md` — this is already logged as open impediment **IMP-001** in `docs/handoffs/workflow-state.md` (Severity: High, "will block Phase 14").
- **Phase 14 (Test Execution Summary)** will require the orchestrator to obtain explicit user approval before any test/build command is run. This strategy document does not run, and must not run, any such command in Phase 05.
- **Fallback if approval is not granted in time:** per `.claude/rules/review-and-test-reporting.md` and CLAUDE.md Section 13, the Phase 14 Test Execution Summary must state: "Tests not run," the reason (pending human approval per IMP-001), and the recommended command(s) to run (e.g., `dotnet test`, `dotnet test --collect:"XPlat Code Coverage"`, `ng test --watch=false`, `ng test --watch=false --code-coverage`). A partial execution summary using only manually-observed evidence (e.g., a manual E2E walkthrough log) may still be recorded where it does not depend on the blocked commands.
- No test/build command has been executed as part of producing this strategy document.

---

## 9. Definition of Ready / Definition of Done — Testing Checkpoints

This document does not redefine the Definition of Ready or Definition of Done (owned by `.claude/rules/definition-of-ready.md` and `.claude/rules/definition-of-done.md`). It confirms the testing-specific checkpoints already established there apply to every backlog item created from Phase 07 onward:

**Definition of Ready — testing-relevant checkpoints confirmed applicable:**
- Acceptance criteria are documented (already true for all 8 user stories in `docs/requirements.md`).
- Required test approach is defined — satisfied for MVP scope by this document; each backlog item at Phase 07 should reference the relevant row(s) of the Section 2 traceability matrix.
- NFR impact is understood — satisfied by Section 7's mapping of NFR categories with a "test" validation method to backlog-item-level test scenarios.

**Definition of Done — testing-relevant checkpoints confirmed applicable:**
- Tests are added or updated (Phase 13).
- Test execution summary exists (Phase 14), following the required format in `.claude/rules/review-and-test-reporting.md`.
- Critical and High findings (QA-* IDs) are resolved or explicitly accepted by the Human PO before merge.

No changes are made to the DoR/DoD rule files themselves.

---

## 10. Open Questions and Risks

| ID | Item | Status |
|---|---|---|
| QA-STRAT-OQ-001 | Whether NFR-TEST-005's 80% coverage target is formally recorded as PO-confirmed (this document assumes confirmation per the task brief; the orchestrator should verify this against the actual PO confirmation record before Phase 14 treats it as a gate). | Open — non-blocking for Phase 05 |
| QA-STRAT-OQ-002 | Whether a dedicated E2E automation tool (Playwright/Cypress) will be introduced in a future sprint; MVP relies on manual/exploratory E2E per Section 1.4 to avoid an unapproved new dependency. | **Resolved 2026-07-06** — Human PO explicitly approved introducing Playwright for MVP, including the `npm install`/`npx playwright install` commands and local server startup for test execution. Automated E2E is now implemented and is the primary E2E mechanism per Section 1.4 (v1.1). See `docs/handoffs/13c-functional-tester-to-sdlc-orchestrator-e2e-test-writing.md`. |
| RISK — IMP-001 dependency | Phase 14 test execution is blocked pending human approval to run `dotnet`/`npm`/`ng` commands. This strategy's coverage targets and scenario counts cannot be confirmed as *passing* until that approval is granted and commands are run. | Open — tracked as IMP-001 in `docs/handoffs/workflow-state.md` |

No requirement, business rule, or NFR decision was reopened in producing this document.

---

## 12. Document Changelog

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-07-03 | Initial test strategy and acceptance test plan (Phase 05). |
| 1.1 | 2026-07-06 | Phase 13 extension: Section 1.4 updated to make automated E2E via Playwright the primary E2E mechanism for MVP, per explicit Human PO approval resolving `QA-STRAT-OQ-002` (Section 10). Manual/exploratory E2E retained as a documented fallback/supplement. No requirement, business rule, or NFR decision reopened; no other section altered. |

---

*End of Test Strategy and Acceptance Test Plan v1.1.*
