---
applyTo: "**/*{Test,Tests,test,spec}.{cs,ts}"
---

# Testing Instructions

These instructions apply to backend and frontend test files.

## General Testing Principles

- Treat tests as production-quality code.
- Prefer meaningful tests over superficial coverage.
- Cover success paths, failure paths, validation, edge cases, and important business rules.
- Keep tests readable and maintainable.
- Do not test implementation details unless necessary.
- Use clear test names that describe expected behavior.

## Parallel Testing

- Testing should begin early, not only after implementation is complete.
- Functional testers should define scenarios before or during development.
- Test automation can start once contracts, DTOs, or UI flows are stable enough.
- Use API contracts, mock data, and agreed interfaces to unblock testing.

## Test-First Preference

- Prefer test-first development for business logic and core components.
- For feature work, define important test scenarios before implementation.
- Update tests when behavior changes.

## Coverage

- Aim for at least 80% coverage where measurable.
- Do not chase coverage numbers with low-value tests.
- Prioritize core business logic, validation, error handling, and API behavior.

## Backend Testing

- Prefer xUnit for backend unit tests.
- Create unit, integration, and API/contract tests where appropriate.
- Mock external dependencies when useful.
- Do not mock the system under test unnecessarily.
- Integration tests should verify realistic application behavior.
- API/contract tests should verify request/response shape, status codes, and important error cases.

## Frontend Testing

- Prefer Vitest for frontend unit/component tests when compatible with the Angular setup.
- Angular project setup should drive the final test approach.
- Use Playwright for e2e tests.
- Component tests should verify user-visible behavior.
- E2E tests should cover critical user journeys.

## Test Data

- Use clear and realistic test data.
- Avoid sensitive real personal data.
- Prefer builders or helper methods when setup becomes repetitive.
- Keep test data easy to understand.

## Running Tests

After changing code:

- Run relevant tests when possible.
- If tests cannot be run, explain why.
- Provide exact commands the developer should run.
- Do not claim tests passed unless they were actually run.