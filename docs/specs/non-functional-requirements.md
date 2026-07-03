# Non-Functional Requirements Specification — SkyRoute Travel Platform MVP

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | NFR-001 |
| Version | 1.0 |
| Date | 2026-07-03 |
| Status | Draft — Pending Human Product Owner Confirmation of Proposed Numeric Targets |
| Owner | solution-architect |
| Approvers | Product Owner (numeric targets flagged in Section 12 require explicit confirmation) |
| Source | `docs/requirements.md` v1.4 (Approved), specifically Section 3.10 (Design Principles and Architectural Constraints) and Section 5 (Non-Functional Requirements — High-Level) |
| Phase | Phase 04 — Non-Functional Requirements Specification |
| Governance | `.claude/rules/nfr-governance.md`, CLAUDE.md Section 11 |

### Change History

| Version | Date | Author | Change Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | solution-architect | Initial NFR specification, elevating `docs/requirements.md` Section 5 (high-level targets) and Section 3.10 (DP-* architectural constraints) into measurable, prioritized, traceable, testable NFRs across all 14 governance categories. |

---

## 1. Purpose and Scope

This document is the formal, measurable Non-Functional Requirements specification for the SkyRoute MVP, superseding the high-level targets stated in `docs/requirements.md` Section 5 with concrete, testable thresholds. It does not change, reopen, or contradict any approved decision in `docs/requirements.md` v1.4 — including OQ-001–OQ-006, BR-001–BR-013, the pricing rules (BR-001, BR-002), the booking reference format (BR-004), or any DP-* architectural constraint in Section 3.10. This document elevates those existing constraints into NFRs with measurement and validation methods attached.

Scope covers the MVP as defined in `docs/requirements.md` v1.4: a local-only, in-memory-persisted, unauthenticated Angular 22 + ASP.NET Core 10 flight search and booking application. NFRs relating to future-state capability (auth protocols, cloud deployment, database persistence, multi-tenancy, alternative transport protocols) are written as **readiness/addability NFRs** — they specify what the architecture must not preclude, not what must be built now. This mirrors the "Note on YAGNI" pattern already established in `docs/requirements.md` Section 3.10.

## 2. NFR Methodology

Every NFR in this document states, at minimum:

