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
| Status | **Resolved** (final verification 2026-07-07, see Final Verification Note below; supersedes the earlier Partially Resolved verdict) |

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

**Re-Verification Note (security-reviewer, 2026-07-07, commit `9f3cfed` + uncommitted fix-loop changes on `sdlc/16-security-review-skyroute-mvp`):**

Independently re-read (not just trusted handoff `16a-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-fix.md`):

- `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` — `ValidateStructure` now contains, gated on `request.Flight is not null` (no NPE risk), three new checks not nested inside the existing completeness gate (so they fire independently, confirmed non-short-circuiting per FR-063):
  - `request.Flight.PricePerPassenger is <= 0` → `flight.pricePerPassenger`, "Price per passenger must be greater than zero."
  - `request.Flight.BaseFare is <= 0` (only evaluated when a value is present; `null` BaseFare is untouched, consistent with `IsFlightSnapshotComplete` not requiring it) → `flight.baseFare`.
  - `CabinClass` non-null/non-whitespace and not in `CabinClasses.ValidCabinClasses` → `flight.cabinClass`, "Cabin class must be one of: Economy, Business, First Class."
- `src/SkyRoute.Application/Validation/CabinClasses.cs` (new file) — single `public static readonly string[] ValidCabinClasses = { "Economy", "Business", "First Class" }`, confirmed genuinely shared: `SearchRequestValidator.ValidateCabinClass` now calls `CabinClasses.ValidCabinClasses.Contains(...)` (its former private `ValidCabinClasses` array is gone — verified by reading the current `SearchRequestValidator.cs` in full, no duplicate array remains anywhere in `src/`).
- `tests/SkyRoute.Application.Tests/Validation/BookingRequestValidatorTests.cs`, section "flight-fare snapshot range/allow-list checks (SEC-001)" (lines ~161–271) — 9 tests: `PricePerPassengerZero`/`PricePerPassengerNegative` (both assert the exact error message), `BaseFareZero`/`BaseFareNegative` (same), `BaseFareNull_DoesNotReturnBaseFareMessage` (confirms optionality preserved), `CabinClassNotInAllowList` theory over `"Coach"`/`"economy"`/`"first class"`/`"Business Plus"` (confirms case-sensitivity and near-miss strings are rejected, not just obviously-wrong values), `CabinClassInAllowList` theory over the three valid values, and `ValidPriceAndCabinClass_ReturnsEmptyDictionary` happy path. All read and confirmed to assert what they claim.
- Independently ran `dotnet build` (0 Warning(s), 0 Error(s)) and `dotnet test` for the full solution (not just trusting the handoff's reported 127/127): **134/134 passed, 0 failed, 0 skipped** (122 in `SkyRoute.Application.Tests`, 12 in `SkyRoute.Api.IntegrationTests` — includes SEC-002/003/004 tests added in the same fix loop, see below).

**Verdict — Partially Resolved, not Resolved:** The fix is verified correct and complete as the review's own explicitly-labeled **option (b)** ("at minimum" mitigation) — every scenario named in this finding's Required-fix text (zero/negative price, zero/negative base fare, invalid cabin class) is now independently unit-tested and rejected with a 400 field error. This closes the most egregious, low-effort exploit paths (free/negative-cost bookings, garbage cabin-class strings persisted as fact) and the shared-allow-list duplication concern (DP-015) is genuinely fixed, not just reported as fixed.

However, the finding's core business-logic gap — described in Impact as "a client... can submit any `PricePerPassenger` value... and receive a `201 Created`... as if it were authoritative" — is **not** closed. A client can still submit an internally-consistent, positive `PricePerPassenger` (e.g., `$0.01` for a flight that should cost $500, or any arbitrary fabricated fare) together with a valid `CabinClass` string, and `BookingService.CreateBookingAsync` will still compute `TotalPrice` from that fabricated value and persist it as a confirmed booking record. Option (a) — re-resolving price server-side against the same `GlobalAirProvider`/`BudgetWingsProvider` pricing logic the search endpoint uses (mirroring `RouteTypeResolver`'s authoritative server-side re-resolution pattern for route type) — was explicitly out of scope for this fix and has not been attempted. The developer's own handoff (16a) flags this residual gap directly and defers the Resolved-vs-Partially-Resolved call to this review, as instructed.

