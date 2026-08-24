# telengard-next-slice skill-local telemetry

Runtime telemetry stays in this directory so the skill carries its own improvement history without depending on repository-global logging.

- `events.jsonl` is the append-only source record and is intentionally ignored by Git.
- `current-session.json` is ephemeral session state and is intentionally ignored by Git.
- `report.md` / `report.json` are derived reports and are intentionally ignored by Git.
- `scripts/skill_telemetry.py report --write` regenerates the report.
- Reports are grouped by `skill_fingerprint` so revisions can be compared without overwriting prior observations.

The next-slice report tracks candidate selection survival, rejection reasons, context escape/misses, Godot resolution, canonical full-verification first-attempt success, review reruns/results, merge-gate bottlenecks, ready-for-human outcomes, and transaction-integrity signals such as second-slice attempts.

Record normalized facts and outcomes, not private selection reasoning. Do not store prompts, diff/source contents, command stdout/stderr, environment variables, credentials, PR/API payloads, or access tokens. The helper redacts secret-like command arguments and telemetry failure never blocks the implementation workflow.

Use the command wrapper only for consequential repository checks such as task-index validation, Godot doctor, focused/subsystem tests, builds, generators, ticket verification, and the canonical full gate. Routine reads, searches, `git status`, metadata inspection, and diff review should run normally rather than becoming telemetry noise.
