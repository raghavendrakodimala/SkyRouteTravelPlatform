# Spec-Driven Development Rules

Owner concept: this file owns the canonical per-work-type spec requirements, spec statuses, and traceability chain.

No implementation should begin before required specs are ready.

## Backend/API Work Requires

- User story
- Acceptance criteria
- API contract
- Validation rules
- Error response rules
- Security expectations
- Test scenarios

## Frontend/UI Work Requires

- User story
- Acceptance criteria
- UI flow
- Form behavior
- Error/loading/empty states
- Accessibility expectations
- Test scenarios

## DevOps Work Requires

- Technical task
- Build/test/deployment expectations
- Environment assumptions
- Secret handling rules
- Rollback/recovery expectations if applicable

## Spec Statuses

Use:

- Draft
- In Review
- Approved
- Implemented
- Superseded
- Rejected

## Traceability

Every requirement should trace to:

- User story
- Feature spec
- Implementation task
- Test case
- Review result
