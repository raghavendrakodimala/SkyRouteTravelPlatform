# Orchestration Rules

The main Claude agent coordinates the SDLC workflow.

For every phase:

1. Determine responsible agent.
2. Delegate work.
3. Inspect result.
4. Validate expected artifacts.
5. Validate handoff note.
6. Update `docs/handoffs/workflow-state.md`.
7. Continue to next agent.

Do not skip specialist agents when their specialty is needed.

Do not implement code before spec readiness.

Do not proceed past a blocked handoff.

Do not commit, merge, push, or delete branches unless the human user explicitly instructs it.
