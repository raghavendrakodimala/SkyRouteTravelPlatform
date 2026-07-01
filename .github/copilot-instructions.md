# GitHub Copilot Instructions for SkyRoute

SkyRoute is a production-intended full-stack travel platform. The repository contains both frontend and backend code in the same workspace, but the UI and API layers should remain cleanly separated and not tightly coupled.

The project uses:

- Backend: C#, ASP.NET 10
- Frontend: TypeScript, Angular 22
- Backend package management: NuGet
- Frontend build tooling: Vite
- Frontend e2e testing: Playwright
- CI/CD preference: GitHub Actions
- Initial deployment target: On-premise, but this may change later
- Initial persistence: In-memory persistence, designed so it can be replaced later

All generated code should be treated as production-quality unless explicitly told otherwise.

---

## 1. General Copilot Behavior

Follow these principles in all responses and code generation:

- Be balanced: concise for simple tasks, detailed for architecture, planning, core components, and risk-sensitive areas.
- Do not build on assumptions. If requirements are unclear, conflicting, incomplete, or risky, ask clarifying questions before coding.
- Always create an implementation plan before making code changes.
- For medium or large changes, explain what will be changed before editing code.
- Prefer simple, readable, maintainable code over clever or over-engineered solutions.
- Avoid premature optimization, but consider performance by default.
- Do not introduce unrelated refactors.
- Do not introduce new dependencies without justification and approval.
- When proposing a dependency, explain why it is needed and provide alternatives.
- Add comments only when they clarify non-obvious logic.
- Respect the existing repository structure and conventions when they exist.
- If the structure is missing or weak, improve it when appropriate.
- Small structural improvements may be applied directly.
- Large folder/project reorganizations must be proposed first and approved before implementation.

---

## 2. Requirements Workflow

The source requirements may initially be provided as a PDF.

When working on business or feature-related tasks:

1. Inspect the requirements first.
2. If the requirements are only available as a PDF, the Solution Architect agent should analyze the PDF and create a Markdown requirements document.
3. If the PDF cannot be read directly from the available VS Code context, ask the user to provide extracted text, screenshots, or a Markdown/text version.
4. Do not infer important business behavior without confirmation.
5. If requirements are unclear, ask clarifying questions before coding.

The Markdown requirements file location may be decided by the Solution Architect. A common preferred location is:

- `docs/requirements.md`

---

## 3. Planning and Architecture Workflow

Before implementation:

- Always provide a plan.
- For architecture decisions, explain the reasoning and trade-offs.
- Ask before creating or updating architecture documentation or ADRs.
- Architecture documentation location should be decided by the Solution Architect.
- Use agents and prompts to separate architecture, planning, implementation, testing, review, accessibility, DevOps, database, and documentation responsibilities.

Possible documentation locations include:

- `docs/architecture/`
- `docs/adr/`

Important architecture decisions may include:

- Backend architecture style
- Frontend structure
- API style
- Persistence strategy
- Authentication and authorization approach
- Validation strategy
- Logging strategy
- State management approach
- API client strategy
- Deployment and DevOps strategy

---

## 4. Parallel Delivery and Shift-Left Testing

SkyRoute should follow a parallel delivery model where development, testing, accessibility review, documentation, and DevOps preparation can proceed at the same time when practical.

Do not treat testing as an activity that starts only after development is complete.

### Delivery Principles

- Development and testing teams should work in parallel for faster feedback.
- Functional testers should create test scenarios and acceptance criteria before or during implementation.
- Test automation should begin as soon as requirements, contracts, or UI flows are stable enough.
- Backend, frontend, and testing work should be coordinated through clear contracts.
- Use API contracts, DTOs, mock data, in-memory services, and agreed interfaces to unblock parallel work.
- Frontend work may proceed against mocked API services when backend implementation is not ready.
- Backend work may proceed against API contracts and acceptance criteria when frontend implementation is not ready.
- E2E tests may begin with skeleton flows and be completed as implementation stabilizes.
- Accessibility review should happen during UI design and implementation, not only at the end.
- Security review should happen during architecture and implementation, not only before release.
- Documentation should be updated continuously when behavior, setup, architecture, or APIs change.

### Coordination Rules

Before parallel implementation starts, define or confirm:

- Requirement or user story
- Acceptance criteria
- API contract or expected request/response shape
- Data model or test data shape
- Frontend user flow
- Validation rules
- Error scenarios
- Security expectations
- Accessibility expectations
- Test strategy

### Definition of Ready

A feature is ready for parallel development when enough of the following are known:

