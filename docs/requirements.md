# Requirements Document — SkyRoute Travel Platform MVP

---

## Document Metadata

| Field | Value |
|---|---|
| Document ID | REQ-001 |
| Version | 1.3 |
| Date | 2026-07-03 |
| Status | Approved |
| Authors | solution-architect |
| Approvers | Product Owner |
| Source | Internal Product Requirements |
| Phase | Phase 03 — Requirements Analysis (updated: protocol extensibility review) |

---

## Table of Contents

1. Project Overview
2. User Stories
3. Functional Requirements (including 3.10 Design Principles and Architectural Constraints)
4. Business Rules
5. Non-Functional Requirements (High-Level)
6. Assumptions
7. Out of Scope for MVP
8. Open Questions

### Change History

| Version | Date | Author | Change Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | solution-architect | Initial approved requirements baseline |
| 1.1 | 2026-07-03 | solution-architect | Added Section 3.10 (Design Principles and Architectural Constraints); strengthened FR-046, FR-051, FR-054/FR-055; updated BR-008; added BR-011, BR-012, BR-013 |
| 1.2 | 2026-07-03 | solution-architect | Added DP-AUTH-001–DP-AUTH-004 (auth extensibility), DP-PERSIST-001–DP-PERSIST-005 (persistence portability), DP-DB-001–DP-DB-004 (multi-database support), DP-TENANT-001–DP-TENANT-007 (multi-tenancy seam); updated BR-008 to specify domain-object-only interface; updated BR-012 with Angular auth seam note; added YAGNI-007; added ASM-013, ASM-014; updated Section 7 Out of Scope |
| 1.3 | 2026-07-03 | solution-architect | Added DP-PROTOCOL-001–DP-PROTOCOL-006 (protocol extensibility and transport agnosticism); strengthened DP-PERSIST-002 to cover serialization annotations on domain models; added YAGNI-008–YAGNI-011 (GraphQL, gRPC, WebSocket/SignalR, HTTP/3 in MVP); added ASM-015; updated Section 7 Out of Scope items 24–27 |

---

## 1. Project Overview

### 1.1 What SkyRoute Is

SkyRoute is a travel aggregator platform that allows users to search, compare, and book flights across multiple airline providers. The platform aggregates flight data from airline provider APIs — each with its own pricing model — and presents a unified search and booking experience to the end user.

The MVP delivers the core Flight Search and Booking module: a production-representative slice of the platform demonstrating extensible provider architecture, per-passenger booking records, and a clean Angular + ASP.NET Core full-stack implementation.

### 1.2 Delivery Goal

Deliver a working full-stack application (Angular 17 frontend + ASP.NET Core 8 backend) that:

- Accepts flight search criteria from a user.
- Aggregates results from two mock airline providers (GlobalAir and BudgetWings).
- Displays sortable flight results with pricing.
- Allows the user to select a flight, enter per-passenger details, and confirm a booking.
- Returns and displays a booking reference code.
- Demonstrates extensible provider architecture capable of onboarding additional providers without significant rework.

### 1.3 Technology Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 22 (standalone components) |
| Backend | ASP.NET Core 10 Web API |
| Persistence | In-memory (no database) |
| Deployment | Local only — no cloud deployment required |

---

## 2. User Stories

### US-001 — Search for Available Flights

**Role:** Traveller  
**Action:** I want to search for flights by specifying origin, destination, departure date, number of passengers, and cabin class  
**Business Value:** So that I can find available flights that match my travel plans and compare options across providers

**Priority:** Must Have

**Acceptance Criteria:**

1. The search form presents dropdown selectors for origin and destination airports from a static list of at least 6 airports across at least 2 countries.
2. The search form provides a date input for departure date.
3. The search form provides a numeric input or selector for number of passengers, accepting values from 1 to 9 inclusive.
4. The search form provides a selector for cabin class with exactly three options: Economy, Business, First Class.
5. The user cannot submit the form while any required field is empty or invalid.
6. On valid submission, the frontend calls the backend search API and displays a loading indicator until results are returned.
7. All four fields (origin, destination, date, passengers, cabin class) are submitted to the backend as part of the search request.
8. The user cannot select the same airport as both origin and destination.
9. The user cannot select a departure date in the past.

---

### US-002 — View Flight Search Results

**Role:** Traveller  
**Action:** I want to see a list of available flights returned from the search, with all relevant flight details and pricing  
**Business Value:** So that I can compare options and identify the best flight for my needs

**Priority:** Must Have

**Acceptance Criteria:**

1. Each flight result displays: airline provider name, flight number, departure time, arrival time, flight duration, cabin class, and price.
2. The price displayed prominently is the TOTAL price for all passengers combined (per-passenger price × passenger count), formatted as USD currency to 2 decimal places.
3. The per-passenger price is displayed as secondary information alongside the total price (e.g., "USD 320.00 total / USD 160.00 per person").
4. The total price and per-passenger price are visually distinguishable — the total is the primary figure.
5. A loading indicator is shown while the search request is in flight.
6. If the search returns zero results, a clear empty state message is displayed (e.g., "No flights found for your search. Please try different criteria.").
7. If the search request fails, a user-facing error message is displayed without exposing backend details.
8. Results remain displayed until the user initiates a new search.

---

### US-003 — Sort Flight Results

**Role:** Traveller  
**Action:** I want to sort the flight results by price, duration, or departure time without triggering a new API call  
**Business Value:** So that I can quickly find the cheapest, fastest, or most convenient flight without waiting for a round trip to the server

**Priority:** Must Have

**Acceptance Criteria:**

1. The results view provides a sort control with the following options:
   - Price: low to high
   - Price: high to low
   - Duration: shortest first
   - Departure time: earliest first
2. Selecting a sort option re-orders the displayed results immediately without making an additional API call.
3. The currently active sort option is visually indicated.
4. Sorting is applied consistently — all results in the current result set are re-ordered, not just a subset.
5. A default sort order (price low to high) is applied when results are first displayed.

---

### US-004 — Select a Flight and Initiate Booking

**Role:** Traveller  
**Action:** I want to select a flight from the results and proceed to a booking screen  
**Business Value:** So that I can confirm my choice and provide passenger details to complete the booking

**Priority:** Must Have

**Acceptance Criteria:**

1. Each flight result has a selectable action (e.g., "Select" or "Book") that navigates to the booking screen.
2. The booking screen displays a summary of the selected flight: route (origin → destination), provider name, flight number, departure time, arrival time, cabin class.
3. The booking screen displays a price breakdown: per-passenger price, number of passengers, and total price.
4. The booking screen does not require a new API call to display the selected flight summary — the data is carried from the search results.

---

### US-005 — Enter Passenger Details

**Role:** Traveller  
**Action:** I want to enter contact and document details for each passenger  
**Business Value:** So that the booking system can record accurate passenger information required for travel

**Priority:** Must Have

**Acceptance Criteria:**

1. The booking screen presents one passenger details form section per passenger (e.g., 2 passengers = 2 form sections).
2. Each passenger form section collects: full name, email address, and a document number field.
3. The first passenger (lead passenger) provides the primary contact email.
4. All subsequent passengers also provide an email address (individual records are required).
5. The document number field label changes based on route type:
   - International route (origin country differs from destination country): label is "Passport Number"
   - Domestic route (origin country equals destination country): label is "National ID"
6. The document number field validation rule changes with the label:
   - Passport Number: 6–9 alphanumeric characters, uppercase letters and digits only, no spaces.
   - National ID: 5–20 alphanumeric characters, may include hyphens, no spaces.
7. Full name is required and must be at least 2 characters.
8. Email address is required and must conform to standard email format.
9. Document number is required and must pass the applicable validation for the route type.
10. Form submission is blocked if any passenger section contains invalid or missing data.

---

### US-006 — Confirm Booking and Receive Reference

**Role:** Traveller  
**Action:** I want to submit the booking and receive a booking reference code  
**Business Value:** So that I have confirmation that my booking was recorded and a reference I can use to retrieve it

**Priority:** Must Have

**Acceptance Criteria:**

1. A "Confirm Booking" action is present on the booking screen.
2. On submission, the frontend calls the backend booking API with the selected flight details and all passenger records.
3. A loading indicator or in-progress state is shown while the booking request is being processed.
4. On success, the confirmation screen displays:
   - The booking reference code (format: `SKY-INT-XXXXXX` or `SKY-DOM-XXXXXX`)
   - The booked flight summary (route, provider, flight number, departure time, arrival time, cabin class)
   - The total price paid
   - A list of all passengers with their names
5. The booking reference is visually prominent on the confirmation screen.
6. If the booking fails, a user-facing error message is displayed without exposing backend error internals.
7. Once confirmed, re-submitting the same booking is not possible without navigating back and starting again.

