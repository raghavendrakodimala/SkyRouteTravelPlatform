---
applyTo: "**/*.{cs,csproj,sln,json}"
---

# Backend ASP.NET Instructions

These instructions apply to backend C#, ASP.NET, project, and related configuration files.

## Technology

- Use C# and ASP.NET 10.
- Treat backend code as production-intended.
- Prefer clear, maintainable, testable code.
- Follow standard C# and ASP.NET naming conventions.
- Respect existing project structure and conventions.

## Architecture

- Let the Solution Architect decide the final backend architecture after requirements review.
- Suitable options may include Minimal APIs, Controllers, feature folders, layered architecture, Clean Architecture, or vertical slice architecture.
- Keep business logic out of controllers and endpoint definitions.
- Keep persistence concerns behind abstractions.
- Do not tightly couple API behavior to in-memory storage.
- Prefer dependency injection for services, validators, repositories, and infrastructure components.

## Persistence

- Start with in-memory persistence unless a real database has been explicitly selected.
- Design persistence so it can later be replaced by SQL Server, PostgreSQL, or another real database.
- Do not put business rules directly inside in-memory data structures.
- Use interfaces or abstractions for persistence boundaries.
- Let the Database Engineer or Solution Architect decide the long-term data access approach.

## API Design

- Use Swagger/OpenAPI for API documentation.
- Keep OpenAPI behavior synchronized with actual API implementation.
- Use clear request and response models.
- Use appropriate HTTP methods and status codes.
- Avoid leaking implementation details through API responses.
- Prefer consistent route naming and versioning strategy when applicable.

## Validation

- Validate all external input on the backend.
- External input includes request bodies, route parameters, query strings, headers, and uploaded files.
- The validation approach should be decided by the Solution Architect and Lead Full Stack Engineer.
- Frontend validation may improve UX but must not replace backend validation.

## Error Handling

- Use centralized error handling.
- Return consistent, safe error responses.
- Do not expose stack traces or internal exception details in production.
- Detailed error messages may be allowed only in local/debug/development mode.
- Use appropriate HTTP status codes.

## Logging

- Use structured logging practices.
- Do not log secrets, passwords, tokens, payment information, or unnecessary personal data.
- The logging provider and strategy should be decided by the Solution Architect and DevOps Engineer.

## Security

- Follow OWASP Top 10 and secure coding practices.
- Never hardcode secrets, API keys, connection strings, passwords, or tokens.
- Use configuration, user secrets, environment variables, or approved secret stores.
- Authentication and authorization are not decided yet; do not implement them based on assumptions.
- The Solution Architect should decide and document authentication and authorization before implementation.

## Testing

- Prefer xUnit for backend unit tests.
- Create unit, integration, and API/contract tests where appropriate.
- Prefer test-first development for business logic and core components.
- Aim for meaningful coverage and at least 80% coverage where measurable.
- Run relevant tests after backend changes when possible.