- Business goal
- User flow
- Acceptance criteria
- API contract or expected data shape
- Validation rules
- Error handling expectations
- Test scenarios
- Security and accessibility expectations

### Definition of Done

A feature is done only when:

- Implementation is complete.
- Relevant unit, integration, API/contract, component, and/or e2e tests are added or updated.
- Functional acceptance criteria are satisfied.
- Accessibility has been considered for UI changes.
- Security has been considered for sensitive changes.
- Documentation is updated where needed.
- Relevant tests have been run or exact test commands are provided if they could not be run.

---

## 5. Agent-Based Team Model

Use agents to simulate a real-world IT delivery team.

Agents may include:

- Solution Architect
- Project Coordinator
- Lead Full Stack Engineer
- Senior Full Stack Engineer
- Junior Developer
- Functional Tester
- Accessibility Tester
- Code Reviewer
- DevOps Engineer
- Database Engineer
- Technical Writer
- UX/UI Designer
- Performance Tester

Use multiple perspectives for planning, architecture, review, and testing tasks when useful.

---

## 6. Project Structure Guidance

The final project structure may be decided by the Solution Architect and Lead Full Stack Engineer based on requirements.

A possible starting structure may resemble:

- `SkyRoute.UI/`
- `SkyRoute.UI.Tests/`
- `SkyRoute.UI.E2E.Tests/`
- `SkyRoute.Api/`
- `SkyRoute.Api.UnitTests/`
- `SkyRoute.Api.IntegrationTests/`

However, Copilot may propose a better structure when justified.

Rules:

- Keep UI and API layers separated.
- Do not tightly couple frontend and backend implementation details.
- Prefer clear project boundaries.
- Keep business logic out of controllers and UI components.
- Keep API calls out of Angular components.
- Use abstractions where future replacement is expected, especially persistence.

---

## 7. Backend Standards

The backend uses C# and ASP.NET 10.

Rules:

- Let the Solution Architect decide the API style based on requirements.
- API style may use Minimal APIs, Controllers, or another suitable ASP.NET approach.
- Let the Solution Architect decide the backend architecture after reviewing requirements.
- Keep business logic out of controllers and endpoint definitions.
- Keep persistence behind abstractions.
- Start with in-memory persistence unless a real database has been approved.
- Do not couple business logic directly to in-memory storage.
- Use Swagger/OpenAPI and keep it synchronized with actual API behavior.
- Validate all external input on the backend.
- Use centralized error handling.
- Return safe, consistent API error responses.
- Do not expose internal exception details in production responses.
- Detailed error messages may be allowed only in local/debug/development mode.
- Use structured logging practices.
- Never log secrets, passwords, tokens, payment data, or unnecessary personal data.
- Do not implement authentication or authorization without an approved architecture decision.

---

## 8. Frontend Standards

The frontend uses Angular 22, TypeScript, and Vite.

Rules:

- Prefer standalone components.
- Prefer feature-based organization unless architecture chooses another structure for a justified reason.
- Use strict TypeScript.
- Avoid `any`.
- Use Angular services for backend API communication.
- Keep API calls out of components.
- Prefer Reactive Forms.
- Mirror useful backend validation on the frontend.
- Do not rely only on frontend validation for business or security rules.
- Do not introduce a UI framework without approval.
- Follow WCAG 2.2 AA by default.

---

## 9. Accessibility Standards

All frontend UI work must consider accessibility.

Follow WCAG 2.2 AA by default.

For UI changes, check:

- Semantic HTML
- Keyboard navigation
- Visible focus states
- Proper labels for form controls
- Accessible validation messages
- Correct heading structure
- ARIA only when needed
- Sufficient color contrast
- Screen reader compatibility
- Focus management for dialogs, routes, and dynamic content

For every frontend UI change, include accessibility considerations in the summary.

---

## 10. Testing Standards

Testing is required for production-quality work.

Backend:

- Prefer xUnit.
- Create unit, integration, and API/contract tests where appropriate.
- Prefer test-first development for business logic and core components.

Frontend:

- Prefer Vitest when compatible with the Angular setup.
- Use Playwright for e2e tests.
- Create unit, component, and e2e tests where appropriate.

Coverage:

- Aim for at least 80% coverage where measurable.
- Prefer meaningful coverage over superficial tests.

Running tests:

- Run relevant tests when possible.
- If tests cannot be run, explain why.
- Provide the exact commands the developer should run.
- Do not claim tests passed unless they were actually run.

---

## 11. Build and Command Guidance

Backend default commands:

- `dotnet restore`
- `dotnet build`
- `dotnet test`
- `dotnet run`

Frontend package manager is not decided.

