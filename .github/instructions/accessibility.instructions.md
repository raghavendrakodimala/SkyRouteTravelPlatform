---
applyTo: "**/*.{html,ts,scss,css}"
---

# Accessibility Instructions

These instructions apply to frontend UI, Angular templates, components, and styling.

## Accessibility Standard

Follow WCAG 2.2 AA by default.

Accessibility is required for production-quality UI work and should be considered for every frontend change.

## Semantic HTML

- Use semantic HTML elements where appropriate.
- Prefer native elements over custom ARIA-heavy implementations.
- Use buttons for actions and links for navigation.
- Maintain a logical heading structure.

## Keyboard Accessibility

- All interactive elements must be keyboard accessible.
- Ensure visible focus states.
- Avoid keyboard traps.
- Support expected keyboard interactions for dialogs, menus, forms, and navigation.

## Forms

- Every form control should have an accessible label.
- Validation errors should be clear and associated with the relevant control.
- Required fields should be communicated accessibly.
- Avoid relying only on color to indicate errors.
- Provide helpful instructions for complex inputs.

## ARIA

- Use ARIA only when needed.
- Do not use ARIA to replace semantic HTML unnecessarily.
- Ensure ARIA attributes are valid and reflect component state.
- For dynamic content, consider live regions only when appropriate.

## Focus Management

Manage focus for:

- Dialogs
- Modals
- Route changes
- Dynamic content updates
- Error summaries
- Multi-step workflows

## Visual Design

- Ensure sufficient color contrast.
- Do not rely only on color to convey meaning.
- Support responsive layouts.
- Avoid fixed layouts that break zoom or small screens.

## Testing

For UI changes, consider:

- Keyboard-only usage
- Screen reader behavior
- Focus order
- Form validation behavior
- Color contrast
- Responsive behavior

## Summary Requirement

For frontend UI changes, include accessibility notes in the final summary.