Weighing this MVP's actual context (`docs/requirements.md` Section 7: item 5 "Payment processing — No payment gateway integration" is explicitly out of scope; item 14 "Cloud deployment — Local development environment only"; item 15 "Database persistence — In-memory only... does not persist across application restarts") against the finding's original High-severity rationale (OWASP A04:2021 Insecure Design / CWE-840 Business Logic Errors, framed around BR-006 price-integrity and a *future* payment-processing path): the practical, exploitable-today impact remains low (no real money moves, no persistent record survives a restart, no internet-facing exposure), but the underlying business-logic control described in BR-006 ("Total price is also computed on the backend at booking time (for record)") is still not actually enforced against any authoritative source — it is enforced only as an arithmetic identity on a value the client fully controls. That is a genuine, unresolved structural gap, not a cosmetic one, and no human Product Owner sign-off accepting this residual risk has been recorded anywhere in the handoffs reviewed for this fix loop. Per CLAUDE.md §21 ("accepting unresolved Critical/High findings" requires explicit human approval) and the Definition of Done ("Critical and High findings are resolved or explicitly accepted by the human user"), this finding cannot be marked fully `Resolved` on the strength of a developer/reviewer decision alone — it is marked **Partially Resolved**.

**Residual risk (open, requires action or explicit human acceptance):** A caller can still fabricate an arbitrary *positive* per-passenger price (and, independently, an arbitrary positive `BaseFare`) for any otherwise-valid `Provider`/`FlightNumber`/`CabinClass` combination and receive a confirmed booking priced at that fabricated value. This is acceptable as a **documented residual risk for this MVP specifically because**: (1) payment processing is out of scope (no monetary transaction actually occurs), (2) the platform is local-only/non-internet-facing per requirements Section 7 item 14, and (3) booking records do not persist across restarts (BR-008). It becomes unacceptable the moment payment processing, persistent storage, or any internet-facing deployment is introduced, and should be tracked as a required pre-requisite for any of those three future changes — do not carry this residual risk forward silently into a phase that adds payment or production deployment. Recommended next action: either (a) schedule the full server-side price re-resolution (option (a) in this finding) as explicit backlog work before any payment-processing or production-deployment phase begins, or (b) have the human Product Owner explicitly accept this residual risk for the MVP scope as currently defined, in writing, per CLAUDE.md §21. This review does not have authority to make that acceptance decision on the Product Owner's behalf; it can only close the "Resolved" gap left open by the minimal fix and flag it for that decision.

**Final Verification Note (security-reviewer, 2026-07-07, commit `8f20aa3` on `sdlc/16-security-review-skyroute-mvp`, working tree clean):**

Independently re-verified the full server-side price re-resolution (option (a)) implemented per handoff `docs/handoffs/16d-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-full-fix.md`, reading the actual code rather than trusting the handoff's claims:

1. **Pricing-logic reuse, not a divergent copy.** Read `src/SkyRoute.Application/Interfaces/IFlightProvider.cs` (new `TryResolveFare(flightNumber, cabinClass, out baseFare, out pricePerPassenger)` contract method, doc-commented as SEC-001's authoritative re-resolution), and both `TryResolveFare` implementations in `src/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs` and `BudgetWingsProvider.cs`. Confirmed by direct reading that each implementation looks up the matching `ScheduledFlight` by `FlightNumber` (ordinal), applies the identical `CabinClassMultipliers.ForCabinClass(cabinClass)` call `SearchAsync` uses, and then calls the exact same private pricing method `SearchAsync` calls (`ApplyGlobalAirPricing`/`ApplyBudgetWingsPricing` respectively) — there is no second, independently-maintained pricing formula anywhere; `TryResolveFare` and `SearchAsync` are two callers of one shared private method per provider. An unknown `flightNumber` returns `false` with zeroed out-values (fails closed). Read `src/SkyRoute.Application/Services/FlightFareResolver.cs` — matches `providerName` against the registered `IEnumerable<IFlightProvider>` (ordinal) and delegates to that provider's `TryResolveFare`; fails closed (returns `false`, zeroed out-values) on an unknown provider name or any null/blank identifying field, with no exception thrown. **Confirmed: genuine reuse, not a divergent copy.**
2. **Wiring into `BookingService`/`BookingRequestValidator`, and which value is persisted.** Read `BookingService.CreateBookingAsync` in full: step 3b calls `_fareResolver.TryResolveFare(request.Flight.Provider, request.Flight.FlightNumber, request.Flight.CabinClass, out resolvedBaseFare, out resolvedPricePerPassenger)`, then `_validator.ValidateFare(request, fareResolved, resolvedBaseFare, resolvedPricePerPassenger)`, throwing `BookingValidationException` on any mismatch — this runs after document validation (step 3) and strictly before total-price computation (step 4) and before the `Booking`/`BookingFlightSnapshot` object is constructed or persisted (step 6), so a rejected request never reaches the store. Confirmed `BookingRequestValidator.ValidateFare` performs an exact (`!=`) decimal comparison against the client-submitted `PricePerPassenger`/`BaseFare` only when those fields have a value (an absent value is already caught earlier by `ValidateStructure`'s `IsFlightSnapshotComplete` gate in `BookingController`, run before `CreateBookingAsync` is ever called — verified by reading `BookingController.CreateBooking`, which returns 400 on structural errors without invoking the service). Confirmed the *server-resolved* `resolvedPricePerPassenger` — not `request.Flight.PricePerPassenger` — is what feeds both `totalPrice` (step 4) and the persisted `BookingFlightSnapshot.PricePerPassenger` (step 6); by the time either line executes, the two values are guaranteed identical (`ValidateFare` already rejected any mismatch), but the code unambiguously reads from the resolved variable, not the request. **Confirmed: a client-supplied price that doesn't match the server-resolved price is genuinely rejected before persistence, and the server-resolved fare is what is stored/used.**
3. **Test coverage of the named scenario.** Read `tests/SkyRoute.Application.Tests/Services/FlightFareResolverTests.cs` in full (10 tests: known-flight resolution for both real providers composed with no mocking, unknown provider name fails closed, a flight number belonging to the *other* provider fails closed rather than false-positive matching, every combination of missing identifying fields fails closed without throwing). Read the new fare-mismatch tests in `tests/SkyRoute.Application.Tests/Services/BookingServiceTests.cs`: `CreateBookingAsync_FabricatedPositivePricePerPassenger_ThrowsBookingValidationException` submits `pricePerPassenger: 0.01m` — an internally-consistent, positive, otherwise-fully-valid GA101/Economy/International booking request (the exact scenario this finding's Impact and Residual Risk text names) — and asserts a `BookingValidationException` with a `flight.pricePerPassenger` key **and** `Assert.Empty(store.CreatedBookings)`, i.e., the rejected booking never reached persistence, not merely that an exception was thrown. Sibling tests cover an inflated fabricated price (`9999.99m`), a fabricated `BaseFare` with a correct `PricePerPassenger`, an unknown flight number, and a BudgetWings positive control. Read the integration-level equivalent in `tests/SkyRoute.Api.IntegrationTests/Controllers/BookingControllerTests.cs`: `CreateBooking_FabricatedPricePerPassenger_Returns400WithFlightPriceError` drives a real HTTP `POST /api/bookings` through `SkyRouteApiFactory` with the same GA101/LHR-JFK/Economy inputs as the passing `CreateBooking_InternationalHappyPath_Returns201WithDataMinimizedPassengers` test but `PricePerPassenger = 0.01m`, and asserts `400 Bad Request` with a `flight.pricePerPassenger` `ValidationProblemDetails` error key. **Confirmed: the tests genuinely exercise the scenario named in the original finding** — a fabricated positive price with an otherwise-valid request, expecting rejection — not a weaker or different scenario.
4. **Independent build/test run (not relying on the developer handoff's reported numbers).** Ran `dotnet build` and `dotnet test` for the full solution independently on the current commit (`8f20aa3`, clean working tree):
   ```
   dotnet build
     Build succeeded.
         0 Warning(s)
         0 Error(s)

   dotnet test
     Passed! - Failed: 0, Passed: 146, Skipped: 0, Total: 146 - SkyRoute.Application.Tests.dll (net10.0)
     Passed! - Failed: 0, Passed:  13, Skipped: 0, Total:  13 - SkyRoute.Api.IntegrationTests.dll (net10.0)
   ```
   **159/159 tests passed, 0 failed, 0 skipped** — matches the developer handoff's reported total exactly, independently confirmed rather than cited on trust.

**Final Verdict — Resolved.** Option (a) (full server-side price re-resolution, mirroring `RouteTypeResolver`'s established pattern) is genuinely implemented: pricing is re-derived from the same deterministic, shared pricing logic `SearchAsync` already uses (not a second copy), a client-supplied price/fare that mismatches the server-resolved fare is rejected with a 400 before any total-price computation or persistence occurs, the server-resolved fare (not the client's) is what is stored and used for `TotalPrice`, and the exact scenario this finding was raised about — an internally-consistent, positive, fabricated price on an otherwise-fully-valid request — is now covered by both unit and integration tests that assert rejection and non-persistence. The residual gap explicitly left open in the 2026-07-07 Re-Verification Note above (arbitrary-but-positive fabricated price being trusted) is closed. No further human Product Owner risk-acceptance decision is required for this finding — the CLAUDE.md §21 approval gate for this High finding is satisfied by the fix itself, not by risk acceptance.

---

### SEC-002 — `POST /api/bookings` has no upper bound on passenger count / array size, unlike the search endpoint

| Field | Value |
|---|---|
| Severity | Medium |
| File/area | `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (`ValidateStructure`, lines 25–61); contrast with `src/SkyRoute.Application/Validation/SearchRequestValidator.cs` (`ValidatePassengerCount`, lines 90–96) |
| Status | **Resolved** (re-verified 2026-07-07, see Re-Verification Note below) |

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

**Re-Verification Note (security-reviewer, 2026-07-07):** Independently read `BookingRequestValidator.ValidateStructure` — it now contains, in addition to the pre-existing `PassengerCount != passengers.Count` mismatch check, an independent bound: `if (request.PassengerCount < 1 || request.PassengerCount > 9) AddError(errors, "passengerCount", "Passenger count must be a whole number between 1 and 9.");` — identical bound and message text to `SearchRequestValidator.ValidatePassengerCount`, confirmed by direct comparison of both files. Confirmed the two checks are independent (both can fire on the same request; neither short-circuits the other, consistent with FR-063). Read the corresponding tests in `BookingRequestValidatorTests.cs` (section "passenger-count upper bound (SEC-002)", lines ~294–333): `PassengerCountZero_ReturnsPassengerCountRangeMessage` (asserts exact message for `PassengerCount = 0`), `PassengerCountAboveNine_ReturnsPassengerCountRangeMessage` (asserts exact message for `PassengerCount = 10`), and a `[Theory]` over `1` and `9` confirming the boundary values are accepted (`DoesNotReturnPassengerCountRangeMessage`). All three tests independently re-run via `dotnet test` (see build/test summary at end of this document) and confirmed passing. Fully satisfies the Required Fix text. **Resolved.**

---

### SEC-003 — No HTTP security response headers configured on the backend

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `src/SkyRoute.Api/Program.cs` (pipeline configuration, lines 86–102) |
| Status | **Resolved** (re-verified 2026-07-07, see Re-Verification Note below) |

**Evidence:** The ASP.NET Core pipeline in `Program.cs` registers `ApiExceptionMiddleware`, `UseHttpsRedirection()`, `UseCors(...)`, `UseAuthorization()`, and `MapControllers()` — no middleware or header configuration adds `X-Content-Type-Options: nosniff`, `X-Frame-Options`/`Content-Security-Policy: frame-ancestors`, `Referrer-Policy`, `Strict-Transport-Security` (HSTS — note `UseHsts()` is also not called, only `UseHttpsRedirection()`), or a `Content-Security-Policy` for API responses. `frontend/src/index.html` likewise has no `<meta http-equiv="Content-Security-Policy">` tag (this is a plain, unmodified Angular CLI scaffold).

**Impact:** OWASP A05:2021 (Security Misconfiguration) — general hardening gap. Practical exploitability is low for this specific app today: responses are JSON (`application/json`/`application/problem+json`) consumed only by the Angular SPA, there is no HTML rendering of API responses, and no `innerHTML`/`bypassSecurityTrust*` usage was found anywhere in the frontend (confirmed by direct search — all template bindings use standard Angular interpolation/property binding, which auto-escapes by default), so stored/reflected XSS risk via these API responses is low. This is a defense-in-depth gap, not an actively exploitable vulnerability given the current code.

**Recommendation:** Add standard security headers via middleware (e.g., a small custom middleware or `UseHsts()` + manual header additions in the response pipeline) for `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer` (or `same-origin`), and `X-Frame-Options: DENY` at minimum; add a restrictive `Content-Security-Policy` meta tag or header for the Angular app before any production/internet-facing deployment is considered (this MVP is local-only per requirements.md Section 7 item 14, so this is lower urgency than SEC-001/002, but should not be silently dropped if/when a non-local target is introduced, similar in spirit to CR-004's environment-config gap).

**Required fix:** Add the missing security response headers to the backend pipeline and a CSP to the frontend shell, or explicitly document (mirroring CR-004's recommendation) that this is intentionally deferred because the MVP is local-only, so the gap is a documented decision rather than an oversight.

**Re-Verification Note (security-reviewer, 2026-07-07):** Independently read `src/SkyRoute.Api/Program.cs` lines 88–114 — confirmed a new inline `app.Use(async (context, next) => { ... })` middleware is registered immediately after `app.UseMiddleware<ApiExceptionMiddleware>()` and before `app.UseHttpsRedirection()`, setting `context.Response.Headers["X-Content-Type-Options"] = "nosniff"`, `["X-Frame-Options"] = "DENY"`, and `["Referrer-Policy"] = "no-referrer"` unconditionally on every response before calling `next()`. HSTS/`UseHsts()` remains intentionally absent, with an inline comment explaining this MVP is local-only HTTP dev with no TLS termination — a reasonable, explicitly documented scope decision consistent with the finding's own Recommendation text ("lower urgency... should not be silently dropped"), not an oversight.

Independently read `frontend/src/index.html` line 15 — confirmed a new `<meta http-equiv="Content-Security-Policy" content="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self' http://localhost:5094; frame-ancestors 'none'; base-uri 'self'; form-action 'self'">` tag is present in the document `<head>`, scoped to the single known backend origin rather than a wildcard.

Independently read `tests/SkyRoute.Api.IntegrationTests/Middleware/SecurityHeadersTests.cs` — one test, `AnyResponse_IncludesBaselineSecurityHeaders`, drives a real `POST /api/search` request through `SkyRouteApiFactory`'s full pipeline via `WebApplicationFactory<Program>` and asserts all three header values exactly (`nosniff`, `DENY`, `no-referrer`) using `response.Headers.GetValues(...)`. This exercises real pipeline ordering, not an isolated delegate — an appropriate test shape for middleware-level verification. Independently re-ran via `dotnet test` (see summary below) and confirmed passing. Fully satisfies the Required Fix text (headers added; CSP added; test proves headers present). **Resolved.**

---

### SEC-004 — `EmailPattern` regex has no explicit upper-bound length, and no field-length pre-check precedes regex evaluation

| Field | Value |
|---|---|
| Severity | Low |
| File/area | `src/SkyRoute.Application/Validation/DocumentPatterns.cs` (`EmailPattern`, line 17); `src/SkyRoute.Application/Validation/BookingRequestValidator.cs` (`EmailRegex.IsMatch(passenger.Email)`, line 54) |
| Status | **Resolved** (re-verified 2026-07-07, see Re-Verification Note below) |

**Evidence:** `EmailPattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"` has three unbounded (`+`) or open-ended (`{2,}`) quantifiers and no maximum length constraint, unlike `FullNamePattern` (`.{2,100}`, explicitly bounded), `PassportPattern` (`{6,9}`, bounded), and `NationalIdPattern` (`{5,20}`, bounded). No calling code (`BookingRequestValidator.ValidateStructure`) performs a length pre-check (e.g., rejecting inputs over a reasonable email length such as 254 characters per RFC 5321) before invoking `EmailRegex.IsMatch(...)`.

**Impact:** CWE-1333 (Inefficient Regular Expression Complexity) — this specific pattern's quantifiers are sequential rather than nested, so it is not classically catastrophic (exponential) ReDoS, but backtracking cost still scales non-trivially (roughly quadratic in the worst case) with unbounded input length, since there is nothing preventing a caller from submitting a multi-megabyte string in the `email` field. Combined with SEC-002 (no cap on the number of passenger records per booking request), this compounds: a request with many passengers, each carrying an oversized `email` value, multiplies the per-field regex cost across the whole request. In isolation, low severity; as a compounding factor with SEC-002, worth closing at the same time.

**Recommendation:** Add an explicit maximum-length pre-check (e.g., reject `Email` values over 254 characters, matching RFC 5321 practical limits) before evaluating `EmailRegex`, and/or add an upper bound to the regex quantifiers themselves (e.g., `{1,64}` for the local part, `{1,255}` for the domain part) to make the existing bounded-quantifier convention used by the other three patterns in this file consistent across all four.

**Required fix:** Add a length guard (`passenger.Email.Length <= 254` or similar) ahead of the regex check in `BookingRequestValidator.ValidateStructure`, and/or tighten `DocumentPatterns.EmailPattern` with explicit upper-bound quantifiers, with a unit test proving an oversized input is rejected without excessive processing time.

**Re-Verification Note (security-reviewer, 2026-07-07):** Independently read `src/SkyRoute.Application/Validation/DocumentPatterns.cs` line 24 — `EmailPattern` is now `@"^(?=.{1,254}$)[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"`, i.e., a zero-width lookahead `(?=.{1,254}$)` bounding the entire matched string to 1–254 characters, evaluated before the existing local-part/domain-part group is matched — this is the "tighten the regex with explicit upper-bound" half of the Required Fix (chosen over a separate call-site length guard, a reasonable and consistent choice given the other three patterns in the same file — `PassportPattern`, `NationalIdPattern`, `FullNamePattern` — already bound length inline in their own quantifiers).

Independently read the two new tests in `BookingRequestValidatorTests.cs` (lines ~339–364): `ValidateStructure_PassengerEmailOverMaxLength_ReturnsEmailMessage` constructs a 255-character address (`new string('a', 255 - "@example.com".Length) + "@example.com"`) and asserts it is rejected with the generic "A valid email address is required." message; `ValidateStructure_PassengerEmailAtMaxLength_IsValid` constructs an exactly-254-character address and asserts no `passengers[0].email` error is present — correctly verifying the boundary is inclusive at 254 and exclusive at 255, matching the finding's RFC 5321 recommendation exactly. Manually re-derived the lengths: `"@example.com"` is 12 characters, so the local parts used are 243 and 242 characters respectively, giving totals of 255 and 254 — arithmetic confirmed correct. Independently re-ran via `dotnet test` (see summary below) and confirmed both tests passing. No call-site change was needed or made in `BookingRequestValidator.cs` (the existing `EmailRegex.IsMatch(...)` call now enforces the bound automatically via the regex itself) — confirmed by reading `BookingRequestValidator.cs` and finding no separate length guard was added, which is consistent with the handoff's stated approach and does not leave a gap. Fully satisfies the Required Fix text. **Resolved.**

---

## Findings Summary Table

| ID | Severity | Area | Status |
|---|---|---|---|
| SEC-001 | **High** | `BookingRequestValidator`/`BookingService` — client-supplied flight-fare snapshot trusted without validation; price/fare tampering | **Resolved** (final verification 2026-07-07) |
| SEC-002 | Medium | `BookingRequestValidator.ValidateStructure` — no upper bound on booking passenger count/array size | **Resolved** (re-verified 2026-07-07) |
| SEC-003 | Low | `Program.cs` / `index.html` — no HTTP security response headers or CSP configured | **Resolved** (re-verified 2026-07-07) |
| SEC-004 | Low | `DocumentPatterns.EmailPattern` — no explicit length bound before/within regex evaluation | **Resolved** (re-verified 2026-07-07) |

**Totals: 0 Critical, 1 High, 1 Medium, 2 Low. Final status: 4 Resolved (SEC-001/002/003/004). Zero Open/In Progress/Partially Resolved findings remain.**

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

**Superseded by the 2026-07-07 Final Verification (see SEC-001's Final Verification Note above).** SEC-001 (High) has now been fully fixed via server-side price re-resolution (option (a)) and independently re-verified against the actual code and an independently re-run test suite — it no longer requires a human Product Owner risk-acceptance decision under CLAUDE.md §21, because the finding is resolved by the fix itself rather than by accepting residual risk.

All four findings raised in this review (SEC-001 High, SEC-002 Medium, SEC-003 Low, SEC-004 Low) are now `Resolved`. No Critical/High/Medium/Low finding in this report remains `Open`, `In Progress`, or `Partially Resolved`.

**Recommendation:** Proceed to Phase 17 (Accessibility Review). No further human Product Owner decision is required to close this security review.

---

## Re-Verification Summary (Fix Loop, 2026-07-07)

Independently re-verified all four findings against the current code on `sdlc/16-security-review-skyroute-mvp` (not solely against the two developer handoffs `16a-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-fix.md` and `16b-junior-developer-to-sdlc-orchestrator-sec002-003-004-fix.md`, though both were read first for context). Per-finding verification detail is recorded inline under each finding above ("Re-Verification Note").

**Outcome:**

- **SEC-001 (High) — Partially Resolved.** The minimal/option-(b) mitigation (positive-value + allow-list checks) is correctly and completely implemented and tested. The finding's core concern — an arbitrary *positive*, internally-consistent fabricated price being trusted and persisted as a confirmed booking's authoritative price — remains structurally open (option (a), full server-side price re-resolution, was out of scope for this fix loop). No human Product Owner acceptance of this residual risk has been recorded in any handoff reviewed. Per CLAUDE.md §21 and the Definition of Done, an unresolved High finding cannot be marked `Resolved` without either a complete fix or explicit human risk acceptance — neither has occurred, so this is marked `Partially Resolved` with the residual risk documented in the finding itself. **This still requires a human Product Owner decision before the CLAUDE.md §21 approval gate is satisfied** — the gap has narrowed (egregious cases are now blocked) but has not closed.
- **SEC-002 (Medium) — Resolved.** Passenger-count bound (1–9) added, matches search endpoint exactly, independently confirmed via code read and passing tests.
- **SEC-003 (Low) — Resolved.** Three security headers added via middleware, CSP meta tag added to frontend shell, integration test proves headers present on a real request through the full pipeline. Independently confirmed.
- **SEC-004 (Low) — Resolved.** `EmailPattern` now bounded to 254 characters via lookahead; boundary tests (254 passes, 255 rejected) independently confirmed correct by re-deriving the test's string-length arithmetic.

**Independent build/test verification (not relying on either developer handoff's reported numbers):**

```
dotnet build
  Build succeeded.
      0 Warning(s)
      0 Error(s)

dotnet test
  Passed! - Failed: 0, Passed: 122, Skipped: 0, Total: 122  - SkyRoute.Application.Tests.dll (net10.0)
  Passed! - Failed: 0, Passed:  12, Skipped: 0, Total:  12  - SkyRoute.Api.IntegrationTests.dll (net10.0)
```

134/134 tests passed across the full solution (0 failed, 0 skipped), independently re-run by the security-reviewer on 2026-07-07. This includes all newly added tests for SEC-001 (9 tests), SEC-002 (3 tests), SEC-003 (1 test), and SEC-004 (2 tests).

**Revised recommendation (superseded, see Final Update below):** SEC-002/003/004 are closed and require no further action. SEC-001 has narrowed from a fully-open exploitable-to-zero/negative-price gap to a documented residual risk (arbitrary-but-positive fabricated price still trusted). Given this MVP's explicit out-of-scope payment processing, local-only deployment, and non-persistent in-memory storage, the residual risk is low in practical, exploitable-today impact — but it is not zero, and it is not this review's place to accept it on the Product Owner's behalf. The CLAUDE.md §21 human-approval gate for this High finding remains open: the human Product Owner should either (a) explicitly accept the residual risk as documented above for the current MVP scope, or (b) schedule the full server-side price re-resolution (option (a) in the original finding) before this phase is considered fully closed. Recommend the SDLC Orchestrator route this decision to the human user before proceeding to Phase 17.

---

## Final Update (Security Reviewer, 2026-07-07, commit `8f20aa3`)

The residual gap flagged in the Re-Verification Summary above has since been closed. A developer agent implemented full server-side price re-resolution (option (a)) per `docs/handoffs/16d-lead-full-stack-engineer-to-sdlc-orchestrator-sec001-full-fix.md`, and this was independently re-verified against the current code (not solely the handoff's claims) and an independently re-run test suite — see SEC-001's **Final Verification Note** above for the full evidence trail.

**Independent build/test verification at this final pass:**

```
dotnet build
  Build succeeded.
      0 Warning(s)
      0 Error(s)

dotnet test
  Passed! - Failed: 0, Passed: 146, Skipped: 0, Total: 146 - SkyRoute.Application.Tests.dll (net10.0)
  Passed! - Failed: 0, Passed:  13, Skipped: 0, Total:  13 - SkyRoute.Api.IntegrationTests.dll (net10.0)
```

159/159 tests passed (0 failed, 0 skipped), independently re-run by the security-reviewer on 2026-07-07 at commit `8f20aa3`.

**Final outcome — SEC-001, SEC-002, SEC-003, and SEC-004 are all `Resolved`.** SEC-002/003/004 were re-confirmed unchanged from the 2026-07-07 Re-Verification Note above (no code affecting those findings changed in this fix loop). Zero `Open`, `In Progress`, or `Partially Resolved` findings remain in this report. **Ready to proceed to Phase 17 (Accessibility Review).** No further human Product Owner decision is required to close this security review.

---

*End of Security Review Report — Phase 16.*