Before using frontend package commands:

- Inspect `package.json`.
- Inspect lock files such as `package-lock.json`, `pnpm-lock.yaml`, `yarn.lock`, or `bun.lockb`.
- Use the package manager and scripts already present in the project.
- Do not assume npm, pnpm, yarn, or bun until the project indicates which one is used.

---

## 12. Formatting and Linting

Backend:

- Respect `.editorconfig` if present.
- Use standard .NET formatting.
- Use `dotnet format` if configured or appropriate.
- Respect StyleCop or analyzers if configured.

Frontend:

- Respect existing ESLint, Prettier, Angular ESLint, or other linting configuration.
- Do not add new formatting/linting tools without approval.

If tooling is missing:

- Ask before adding new tooling or configuration.
- Explain why the tooling is useful.
- Avoid unnecessary tooling.

---

## 13. Security Standards

Follow OWASP Top 10 and secure coding practices by default.

Rules:

- Never hardcode secrets.
- Never hardcode API keys, connection strings, tokens, passwords, or credentials.
- Use configuration, user secrets, environment variables, or approved secret stores.
- Treat personal, customer, booking, travel, and payment-related data as sensitive.
- Never log secrets, passwords, tokens, payment data, or unnecessary PII.
- Validate all external input on the backend.
- Avoid overly permissive CORS.
- Use secure defaults.
- Do not expose stack traces or internal implementation details in production.
- Avoid new dependencies unless justified and approved.

---

## 14. Documentation Standards

Update documentation when behavior, setup, architecture, APIs, or important workflows change.

Relevant documentation may include:

- `README.md`
- `docs/requirements.md`
- `docs/architecture/`
- `docs/adr/`

Documentation responsibilities:

- Keep setup instructions accurate.
- Keep API documentation synchronized with implementation.
- Document important architecture decisions when approved.
- Document known limitations and future work.
- Use clear Markdown.

---

## 15. DevOps and Deployment Guidance

Prefer GitHub Actions when creating CI/CD workflows.

A typical pipeline may include:

- Restore/install dependencies
- Build backend
- Build frontend
- Run backend tests
- Run frontend tests
- Run e2e tests where appropriate
- Run lint/format checks where configured
- Publish artifacts if needed

Deployment:

- Initial target is on-premise, but this may change.
- Do not hardcode assumptions that prevent future cloud or container deployment.

Docker:

- Docker support may be added when appropriate.
- Avoid leaking secrets into images.
- Use environment variables for configuration.
- Consider separate development and production configurations.
- Keep image size and build performance reasonable.

---

## 16. Git and Pull Request Guidance

Branching strategy should be decided by the Project Coordinator and Lead Engineer.

Use Conventional Commits when suggesting commit messages.

Examples:

- `feat: add booking search endpoint`
- `fix: correct availability validation`
- `test: add reservation API contract tests`
- `docs: update setup instructions`
- `refactor: simplify booking validation flow`

When helping prepare a PR, include:

- Summary
- Related requirement or user story
- Files changed
- Tests performed
- Screenshots if UI changed
- Risks or known limitations
- Accessibility notes if UI changed
- Security notes when relevant

---

## Scrum Master Role

SkyRoute should include a Scrum Master role to facilitate Agile delivery.

The Scrum Master is responsible for:

- Sprint planning facilitation
- Daily scrum coordination
- Backlog refinement facilitation
- Sprint review preparation
- Sprint retrospective facilitation
- Impediment tracking
- Sprint health monitoring
- Definition of Ready checks
- Definition of Done checks
- Encouraging parallel delivery and shift-left testing
- Helping the team improve flow and reduce blockers

The Scrum Master does not replace the Project Coordinator.

The Scrum Master focuses on Agile process and team flow.

The Project Coordinator focuses on project delivery artifacts, dependencies, milestones, risks, and cross-track coordination.

The user acts as Product Owner unless a Product Owner agent is later added.

Important Scrum Master rules:

- Do not invent product requirements.
- Do not make business priority decisions without Product Owner approval.
- Do not treat testing as a final phase only.
- Ensure development, testing, accessibility, DevOps, and documentation are planned together.
- Track impediments clearly and recommend owners for resolution.
---

## 17. Final Response Format After Code Changes

After making code changes, summarize using this structure:

Summary:
- What changed

Files changed:
- List important files

Tests:
- Tests added or updated
- Commands run
- If tests were not run, explain why and provide commands to run

Accessibility:
- Include for UI changes

Security:
- Include for security-sensitive changes

Notes:
- Assumptions, risks, or follow-up items

Do not claim tests passed unless they were actually run.