---

### US-007 — Provider Extensibility

**Role:** Platform Engineer  
**Action:** I want the backend to be architected so that new airline providers can be added with minimal code changes  
**Business Value:** So that the platform can onboard new providers without rearchitecting the aggregation layer

**Priority:** Must Have

**Acceptance Criteria:**

1. The backend defines a common `IFlightProvider` interface (or equivalent abstraction) that all providers implement.
2. Adding a new provider requires only: implementing the interface and registering the implementation — no changes to the aggregation logic.
3. The aggregation layer queries all registered providers concurrently and merges the results.
4. A failure in one provider does not prevent results from other providers from being returned.
5. Each flight result in the response carries a `provider` field identifying which provider returned it.
6. The two initial providers (GlobalAir and BudgetWings) each implement the interface with their respective mock data and pricing logic.

---

### US-008 — View Airport Selection

**Role:** Traveller  
**Action:** I want to select origin and destination airports from a clear list  
**Business Value:** So that I can easily identify and select the airports I need without ambiguity

**Priority:** Must Have

**Acceptance Criteria:**

1. Each airport in the dropdown displays at minimum: airport code, city name, and country.
2. At least 6 airports are available across at least 2 countries.
3. The list is static — no live API call is required to populate it.
4. The same airport cannot be selected as both origin and destination simultaneously.

---

## 3. Functional Requirements

### 3.1 Flight Search

| ID | Requirement | Priority |
|---|---|---|
| FR-001 | The system shall provide a flight search endpoint that accepts: origin airport code, destination airport code, departure date, passenger count (1–9), cabin class (Economy / Business / First Class), and trip type (currently: OneWay only — see BR-013). | Must Have |
| FR-002 | The system shall validate that origin and destination are not identical. If identical, return a 400 Bad Request with a descriptive error message. | Must Have |
| FR-003 | The system shall validate that departure date is not in the past. If past, return a 400 Bad Request. | Must Have |
| FR-004 | The system shall validate that passenger count is an integer between 1 and 9 inclusive. Values outside this range return a 400 Bad Request. | Must Have |
| FR-005 | The system shall validate that cabin class is one of the three permitted values. An unrecognised value returns a 400 Bad Request. | Must Have |
| FR-005b | The system shall validate that trip type is `OneWay`. Any other value, or an absent value, returns a 400 Bad Request. See BR-013 for rationale. | Must Have |
| FR-006 | The system shall validate that origin and destination airport codes are known values from the static airport list. Unknown codes return a 400 Bad Request. | Must Have |
| FR-007 | The backend shall query all registered flight providers concurrently when processing a search request. | Must Have |
| FR-008 | The backend shall aggregate results from all providers into a single response array, including a `provider` field on each result identifying its source. | Must Have |
| FR-009 | If a provider call fails or throws an exception, the backend shall log the error and return the remaining provider results. The overall search request shall not fail due to a single provider failure. | Must Have |
| FR-010 | Each flight result in the search response shall include: provider name, flight number, origin airport code, destination airport code, departure datetime, arrival datetime, duration in minutes, cabin class, per-passenger base fare, and the calculated per-passenger price after provider pricing rules are applied. | Must Have |
| FR-011 | The backend shall apply provider-specific pricing rules when returning flight results (see Business Rules BR-001 and BR-002). | Must Have |
| FR-012 | The search response shall not include total price — total price calculation is the responsibility of the frontend, derived from per-passenger price × passenger count. | Must Have |
| FR-013 | The frontend shall display the loading state for the duration of the search request, from submission until the response is received or an error occurs. | Must Have |
| FR-014 | The frontend shall display an empty state message when the search returns zero results. | Must Have |
| FR-015 | The frontend shall display a user-facing error message if the search API call fails. | Must Have |

### 3.2 Search Results Display

| ID | Requirement | Priority |
|---|---|---|
| FR-016 | Each flight result card/row shall display: provider name, flight number, departure time (formatted HH:mm), arrival time (formatted HH:mm), flight duration (formatted as hours and minutes, e.g., "2h 15m"), cabin class, total price for all passengers (prominently), and per-passenger price (as secondary information). | Must Have |
| FR-017 | The total price shall be calculated on the frontend as: per-passenger price × passenger count, displayed formatted as USD currency to 2 decimal places (e.g., "USD 320.00"). | Must Have |
| FR-018 | Both the total price and per-passenger price shall be visible on each result with clear labelling, e.g., "USD 320.00 total / USD 160.00 per person". The total must be the primary visual element. | Must Have |
| FR-019 | The origin and destination airport information shall be clearly visible on each result (at minimum airport codes; city names preferred). | Should Have |

### 3.3 Sorting

| ID | Requirement | Priority |
|---|---|---|
| FR-020 | The frontend shall provide a sort control offering these options: Price low to high, Price high to low, Duration shortest first, Departure time earliest first. | Must Have |
| FR-021 | Sorting shall be applied entirely on the frontend to the in-memory result set — no additional API call shall be made when the sort option changes. | Must Have |
| FR-022 | The default sort order on initial results display shall be Price low to high. | Must Have |
| FR-023 | The currently selected sort option shall be visually indicated in the sort control. | Must Have |
| FR-024 | Sort order shall be re-applied consistently to the full result set, not a subset. | Must Have |

### 3.4 Booking Flow

| ID | Requirement | Priority |
|---|---|---|
| FR-025 | The frontend shall navigate to a booking screen when the user selects a flight from the results. The selected flight data shall be passed to the booking screen without an additional API call. | Must Have |
| FR-026 | The booking screen shall display a flight summary: origin airport, destination airport, provider name, flight number, departure time, arrival time, cabin class. | Must Have |
| FR-027 | The booking screen shall display a price breakdown: per-passenger price, number of passengers, and total price. | Must Have |
| FR-028 | The booking screen shall render one passenger detail form section per passenger, numbered clearly (e.g., "Passenger 1", "Passenger 2"). | Must Have |
| FR-029 | Each passenger form section shall collect: full name (required, min 2 chars), email address (required, valid email format), document number (required, format depends on route type). | Must Have |
| FR-030 | The document number field label and validation rule shall be determined by route type (see Business Rule BR-003). The label and validation rule must update immediately when the search criteria are known — they are fixed for a given booking. | Must Have |
| FR-031 | Form submission (Confirm Booking) shall be blocked and the button disabled if any passenger section has validation errors or empty required fields. | Must Have |
| FR-032 | On clicking Confirm Booking, the frontend shall call the backend booking API with all passenger records and the selected flight identifier. | Must Have |
| FR-033 | A loading/in-progress state shall be displayed while the booking request is being processed. | Must Have |
| FR-034 | On a successful booking response, the frontend shall navigate to or display a booking confirmation screen. | Must Have |
| FR-035 | The booking confirmation screen shall display: booking reference code, selected flight summary (route, provider, flight number, departure time, arrival time, cabin class), total price, and the full name of each passenger. | Must Have |
| FR-036 | The booking reference code shall be visually prominent on the confirmation screen (e.g., large font, distinct styling). | Must Have |
| FR-037 | If the booking API call fails, the frontend shall display a user-facing error message without revealing backend internals. | Must Have |
| FR-038 | A confirmed booking shall not be re-submittable from the confirmation screen without a deliberate navigation action back to search. | Must Have |

### 3.5 Booking — Backend API

| ID | Requirement | Priority |
|---|---|---|
| FR-039 | The backend shall expose a POST booking endpoint that accepts: the selected flight identifier (or sufficient flight details to uniquely identify the flight and provider), passenger count, cabin class, and an array of passenger detail records. | Must Have |
| FR-040 | Each passenger detail record in the booking request shall contain: full name, email address, document type (Passport / National ID), document number. | Must Have |
| FR-041 | The backend shall validate all passenger records before creating a booking. Invalid records return 400 Bad Request with field-level error details. | Must Have |
| FR-042 | The backend shall generate a booking reference code on successful booking creation using the format defined in BR-004. | Must Have |
| FR-043 | The backend shall persist the booking in memory (singleton in-memory store), including: booking reference, flight details snapshot, all passenger records, cabin class, total price, and timestamp. | Must Have |
| FR-044 | The backend booking response shall return: booking reference code, flight details (route, provider, flight number, times), cabin class, total price, and the array of passenger records. | Must Have |
| FR-045 | The booking store shall use a singleton lifetime, persisting all bookings in memory for the duration of the application process. Data is expected to reset on application restart. | Must Have |

### 3.6 Provider Architecture

