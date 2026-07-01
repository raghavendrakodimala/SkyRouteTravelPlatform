---
applyTo: "**/*.{cs,ts,html,json,yml,yaml,config}"
---

# Security Instructions

These instructions apply to backend, frontend, configuration, and workflow files.

## Security Baseline

- Follow OWASP Top 10 and secure coding practices.
- Treat SkyRoute as production-intended.
- Treat customer, travel, booking, payment, and personal data as sensitive.
- Avoid insecure defaults.
- Do not implement security-sensitive behavior based on assumptions.

## Secrets

Never hardcode:

- Passwords
- API keys
- Tokens
- Connection strings
- Client secrets
- Certificates
- Private keys

Use approved mechanisms such as:

- Environment variables
- .NET user secrets for local development
- Secure configuration providers
- Approved secret stores

## Sensitive Data

Treat the following as sensitive by default:

- Names
- Email addresses
- Phone numbers
- Addresses
- Identification documents
- Passport or travel document data
- Payment data
- Booking history
- Customer or guest profiles
- Authentication tokens
- Authorization data

## Logging

- Never log secrets.
- Never log passwords or tokens.
- Never log payment information.
- Avoid logging unnecessary personal data.
- Use structured logging without exposing sensitive values.

## Authentication and Authorization

- Authentication and authorization are not decided yet.
- The Solution Architect should decide and document the approach before implementation.
- Do not default to JWT, cookies, ASP.NET Identity, OAuth, OIDC, or Entra ID without approval.
- Authorization should be explicit and requirement-driven.
- Role or permission models should be based on requirements.

## Input Validation

Validate all external input on the backend, including:

- Request bodies
- Route parameters
- Query strings
- Headers when security-sensitive
- Uploaded files

Frontend validation improves user experience but does not replace backend validation.

## CORS and HTTP Security

- Avoid overly permissive CORS.
- Do not allow all origins unless explicitly approved for local development.
- Keep development-only configuration separate from production configuration.
- Use secure defaults for HTTP-related behavior.

## Error Handling

- Use safe, consistent error responses.
- Do not expose stack traces or internal implementation details in production.
- Detailed errors may be used only in local/debug/development mode.

## Dependencies

- Do not add dependencies without justification and approval.
- Prefer built-in platform capabilities when sufficient.
- Consider security, maintenance, license, and long-term support before adding dependencies.

## Security Review

When reviewing code, check for:

- Hardcoded secrets
- Missing validation
- Broken access control
- Injection risks
- Insecure configuration
- Sensitive data exposure
- Unsafe logging
- Insecure error handling
- Overly permissive CORS
- Unnecessary dependencies