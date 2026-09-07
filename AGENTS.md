## Agent skills

### Issue tracker

Issues and specs live as local Markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical five-role triage vocabulary is used. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/`). See `docs/agents/domain.md`.

## Output review loop

Hard rule: every artifact (production source, tests, DLL artifacts, audit reports) ships only after the red-first TDD and dual-axis independent review loop closes CLEAN. Read `docs/agents/output-review-loop.md` before any production change or any formal output.

## Large writes

Generating a new file or a large edit expected to stream for over ~2 minutes (red-test files, long reports, bulk refactors) → read `docs/agents/large-write-batching.md` first and land the content in batches, each batch one tool call that finishes in seconds. Provider gateways cut long streams mid-generation; a one-shot large write gets truncated and burns its retry budget.