| ID | Requirement | Priority |
|---|---|---|
| FR-046 | The backend shall define the following C# interfaces as its primary abstraction boundaries: `IFlightProvider` (provider integration contract), `IFlightAggregatorService` (aggregation orchestration contract), `IBookingService` (booking business logic contract), and `IBookingStore` (persistence contract). Each interface must have a single, clearly scoped responsibility. These interfaces are required for Dependency Inversion compliance and unit-testability of each layer in isolation. | Must Have |
| FR-047 | The `IFlightProvider` interface shall be the only integration point between the aggregation layer and individual providers. The aggregation layer must not contain provider-specific logic. | Must Have |
| FR-048 | Two concrete provider implementations shall exist: `GlobalAirProvider` and `BudgetWingsProvider`. Each shall return a realistic, hardcoded set of flights for any given search input. | Must Have |
| FR-049 | The backend aggregation layer shall invoke all registered providers concurrently (e.g., using `Task.WhenAll`). | Must Have |
| FR-050 | A provider that throws an exception shall have its exception caught and logged; its results shall be treated as an empty collection. The aggregation shall continue with remaining provider results. | Must Have |
| FR-051 | Both providers shall apply their respective pricing rules (BR-001 and BR-002) internally, returning the final calculated per-passenger price in their results. Pricing logic must be implemented as a named, isolated method or named constants within each provider class — not inline within the data retrieval logic. This ensures pricing can be located, read, and modified without touching flight data construction logic. A separate pricing class or interface is not required for MVP (YAGNI). | Must Have |
| FR-052 | Each provider shall tag its results with its provider name so the aggregated response identifies the source of each flight. | Must Have |
| FR-053 | The list of providers shall be registered via dependency injection (e.g., as `IEnumerable<IFlightProvider>`). Adding a new provider requires only: a new implementation class and a DI registration — no changes to aggregation logic. | Must Have |

### 3.7 Airport Data

| ID | Requirement | Priority |
|---|---|---|
| FR-054 | The backend shall expose a GET airports endpoint returning the full static airport list. | Should Have |
| FR-055 | Alternatively, the airport list may be hardcoded in the Angular frontend as a static TypeScript constant exported from a single `airports.constants.ts` file. Regardless of whether the data comes from an API or a frontend constant, it must originate from exactly one source — no airport data shall be duplicated across files or layers. If an API endpoint is used, the frontend must consume it exclusively; if a frontend constant is used, the backend may independently use its own equivalent constant for validation — but each copy must be the single authoritative source for its respective layer (DRY within each layer boundary). A dedicated API endpoint is preferred for purity but not mandatory for MVP. | Should Have |
| FR-056 | The static airport list shall contain at least 6 airports across at least 2 countries. | Must Have |
| FR-057 | Each airport record shall contain at minimum: IATA code (3 uppercase letters), city name, country name, and display name. | Must Have |
| FR-058 | The airport list shall include airports in at least 2 different countries to enable testing of both international and domestic routing logic. | Must Have |
| FR-059 | The airport list shall include at least 2 airports in the same country to enable testing of domestic route logic. | Must Have |

**Recommended Minimum Airport List (for reference — engineer may extend):**

| Code | City | Country |
|---|---|---|
| LHR | London | United Kingdom |
| MAN | Manchester | United Kingdom |
| JFK | New York | United States |
| LAX | Los Angeles | United States |
| DXB | Dubai | United Arab Emirates |
| SYD | Sydney | Australia |

This gives domestic routes (LHR–MAN, JFK–LAX) and international routes (LHR–JFK, LHR–DXB, JFK–SYD, etc.).

### 3.8 Input Validation

| ID | Requirement | Priority |
|---|---|---|
| FR-060 | All API inputs shall be validated on the backend. Frontend validation is also required but backend validation is authoritative. | Must Have |
| FR-061 | Search request validation rules: origin and destination are known airport codes (non-empty, 3 uppercase alpha), they are not identical, departure date is today or future, passenger count is 1–9, cabin class is one of Economy/Business/First Class. | Must Have |
| FR-062 | Booking request validation rules: flight identifier is provided, passenger count matches passenger array length, each passenger has a non-empty full name (min 2 chars), valid email, non-empty document number conforming to the route-type document format. | Must Have |
| FR-063 | Validation errors shall return HTTP 400 with a structured error response body identifying which field(s) failed and why. | Must Have |
| FR-064 | Full name must not contain numeric characters only; it must contain at least one letter. | Should Have |
| FR-065 | Email addresses shall be validated against RFC 5322 simplified pattern (standard .NET email validation is acceptable). | Must Have |

### 3.9 Error Handling

| ID | Requirement | Priority |
|---|---|---|
| FR-066 | The backend shall return structured JSON error responses for all error conditions (4xx and 5xx). | Must Have |
| FR-067 | 400 Bad Request: returned for validation failures, with field-level error detail where applicable. | Must Have |
| FR-068 | 404 Not Found: returned when a resource (e.g., flight by ID, booking by reference) is not found. | Must Have |
| FR-069 | 500 Internal Server Error: returned for unhandled exceptions. The response body must not include stack traces or internal exception details in production mode. | Must Have |
| FR-070 | Provider failures during aggregation are NOT surfaced as 500 errors to the end user — they are silently degraded with the failing provider's results excluded. The failure must be logged internally. | Must Have |
| FR-071 | The frontend shall handle API error responses gracefully, displaying user-friendly messages. Raw HTTP error codes must not be shown to the user. | Must Have |
| FR-072 | Network failures on the frontend (timeout, connection refused) shall result in a user-facing message advising the user to try again. | Should Have |

### 3.10 Design Principles and Architectural Constraints

This section captures measurable, implementable architectural requirements derived from applying SOLID, DRY, KISS, YAGNI, and Separation of Concerns to the SkyRoute MVP. These are not aspirational guidelines — they are constraints that govern how the system must be built.

#### Backend — Required Interfaces (Dependency Inversion)

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-001 | The backend must define and use `IFlightProvider` as the sole integration contract between the aggregation layer and all provider implementations. No code outside a provider class may reference a concrete provider class. | DIP, OCP | Must Have |
| DP-002 | The backend must define and use `IBookingStore` as the sole persistence contract. `IBookingService` and any other consumer must depend on `IBookingStore`, not on `InMemoryBookingStore` or any concrete class. | DIP | Must Have |
| DP-003 | The backend must define and use `IBookingService` as the contract between the booking API controller and booking business logic. The controller must not contain booking business logic directly. | DIP, SRP | Must Have |
| DP-004 | The backend must define and use `IFlightAggregatorService` as the contract between the search API controller and aggregation orchestration logic. The controller must not directly call `IFlightProvider` implementations or orchestrate concurrent calls. | DIP, SRP | Must Have |

Note on YAGNI: `IAirportRepository` is explicitly NOT required for MVP. The static airport list is a constant, not a persistence concern. Wrapping a static list in a repository interface adds indirection without benefit at this scale. A single `AirportDataService` class (concrete, not behind an interface) or a typed static constant is sufficient. If the airport source changes to a database or API in a future sprint, the interface can be introduced at that point.

Note on YAGNI: `INotificationService` is explicitly NOT required for MVP. Email confirmation is out of scope (Section 7, item 11). The booking service must not call any notification mechanism; the seam is preserved by the absence of notification calls, not by an interface placeholder.

#### Backend — Separation of Concerns

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-005 | Business logic (pricing calculation, booking reference generation, route-type determination, validation logic) must reside in service classes, not in controllers. Controllers must be thin: validate model binding, call a service method, return the result. | SRP | Must Have |
| DP-006 | Pricing logic within each provider must be implemented as one or more named, isolated methods or clearly named constants within the provider class — not inlined inside the flight data construction loop. The method or constants must be independently readable and testable without understanding the full provider implementation. A separate pricing class or interface is not required for MVP. | SRP (within class), DRY | Must Have |
| DP-007 | A single centralised exception-handling middleware (or ASP.NET Core exception filter) must handle all unhandled exceptions and produce the 500 error response body. Individual controllers must not duplicate this logic. See BR-011. | DRY, SRP | Must Have |
| DP-008 | No service method or repository method may contain authentication or authorisation logic. Auth is a cross-cutting concern and must be expressible as middleware or controller-level attributes only. See BR-012. | OCP, SRP | Must Have |

#### Frontend — Separation of Concerns

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-009 | Angular components must handle only presentation logic: rendering data, responding to user interactions, and delegating to services. Components must not construct HTTP requests, parse API responses, or compute business values (e.g., total price) beyond what is required to render a display value. | SRP | Must Have |
| DP-010 | All HTTP communication with the backend must be performed through Angular service classes (e.g., `FlightSearchService`, `BookingService`). Components must not directly inject or use `HttpClient`. | SRP, DIP | Must Have |
| DP-011 | Total price calculation (per-passenger price × passenger count) must be performed in exactly one place on the frontend — in a service or in a named utility function. It must not be duplicated across the search results component and the booking component. | DRY | Must Have |
| DP-012 | Airport data, if stored as a frontend constant, must be defined in exactly one file (`airports.constants.ts` or equivalent). It must not be duplicated in component files or service files. | DRY | Must Have |
| DP-013 | Reactive state management must use a consistent pattern throughout the application. Signals and Observables must not be mixed arbitrarily for the same data flow. If Observables (RxJS) are used for HTTP calls, a consistent unwrapping strategy (e.g., `async` pipe, `.subscribe` in service) must be applied uniformly. | KISS | Must Have |