- **ID** — category-prefixed identifier (e.g., `NFR-PERF-001`).
- **Requirement** — the specific, unambiguous statement.
- **Priority** — MoSCoW (Must Have / Should Have / Could Have / Won't Have This Release).
- **Target / Measurement** — a measurable threshold where one is meaningful; marked **[PROPOSED — requires PO confirmation]** if the specific number is new information introduced by this document rather than already stated in `docs/requirements.md`.
- **Validation Method** — how the NFR is verified: unit test, integration test, load test, code review, manual check, or static analysis.
- **Traceability** — the user story (US-*), functional requirement (FR-*), business rule (BR-*), or design principle (DP-*) in `docs/requirements.md` this NFR elevates or depends on.
- **Architecture Link** — the DP-* architectural constraint(s) from `docs/requirements.md` Section 3.10 that structurally enable or govern this NFR.
- **Backlog Link** — marked **"To be linked at Phase 07 Backlog Creation"** throughout, since the project backlog does not yet exist.

Where `docs/requirements.md` already states a specific number (e.g., "under 2 seconds," "WCAG 2.1 AA," "4.5:1 contrast"), this document treats that number as **already approved** and elevates it with load assumptions, measurement percentile, and validation method — it is not re-flagged for PO confirmation. Only *new* numeric thresholds introduced by this document (e.g., percentile definitions, specific coverage percentages, specific frame-time budgets) are flagged for confirmation.

---

## 3. Performance

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-PERF-001 | The flight search API (`POST /search` or equivalent) shall return a response within the target latency under local/mock-data load conditions (single developer machine, both mock providers responding, no artificial network latency). | Must Have | p95 latency < 2000 ms; p50 (median) < 800 ms **[PROPOSED — percentile definition is new; the 2s figure itself is already approved in requirements.md Section 5.1]**, measured over a run of at least 20 sequential requests under a single-user local load assumption (no concurrent-user load testing required for MVP; see NFR-SCALE-001). | Manual timing check during functional testing (Phase 05/14) using browser DevTools Network tab or `dotnet` integration test with `Stopwatch`; optionally a lightweight scripted timing harness (no dedicated load-test tool required for MVP). | US-001 AC6, FR-001–FR-015, requirements.md §5.1 | DP-001 (provider seam enables provider timing isolation), FR-049 (concurrent provider invocation via `Task.WhenAll`) |
| NFR-PERF-002 | The booking API (`POST /booking` or equivalent) shall return a response within the target latency under the same local/mock-data load conditions. | Must Have | p95 latency < 1000 ms; p50 < 400 ms **[PROPOSED — percentile definition; 1s figure already approved]**, single-user local load assumption. | Manual timing check / integration test with `Stopwatch`, same method as NFR-PERF-001. | US-006, FR-039–FR-045, requirements.md §5.1 | DP-002, DP-PERSIST-001 (in-memory store has negligible I/O latency by design) |
| NFR-PERF-003 | Frontend sort re-ordering (US-003) shall complete with no perceptible lag when the user changes the sort option. | Must Have | Sort operation (re-order + re-render of the results list) shall complete in under 100 ms for result sets of up to 50 flights **[PROPOSED — specific frame-time budget and result-set size assumption; requirements.md §5.1/§9.3 states "instantaneous/no perceptible delay" without a number]**. | Manual check using browser DevTools Performance panel during functional/UI testing (Phase 05/14); optionally an Angular component test asserting sort completes within a bounded number of change-detection cycles. | US-003 AC2, FR-020–FR-024, requirements.md §5.1 | DP-011 (single total-price calculation location keeps sort re-render cheap — no recomputation of derived values duplicated across components), DP-013 (consistent reactive state pattern avoids redundant re-renders) |
| NFR-PERF-004 | The airport dropdown (US-008) shall populate with no visible delay, since the data is static. | Should Have | Dropdown population < 50 ms after component initialization **[PROPOSED]**. | Manual check during functional testing. | US-008, FR-054–FR-059 | DP-012 (single-source static constant avoids duplicate lookups) |
| NFR-PERF-005 | Adding a new flight provider (US-007) shall not degrade existing search response time beyond the NFR-PERF-001 target, because providers are queried concurrently rather than sequentially. | Must Have | p95 search latency with N providers registered shall not exceed the single-slowest-provider response time plus a fixed aggregation overhead of <100 ms **[PROPOSED]** — i.e., latency is bounded by the slowest provider, not the sum of all providers. | Code review confirming `Task.WhenAll` (or equivalent concurrent invocation) is used (FR-049); manual timing comparison with 2 vs. 3 mock providers registered if a third mock provider is added during testing. | US-007, FR-007, FR-049, requirements.md §5.2 | DP-001, DP-004 (aggregation orchestration contract), FR-053 (DI-registered provider list) |

---

## 4. Scalability

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-SCALE-001 | The MVP is not required to support concurrent multi-user load or horizontal scaling. No load testing beyond single-user manual timing (NFR-PERF-001/002) is required for MVP sign-off. | Must Have | N/A — explicitly out of scope for load/concurrency testing in MVP; this NFR documents the boundary rather than a target. | Reviewable — confirmed at Phase 18 (Performance Review) as "Not Applicable — local single-user MVP" if no concurrency testing is performed. | requirements.md §5.2, Out of Scope item 14 | — |
| NFR-SCALE-002 | The in-memory booking store must remain thread-safe under concurrent request handling by the ASP.NET Core runtime (multiple simultaneous HTTP requests to the same process), even though multi-user *load* testing is out of scope. | Must Have | Zero data-race defects under concurrent write access (e.g., `ConcurrentDictionary` or equivalent synchronization); verified structurally, not via stress testing. | Code review confirming a thread-safe collection type backs `IBookingStore`'s implementation (BR-008); optionally a targeted concurrency unit test issuing parallel booking creations and asserting no lost writes / no duplicate reference collisions. | BR-008, FR-045 | DP-002, DP-PERSIST-001, DP-PERSIST-003 |
| NFR-SCALE-003 | The persistence and provider seams (`IBookingStore`, `IFlightProvider`) must not structurally preclude horizontal scaling if the application is later deployed with multiple instances behind a load balancer (e.g., no in-process-only state that would break correctness across instances, beyond the explicitly accepted in-memory-store limitation of BR-008). | Should Have | Reviewable, not numerically measured — this is an architecture-review-time check, not a runtime target. | Code review at Phase 06 (Architecture Planning) confirming no additional in-process-only state is introduced beyond the already-accepted `IBookingStore` limitation. | ASM-001, ASM-014, DP-PERSIST-004 | DP-PERSIST-001–005, DP-CLOUD-005 |

---

## 5. Availability and Reliability

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-AVAIL-001 | No uptime/SLA target applies to the MVP — it is a local development-only deliverable. | Must Have | N/A — explicitly no uptime requirement. | Reviewable — documented, not tested. | requirements.md §5.3, Out of Scope item 14 | — |
| NFR-AVAIL-002 | A single provider failure (exception, timeout, or malformed response) shall not cause the overall search request to fail. The search endpoint shall return HTTP 200 with the remaining providers' results and shall not return a 500 error due to a provider-only failure. | Must Have | 100% of single-provider-failure scenarios (as exercised by test cases) must result in a 200 response containing the surviving provider's results; zero 500 responses attributable solely to a provider exception. | Unit/integration test: inject a provider stub that throws, assert aggregation returns remaining results and does not propagate the exception to the controller (Phase 13 Test Writing); code review confirming try/catch at the aggregation boundary (FR-050). | BR-007, FR-009, FR-050, requirements.md §5.3 | DP-001, DP-004, DP-ZEROTRUST-002 (interface boundary integrity ensures the fault isolation cannot be bypassed) |
| NFR-AVAIL-003 | Provider failures must be logged with sufficient detail (provider name + exception message) to support diagnosis, without surfacing that detail to the end user. | Must Have | 100% of caught provider exceptions produce exactly one log entry containing provider name and exception message/type. | Code review + unit test asserting the logger is invoked with provider name and exception detail when a provider throws. | BR-007, FR-009, FR-070, requirements.md §5.9 | DP-007 (centralised exception handling does not apply here — this is provider-level catch, distinct from the global 500 handler) |
| NFR-AVAIL-004 | The backend shall recover cleanly on restart with an empty booking store — this data-loss-on-restart behaviour is an accepted design characteristic (BR-008/ASM-001), not a reliability defect, and must not cause the application to fail to start. | Must Have | 100% successful application startup with an empty `IBookingStore` after any restart. | Manual check: restart the backend process and confirm it starts without error and `GET` for any previously created booking reference correctly returns 404 (not a crash). | BR-008, ASM-001 | DP-002, DP-PERSIST-001 |

---

## 6. Security

Security NFRs for the MVP are split into two groups, per the task brief: (a) what IS required now, and (b) what the architecture must not preclude later. OWASP Top 10 (2021) and OWASP ASVS are used as the baseline standard for the "required now" group, consistent with CLAUDE.md's approved external-source list. NIST SP 800-207 (Zero Trust Architecture) is the baseline standard for the "must not preclude" group, consistent with its citation in requirements.md's DP-ZEROTRUST section.

### 6.1 Required Now (BR-010 in effect — no authentication for MVP)

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-SEC-001 | All API inputs must be validated server-side; validation failures return HTTP 400 with field-level detail. This is the primary MVP mitigation for OWASP Top 10 A03:2021 (Injection) and A04:2021 (Insecure Design), given no auth boundary exists. | Must Have | 100% of the validation rules enumerated in FR-060–FR-065 have a corresponding backend check; zero endpoints accept unvalidated input. | Unit tests per validation rule (Phase 13); code review checklist mapped to FR-060–FR-065 (Phase 15/16). | FR-060–FR-065, requirements.md §5.4 | DP-014 (backend validation authoritative), DP-015 (named validators, not inline patterns) |
| NFR-SEC-002 | Error responses (4xx/5xx) must never include stack traces, internal exception type names, or internal file paths, addressing OWASP A05:2021 (Security Misconfiguration) / information disclosure. | Must Have | 0 occurrences of stack-trace or internal-type leakage across all tested error paths (400/404/500). | Manual + automated check: trigger a 500 (e.g., simulate an unhandled exception) and inspect response body for absence of stack trace/class names; code review of the centralised exception middleware (DP-007). | FR-069, BR-011, requirements.md §5.4 | DP-007 |
| NFR-SEC-003 | No sensitive data — passport numbers, national ID numbers, full email addresses — shall appear in application logs. | Must Have | 0 occurrences of raw document numbers or full email addresses in log output across all logged code paths; logs may reference "passenger record created" or a masked/partial identifier, never the raw value. | Code review of all `ILogger` call sites for passenger/booking data; manual log inspection during functional testing (Phase 05/14) confirming no PII in console/log output. | requirements.md §5.4, §5.9 | DP-DEPLOY-006 (log sink is configurable but content rules are independent of sink) |
| NFR-SEC-004 | No secrets, API keys, or credentials shall be committed to source control or hardcoded in configuration files checked into git — consistent with DP-DEPLOY-005, even though no real secret currently exists in the MVP (BR-010: no auth). | Must Have | 0 secret values present in any tracked file (verified by absence, since no secret exists yet in MVP). | Code review / manual repository scan before merge (Phase 15). | DP-DEPLOY-005, requirements.md §5.4 | DP-DEPLOY-005, DP-CLOUD-001 |
| NFR-SEC-005 | The application must not assume implicit trust based on caller network location (e.g., no "internal call" bypass of validation), addressing structural precursors to OWASP A01:2021 (Broken Access Control) even though no access-control layer exists yet in MVP. | Must Have | 0 code paths where validation or processing logic branches on network origin/IP range to skip a check. | Code review search for IP-based or network-topology-based conditional logic (per DP-ZEROTRUST-001 verifiability note). | DP-ZEROTRUST-001, requirements.md §5.4 | DP-ZEROTRUST-001 |
| NFR-SEC-006 | Cross-Origin Resource Sharing (CORS) must be explicitly and narrowly configured to allow only the known Angular dev server origin (`localhost:4200` per ASM-012), not a wildcard (`*`) origin, addressing OWASP A05:2021 (Security Misconfiguration). | Must Have | CORS policy allows exactly one configured origin (or an explicit, reviewable list) — no wildcard origin in any environment configuration. | Code review of CORS policy registration in `Program.cs`/configuration. | ASM-012 | DP-DEPLOY-001 (environment-specific values, including CORS origins, externalised to configuration) |
| NFR-SEC-007 | Document number and email validation must use the named patterns/validators defined once per layer (DP-015), preventing inconsistent or weakened validation logic from being introduced ad hoc, which is itself a mitigation against malformed-input-based defects (OWASP A04:2021). | Must Have | 0 inline regex/pattern duplication for document number or email validation outside the named validator/constant. | Code review (static grep for duplicated pattern literals). | FR-064, FR-065, DP-015 | DP-014, DP-015 |

### 6.2 Must Not Preclude Later (Zero Trust and Identity Protocol Readiness)

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-SEC-008 | The authentication seam (`AuthService` on the frontend; the ASP.NET Core authentication pipeline on the backend) must be structurally capable of hosting OIDC, OAuth 2.0, SAML 2.0, or an SSO broker without requiring changes to any service class, controller, or Angular component. | Must Have | Reviewable, not runtime-measured for MVP: 0 references to `ClaimsPrincipal`/`IIdentity`/scheme-name checks in service or repository code; exactly 1 `AuthService` class on the frontend. | Code review checklist directly mirroring the Verifiability note under DP-AUTH-005/006 in requirements.md. | DP-AUTH-001–006, ASM-021 | DP-AUTH-001–006 |
| NFR-SEC-009 | No component may bypass a defined interface boundary to reach another component's internals directly, preserving the least-privilege/per-call-verification structure that Zero Trust adoption would later formalize (NIST SP 800-207). | Must Have | 0 occurrences of cross-component access outside the DP-001–DP-004 interface boundaries. | Code review (Phase 15/16), mirroring DP-ZEROTRUST-002 verifiability note. | DP-ZEROTRUST-002 | DP-ZEROTRUST-002, DP-001–DP-004 |
| NFR-SEC-010 | No hardcoded trust shortcut (bypass token, unconditionally trusted header) may exist anywhere in the codebase. | Must Have | 0 occurrences of an unconditionally accepted "trusted" header or bypass token. | Code review search per DP-ZEROTRUST-005 verifiability note. | DP-ZEROTRUST-005 | DP-ZEROTRUST-005 |
| NFR-SEC-011 | Identity/policy checks, if introduced, must be evaluated per request rather than cached for a session/connection lifetime, consistent with statelessness already required by DP-PROTOCOL-003b. | Should Have | Reviewable — no runtime check exists in MVP since no auth exists; confirmed structurally at the point a policy seam (DP-POLICY-002) is exercised. | Code review, deferred to when a policy is introduced (future sprint). | DP-ZEROTRUST-003 | DP-ZEROTRUST-003, DP-PROTOCOL-003b |

**Standards referenced:** OWASP Top 10 (2021) categories A01 (Broken Access Control), A03 (Injection), A04 (Insecure Design), A05 (Security Misconfiguration) — cited by name from general knowledge of the OWASP Top 10 project; NIST SP 800-207 (Zero Trust Architecture) — already cited in requirements.md §3.10 DP-ZEROTRUST section. No web fetch was required to name these categories; if the security-reviewer (Phase 16) requires the current OWASP Top 10 category numbering confirmed against owasp.org at review time, that is within their remit.

---

## 7. Privacy and Data Protection

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-PRIV-001 | Passenger personal data (full name, email, document number) must be held only in memory for the lifetime of the process and must not be written to any persistent file, external service, or third-party system in the MVP. | Must Have | 0 occurrences of passenger PII written to disk, external API, or third-party log aggregator. | Code review confirming `IBookingStore`'s only implementation is in-memory (BR-008) and no outbound integration exists (ASM-007, Out of Scope items 5, 6, 11). | BR-008, BR-005, ASM-001 | DP-PERSIST-001, DP-CLOUD-004 |
| NFR-PRIV-002 | Document numbers (passport/national ID) must never be logged in raw form (restates NFR-SEC-003 in privacy terms — data minimization). | Must Have | 0 occurrences of raw document numbers in logs. | Same as NFR-SEC-003. | requirements.md §5.4, §5.9 | — |
| NFR-PRIV-003 | The multi-tenancy seam (`ITenantContext`) must be present so that, if the platform later serves multiple distinct customers/organisations, tenant-level data isolation can be enforced without a service-layer rewrite — a privacy-relevant future requirement given each tenant's bookings would need to remain inaccessible to other tenants. | Must Have | Reviewable — confirmed structurally, not runtime-tested, since MVP is single-tenant (`TenantId = "default"`). | Code review confirming `ITenantContext` injection into `BookingService` and `tenantId` parameters on `IBookingStore` query methods (DP-TENANT-003, DP-TENANT-006). | DP-TENANT-001–007, ASM-013 | DP-TENANT-001–007 |

---

## 8. Accessibility

Requirements.md §5.5 already establishes WCAG 2.1 Level AA as the baseline. This section elevates that baseline into specific, testable NFRs. A dedicated Accessibility Review occurs in Phase 17; these NFRs set the bar that review will be conducted against, per WCAG 2.1 (W3C) success criteria naming already used in requirements.md.

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-A11Y-001 | All interactive controls (search form fields, sort control, Select/Book buttons, passenger form fields, Confirm Booking button) must be fully operable using only the keyboard (Tab, Shift+Tab, Enter, Space, arrow keys where applicable), with no keyboard trap. | Must Have | 100% of interactive controls reachable and operable via keyboard alone, verified by a full keyboard-only walkthrough of all 8 user stories' primary paths. | Manual keyboard-only walkthrough during Phase 17 Accessibility Review; corresponds to WCAG 2.1 SC 2.1.1 (Keyboard) and SC 2.1.2 (No Keyboard Trap). | requirements.md §5.5, US-001–US-006 | DP-009 (components handle presentation/interaction only, enabling standard focus/keyboard behaviour) |
| NFR-A11Y-002 | Every form field (search form, passenger detail forms) must have a visible label with a programmatic association (`<label for>`, `aria-label`, or `aria-labelledby`). | Must Have | 100% of form fields have a programmatically associated label — 0 fields rely on placeholder text alone. | Manual check with browser accessibility inspector / axe DevTools during Phase 17; corresponds to WCAG 2.1 SC 1.3.1 (Info and Relationships), SC 4.1.2 (Name, Role, Value). | requirements.md §5.5, US-001 AC1–4, US-005 AC1–2 | — |
| NFR-A11Y-003 | Error messages (validation errors, search/booking API errors, empty-state messages) must be announced to assistive technology without requiring the user to be visually focused on the error, using ARIA live regions (`aria-live="polite"` or `"assertive"` as appropriate) or equivalent. | Must Have | 100% of dynamically injected error/empty-state messages are wrapped in an ARIA live region or use `role="alert"`. | Manual screen-reader spot check (e.g., NVDA or VoiceOver) during Phase 17, or automated axe-core check for live-region presence. | requirements.md §5.5, US-002 AC6–7, US-005 AC10, US-006 AC6 | DP-009 |
| NFR-A11Y-004 | Colour contrast for all text must meet WCAG 2.1 AA minimum ratios: 4.5:1 for normal text, 3:1 for large text (≥18pt or ≥14pt bold). | Must Have | 100% of text elements pass the stated contrast ratio, measured against the final rendered theme/colour palette. | Automated contrast check (axe DevTools, Chrome Lighthouse, or WebAIM Contrast Checker) during Phase 17; corresponds to WCAG 2.1 SC 1.4.3 (Contrast Minimum). | requirements.md §5.5 (already states this ratio explicitly) | — |
| NFR-A11Y-005 | The booking reference code, displayed prominently per US-006 AC5, must remain accessible to screen readers (not conveyed by styling/visual prominence alone) — e.g., not an image of text, and its "prominent" styling must not rely on `aria-hidden` content or decorative-only markup to convey the value. | Must Have | The booking reference text node is present in the accessible DOM tree and readable by a screen reader in document order. | Manual screen-reader check during Phase 17. | US-006 AC5, FR-036 | — |
| NFR-A11Y-006 | Focus order must follow a logical reading/interaction sequence through each screen (search form → results → booking form → confirmation), and focus must move sensibly on navigation (e.g., to a heading or the first field) when transitioning between the search results screen and the booking screen. | Should Have | 0 instances of illogical focus order or lost focus (focus landing on `<body>`) after a screen transition, verified during the Phase 17 walkthrough. | Manual check during Phase 17; corresponds to WCAG 2.1 SC 2.4.3 (Focus Order). | US-004 AC1, US-006 AC4 | — |

---

## 9. Usability

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-USE-001 | The search form must be usable without external instructions — all fields must be self-explanatory via labels, placeholder examples, and constraints communicated inline (e.g., passenger count range 1–9 shown in the UI). | Must Have | Reviewable — assessed qualitatively during Phase 05 (Test Strategy) acceptance walkthroughs and Phase 17; no automated metric. | Manual usability walkthrough / functional test observation. | requirements.md §5.6, US-001 | — |
| NFR-USE-002 | Total price and per-passenger price must be visually distinguishable on every screen where both appear (results list, booking screen, confirmation), with the total always the visually dominant figure. | Must Have | 100% of the 3 screens (results, booking, confirmation) render total price with greater visual weight (font size/weight or position) than per-passenger price. | Manual/visual check during functional testing (Phase 05/14) and code review of the relevant Angular templates. | US-002 AC2–4, US-004 AC3, US-006 AC4, FR-016–FR-018, FR-027, FR-035 | DP-011 |
| NFR-USE-003 | Empty-state and error messages must include actionable guidance (e.g., "try different criteria," "try again") rather than a bare failure notice. | Must Have | 100% of the empty-state and error-message strings implemented for US-002 AC6–7 and US-006 AC6 include an actionable next step. | Manual content review during functional testing. | requirements.md §5.6, US-002 AC6–7, US-006 AC6, FR-071–FR-072 | — |

---

## 10. Maintainability

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-MAINT-001 | Backend code must comply with the four required interface boundaries (DP-001–DP-004) with no concrete-class references outside their DI registration. | Must Have | 0 violations found at code review (Phase 15) — no controller or service references a concrete provider, store, or aggregator class directly. | Code review (Phase 15), per DP-001–DP-004 and DP-PERSIST-003. | DP-001–DP-004, requirements.md §5.7 | DP-001–DP-004 |
| NFR-MAINT-002 | No magic numbers/strings for pricing constants, airport data, or validation patterns — all must be named constants or configuration, per requirements.md §5.7. | Must Have | 0 inline literal pricing multipliers/floors, airport records, or validation regex patterns found outside their named constant/config location, at code review. | Code review (Phase 15). | requirements.md §5.7, DP-006, DP-015, DP-012 | DP-006, DP-012, DP-015 |
| NFR-MAINT-003 | Domain models, service interfaces, and service implementations must not reference third-party library types in public signatures — only .NET BCL types and framework-standard abstractions are permitted, preserving upgrade flexibility. | Must Have | 0 third-party (non-BCL, non-`Microsoft.Extensions.*`) type references in service/domain public signatures, at code review. | Code review (Phase 15), per DP-UPGRADE-001 verifiability note. | DP-UPGRADE-001, ASM-016 | DP-UPGRADE-001–004 |
| NFR-MAINT-004 | Framework and library version numbers must be defined in exactly one location per ecosystem (`package.json`; `.csproj`), with no duplication elsewhere. | Must Have | 0 hardcoded/duplicated version strings found outside the two manifest locations, at code review. | Code review (Phase 15). | DP-UPGRADE-004 | DP-UPGRADE-004 |
| NFR-MAINT-005 | Angular components must be standalone (Angular 22 standalone component pattern) and organised into clear feature areas (search, results, booking, confirmation). | Must Have | 100% of components use the standalone pattern; feature-area folder structure is present and reviewable. | Code review (Phase 15). | requirements.md §5.7 | DP-009, DP-010 |
| NFR-MAINT-006 | Any future business/eligibility policy rule must be expressible as a single, independently named policy check rather than inline branching scattered across service methods. | Must Have | Reviewable — 0 multi-branch inline eligibility/authorisation-style logic found embedded directly in service methods, at code review. | Code review (Phase 15/16), per DP-POLICY-001 verifiability note. | DP-POLICY-001–003, ASM-017 | DP-POLICY-001–003 |

---

## 11. Testability

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-TEST-001 | Every class implementing a required backend interface (DP-001–DP-004) must be unit-testable in isolation via constructor-injected dependencies substitutable with test doubles. | Must Have | 100% of `IFlightProvider`, `IFlightAggregatorService`, `IBookingService`, `IBookingStore` implementations are unit-tested with mocked/faked dependencies (Phase 13). | Unit test suite review (Phase 13/14) confirming each interface implementation has at least one isolated unit test using a mock/stub/fake. | DP-017, requirements.md §5.8 | DP-017 |
| NFR-TEST-002 | Booking reference generation logic (BR-004) must be independently unit-testable without instantiating a full booking flow. | Must Have | At least 1 unit test directly exercises `GenerateBookingReference(routeType)` (or equivalent) for both `INT` and `DOM` route types, asserting format compliance (`SKY-[TYPE]-XXXXXX`, 14 chars, uppercase alphanumeric suffix). | Unit test review (Phase 13/14). | BR-004, DP-018 | DP-018 |
| NFR-TEST-003 | Pricing calculation methods (BR-001, BR-002) must be independently unit-testable given only a base fare input, with no HTTP context, search result object, or database dependency required. | Must Have | At least 2 unit tests per provider (GlobalAir, BudgetWings) covering: a standard case and the BudgetWings floor-boundary case (base fare $25.00 → $29.99; base fare $30.00 → $29.99 per BR-002 example), asserting exact rounded output. | Unit test review (Phase 13/14). | BR-001, BR-002, DP-019 | DP-019, DP-006 |
| NFR-TEST-004 | Angular services must be testable via `TestBed` with `HttpClientTestingModule` substituted for the real `HttpClient`; components must not instantiate `HttpClient` directly. | Must Have | 100% of Angular services making HTTP calls have at least one `TestBed`-based unit test using `HttpClientTestingModule`; 0 components directly import `HttpClient`. | Unit test review + code review (Phase 13/15), per DP-020 and DP-PROTOCOL-006 verifiability notes. | DP-020, DP-PROTOCOL-006 | DP-020, DP-010, DP-PROTOCOL-006 |
| NFR-TEST-005 | Minimum unit test coverage for service-layer business logic (pricing calculation, booking reference generation, route-type determination, search/booking aggregation orchestration) should meet a defined coverage floor to give confidence in refactoring safety ahead of reviews. | Should Have | **[PROPOSED — requires PO/Scrum Master confirmation]** Suggested target: **80% line coverage** for backend service-layer classes (`*Service`, `*Provider`, aggregation logic) — excluding controllers, DTOs/domain models, and `Program.cs` startup wiring. This is a suggested target, not a unilateral mandate; the human PO/Scrum Master should confirm or adjust before it is treated as a gate. | Coverage report from `dotnet test --collect:"XPlat Code Coverage"` (or equivalent), reviewed at Phase 14 (Test Execution Summary). | requirements.md §5.8, DP-017–DP-020 | DP-017–DP-020 |
| NFR-TEST-006 | Provider fault-isolation behaviour (BR-007) must have explicit automated test coverage — not left to manual verification only. | Must Have | At least 1 integration/unit test simulates a throwing provider and asserts the aggregation layer returns the surviving provider's results with a 200 response. | Test suite review (Phase 13/14). | BR-007, NFR-AVAIL-002 | DP-001, DP-004 |

---

## 12. Observability and Logging

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-OBS-001 | Provider failures must be logged with provider name and exception message/type at `Warning` or `Error` level. | Must Have | 100% of caught provider exceptions produce a log entry at `Warning`/`Error` level including provider name. | Code review + unit test asserting logger invocation (shared with NFR-AVAIL-003). | BR-007, FR-009, requirements.md §5.9 | DP-007 |
| NFR-OBS-002 | Structured logging (e.g., Microsoft.Extensions.Logging with structured/JSON-capable providers, or Serilog) should be used rather than unstructured string concatenation, to support future log aggregation. | Should Have | Reviewable — logging calls use structured message templates (e.g., `logger.LogError("Provider {ProviderName} failed: {Message}", name, msg)`) rather than string-concatenated messages. | Code review (Phase 15). | requirements.md §5.9 | DP-DEPLOY-006 |
| NFR-OBS-003 | Log output must be written to standard output/error or a configurable sink, not a hardcoded filesystem path, so that containerised or cloud-native log aggregation works without code changes later. | Must Have | 0 hardcoded filesystem log paths found at code review; default logging configuration writes to console. | Code review (Phase 15), per DP-DEPLOY-006. | DP-DEPLOY-006 | DP-DEPLOY-006 |
| NFR-OBS-004 | No PII (document numbers, full email addresses) appears in any log output (restates NFR-SEC-003/NFR-PRIV-002 for completeness under this category). | Must Have | 0 occurrences at manual log inspection. | Same as NFR-SEC-003. | requirements.md §5.4, §5.9 | — |

---

## 13. Compatibility

Elevates requirements.md §5.10 into explicit version-pinned NFRs.

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-COMPAT-001 | The frontend must run correctly on the latest 2 major versions of each evergreen browser: Chrome, Edge, Firefox, and Safari. | Must Have | Manual smoke test of the core flows (search → results → booking → confirmation) passes on at least Chrome (latest) and one additional evergreen browser (Edge or Firefox) before Phase 14 sign-off; Safari verification is best-effort if a macOS test environment is available **[PROPOSED — specific minimum browser-pair for MVP sign-off, since exhaustive 4-browser × 2-version manual testing is disproportionate for MVP scope; requirements.md §5.10 states the target browser set but not a minimum verified subset]**. | Manual cross-browser smoke test (Phase 14). | requirements.md §5.10 | — |
| NFR-COMPAT-002 | The backend must run on .NET 10 and remain cross-platform (Windows and macOS at minimum, per the existing engineering environment; Linux compatibility is a natural consequence of ASP.NET Core's cross-platform runtime but is not separately verified in MVP). | Must Have | Backend builds and runs successfully via `dotnet run`/`dotnet build` on the developer's OS (Windows, per the current environment); no OS-specific code paths present. | Build/run verification (Phase 12/14); code review confirming no OS-specific path handling (DP-DEPLOY-002). | requirements.md §5.10 | DP-DEPLOY-002 |
| NFR-COMPAT-003 | The Node.js version used for the Angular CLI and build toolchain must be a version officially supported by Angular 22's documented compatibility matrix. | Must Have | The `package.json` `engines` field (or project README) specifies the Node.js version range aligned with Angular 22's official support matrix at time of build. | Manual check against Angular's official version-compatibility documentation (angular.dev) during setup; code review of `package.json`. | requirements.md §5.10 | DP-UPGRADE-004 |

---

## 14. Deployability

Elevates DP-DEPLOY-001–006 and DP-CLOUD-001–005 into concrete, verifiable NFRs, applying 12-factor-app alignment as the governing standard (already invoked in requirements.md §3.10).

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-DEPLOY-001 | Changing the deployment target (local machine, CI/CD pipeline runner, on-premises server, or cloud VM/container) shall require zero source-code changes — configuration/environment changes only. | Must Have | **Acceptance test:** a reviewer can enumerate every environment-specific value (API base URL, CORS origins, connection strings, port numbers, feature flags) and confirm each resolves through `IConfiguration`/environment variables/Angular environment files rather than an inline literal. 0 hardcoded environment-specific values found. | Code review (Phase 15/18), per DP-DEPLOY-001 verifiability note. | DP-DEPLOY-001, ASM-020 | DP-DEPLOY-001 |
| NFR-DEPLOY-002 | Build, test, and run commands must be scriptable and non-interactive, executable identically by a human at a terminal or by any CI/CD system. | Must Have | 100% of the build (`dotnet build`, `ng build`), test (`dotnet test`, `ng test --watch=false`), and run commands complete with no manual/interactive prompt required. | Manual command execution check (Phase 12/14) confirming no interactive prompt blocks completion. | DP-DEPLOY-004 | DP-DEPLOY-004 |
| NFR-DEPLOY-003 | The application must remain containerisable without source code changes, even though no container image is built in the MVP. | Must Have | Reviewable — 0 code paths assume a non-ephemeral local filesystem beyond what is explicitly safe to lose (i.e., nothing beyond the in-memory store, which is already accepted as ephemeral). | Code review (Phase 15/18), per DP-DEPLOY-003. | DP-DEPLOY-003 | DP-DEPLOY-003 |
| NFR-DEPLOY-004 | No secret may be committed to source control or hardcoded in a tracked configuration file (restates NFR-SEC-004 under the deployability lens, since secret handling is a deployment-readiness concern as well as a security one). | Must Have | 0 secret values in tracked files. | Same as NFR-SEC-004. | DP-DEPLOY-005 | DP-DEPLOY-005 |
| NFR-DEPLOY-005 | Any future integration with an external managed service (database, secrets manager, blob storage, managed cache) must be introduced behind an application-owned interface — never a direct cloud-vendor SDK reference in a service or controller class. | Must Have | 0 occurrences of `using Azure.*`, `using Amazon.*`, `using Google.Cloud.*`, or equivalent cloud-vendor SDK namespaces anywhere in the MVP codebase (none should exist, since no cloud integration is implemented). | Code review search (Phase 15/18), per DP-CLOUD-005 verifiability note. | DP-CLOUD-001–005, ASM-019 | DP-CLOUD-001–005 |
| NFR-DEPLOY-006 | Database connection configuration, if/when a real database is introduced, must be sourced exclusively through `IConfiguration`/environment variables — never hardcoded — consistent with the already-approved DP-DB-005. | Must Have | Not applicable to MVP runtime (no real database configured) — reviewable as a forward constraint only. | Code review at the point a real `IBookingStore` implementation is introduced (future sprint), per DP-DB-005. | DP-DB-005 | DP-DB-005, DP-PERSIST-004 |

---

## 15. Data Integrity

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-DATA-001 | Every generated booking reference must be unique within the in-memory store; a collision must trigger regeneration before the booking is saved. | Must Have | 0 duplicate booking references stored, verified by a uniqueness check on every create operation; at least 1 unit test simulates a forced collision and asserts regeneration occurs. | Unit test (Phase 13/14); code review of the collision-check logic (BR-004). | BR-004 | DP-002, DP-018 |
| NFR-DATA-002 | Total price stored on a booking record must equal per-passenger price (after provider pricing rules) × passenger count, rounded to exactly 2 decimal places, computed independently on the backend at booking time (not merely trusted from the frontend request). | Must Have | 100% of created booking records have a stored total price that is server-recomputed and matches the expected value to the cent, for all tested pricing scenarios (including the BudgetWings floor case). | Unit/integration test asserting server-side recomputation of total price at booking creation, independent of any client-submitted total. | BR-006, FR-044 | DP-006, DP-019 |
| NFR-DATA-003 | A booking record must contain exactly n passenger records for an n-passenger booking — no merged, missing, or duplicated passenger records. | Must Have | 100% of created bookings have passenger-array length equal to the requested passenger count, verified by test for n = 1 and n = 9 (boundary values). | Unit/integration test (Phase 13/14). | BR-005, FR-040, FR-043 | DP-002 |
| NFR-DATA-004 | Route-type determination (domestic vs. international) must be re-evaluated authoritatively on the backend at booking time, not merely trusted from frontend state, even though the frontend may compute its own copy for immediate label/validation feedback. | Must Have | 100% of booking creation requests have route type (and therefore document-type validation) determined server-side from the airport records, independent of any client-submitted route-type flag. | Unit/integration test asserting backend-side route-type recomputation. | BR-003, DP-016 | DP-016 |

---

## 16. On-Premise and Cloud Readiness

This section is the NFR-level statement of what `docs/requirements.md` §3.10 already constrains at the architecture level (DP-CLOUD-001–005, DP-DEPLOY-001–006). The MVP itself is local-only (Out of Scope item 14); these NFRs govern **addability**, not present-state capability.

| ID | Requirement | Priority | Target / Measurement | Validation Method | Traceability | Architecture Link |
|---|---|---|---|---|---|---|
| NFR-ONPREM-001 | The application must be deployable to a private/on-premises server using the same configuration-externalisation mechanism used for local development — no code branch specific to an on-premises target. | Must Have | Reviewable — 0 on-premises-specific code paths exist or would be needed, since all environment values are already externalised (NFR-DEPLOY-001). | Code review (Phase 18), consistent with DP-DEPLOY-001–002. | DP-DEPLOY-001, DP-DEPLOY-002 | DP-DEPLOY-001, DP-DEPLOY-002 |
| NFR-ONPREM-002 | The application must be deployable to a cloud provider (Azure, AWS, or GCP compute) using configuration changes only, consistent with the persistence (`IBookingStore`) and provider (`IFlightProvider`) interface-seam pattern already established as the reference pattern for all future managed-cloud-service integrations. | Must Have | Reviewable — confirmed by the absence of any cloud-vendor SDK reference in service/domain code (shared measurement with NFR-DEPLOY-005) and by confirming all cloud-relevant configuration (connection strings, endpoints, region, managed-identity settings) is sourced via `IConfiguration`. | Code review (Phase 18), per DP-CLOUD-003 and DP-CLOUD-005. | DP-CLOUD-001–005, ASM-019 | DP-CLOUD-001–005 |
| NFR-ONPREM-003 | The persistence layer must be replaceable with a real database (relational or document, on-premises or managed-cloud-hosted) with zero changes to `BookingService` or any controller — only a new `IBookingStore` implementation, DI registration, and configuration. | Must Have | Reviewable — structurally guaranteed if DP-PERSIST-001–005 and DP-DB-001–005 hold; no runtime test possible in MVP since no real database exists. | Code review (Phase 06/18), per DP-PERSIST-004 and DP-DB-005 verifiability notes. | DP-PERSIST-001–005, DP-DB-001–005, ASM-014 | DP-PERSIST-001–005, DP-DB-001–005 |
| NFR-ONPREM-004 | CI/CD pipeline execution (Jenkins, GitHub Actions, Azure DevOps, or any equivalent) must be able to invoke build/test/run commands identically to local execution, with no interactive step (restates NFR-DEPLOY-002 under this category for completeness). | Must Have | Same measurement as NFR-DEPLOY-002. | Same as NFR-DEPLOY-002. | DP-DEPLOY-004 | DP-DEPLOY-004 |

---

## 17. Summary — Proposed Numeric Targets Requiring Human Product Owner / Scrum Master Confirmation

The following numeric thresholds are **new information** introduced by this NFR specification — they are not already stated in `docs/requirements.md` and must be explicitly confirmed (or adjusted) by the Human Product Owner / Scrum Master before being treated as an enforced gate at later phases (Test Strategy, Code Review, Test Execution).

| # | NFR ID | Proposed Target | Rationale | If Not Confirmed |
|---|---|---|---|---|
| 1 | NFR-PERF-001 | p95 < 2000 ms / p50 < 800 ms, percentile-based measurement over ≥20 sequential local requests | Requirements.md states "under 2 seconds" without specifying percentile or sample size | Falls back to the qualitative "under 2 seconds, typical mock data volumes" statement already approved |
| 2 | NFR-PERF-002 | p95 < 1000 ms / p50 < 400 ms | Requirements.md states "under 1 second" without percentile | Falls back to qualitative "under 1 second" statement |
| 3 | NFR-PERF-003 | Sort re-render < 100 ms for result sets up to 50 flights | Requirements.md/US-003 says "no perceptible delay" without a number | Falls back to qualitative "no perceptible lag" |
| 4 | NFR-PERF-004 | Airport dropdown population < 50 ms | New, minor, low-risk | Falls back to qualitative "immediately usable" |
| 5 | NFR-PERF-005 | Aggregation overhead < 100 ms above slowest provider | New — operationalises requirements.md §5.2's qualitative "should not degrade" statement | Falls back to qualitative statement; verified by code review of concurrent invocation only |
| 6 | NFR-TEST-005 | 80% line coverage target for backend service-layer classes | New — requirements.md §5.8 requires unit-testability but sets no coverage percentage | No coverage gate enforced; testability requirement (DP-017–020) still applies structurally |
| 7 | NFR-COMPAT-001 | Minimum verified browser pair for Phase 14 sign-off (Chrome + one of Edge/Firefox; Safari best-effort) | Requirements.md names 4 browsers × latest-2-versions without specifying a minimum verified subset for MVP timeline | Falls back to best-effort testing across whatever browsers are available before the deadline |

All other numeric targets in this document either restate a number already explicit in `docs/requirements.md` (e.g., WCAG 4.5:1/3:1 contrast, the 2s/1s performance figures themselves, the 6-airport/2-country minimum) or are stated as "reviewable" / structural checks with no numeric threshold, and therefore do not require separate confirmation.

---

## 18. Traceability and Backlog Linkage Note

Every NFR in this document traces to at least one user story (US-*), functional requirement (FR-*), business rule (BR-*), or design principle (DP-*) in `docs/requirements.md` v1.4, and cites the specific DP-* architectural constraint(s) that structurally satisfy it, per the table columns above. Backlog item linkage is not yet possible: the project backlog does not exist until Phase 07 (Project Backlog Creation). Every NFR should be revisited at Phase 07 to attach a backlog item ID; until then, treat this section as the authoritative NFR-to-requirement traceability record.

---

## 19. Validation Method Summary

| Validation Method | NFR Categories Using It | Phase |
|---|---|---|
| Unit test | Performance (indirectly), Reliability, Testability, Data Integrity, Security (validation rules) | Phase 13 (Test Writing) |
| Integration test | Reliability (provider fault isolation), Data Integrity | Phase 13 |
| Manual timing check | Performance | Phase 05/14 |
| Code review | Security, Maintainability, Deployability, Cloud/On-Prem Readiness, Compatibility | Phase 15 (Code Review), Phase 16 (Security Review), Phase 18 (Performance Review) |
| Manual accessibility walkthrough / axe-core / screen reader | Accessibility | Phase 17 (Accessibility Review) |
| Manual cross-browser smoke test | Compatibility | Phase 14 |
| Coverage report | Testability | Phase 14 |
| Reviewable / structural (no runtime test possible in MVP) | Zero Trust readiness, Cloud readiness, Database readiness, Multi-tenancy readiness, Protocol addability | Phase 06 (Architecture Planning), Phase 15/16/18 |

---

*End of Non-Functional Requirements Specification v1.0.*
