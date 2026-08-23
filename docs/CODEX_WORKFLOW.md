# Codex workflow rationale

The executable next-slice workflow lives in
[`/.codex/skills/telengard-next-slice/SKILL.md`](../.codex/skills/telengard-next-slice/SKILL.md).
The repository constitution is [`/AGENTS.md`](../AGENTS.md), and independent
review is defined by
[`/.codex/skills/telengard-review/SKILL.md`](../.codex/skills/telengard-review/SKILL.md).
This page explains the design for humans; it is not a second copy of the
workflow contract.

## Why the loop is transactional

Each autonomous run advances one coherent slice from fresh repository evidence.
The agent selects from compact scheduling metadata, loads the selected ticket's
declared context, implements only that slice, verifies it, reviews the actual
diff, updates truthful durable records, and stops after handoff. A later run
re-evaluates the repository instead of carrying selection assumptions across a
merge.

## Progressive disclosure

Selection starts with the root contract, the next-slice skill, and the generated
[`docs/tasks/index.json`](tasks/index.json). The full human ledger, invariants,
architecture, development guidance, specification sections, source, tests, and
ExecPlans are loaded only after a candidate is selected or narrowed. New or
materially revised tickets declare required and conditional context in YAML front
matter; for those tickets, missing or contradictory context is a hard stop. Legacy
tickets use the generated index and existing ticket sections as a compatibility
fallback until they are revised.

The current human-facing milestone is the Playable Godot Vertical Slice: TEL-127
proves the playable client and TEL-128 separately governs Art Production Ready.
Detailed client methodology and gate evidence remain in the presentation
blueprints and gate documents rather than in this workflow rationale.

The task index is generated from the human ledger plus the explicit scheduling
metadata overlay in `docs/tasks/index-overrides.json` with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/task-index.ps1 -Mode Generate
```

Verification checks that the committed projection is current:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/task-index.ps1 -Mode Check
```

The ledger remains authoritative for ticket IDs, titles, and status. The
override file is authoritative only for scheduling metadata that the prose
ledger does not carry: dependencies, tracks, risk tags, context, review, and
verification. The projection keeps active/non-complete records and stores
completed tickets as a compact ID set for dependency checks. It is therefore a
scheduling surface, not a duplicate of ticket prose.

## Human invocation

From a repository-rooted Codex session:

```text
$telengard-next-slice
Advance Telengard Reloaded by exactly one safe logical implementation slice.
```

The workflow may be run on a conservative recurring schedule. It must stop on
overlapping work, unresolved product decisions, unavailable verification, or an
ambiguous merge gate. It must never weaken repository gates to keep automation
moving.

## Durable status

`docs/tasks/README.md` remains the human TEL status ledger. Ticket files define
scope and acceptance. `docs/tasks/index.json` is the compact scheduling
projection. `docs/BUILD_STATUS.md` remains append-only historical evidence.
Generated projections are updated through repository tooling, never by hand.