#### Validation — Single Source of Truth

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-014 | Backend validation rules are authoritative (FR-060). Frontend validation is a convenience layer that mirrors the backend rules to provide immediate feedback. If the two diverge, the backend is correct. | DRY (within layer) | Must Have |
| DP-015 | Document number validation patterns (passport: 6–9 uppercase alphanumeric; national ID: 5–20 alphanumeric with hyphens) must each be defined as a named constant or named validator in both the backend and the frontend. The pattern string must not be duplicated inline across multiple classes or components. | DRY | Must Have |
| DP-016 | Route-type determination logic (domestic vs international based on airport country equality) must exist in exactly one location on the backend (a named service method or utility). The frontend may implement its own equivalent for immediate label/validation feedback, but the backend determination is authoritative and must be re-evaluated at booking time. | DRY, SRP | Must Have |

#### Testability Requirements

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-017 | Every class that implements a required interface (DP-001 through DP-004) must be unit-testable in isolation: its dependencies must be injectable through constructor injection, enabling substitution with test doubles (mocks/stubs/fakes). | Testability, DIP | Must Have |
| DP-018 | The booking reference generation logic (BR-004) must be extractable into a named method (e.g., `GenerateBookingReference(routeType)`) that is independently unit-testable without instantiating a full booking flow. | Testability, SRP | Must Have |
| DP-019 | Pricing calculation methods (BR-001, BR-002) must each be independently unit-testable: given a base fare input, the method returns the correct calculated price. They must not require an HTTP context, a search result object, or a database call to be invoked. | Testability, SRP | Must Have |
| DP-020 | Angular service classes must be testable using Angular's `TestBed` with the `HttpClientTestingModule` substituted for the real `HttpClient`. Services must not instantiate `HttpClient` directly. | Testability, DIP | Must Have |

#### Explicit YAGNI and KISS Guards

The following abstractions are explicitly excluded from the MVP. Introducing any of these without Product Owner approval is a scope violation.

| ID | Item Excluded | Reason |
|---|---|---|
| YAGNI-001 | `IAirportRepository` interface | Airport data is a static constant in MVP; no persistence or remote fetch is required |
| YAGNI-002 | `INotificationService` interface | Email/notification is out of scope for MVP (Section 7, item 11) |
| YAGNI-003 | `IPricingStrategy` / `IFlightPricingStrategy` interface | Two pricing rules exist; they live inside their respective provider classes; extracting a strategy interface adds indirection without current value |
| YAGNI-004 | Flight caching layer or `IFlightCache` interface | Mock providers are in-process and fast; caching adds complexity without benefit for local MVP |
| YAGNI-005 | Event sourcing, domain events, or message bus | No async processing, no integrations, no audit trail requirement for MVP |
| YAGNI-006 | Repository pattern on top of IBookingStore | `IBookingStore` is already a sufficient abstraction; wrapping it further in a repository adds a layer with no benefit at this data scale |
| YAGNI-007 | Specific database driver or ORM configuration (EF Core, Dapper, MongoDB driver) | No real database is used in MVP; the persistence-agnostic interface design (DP-PERSIST-001–DP-PERSIST-005) is required; the driver that implements it is not |

#### Authentication and Authorisation Extensibility (v1.2)

These requirements ensure that authentication and authorisation mechanisms can be added, replaced, or extended without modifying business logic, service classes, or Angular components.

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-AUTH-001 | No service class, repository class, or domain object may accept `ClaimsPrincipal`, `IIdentity`, or any authentication-scheme-specific type as a constructor or method parameter. If a service requires caller identity information, it must be provided as a resolved domain scalar (e.g., `string userId`, `string tenantId`) — not as a raw security principal. This ensures services are callable by any authenticated identity type without modification. | OCP, SRP | Must Have |
| DP-AUTH-002 | No service class, repository class, or business logic may inspect the authentication scheme name at runtime (e.g., must not evaluate `User.Identity.AuthenticationType`, must not branch on `"Bearer"` vs `"Cookie"` vs `"ApiKey"`). Authentication scheme selection and identity extraction are the sole responsibility of the ASP.NET Core authentication pipeline. | OCP | Must Have |
| DP-AUTH-003 | The Angular frontend must define a single `AuthService` class as the authoritative point of all authentication-related behaviour on the frontend (token storage, header injection, login/logout state). In the MVP, this service is a no-op: it does nothing, stores no tokens, and injects no headers. No Angular component or feature service (e.g., `FlightSearchService`, `BookingService`) may directly reference an authentication library or embed token-handling logic. When a real auth provider is introduced, only the `AuthService` implementation changes. | OCP, DIP | Must Have |
| DP-AUTH-004 | Authorisation policies (what a user is allowed to do) must be expressible independently of authentication schemes (how identity is proved). When auth is introduced in a future sprint, named ASP.NET Core authorisation policies (e.g., `"RequireBookingOwner"`) must be the mechanism — not inline identity checks inside service methods. This requirement is a design constraint for the MVP implementation: no service method may contain logic that will need to be replaced by a policy. | OCP, SRP | Must Have |

Note on YAGNI: No auth middleware, auth library, policy class, or token handler is required in the MVP. These requirements constrain what must NOT be done (no scheme-specific branching, no security principal in service signatures, no embedded auth logic in components) — they do not require any new implementation beyond the no-op `AuthService` class on the frontend.

#### Persistence Layer Portability (v1.2)

These requirements ensure that `InMemoryBookingStore` can be replaced with any persistence technology (relational, document, in-memory cache) by adding a new class and updating a DI registration, with zero changes to service classes or controllers.

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-PERSIST-001 | The `IBookingStore` interface must expose only abstract operations with domain object parameters and return types. The interface must not include `IQueryable<T>`, `DbSet<T>`, `IMongoCollection<T>`, `ConcurrentDictionary<,>`, or any other persistence-technology-specific type in any method signature or property. Permitted types in the interface are: domain model POCOs (`Booking`, `PassengerDetail`), primitive scalars (e.g., `string`, `int`, `Guid`), and standard .NET collection types (`IReadOnlyList<T>`, `IEnumerable<T>`). | DIP, OCP | Must Have |
| DP-PERSIST-002 | Domain model classes (`Booking`, `PassengerDetail`, `FlightResult`) must be persistence-agnostic POCOs. They must not carry ORM attributes (`[Key]`, `[Column]`, `[Table]`, `[Required]` from `System.ComponentModel.DataAnnotations` used as ORM mapping), Bson attributes (`[BsonElement]`, `[BsonId]`), serialization annotations (`[JsonPropertyName]`, `[JsonIgnore]`, `[JsonConverter]` from `System.Text.Json` or `Newtonsoft.Json`), or GraphQL schema annotations (`[GraphQLName]`, `[GraphQLType]`, `[GraphQLIgnore]`). Persistence mapping, JSON serialization mapping, and GraphQL type mapping all belong exclusively in the infrastructure or presentation adapter layer — not on domain objects. See also DP-PROTOCOL-002, which extends this constraint with the full transport-agnosticism rationale. | SRP | Must Have |
| DP-PERSIST-003 | All service classes that interact with persistence must depend on `IBookingStore` exclusively. The concrete implementation class (`InMemoryBookingStore` or any future implementation) must never be referenced outside of the DI registration. This applies to constructor parameters, field types, and local variable declarations in all service classes and controllers. | DIP | Must Have |
| DP-PERSIST-004 | A future `EfCoreBookingStore` or `MongoBookingStore` implementation must be addable with zero changes to `BookingService`, any controller, or the `IBookingStore` interface. The only permitted changes are: adding the new class file, adding infrastructure configuration (connection string, entity configuration, collection mapping), and updating the DI registration. | OCP | Must Have |
| DP-PERSIST-005 | The `IBookingStore` interface must define operations at a business-meaningful level of abstraction: `CreateAsync`, `GetByReferenceAsync`, `ListByTenantAsync` (paged), and any other operations the service layer requires. It must not define operations at a query-construction level (e.g., `FindWhere(Expression<Func<Booking, bool>>)`) — that would leak the ORM's query model into the interface. | DIP | Must Have |

#### Multi-Database Support (v1.2)

