# Security Review Report — Phase 16

| Field | Value |
|---|---|
| Document ID | SEC-PHASE-16 |
| Date | 2026-07-07 |
| Branch | `sdlc/16-security-review-skyroute-mvp` |
| Phase | Phase 16 — Security Review |
| Reviewer | security-reviewer |
| Scope | `src/SkyRoute.Api/`, `src/SkyRoute.Application/`, `src/SkyRoute.Infrastructure/`, `frontend/src/app/`, `frontend/src/environments/`, `frontend/src/index.html`, `frontend/package.json`, backend `appsettings*.json` / `launchSettings.json` / `.csproj` files |
| Reference baselines | `docs/requirements.md` v1.4 (Section 7 Out of Scope, BR-001–BR-012), `docs/specs/non-functional-requirements.md` Section 6 (NFR-SEC-001–011), `docs/architecture/architecture-plan.md` (AD-004/AD-005), `docs/reviews/code-review-phase-15.md` (CR-001–CR-005, not re-reported here) |
| Standards referenced | OWASP Top 10 (2021): A03 Injection, A04 Insecure Design, A05 Security Misconfiguration, A08 Software and Data Integrity Failures; CWE-20 (Improper Input Validation), CWE-770 (Allocation of Resources Without Limits), CWE-840 (Business Logic Errors), CWE-1333 (Inefficient Regular Expression Complexity) |
| Prior findings referenced (not re-reported) | CR-001–CR-005 (Phase 15, non-security), QA-001/002/004/005 (Open), QA-003 (Resolved) |

---

## Summary

This review independently examined the full backend (`SkyRoute.Api`, `SkyRoute.Application`, `SkyRoute.Infrastructure`) and frontend (`frontend/src/app`) source delivered in Phase 12, plus configuration files (`appsettings*.json`, `launchSettings.json`, `environment.ts`, `index.html`, `package.json`), against the security-relevant NFRs the Solution Architect scoped for this MVP in `docs/specs/non-functional-requirements.md` Section 6 (NFR-SEC-001–011) and general OWASP Top 10 (2021) risk categories.

**The MVP's explicit no-authentication decision (BR-010, Out of Scope item 1/23) is a documented, approved product-scope decision, not treated as a finding in this review.** The `AuthService` no-op stub and the `ITenantContext`/`DefaultTenantContext` seam were confirmed structurally sound and consistent with NFR-SEC-008/009/010 (no `ClaimsPrincipal`/`IIdentity` usage anywhere in `src/`, no hardcoded trust-bypass header/token found, no network-origin-based conditional logic found). Centralized exception handling (`ApiExceptionMiddleware`), CORS configuration (narrow, non-wildcard, externalized to `appsettings.json`), PII/logging hygiene (only the booking reference is logged — never document numbers, emails, or full passenger records), secret handling (no secrets, keys, or connection strings found anywhere in tracked source), and use of a cryptographically secure RNG (`RandomNumberGenerator`, not `System.Random`) for booking references were all independently verified and found compliant with NFR-SEC-002/003/004/005/006/007 — no new findings are raised in those specific areas.

Four findings were raised, all `Open`. **One High-severity finding (SEC-001) exists** — a client-side-trusted flight-fare snapshot on the booking endpoint that is never cross-validated against the server's own search/pricing logic, enabling arbitrary price/fare tampering on any booking record. This finding triggers the CLAUDE.md §21 human-approval gate for unresolved High findings before proceeding past this phase. The remaining three findings (SEC-002 Medium, SEC-003 Low, SEC-004 Low) are defense-in-depth/hardening gaps, consistent in severity profile with Phase 15's code review.

---

## Findings

### SEC-001 — Booking endpoint trusts an unvalidated, client-supplied flight-fare snapshot, enabling price and fare-detail tampering

