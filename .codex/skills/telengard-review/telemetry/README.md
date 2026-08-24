# telengard-review skill-local telemetry

Runtime telemetry stays in this directory so the skill carries its own improvement history without depending on repository-global logging.

- `events.jsonl` is the append-only source record and is intentionally ignored by Git.
- `current-session.json` is ephemeral session state and is intentionally ignored by Git.
- `report.md` / `report.json` are derived reports and are intentionally ignored by Git.
- `scripts/skill_telemetry.py report --write` regenerates the report.
- Reports are grouped by `skill_fingerprint` so revisions can be compared without overwriting prior observations.

The review-specific report tracks lane routing, trigger distribution, actionable P0/P1/P2 lane yield, escalations, runtime-profile fallbacks, required-context misses, unexpected context, retries, and finding deduplication. A clean lane is not automatically evidence that the lane is unnecessary; telemetry is diagnostic evidence, not an automatic policy editor.

Record semantic classifications and counts, not reviewer reasoning. Do not store prompts, diff contents, finding prose, command stdout/stderr, environment variables, credentials, or API payloads. The helper redacts secret-like command arguments and telemetry failure never blocks the review itself.

Use the command wrapper only for consequential test/verification commands needed to falsify a review target. Routine file reads, searches, `git status`, and diff inspection should run normally rather than becoming telemetry noise.