These requirements ensure the architecture does not preclude using different persistence technologies for different data stores — while maintaining YAGNI compliance for the MVP.

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-DB-001 | Each repository interface (`IBookingStore`, and any future `IFlightStore` or equivalent) must be independently implementable with any persistence technology. No interface may have a structural dependency on another interface's persistence technology. Concretely: `IBookingStore` operations must not reference types from a flight persistence namespace, and vice versa. | SRP, DIP | Must Have |
| DP-DB-002 | The service layer must not assume that two different store interfaces share the same persistence technology, connection, or transaction context. Service methods must not attempt to coordinate a transaction across `IBookingStore` and any other store interface unless an explicit unit-of-work abstraction is introduced at that time. For MVP, no cross-store transactions exist or are required. | SRP | Must Have |
| DP-DB-003 | Domain model objects must not contain navigation properties, lazy-loading proxies, or other ORM-coupling constructs that would break when the domain model is used with a different persistence technology. Domain models are data-transfer objects for the service layer; persistence representation is the concern of the infrastructure layer. | SRP | Must Have |
| DP-DB-004 | YAGNI guard: no specific RDBMS (SQL Server, PostgreSQL, SQLite) or NoSQL database (MongoDB, CosmosDB, Redis) implementation is required or configured in the MVP. The persistence-agnostic interface design (DP-PERSIST-001–DP-PERSIST-005) is the required deliverable. Introducing a specific database driver or ORM without Product Owner approval is a scope violation (see YAGNI-007). | YAGNI | Must Have |

#### Multi-Tenancy Seam (v1.2)

These requirements establish a near-zero-cost tenancy seam that prevents a service-layer rewrite if multi-tenant support is introduced in a future sprint. The YAGNI test is satisfied: the seam consists of a single-property interface, a trivial default implementation, and a `TenantId` field on the `Booking` domain model — all carrying negligible implementation cost against a high future retrofit cost.

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-TENANT-001 | The backend must define an `ITenantContext` interface with a single read-only property: `string TenantId`. This interface must reside in the domain or application layer, not in the infrastructure layer. | DIP, OCP | Must Have |
| DP-TENANT-002 | The MVP must provide a `DefaultTenantContext` class implementing `ITenantContext` that returns a constant value of `"default"` for `TenantId`. This class must be registered in DI as the implementation of `ITenantContext` for the MVP. | OCP | Must Have |
| DP-TENANT-003 | `BookingService` must receive `ITenantContext` via constructor injection. All booking operations that create or query booking records must use `ITenantContext.TenantId` to scope the operation. In the MVP, this always resolves to `"default"` — the behaviour is functionally single-tenant — but the seam is present so a future `JwtTenantContext` or `HeaderTenantContext` can be substituted with zero changes to `BookingService`. | DIP, OCP | Must Have |
| DP-TENANT-004 | When multi-tenancy is introduced in a future sprint, the only required changes are: a new `ITenantContext` implementation (resolving `TenantId` from a JWT claim, subdomain, or request header) and an updated DI registration. No changes to `BookingService`, any controller, or the `IBookingStore` interface are required or permitted by this design. If such changes are found to be necessary, it is a design violation of DP-TENANT-003. | OCP | Must Have |
| DP-TENANT-005 | The `Booking` domain model must include a `TenantId` field (type `string`, default value `"default"`). This field must be stored in the in-memory store alongside the booking record so that a future persistence implementation can enforce row-level isolation without requiring a schema migration or domain model change. | OCP | Must Have |
| DP-TENANT-006 | The `IBookingStore` interface must include `string tenantId` as a parameter in all query operations that retrieve or list bookings (e.g., `GetByReferenceAsync(string reference, string tenantId)`, `ListByTenantAsync(string tenantId, int page, int pageSize)`). The in-memory MVP implementation may ignore this parameter for isolation (since there is only one tenant), but the parameter must be present in the interface. | OCP | Must Have |
| DP-TENANT-007 | The Angular frontend does not require any tenant-awareness in the MVP. Tenant context is resolved server-side. The frontend must not construct or transmit a tenant identifier unless a future requirement explicitly introduces tenant-scoped frontend routing (e.g., subdomain-based). No frontend changes are required to support multi-tenancy. | YAGNI | Should Have |

Note on YAGNI justification for the multi-tenancy seam: The cost of adding `ITenantContext` and `TenantId` now is bounded — one interface, one default implementation, one field on `Booking`, and `tenantId` parameters added to `IBookingStore` query methods. The cost of retrofitting multi-tenancy after `BookingService` has been implemented without it is: modifying `BookingService` (service-layer change), modifying `IBookingStore` (interface change requiring all implementations to update), migrating the in-memory store structure, and migrating any real persistence schema. This passes the YAGNI threshold: the abstraction earns its place by preventing a future cross-cutting rewrite.

#### Protocol Extensibility and Transport Agnosticism (v1.3)

The single architectural guarantee that makes all future protocol adoption (GraphQL, gRPC/Protobuf, HTTP/2, HTTP/3, WebSockets) possible without business logic changes is: the service layer must be completely protocol-agnostic. If this property holds, adding a new protocol means adding a new adapter class that calls existing service interfaces — the service interfaces, their implementations, and the domain models are unchanged.

The requirements below close the gaps that would otherwise cause a protocol addition to require touching business logic — an Open/Closed Principle violation.

**DP-PROTOCOL-001 — Service Layer Protocol Independence**

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-PROTOCOL-001 | No service class method signature may reference any type from `Microsoft.AspNetCore.Http`, `Microsoft.AspNetCore.Mvc`, or any HTTP-infrastructure namespace. Specifically prohibited in service method parameters or return types: `HttpContext`, `IHttpContextAccessor`, `HttpRequest`, `HttpResponse`, `IActionResult`, `ActionResult<T>`, `ObjectResult`, `ClaimsPrincipal` (covered also by DP-AUTH-001), and any other MVC or HTTP-pipeline type. Service methods must accept and return only: domain model POCOs, primitive scalars, standard .NET collection types, and `Task<T>` / `ValueTask<T>` wrappers of these. This ensures that a gRPC service class, a GraphQL resolver, a message queue consumer, or a WebSocket handler can call `IFlightAggregatorService.SearchAsync()` and `IBookingService.CreateBookingAsync()` using identical call signatures to those used by the REST controllers. | OCP, SRP | Must Have |

Rationale: if a service injects `IHttpContextAccessor` or returns `IActionResult`, a gRPC adapter cannot call it without either simulating an HTTP context (wrong) or modifying the service interface (OCP violation). The requirement costs nothing at implementation time — it is a constraint on what not to import.

Verifiability: at code review, inspect all service class files for `using Microsoft.AspNetCore.Http`, `using Microsoft.AspNetCore.Mvc`, and any of the prohibited types in method signatures. Any occurrence is a violation.

**DP-PROTOCOL-002 — Domain Model Serialization Neutrality**

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-PROTOCOL-002 | Domain model classes (`SearchRequest`, `FlightResult`, `BookingRequest`, `Booking`, `PassengerDetail`, and any equivalent domain types) must carry no serialization or schema annotations of any kind. Prohibited annotations include: `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonConverter]` (System.Text.Json or Newtonsoft.Json); `[GraphQLName]`, `[GraphQLType]`, `[GraphQLIgnore]`; `[ProtoMember]`, `[ProtoContract]` (Protobuf-net); and any other attribute that couples a domain model to a specific wire format or schema technology. Each serialization or schema layer (JSON DTO mapping, Protobuf message mapping, GraphQL type mapping) must reside exclusively in its own adapter or presentation class. Domain models are business objects; wire-format representation is an adapter concern. | OCP, SRP | Must Have |

Note: DP-PERSIST-002 (v1.3) already extends this constraint to cover ORM, Bson, JSON, and GraphQL annotations on domain models. DP-PROTOCOL-002 restates the serialization and schema annotation prohibition in the protocol-extensibility context to make the rationale explicit. The two requirements are consistent and reinforce each other.

Verifiability: at code review, inspect all domain model class files for any attribute imports from `System.Text.Json.Serialization`, `Newtonsoft.Json`, `HotChocolate`, `ProtoBuf`, or equivalent serialization namespaces. Any such import in a domain model file is a violation.

**DP-PROTOCOL-003 — HTTP/2 and HTTP/3 via Transport Configuration Only**

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-PROTOCOL-003 | The application must not assume HTTP/1.1 connection semantics anywhere in application code. Prohibited patterns: assuming one connection per request, relying on chunked transfer encoding behaviour specific to HTTP/1.1, implementing long-polling patterns that behave differently under HTTP/2 stream multiplexing, or maintaining server-side session state keyed on connection identity. HTTP/2 and HTTP/3 support is a Kestrel transport configuration concern — enabling either protocol must require only changes to `appsettings.json` or `Program.cs` Kestrel configuration with zero application code changes. | OCP, KISS | Must Have |
| DP-PROTOCOL-003b | All API responses must be stateless per request. The backend must not rely on server-side connection or session state between requests. This property is already satisfied for MVP (the in-memory store is keyed by booking reference, not by connection or session), and must be preserved in all future implementation decisions. | SRP | Must Have |