| Field | Value |
|---|---|
| Severity | High |
| File/area | `src/SkyRoute.Application/Contracts/BookingFlightRequest.cs`; `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (`IsFlightSnapshotComplete`, lines 94–102); `src/SkyRoute.Application/Services/BookingService.cs` (`CreateBookingAsync`, lines 44–104) |
| Status | Open |

**Evidence:**

`POST /api/bookings` accepts a full `BookingFlightRequest` object from the client — `Provider`, `FlightNumber`, `Origin`, `Destination`, `DepartureDateTime`, `ArrivalDateTime`, `CabinClass`, `BaseFare`, and critically `PricePerPassenger` — rather than an opaque flight identifier that the backend re-resolves against its own provider/pricing logic (`GlobalAirProvider`/`BudgetWingsProvider`). This is a deliberate architecture decision (AD-004/AD-005: "domain models double as API request/response contracts... a full flight-detail snapshot carried from search results, not an opaque flight identifier").

`BookingRequestValidator.IsFlightSnapshotComplete` only checks that these fields are *present* (non-null/non-whitespace, `HasValue`):

```csharp
private static bool IsFlightSnapshotComplete(BookingFlightRequest flight) =>
    !string.IsNullOrWhiteSpace(flight.Provider) &&
    !string.IsNullOrWhiteSpace(flight.FlightNumber) &&
    !string.IsNullOrWhiteSpace(flight.Origin) &&
    !string.IsNullOrWhiteSpace(flight.Destination) &&
    flight.DepartureDateTime.HasValue &&
    flight.ArrivalDateTime.HasValue &&
    !string.IsNullOrWhiteSpace(flight.CabinClass) &&
    flight.PricePerPassenger.HasValue;
```

There is no check that `PricePerPassenger`/`BaseFare` is positive, no check that it matches any value the aggregator/providers would actually compute for that `Provider`+`FlightNumber`+`CabinClass` combination (`GlobalAirProvider.ApplyGlobalAirPricing`/`BudgetWingsProvider.ApplyBudgetWingsPricing`), and no check that `CabinClass` is one of the three valid values enforced elsewhere for search (`SearchRequestValidator.ValidCabinClasses` — Economy/Business/First Class).

`BookingService.CreateBookingAsync` then computes the "authoritative" server-side total directly from this untrusted value:

```csharp
var totalPrice = Math.Round(
    request.Flight.PricePerPassenger!.Value * request.PassengerCount,
    2,
    MidpointRounding.AwayFromZero);
