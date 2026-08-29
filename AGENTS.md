## Agent skills

### Issue tracker

Issues and specs live as local Markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical five-role triage vocabulary is used. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/`). See `docs/agents/domain.md`.

## Output review loop

Hard rule: every artifact (production source, tests, DLL artifacts, audit reports) ships only after the red-first TDD and dual-axis independent review loop closes CLEAN. Read `docs/agents/output-review-loop.md` before any production change or any formal output.