Note on YAGNI: HTTP/3 (QUIC transport) configuration is not required in the MVP. Kestrel configuration for HTTP/3 is not a deliverable. This requirement constrains only that the application code must not preclude it.

Verifiability: at code review, inspect controllers, middleware, and services for any use of `Response.Body` streaming patterns, keep-alive assumptions, or session-state dependencies. Inspect for any long-polling endpoint design. Any such pattern is a violation.

**DP-PROTOCOL-004 — GraphQL Addability**

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-PROTOCOL-004 | Adding a GraphQL endpoint to the application in a future sprint must require only: new GraphQL resolver classes, new GraphQL type definition classes (mapping domain objects to GraphQL output types), and a new endpoint registration in `Program.cs`. No changes to `IFlightAggregatorService`, `IBookingService`, `ITenantContext`, `IBookingStore`, or any domain model class are required or permitted as a result of adding GraphQL. GraphQL type definitions must not be placed on domain model classes (DP-PROTOCOL-002). Resolver classes are adapters: they accept GraphQL input types, map to domain objects, call existing service interfaces, and map domain responses to GraphQL output types. | OCP | Should Have |

Note on YAGNI: GraphQL is not implemented in the MVP. No resolver classes, schema definitions, Hot Chocolate (or equivalent) package, or `/graphql` endpoint are required. This requirement is a design constraint: the MVP implementation must not structurally prevent GraphQL from being added without service changes.

Verifiability: satisfied by verifying DP-PROTOCOL-001 (no HTTP types in service signatures) and DP-PROTOCOL-002 (no GraphQL annotations on domain models). If both hold, GraphQL addability is structurally guaranteed.

**DP-PROTOCOL-005 — gRPC and Protobuf Addability**

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-PROTOCOL-005 | Adding a gRPC endpoint to the application in a future sprint must require only: a new `.proto` contract file, new gRPC service classes (one per `.proto` service), and a new gRPC endpoint registration in `Program.cs`. No changes to `IFlightAggregatorService`, `IBookingService`, `ITenantContext`, `IBookingStore`, or any domain model class are required or permitted as a result of adding gRPC. Proto message types and domain model classes must be separate: domain models must never be serialized as Protobuf messages directly, and `.proto`-generated classes must never be passed into service method parameters. gRPC service classes are adapters: they accept proto-generated input messages, map to domain objects, call existing service interfaces, and map domain responses back to proto-generated output messages. | OCP | Should Have |

Note on YAGNI: gRPC is not implemented in the MVP. No `.proto` files, gRPC service classes, Protobuf annotations, or gRPC package registrations are required. This requirement is a design constraint: the MVP implementation must not structurally prevent gRPC from being added without service changes.

Verifiability: satisfied by verifying DP-PROTOCOL-001 (no HTTP types in service signatures) and DP-PROTOCOL-002 (no Protobuf annotations on domain models). If both hold, gRPC addability is structurally guaranteed by the adapter pattern.

**DP-PROTOCOL-006 — Angular Service Layer as Sole Network Boundary**

| ID | Requirement | Principle | Priority |
|---|---|---|---|
| DP-PROTOCOL-006 | Angular components must call only Angular service class methods for all data access — they must never directly reference `HttpClient`, `WebSocket`, `fetch`, or any network primitive. This extends DP-010 with an explicit forward-compatibility rationale: the Angular service layer is the sole network boundary of the frontend. Replacing `HttpClient` with an Apollo GraphQL client, a gRPC-Web client, or a WebSocket-based transport requires changes only to the Angular service class implementations — zero changes to any Angular component, route, or guard. Components are consumers of a service contract, not consumers of a transport protocol. | OCP, DIP | Must Have |

Note on YAGNI: No Apollo GraphQL client, gRPC-Web client, SignalR client, or WebSocket handler is required in the MVP. All Angular services use `HttpClient` exclusively in the MVP. This requirement constrains only that component code must not import or call network primitives directly.

Verifiability: at code review, inspect all Angular component `.ts` files for any import of `HttpClient`, `WebSocket`, or any network library. Any direct network primitive in a component is a violation.

#### Explicit YAGNI Guards — Protocol Implementations (v1.3)

The following protocol implementations are explicitly excluded from the MVP. Introducing any of these without Product Owner approval is a scope violation.

| ID | Item Excluded | Reason |
|---|---|---|
| YAGNI-008 | GraphQL endpoint implementation (resolvers, schema, Hot Chocolate or equivalent package) | Not required in MVP. DP-PROTOCOL-004 ensures it can be added later without service changes. |
| YAGNI-009 | gRPC endpoint implementation (`.proto` files, gRPC service classes, Grpc.AspNetCore package) | Not required in MVP. DP-PROTOCOL-005 ensures it can be added later without service changes. |
| YAGNI-010 | WebSocket or SignalR real-time endpoint (flight price notifications, booking status push) | Not required in MVP. No real-time data requirements exist in the MVP scope. The service-layer protocol independence (DP-PROTOCOL-001) ensures SignalR hubs or WebSocket handlers could call existing services without changes when introduced. |
| YAGNI-011 | HTTP/3 (QUIC) Kestrel configuration | Not required in MVP. Local development uses HTTP/1.1 and HTTP/2. DP-PROTOCOL-003 ensures no application code prevents HTTP/3 from being enabled via configuration in a future environment. |

---

## 4. Business Rules

### BR-001 — GlobalAir Pricing

- Rule: Final per-passenger price = base fare × 1.15 (15% fuel surcharge added)
- Rounding: Result must be rounded to exactly 2 decimal places using standard rounding (0.5 rounds up)
- Example: base fare $100.00 → $115.00; base fare $87.50 → $100.63 (87.50 × 1.15 = 100.625 → $100.63)
- Applied by: `GlobalAirProvider` implementation, applied to every flight result before returning
- Traceability: US-002, FR-011, FR-051

### BR-002 — BudgetWings Pricing

- Rule: Final per-passenger price = max(base fare × 0.90, 29.99) (10% promotional discount with $29.99 minimum floor)
- Rounding: Result must be rounded to exactly 2 decimal places
- The discount is applied to the base fare only — not to any surcharge or tax
- Example: base fare $200.00 → $180.00; base fare $25.00 → $29.99 (floor applied); base fare $30.00 → $27.00 → $29.99 (floor applied)
- Applied by: `BudgetWingsProvider` implementation, applied to every flight result before returning
- Traceability: US-002, FR-011, FR-051

### BR-003 — Document Type Routing

- Rule: If origin airport country equals destination airport country → route is Domestic → document type is National ID
- Rule: If origin airport country differs from destination airport country → route is International → document type is Passport
- The determination is made based on the `country` field of each airport record
- Both the field label ("Passport Number" / "National ID") and the validation rule (see FR-005, US-005 AC6) must reflect the determined document type
- The document type determination is made at booking screen load time and does not change during the booking session
- Traceability: US-005, FR-030, FR-040

### BR-004 — Booking Reference Format

- Format: `SKY-[TYPE]-[XXXXXX]`
- `TYPE` = `INT` for international routes (origin country ≠ destination country); `DOM` for domestic routes (origin country = destination country)
- `XXXXXX` = 6 characters, uppercase alphanumeric (A–Z, 0–9), cryptographically random
- Total length: 14 characters (e.g., `SKY-INT-AB1C2D`, `SKY-DOM-XY9Z8K`)
- Generation: the backend must use a cryptographically secure random source (e.g., `RandomNumberGenerator` in .NET, not `System.Random`) to generate the 6-character suffix
- Uniqueness: the system must verify the generated reference does not already exist in the in-memory store before saving; if a collision occurs, regenerate
- Traceability: US-006, FR-042, OQ-002 (Resolved)

### BR-005 — Per-Passenger Booking Records

- Rule: A booking for n passengers must produce n individual passenger records, each containing: full name, email address, document type, document number
- The lead passenger is Passenger 1 — their email is also treated as the primary booking contact
- All passengers are stored individually — no merged/combined passenger record
- Traceability: US-005, FR-040, FR-043, OQ-001 (Resolved)

### BR-006 — Total Price Calculation

- Rule: Total price displayed to the user = per-passenger price (after provider pricing rules) × number of passengers
- Per-passenger price is computed on the backend and returned in the search results
- Total price is computed on the frontend from per-passenger price × passenger count
- Total price is also computed on the backend at booking time (for record) = per-passenger price × passenger count
- Rounding: the backend must store total price rounded to 2 decimal places
- Traceability: US-002 AC2–AC4, US-006 AC4, FR-017, FR-027, FR-044