```

The code's own comment frames this as "there is no client-submitted total to trust or distrust in this contract... this is a pure computation, not a trust decision" — but `PricePerPassenger` itself *is* the client-submitted, untrusted value being trusted. A client (or any direct API caller bypassing the Angular UI entirely — there is no auth boundary preventing this, and none is required to demonstrate the flaw) can submit any `PricePerPassenger` value, including `0.01`, `0`, or a negative number, and receive a `201 Created` `BookingResponse` with a `TotalPrice` computed from that fabricated value, persisted as a confirmed booking record and echoed back on the confirmation screen as if it were authoritative.

**Impact:** This is a classic parameter/price-tampering vulnerability (OWASP A04:2021 Insecure Design; CWE-840 Business Logic Errors) — the server performs a *computation* on an untrusted input while presenting the result as a trust decision it has already made. `BR-006` ("Total price is also computed on the backend at booking time (for record) = per-passenger price × passenger count") is not actually enforced against any authoritative source of per-passenger price; it is enforced only as an arithmetic identity on whatever value the client supplies. Payment processing is explicitly Out of Scope for this MVP (`docs/requirements.md` Section 7, item 5), so there is no direct monetary-transaction impact today, but the stored `Booking` record's price/fare integrity — the exact guarantee `BR-006` and `NFR-DATA-002` are meant to provide — is not actually enforced, and this would be a direct path to fraudulent low/zero-cost bookings (or negative-price display defects) if payment or any downstream financial process were added without first closing this gap. `CabinClass` is similarly unconstrained on this endpoint (any string value is accepted and persisted, not just the three valid values enforced on search), which is a secondary, lower-impact instance of the same "presence-only, not correctness" validation gap.

**Recommendation:** Do not trust client-submitted price/fare fields at booking time. At minimum, add a positive-value check (`PricePerPassenger > 0`, `BaseFare > 0` if retained) and a `CabinClass` allow-list check (reuse `SearchRequestValidator`'s named constant, per DP-015) as an immediate, low-effort mitigation. The structurally correct fix — consistent with the "not an opaque identifier" architecture note already being revisited for other reasons — is for the server to re-resolve the per-passenger price authoritatively at booking time (e.g., by having the aggregator/providers expose a lookup keyed by `Provider`+`FlightNumber`+`CabinClass`+date, and having `BookingService` use that resolved price rather than the client-submitted one, mirroring how `RouteTypeResolver` already re-resolves route type authoritatively server-side per BR-003/DP-016). This does not need to be resolved in this phase — it is a findings-only review — but it must be resolved or explicitly accepted as risk by the human Product Owner before this MVP's price-tampering exposure is considered acceptable, per CLAUDE.md §21.

**Required fix:** Either (a) re-resolve `PricePerPassenger`/`BaseFare` server-side from the same provider/pricing logic the search endpoint uses, ignoring the client-submitted value entirely, or (b) at minimum add server-side range/allow-list validation (`PricePerPassenger > 0`, `CabinClass` in the same named allow-list used by `SearchRequestValidator`) in `BookingRequestValidator.ValidateStructure`, with unit tests proving a fabricated/zero/negative price or an invalid cabin class is rejected with a 400.

---

### SEC-002 — `POST /api/bookings` has no upper bound on passenger count / array size, unlike the search endpoint

| Field | Value |
|---|---|
| Severity | Medium |
| File/area | `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (`ValidateStructure`, lines 25–61); contrast with `src/SkyRoute.Application/Validation/SearchRequestValidator.cs` (`ValidatePassengerCount`, lines 90–96) |
| Status | Open |

**Evidence:** `SearchRequestValidator.ValidatePassengerCount` explicitly bounds `PassengerCount` to 1–9 (also enforced by `[Range(1, 9)]` on `SearchRequest.PassengerCount`). `BookingRequestValidator.ValidateStructure` has no equivalent bound — it only checks that `request.PassengerCount == passengers.Count`:

```csharp
if (request.PassengerCount != passengers.Count)
{
    AddError(errors, "passengerCount", "Passenger count must match the number of passenger records submitted.");
}
```

A client can submit a `BookingRequest` with, for example, `passengerCount: 50000` and 50,000 matching `PassengerRequest` entries directly to `POST /api/bookings` (bypassing the Angular UI, which never allows selecting more than 9 in the prior search step). Each entry is individually well-formed enough to pass the per-field regex checks (`FullNameRegex`, `EmailRegex`, then later `PassportRegex`/`NationalIdRegex` in `ValidateDocuments`), so the request would proceed through full validation, price computation (compounding with SEC-001 — `totalPrice = PricePerPassenger * 50000`), reference generation, and persistence into the singleton in-memory `_bookings` dictionary, which is never evicted or capped (BR-008/ASM-001).

**Impact:** OWASP A04:2021 Insecure Design / CWE-770 (Allocation of Resources Without Limits) — an unauthenticated caller (no auth boundary exists in this MVP, per BR-010) can submit an arbitrarily large request body to grow server memory/CPU usage per request and grow the unbounded in-memory booking store, with no rate limiting, no request-size limit configured, and no per-request passenger cap on this specific endpoint. Kestrel's default `MaxRequestBodySize` (~28.6 MB) still permits tens of thousands of passenger records per single request. This is a resource-exhaustion/DoS risk, not a data-confidentiality risk, and is bounded by the fact this is a local-only MVP with no internet-facing deployment (`docs/requirements.md` Section 7, item 14) — but the code itself contains no enforcement of the same bound the search flow already enforces.

