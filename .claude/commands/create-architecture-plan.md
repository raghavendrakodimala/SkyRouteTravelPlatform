---
description: Create architecture plan.
argument-hint: "[project/context]"
allowed-tools: Read, Write, Edit, MultiEdit, Grep, Glob, LS, WebFetch, WebSearch, TodoWrite, Task
---

Use the solution-architect agent.

Create/update:

- `docs/architecture/architecture-overview.md`
- `docs/architecture/api-strategy.md`
- `docs/architecture/backend-architecture.md`
- `docs/architecture/frontend-architecture.md`
- `docs/architecture/data-model.md` — owned by the database-engineer agent, delegated by the solution-architect: entities, attributes, keys, relationships, ERD as a mermaid diagram, and a mapping note for the current in-memory persistence, designed as if a relational database will back it. Every API DTO and domain entity must be derivable from it.
- `docs/architecture/persistence-strategy.md`
- `docs/architecture/testing-strategy.md`
- `docs/handoffs/`

The architecture phase is not complete without `docs/architecture/data-model.md`.

Include backend, frontend, API, persistence, data model, validation, error handling, logging, security, accessibility, testing, DevOps, performance, decisions, risks, and trade-offs.