### BR-007 — Provider Fault Isolation

- Rule: If a provider implementation throws any exception during a search, the exception must be caught at the aggregation layer, the provider's results treated as an empty collection, and the remaining providers' results returned normally
- The search API must not return a 500 error due to a single provider failure
- The failure must be logged (provider name + exception message) for observability
- Traceability: US-007, FR-009, FR-050

### BR-008 — In-Memory Booking Persistence

- Rule: All bookings are stored in a singleton in-memory dictionary (keyed by booking reference)
- No external database, file system, or distributed cache is used
- Data does not persist across application restarts — this is accepted by design for MVP
- The in-memory store must be thread-safe (concurrent dictionary or equivalent)
- The concrete in-memory store class must implement `IBookingStore` (see FR-046); the `IBookingService` and all other consumers must depend on `IBookingStore`, not on the concrete class — this ensures the persistence implementation is replaceable with a database-backed implementation without modifying any business logic (Dependency Inversion Principle)
- Interface design constraint (v1.2): The `IBookingStore` interface must expose only abstract operations using domain objects as parameters and return types. The interface must not expose `ConcurrentDictionary<,>`, `IQueryable<T>`, `DbSet<T>`, `IMongoCollection<T>`, or any other persistence-technology-specific type. Method signatures must accept and return domain model POCOs (e.g., `Booking`, `PassengerDetail`) or primitive filter values (e.g., `string bookingReference`, `string tenantId`) only. This constraint ensures any persistence technology — relational, document, or in-memory — can implement the interface without changing the interface definition or any consumer.
- Interface design constraint (v1.2): The `IBookingStore` interface must include `tenantId` as a parameter in all query and mutation operations that scope data to a tenant (see DP-TENANT-006). In the MVP single-tenant implementation, this parameter is always passed as `"default"` and the in-memory store does not enforce isolation — but the parameter must be present so multi-tenant implementations can enforce isolation without requiring an interface change.
- Traceability: FR-043, FR-045, FR-046, ASM-001, ASM-013, DP-002 (Section 3.10), DP-PERSIST-001, DP-PERSIST-002, DP-TENANT-006

### BR-009 — Cabin Class Scope

- Rule: The cabin class (Economy / Business / First Class) selected at search time carries through to the booking
- The mock providers must return flights for the requested cabin class
- The cabin class is stored on each booking record and displayed on the confirmation screen
- Traceability: US-001, US-006, FR-001, FR-035

### BR-010 — No Authentication Required

- Rule: No authentication or authorisation is required for any endpoint in this MVP
- All API endpoints are publicly accessible without tokens, sessions, or login
- Traceability: ASM-004

### BR-011 — Centralised Error Handling

- Rule: All unhandled exceptions and structured error responses must be produced by a single, centralised error handling mechanism (ASP.NET Core exception-handling middleware or equivalent global exception filter)
- Individual controllers and service methods must not each independently construct 500 error response bodies — that responsibility belongs to the centralised handler
- Controllers are responsible only for calling the appropriate service method and returning its result; error shaping for unexpected exceptions is the middleware's concern
- Business-rule validation errors (400 responses) may be returned directly from controllers using the standard model validation pipeline — they are not unhandled exceptions
- Rationale: DRY — prevents each controller re-implementing the same stack-trace-suppression and error-body-formatting logic; SRP — controllers have one reason to change (business flow), not two (business flow + error formatting)
- Traceability: FR-066, FR-069, DP-007 (Section 3.10)

### BR-012 — Auth Middleware Seam

- Rule: No controller action, middleware, or service method may conditionally branch on "is the user authenticated" in a way that cannot be replaced by adding authentication middleware without modifying existing business logic
- Because BR-010 disables auth for MVP, all endpoints are treated as open; when authentication is added in a future sprint, it must be expressible as a middleware concern layered on top of the existing controller/service code without requiring changes to that code
- Concretely: do not embed `if (user == null) return Unauthorized()` checks inside service methods — those belong in middleware or controller-level attributes
- Backend service constraint (v1.2): Service methods must not accept `ClaimsPrincipal`, `IIdentity`, or any authentication-scheme-specific type as a parameter. If a service needs identity information (e.g., caller identity for future audit), it must be passed as a resolved domain value (e.g., a `string userId`) — not as a raw security principal. This ensures service methods are callable by any authenticated identity type without modification.
- Backend service constraint (v1.2): No service method or business logic class may inspect `User.Identity.AuthenticationType` or any authentication scheme name string. Auth scheme selection must remain entirely within the ASP.NET Core authentication pipeline.
- Frontend seam constraint (v1.2): The Angular application must define an `AuthService` (or equivalent named service) as the single point of auth-related behaviour on the frontend. In the MVP, this service is a no-op implementation (no login, no token storage, no header injection). When a real auth provider is introduced (e.g., Auth0, Azure AD B2C, custom JWT), only the `AuthService` implementation changes — no Angular component or feature service may directly reference an auth library or embed token-handling logic. See DP-AUTH-003.
- Rationale: OCP — the system should be open to adding auth without modifying existing business-logic code; auth scheme must be swappable without modifying services or components
- Traceability: ASM-004, Section 7 (Out of Scope item 1), DP-008 (Section 3.10), DP-AUTH-001, DP-AUTH-002, DP-AUTH-003

### BR-013 — Search Model Route Type Field

- Rule: The search request model must include a `tripType` field (or equivalent) that currently accepts only `OneWay` as a valid value; the field must be present and validated rather than assumed
- Rationale: the out-of-scope item for round-trip flights (Section 7, item 12) is a known near-future requirement. If the search model omits the field entirely and assumes one-way, adding return-trip support will require a breaking change to the API contract and the frontend model. Including the field now with a single valid value preserves OCP at zero implementation cost
- The `OneWay` value is the only accepted value for MVP; any other value returns 400 Bad Request
- Traceability: Section 7 (Out of Scope item 12), DP-009 (Section 3.10)

---

## 5. Non-Functional Requirements (High-Level)

Detailed NFRs with measurable targets will be specified in Phase 04 (NFR Specification). The following are high-level targets captured at requirements stage.

### 5.1 Performance

- Search API response time should be acceptable for interactive use (target: under 2 seconds for typical mock data volumes).
- Sorting on the frontend must be instantaneous — no perceptible delay when changing sort order.
- The booking API should respond in under 1 second.

### 5.2 Scalability

- For MVP, in-memory persistence is acceptable. The architecture should not preclude replacing the in-memory store with a real database in a future sprint.
- The provider abstraction must support adding new providers without degrading existing performance.

### 5.3 Availability and Reliability

- The application is intended for local development only — no uptime requirements apply to MVP.
- Provider fault isolation (BR-007) is the key reliability requirement: one provider failure must not bring down the whole search.

### 5.4 Security

- All user inputs must be validated on the backend (FR-060 through FR-065) to prevent injection or malformed data.
- Error responses must not expose stack traces, internal class names, or sensitive system information (FR-069).
- No authentication credentials, secrets, or sensitive data should appear in application logs.
- No security-sensitive data (e.g., full passport numbers) should be logged.

### 5.5 Accessibility

- The frontend UI must meet WCAG 2.1 Level AA as a baseline.
- All form fields must have visible labels and programmatic label associations.
- Error messages must be accessible to screen readers (ARIA live regions or equivalent).
- Interactive controls must be keyboard navigable.
- Colour contrast for all text must meet WCAG 2.1 AA minimum ratios (4.5:1 for normal text, 3:1 for large text).
- A dedicated accessibility review will occur in Phase 17.

### 5.6 Usability

- The search form must be immediately usable without instructions.
- Flight results must be easily scannable.
- Price display must be unambiguous — total vs per-person must be clearly labelled at all times.
- Error and empty states must provide actionable guidance.

### 5.7 Maintainability

- Code must follow SOLID principles.
- The provider abstraction must be genuinely extensible — not tightly coupled to the two initial providers.
- Frontend components must be standalone (Angular 17 standalone component pattern).
- Code must be clearly structured into feature modules (search, results, booking, confirmation).
- No magic numbers — pricing constants, airport data, and validation rules must be named constants or configuration.

### 5.8 Testability

- Business logic (pricing rules, booking reference generation, provider aggregation) must be unit-testable in isolation.
- Provider implementations must be mockable via the `IFlightProvider` interface.
- The in-memory booking store must be injectable and replaceable.

### 5.9 Observability and Logging

- Backend must log provider failures including provider name and exception message.
- Structured logging is preferred (Serilog or Microsoft.Extensions.Logging with JSON output).
- No sensitive data (passport numbers, full email addresses) in logs.