**Recommendation:** Add the same 1–9 (or a deliberately chosen MVP-appropriate) passenger-count bound to `BookingRequestValidator.ValidateStructure`, consistent with `SearchRequestValidator.ValidatePassengerCount`, so the two endpoints enforce the same business rule rather than relying on the Angular UI never allowing more than 9 in practice. Consider also configuring an explicit Kestrel `MaxRequestBodySize`/`KestrelServerLimits.MaxRequestBufferSize` appropriate to the expected request shape as a defense-in-depth measure, independent of the passenger-count check.

**Required fix:** Add an explicit passenger-count range check (e.g., 1–9, matching search) to `BookingRequestValidator.ValidateStructure`, returning a 400 field error (`passengerCount`) when exceeded, with a unit test proving the bound is enforced.

---

### SEC-003 — No HTTP security response headers configured on the backend

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `src/SkyRoute.Api/Program.cs` (pipeline configuration, lines 86–102) |
| Status | Open |

**Evidence:** The ASP.NET Core pipeline in `Program.cs` registers `ApiExceptionMiddleware`, `UseHttpsRedirection()`, `UseCors(...)`, `UseAuthorization()`, and `MapControllers()` — no middleware or header configuration adds `X-Content-Type-Options: nosniff`, `X-Frame-Options`/`Content-Security-Policy: frame-ancestors`, `Referrer-Policy`, `Strict-Transport-Security` (HSTS — note `UseHsts()` is also not called, only `UseHttpsRedirection()`), or a `Content-Security-Policy` for API responses. `frontend/src/index.html` likewise has no `<meta http-equiv="Content-Security-Policy">` tag (this is a plain, unmodified Angular CLI scaffold).

**Impact:** OWASP A05:2021 (Security Misconfiguration) — general hardening gap. Practical exploitability is low for this specific app today: responses are JSON (`application/json`/`application/problem+json`) consumed only by the Angular SPA, there is no HTML rendering of API responses, and no `innerHTML`/`bypassSecurityTrust*` usage was found anywhere in the frontend (confirmed by direct search — all template bindings use standard Angular interpolation/property binding, which auto-escapes by default), so stored/reflected XSS risk via these API responses is low. This is a defense-in-depth gap, not an actively exploitable vulnerability given the current code.

