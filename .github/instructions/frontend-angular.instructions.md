---
applyTo: "**/*.{ts,html,scss,css,json}"
---

# Frontend Angular Instructions

These instructions apply to Angular, TypeScript, templates, styling, and related frontend configuration files.

## Technology

- Use Angular 22.
- Use TypeScript with strict typing.
- Use Vite-based frontend tooling.
- Treat frontend code as production-intended.
- Prefer standalone Angular components.
- Prefer feature-based organization unless the Solution Architect or Lead Full Stack Engineer chooses another structure for a justified reason.

## Angular Structure

Prefer:

- `src/app/core` for app-wide singleton services, interceptors, guards, and configuration.
- `src/app/shared` for reusable components, directives, pipes, and shared utilities.
- `src/app/features` for business features.

Feature folders may contain:

- `pages`
- `components`
- `services`
- `models`
- `routes`
- `state`, if state management is needed

## Component Design

- Keep components focused on UI behavior and presentation.
- Do not place business logic-heavy code directly inside components.
- Keep API calls out of components.
- Use Angular services for backend API communication.
- Prefer clear input/output contracts for reusable components.
- Avoid large components; split into smaller components when it improves readability.

## TypeScript

- Avoid `any`.
- Prefer explicit types where useful.
- Use interfaces or type aliases for API models and view models.
- Do not overcomplicate types unnecessarily.
- Follow Angular and TypeScript conventions.

## State Management

- Let the Solution Architect and Lead Full Stack Engineer decide the state management approach.
- Consider Angular signals, services, RxJS, or NgRx depending on complexity.
- Do not introduce heavy state management unless justified.

## Forms

- Prefer Reactive Forms.
- Keep forms accessible.
- Show clear validation messages.
- Mirror backend validation where appropriate.
- Do not rely only on frontend validation for business or security rules.

## API Communication

- Use Angular services for backend API calls.
- Keep backend URLs/configuration environment-aware.
- Do not hardcode production endpoints.
- Let the Solution Architect and Lead Full Stack Engineer decide whether to use generated typed OpenAPI clients or manually written services.

## Styling

- The UI framework is not decided yet.
- Respect existing styling conventions.
- Do not introduce Angular Material, Bootstrap, Tailwind, or another UI library without approval.
- If a UI framework is needed, explain options and trade-offs first.

## Testing

- Prefer Vitest for frontend unit/component testing when compatible with the Angular setup.
- Angular project setup should drive the final test approach.
- Use Playwright for e2e tests.
- Create unit, component, and e2e tests where appropriate.
- Aim for meaningful coverage and at least 80% coverage where measurable.

## Accessibility

- Follow WCAG 2.2 AA.
- Use semantic HTML.
- Ensure keyboard accessibility.
- Provide labels for form controls.
- Use ARIA only when needed.
- Manage focus for dialogs, routing changes, and dynamic content.
- Include accessibility considerations in summaries for UI changes.