### 5.10 Compatibility

- Frontend: Angular 22, targeting modern evergreen browsers (Chrome, Edge, Firefox, Safari — latest 2 major versions).
- Backend: .NET 10, running locally on Windows or macOS (cross-platform).
- Node.js: version compatible with Angular 22 CLI.

---

## 6. Assumptions

| ID | Assumption | Impact if Wrong |
|---|---|---|
| ASM-001 | In-memory persistence only — all booking data is lost on application restart | Low for MVP; would require persistence layer if elevated to production |
| ASM-002 | Passenger booking records are individual per passenger (OQ-001 resolved) — n passengers = n PassengerDetail records, lead passenger email is primary contact | Form structure confirmed; no impact |
| ASM-003 | Static airport list — no live airport search API required; minimum 6 airports across 2 countries hardcoded | Low; airport list extension is additive |
| ASM-004 | No authentication or authorisation required for MVP endpoints | Any future auth requirement would require API and UI changes |
| ASM-005 | Booking reference format is `SKY-[INT/DOM]-[XXXXXX]` (OQ-002 resolved) — 14 chars, cryptographic random 6-char suffix | Format confirmed; no impact |
| ASM-006 | Mock provider data uses a fixed internal schedule — the same flight schedule is returned for any valid search input (date/route does not filter the mock data, though realistic data shapes should be returned) | Acceptable for MVP; real provider would filter by date/route |
| ASM-007 | Both mock providers (GlobalAir, BudgetWings) are implemented in the backend — no external HTTP calls are made | Correct for MVP; real integration would replace mock with HTTP client |
| ASM-008 | Single sprint delivery — all MVP scope is targeted for Sprint 1 | Scope must be managed to fit the sprint |
| ASM-009 | No seat selection, ancillary products, or loyalty integration for MVP | Scope boundary confirmed |
| ASM-010 | Departure date input uses the browser's native date picker — no custom calendar widget required | Acceptable for MVP; custom date picker would be a future enhancement |
| ASM-011 | Flight duration in mock data is expressed in minutes (integer) and formatted for display on the frontend | Data contract decision; must be consistent between providers |
| ASM-012 | CORS configuration is required on the backend to allow the Angular dev server (localhost:4200) to call the ASP.NET Core API (localhost:5000) | Must be configured correctly or frontend cannot reach backend |
| ASM-013 | The system is designed as single-tenant in MVP. Multi-tenant support requires only a new `ITenantContext` implementation and a new `IBookingStore` implementation — no changes to `BookingService` or any controller are required. This is guaranteed by DP-TENANT-001 through DP-TENANT-006. | If this assumption is violated (i.e., multi-tenancy requires service changes), a design review must be triggered before proceeding |
| ASM-014 | In-memory persistence will be replaced with a real database implementation in a future sprint. Domain models are persistence-agnostic POCOs by design (DP-PERSIST-002). Replacing the persistence layer requires only a new `IBookingStore` implementation and updated DI registration — no changes to `BookingService` or any controller are required. | If this assumption is violated (i.e., a real database requires service changes), a design review must be triggered before proceeding |
| ASM-015 | The service layer is protocol-agnostic by design (DP-PROTOCOL-001 through DP-PROTOCOL-005). Adding GraphQL, gRPC, WebSocket, or any future transport protocol requires only new adapter/resolver classes and endpoint registration — no changes to `IFlightAggregatorService`, `IBookingService`, `ITenantContext`, `IBookingStore`, or any domain model. | If this assumption is violated (i.e., a new protocol requires service interface changes), a design review must be triggered before proceeding |

---

## 7. Out of Scope for MVP

The following items are explicitly excluded from the MVP delivery. They are noted here to prevent scope creep and to document the known future roadmap.

| # | Out of Scope Item | Notes |
|---|---|---|
| 1 | User authentication and login | No auth for MVP (BR-010) |
| 2 | User account management | No profiles, saved preferences |
| 3 | Booking retrieval / "manage my booking" | No GET booking by reference in UI |
| 4 | Booking cancellation | No cancellation flow |
| 5 | Payment processing | No payment gateway integration |
| 6 | Real airline provider API integration | Both providers are mocked (ASM-007) |
| 7 | Live airport search API | Static airport list only (ASM-003) |
| 8 | Seat selection | Out of scope for this sprint |
| 9 | Ancillary products (baggage, meals, insurance) | Out of scope for this sprint |
| 10 | Loyalty/rewards programme integration | Out of scope for this sprint |
| 11 | Email confirmation to passengers | No outbound email |
| 12 | Return / round-trip flights | One-way search only for MVP |
| 13 | Multi-city itineraries | One-way, single leg only |
| 14 | Cloud deployment | Local development environment only for MVP |
| 15 | Database persistence | In-memory only (ASM-001) |
| 16 | Admin portal | No back-office tooling |
| 17 | Flight schedule filtering by date/route | Mock providers return fixed schedules regardless of search date |
| 18 | Price alerts or fare tracking | No notifications |
| 19 | Multi-language / internationalisation | English only |
| 20 | Mobile app | Web only (Angular responsive is preferred but native mobile is out of scope) |
| 21 | Multi-tenant operation (tenant isolation, tenant provisioning, tenant administration) | The architecture supports it via the `ITenantContext` seam (DP-TENANT-001–DP-TENANT-007); not implemented in MVP. MVP operates as a single-tenant system with `TenantId = "default"` throughout. |
| 22 | Real database persistence (relational or document) | The architecture supports it via the persistence-agnostic `IBookingStore` interface (DP-PERSIST-001–DP-PERSIST-005); not implemented in MVP. In-memory persistence only (BR-008, ASM-001, ASM-014). |
| 23 | Authentication and authorisation enforcement | The architecture supports it via the auth middleware seam (BR-012, DP-AUTH-001–DP-AUTH-004); not implemented in MVP. All endpoints are publicly accessible (BR-010). The no-op `AuthService` on the frontend and the `ITenantContext` default implementation are the only auth-related deliverables in MVP. |
| 24 | GraphQL endpoint (resolvers, schema, Hot Chocolate or equivalent) | The architecture supports it via the service-layer protocol independence seam (DP-PROTOCOL-001, DP-PROTOCOL-004, YAGNI-008); not implemented in MVP. Adding GraphQL requires only new resolver classes — no service or domain changes. |
| 25 | gRPC endpoint (`.proto` files, gRPC service classes, Grpc.AspNetCore) | The architecture supports it via the service-layer protocol independence seam (DP-PROTOCOL-001, DP-PROTOCOL-005, YAGNI-009); not implemented in MVP. Adding gRPC requires only new adapter classes — no service or domain changes. |
| 26 | WebSocket or SignalR real-time features (price notifications, booking status push) | No real-time requirements exist in MVP. The architecture supports it via the service-layer protocol independence seam (DP-PROTOCOL-001, YAGNI-010); not implemented in MVP. |
| 27 | HTTP/3 (QUIC transport) Kestrel configuration | Not required for local MVP development. DP-PROTOCOL-003 and YAGNI-011 ensure application code does not preclude HTTP/3 being enabled via configuration in a future environment. |

---

## 8. Open Questions

| ID | Question | Status | Resolution |
|---|---|---|---|
| OQ-001 | How should multi-passenger bookings collect passenger data — one shared form or individual per-passenger records? | Resolved | Individual per-passenger records. n passengers = n PassengerDetail records. Lead passenger is Passenger 1. Industry standard practice (British Airways, Ryanair, American Airlines). See BR-005. |
| OQ-002 | What should the booking reference format be? | Resolved | `SKY-[INT/DOM]-[XXXXXX]` — 14 characters. INT for international, DOM for domestic. 6-character cryptographic random uppercase alphanumeric suffix. Example: `SKY-INT-AB1C2D`. See BR-004. |
| OQ-003 | Should the mock providers filter results by departure date and route, or return a fixed schedule? | Resolved (ASM-006) | Fixed schedule for mock data — same realistic flight set returned for any valid input. Real integration would filter. |
| OQ-004 | Should a GET airports endpoint exist on the backend, or should airport data be hardcoded in the Angular frontend? | Resolved (FR-054, FR-055) | Either is acceptable for MVP. Backend endpoint is preferred for purity. Hardcoded frontend constant is an acceptable MVP shortcut. Decision deferred to implementation phase. |
| OQ-005 | What is the minimum airport count and which specific airports should be included? | Resolved (FR-056–FR-059, Section 3.7) | Minimum 6 airports across at least 2 countries. Must include at least 2 airports in the same country. Recommended list documented in FR-059 note. |
| OQ-006 | What is the target delivery for Sprint 1? | Resolved | Sprint 1 targets full MVP delivery within the current sprint. |