**Recommendation:** Add standard security headers via middleware (e.g., a small custom middleware or `UseHsts()` + manual header additions in the response pipeline) for `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer` (or `same-origin`), and `X-Frame-Options: DENY` at minimum; add a restrictive `Content-Security-Policy` meta tag or header for the Angular app before any production/internet-facing deployment is considered (this MVP is local-only per requirements.md Section 7 item 14, so this is lower urgency than SEC-001/002, but should not be silently dropped if/when a non-local target is introduced, similar in spirit to CR-004's environment-config gap).

**Required fix:** Add the missing security response headers to the backend pipeline and a CSP to the frontend shell, or explicitly document (mirroring CR-004's recommendation) that this is intentionally deferred because the MVP is local-only, so the gap is a documented decision rather than an oversight.

---

### SEC-004 — `EmailPattern` regex has no explicit upper-bound length, and no field-length pre-check precedes regex evaluation

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `src/SkyRoute.Application/Validation/DocumentPatterns.cs` (`EmailPattern`, line 17); `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (`EmailRegex.IsMatch(passenger.Email)`, line 54) |
| Status | Open |

**Evidence:** `EmailPattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"` has three unbounded (`+`) or open-ended (`{2,}`) quantifiers and no maximum length constraint, unlike `FullNamePattern` (`.{2,100}`, explicitly bounded), `PassportPattern` (`{6,9}`, bounded), and `NationalIdPattern` (`{5,20}`, bounded). No calling code (`BookingRequestValidator.ValidateStructure`) performs a length pre-check (e.g., rejecting inputs over a reasonable email length such as 254 characters per RFC 5321) before invoking `EmailRegex.IsMatch(...)`.

**Impact:** CWE-1333 (Inefficient Regular Expression Complexity) — this specific pattern's quantifiers are sequential rather than nested, so it is not classically catastrophic (exponential) ReDoS, but backtracking cost still scales non-trivially (roughly quadratic in the worst case) with unbounded input length, since there is nothing preventing a caller from submitting a multi-megabyte string in the `email` field. Combined with SEC-002 (no cap on the number of passenger records per booking request), this compounds: a request with many passengers, each carrying an oversized `email` value, multiplies the per-field regex cost across the whole request. In isolation, low severity; as a compounding factor with SEC-002, worth closing at the same time.

**Recommendation:** Add an explicit maximum-length pre-check (e.g., reject `Email` values over 254 characters, matching RFC 5321 practical limits) before evaluating `EmailRegex`, and/or add an upper bound to the regex quantifiers themselves (e.g., `{1,64}` for the local part, `{1,255}` for the domain part) to make the existing bounded-quantifier convention used by the other three patterns in this file consistent across all four.

**Required fix:** Add a length guard (`passenger.Email.Length <= 254` or similar) ahead of the regex check in `BookingRequestValidator.ValidateStructure`, and/or tighten `DocumentPatterns.EmailPattern` with explicit upper-bound quantifiers, with a unit test proving an oversized input is rejected without excessive processing time.

---

## Findings Summary Table

| ID | Severity | Area | Status |
|---|---|---|---|
| SEC-001 | **High** | `BookingRequestValidator`/`BookingService` — client-supplied flight-fare snapshot trusted without validation; price/fare tampering | Open |
| SEC-002 | Medium | `BookingRequestValidator.ValidateStructure` — no upper bound on booking passenger count/array size | Open |
| SEC-003 | Low | `Program.cs` / `index.html` — no HTTP security response headers or CSP configured | Open |
| SEC-004 | Low | `DocumentPatterns.EmailPattern` — no explicit length bound before/within regex evaluation | Open |

**Totals: 0 Critical, 1 High, 1 Medium, 2 Low.**

---

## Areas Explicitly Reviewed — No Finding Raised

- **Authentication/authorization (BR-010, Out of Scope items 1/23):** Confirmed as a documented, approved MVP scope decision, not a finding. `AuthService` (frontend) is a no-op stub with no token storage/header injection; `Program.cs`'s `AddAuthorization` policy (`RequireBookingOwner`) is registered but never applied via `[Authorize]` anywhere, consistent with NFR-SEC-008's "structural seam, zero runtime cost" intent. No `ClaimsPrincipal`/`IIdentity` usage found anywhere in `src/` (grep-confirmed).
- **CORS (NFR-SEC-006):** Explicitly origin-restricted via `appsettings.json` `Cors:AllowedOrigins` (`http://localhost:4200`), no wildcard, no `AllowCredentials()`. Compliant.
- **PII/secret logging (NFR-SEC-003, NFR-PRIV-002, NFR-OBS-004):** Only three `ILogger` call sites exist in `src/` (`ApiExceptionMiddleware` — method/path/trace ID only; `FlightAggregatorService` — provider name only; `BookingService` — booking reference only). No document number, email, or full passenger record is logged anywhere. Compliant.
- **Error response handling (NFR-SEC-002, BR-011, FR-069):** `ApiExceptionMiddleware` returns a fixed, generic `application/problem+json` body on any unhandled exception (never a stack trace, exception type, or internal message); no `UseDeveloperExceptionPage()` is registered at all (so this holds even under `ASPNETCORE_ENVIRONMENT=Development`, confirmed via `launchSettings.json`); 400s are `ValidationProblem(...)` results built from the named validators' own field-keyed messages, not framework/exception internals. Compliant.
- **Secret handling (NFR-SEC-004, NFR-DEPLOY-004):** Grep across `src/` and `frontend/src/` for `password`/`secret`/`apikey`/`connectionstring`/`privatekey`/key-like patterns found zero matches. `appsettings.json`/`appsettings.Development.json` contain only `Logging` and `Cors` configuration, no secrets. Compliant.
- **Injection (OWASP A03:2021):** No SQL/NoSQL/OS command construction from user input anywhere (in-memory `ConcurrentDictionary` persistence only, per BR-008/ASM-001 — Out of Scope item 15/22); no dynamic code execution; no file-path construction from user input. Not applicable to this architecture beyond the price-tampering/business-logic concern captured separately in SEC-001.
- **XSS (frontend):** Zero `innerHTML`, `[innerHTML]`, or `bypassSecurityTrust*` usages found anywhere in `frontend/src` (grep-confirmed) — all dynamic content is rendered via standard Angular interpolation/property binding, which auto-escapes by default. `BookingRequestValidator.FullNamePattern`/`SearchRequestValidator` do not restrict passenger full names from containing HTML-special characters, but since nothing in the frontend renders that value via `innerHTML`, stored-XSS risk via this specific field is not currently exploitable in this codebase.
- **Randomness (BR-004):** `BookingReferenceGenerator` uses `System.Security.Cryptography.RandomNumberGenerator.GetInt32(...)`, not `System.Random`. Compliant with NFR-SEC-004/BR-004's cryptographic-randomness requirement (booking references are not security tokens, but the stronger source is already used regardless).
- **Multi-tenancy/IDOR (DP-TENANT-*):** `ITenantContext.TenantId` is a fixed, server-resolved `"default"` value (`DefaultTenantContext`), never derived from any client-controlled header/parameter — no tenant-ID-based IDOR vector exists. `InMemoryBookingStore.GetByReferenceAsync` is defined on the interface but no controller exposes a `GET /api/bookings/{reference}` endpoint in this MVP (confirmed — only `SearchController`/`BookingController` exist), so there is currently no booking-enumeration/IDOR surface at all.
- **Dependency risk:** Backend has zero third-party NuGet package references (`SkyRoute.Api.csproj`/`SkyRoute.Application.csproj`/`SkyRoute.Infrastructure.csproj` reference only the .NET 10 SDK and each other — no `<PackageReference>` entries). Frontend (`frontend/package.json`) depends only on current-generation Angular 22.x, RxJS 7.8, and standard Angular CLI/build tooling — no outdated or unusual third-party runtime dependency was identified by direct inspection. Per the task brief's scope guidance, no `dotnet list package --vulnerable` or `npm audit` command was run (Bash access for this review is restricted to `git status`/`git diff`/`git diff --stat`); this is a reasonable follow-up for the DevOps/CI pipeline (Phase 18 or later) rather than a finding here, given the current dependency surface is minimal.
- **CR-001–CR-005 (Phase 15):** Cross-checked; none are security findings and none are re-reported here, per the task brief's instruction.

---

## Overall Recommendation

**SEC-001 (High) requires human Product Owner review before proceeding further**, per CLAUDE.md §21 ("Always stop for human approval before... accepting unresolved Critical/High findings") and the Definition of Done ("Critical and High findings are resolved or explicitly accepted by the human user"). This is a genuine price/fare-integrity gap in the booking flow's core business rule (BR-006), even though it is bounded in today's practical impact by payment processing being explicitly Out of Scope for this MVP (`docs/requirements.md` Section 7, item 5) and by the MVP being local-only/non-internet-facing (Section 7, item 14).

SEC-002 (Medium), SEC-003 (Low), and SEC-004 (Low) are hardening/defense-in-depth gaps consistent in severity profile with Phase 15's code review findings and do not independently require a human decision gate — they are appropriately deferred to Phase 19 (Findings Fixes) alongside CR-001–CR-005 and QA-001/002/004/005.

**Recommendation:** Do not proceed to Phase 17 (Accessibility Review) until the human Product Owner has reviewed SEC-001 and either (a) approves a fix be scheduled ahead of/within Phase 19, or (b) explicitly accepts the risk for this MVP given its local-only, no-payment scope. This is a phase-sequencing recommendation for the SDLC Orchestrator to act on, not a decision this review is authorized to make on its own.

---

*End of Security Review Report — Phase 16.*
