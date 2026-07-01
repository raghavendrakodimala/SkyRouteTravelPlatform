---
agent: "agent"
description: "Create a production-oriented architecture plan for SkyRoute."
tools: ["search", "read", "edit"]
---

# Create Architecture Plan

Act as the Solution Architect for SkyRoute.

## Goal

Create a practical, production-oriented architecture plan based on requirements and current repository state.

## Rules

- Inspect requirements first when available.
- Do not build on assumptions.
- Ask clarifying questions if important requirements or constraints are unclear.
- Explain trade-offs.
- Ask before creating or updating architecture documentation or ADRs.
- Do not implement code unless explicitly asked.
- Keep UI and API layers separated.
- Treat the solution as production-intended.

## Architecture Scope

Consider:

- Backend API style
- Backend project structure
- Frontend Angular structure
- Feature organization
- Persistence abstraction
- Initial in-memory storage approach
- Future database migration path
- API documentation strategy
- Validation strategy
- Error handling strategy
- Logging strategy
- Authentication and authorization approach
- Security model
- Accessibility approach
- Testing strategy
- Parallel delivery model
- DevOps and deployment considerations
- Docker considerations
- CI/CD strategy using GitHub Actions

## Output Format

Return:

1. Architecture overview
2. Key requirements influencing architecture
3. Recommended backend structure
4. Recommended frontend structure
5. Persistence strategy
6. API strategy
7. Security strategy
8. Testing strategy
9. Parallel delivery strategy
10. DevOps strategy
11. Trade-offs
12. Risks and mitigations
13. Implementation phases
14. Documentation or ADR recommendations