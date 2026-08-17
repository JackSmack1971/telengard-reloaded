# Modern Telengard implementation task ledger

This ledger is derived from the ordered TEL tickets in `docs/modern-telengard-spec.md` §49. The Core Alpha extension tickets below are project-local implementation tasks, not additions or changes to the source specification. Numeric gaps in the specification-defined series remain intentional.

## Rules

- The specification and repository `AGENTS.md` are authoritative.
- These are implementation tasks, not gameplay decisions. Undefined formulas, balancing, and policies remain `CONFIGURATION/TUNING DECISION REQUIRED`.
- Every task preserves renderer-independent authoritative simulation, deterministic replay, explicit save DTOs/migrations, content separation, and documented non-goals.
- Status is `Not started` until implementation and verification evidence exist.

## Ordered ledger

### Foundation

- [TEL-001.md](TEL-001.md) — Create GameState domain model — Not started
- [TEL-002.md](TEL-002.md) — Implement deterministic RNG service — Not started
- [TEL-003.md](TEL-003.md) — Implement command dispatcher — Not started
- [TEL-004.md](TEL-004.md) — Implement domain event bus — Complete
- [TEL-005.md](TEL-005.md) — Implement save/load DTO system — Not started
- [TEL-006.md](TEL-006.md) — Add deterministic simulation test harness — Complete

### Dungeon

- [TEL-010.md](TEL-010.md) — Define dungeon coordinate types — Complete
- [TEL-011.md](TEL-011.md) — Implement procedural floor layout generator — Not started
- [TEL-012.md](TEL-012.md) — Add connectivity validation — Not started
- [TEL-013.md](TEL-013.md) — Implement tile visibility — Not started
- [TEL-014.md](TEL-014.md) — Implement fog-of-war map — Not started
- [TEL-015.md](TEL-015.md) — Implement stairs and floor transitions — Not started
- [TEL-016.md](TEL-016.md) — Persist discovered map state — Not started

### Expedition

- [TEL-020.md](TEL-020.md) — Implement ExpeditionState — Complete
- [TEL-021.md](TEL-021.md) — Implement inn state — Complete
- [TEL-022.md](TEL-022.md) — Implement carried gold — Complete
- [TEL-023.md](TEL-023.md) — Implement secured gold — Complete
- [TEL-024.md](TEL-024.md) — Implement expedition completion — Not started
- [TEL-025.md](TEL-025.md) — Implement expedition suspension — Implemented and verified

### Encounters

- [TEL-030.md](TEL-030.md) — Define monster data schema — Implemented and verified
- [TEL-031.md](TEL-031.md) — Implement encounter trigger system — Implemented and verified
- [TEL-032.md](TEL-032.md) — Implement combat state machine — Implemented and verified
- [TEL-033.md](TEL-033.md) — Implement attack — Implemented and verified
- [TEL-034.md](TEL-034.md) — Implement defend — Implemented and verified
- [TEL-035.md](TEL-035.md) — Implement flee — Implemented and verified
- [TEL-036.md](TEL-036.md) — Implement threat classification — Implemented and verified
- [TEL-037.md](TEL-037.md) — Implement player death — Implemented and verified

### Features

- [TEL-040.md](TEL-040.md) — Create generic dungeon feature system — Implemented and verified
- [TEL-041.md](TEL-041.md) — Implement weighted outcome engine — Implemented and verified
- [TEL-042.md](TEL-042.md) — Implement fountain — Implemented and verified
- [TEL-043.md](TEL-043.md) — Implement altar — Implemented and verified
- [TEL-044.md](TEL-044.md) — Implement pit — Implemented and verified
- [TEL-045.md](TEL-045.md) — Implement teleporter — Implemented and verified

### Knowledge

- [TEL-050.md](TEL-050.md) — Create KnowledgeEntry model — Implemented and verified
- [TEL-051.md](TEL-051.md) — Implement journal observation pipeline — Implemented and verified
- [TEL-052.md](TEL-052.md) — Implement sample counts — Implemented and verified
- [TEL-053.md](TEL-053.md) — Implement confidence progression — Implemented and verified
- [TEL-054.md](TEL-054.md) — Add monster knowledge — Implemented and verified
- [TEL-055.md](TEL-055.md) — Add feature knowledge — Implemented and verified
- [TEL-056.md](TEL-056.md) — Add teleporter mapping — Implemented and verified

### Items

- [TEL-060.md](TEL-060.md) — Define item templates — Implemented and verified
- [TEL-061.md](TEL-061.md) — Implement item instances — Implemented and verified
- [TEL-062.md](TEL-062.md) — Implement unidentified items — Implemented and verified
- [TEL-063.md](TEL-063.md) — Implement affixes — Implemented and verified
- [TEL-064.md](TEL-064.md) — Implement curses — Implemented and verified
- [TEL-065.md](TEL-065.md) — Implement equipment slots — Implemented and verified

### Progression

- [TEL-070.md](TEL-070.md) — Implement XP — Implemented and verified
- [TEL-071.md](TEL-071.md) — Implement levels — Implemented and verified
- [TEL-072.md](TEL-072.md) — Implement spell definitions — Implemented and verified
- [TEL-073.md](TEL-073.md) — Implement spell casting — Implemented and verified
- [TEL-074.md](TEL-074.md) — Implement talent constellations — Implemented and verified

### Legacy

- [TEL-080.md](TEL-080.md) — Implement Classic death — Implemented and verified
- [TEL-081.md](TEL-081.md) — Implement Legacy death — Not started
- [TEL-082.md](TEL-082.md) — Implement Adventure death — Not started
- [TEL-083.md](TEL-083.md) — Persist dead hero records — Not started
- [TEL-084.md](TEL-084.md) — Implement graves — Not started
- [TEL-085.md](TEL-085.md) — Implement heirlooms — Not started

### Presentation

- [TEL-090.md](TEL-090.md) — Create presentation-state adapter — Not started
- [TEL-091.md](TEL-091.md) — Build Modern renderer prototype — Not started
- [TEL-092.md](TEL-092.md) — Build Terminal renderer prototype — Not started
- [TEL-093.md](TEL-093.md) — Verify renderer-independent save compatibility — Not started

## Core Alpha extensions (project-local)

These tickets close requirements that are necessary for the §51 Core Alpha
checklist but are not owned by an existing §49 ticket. They are intentionally
narrow implementation extensions and remain `Not started` until implemented
and verified.

- [TEL-100.md](TEL-100.md) — Define character creation command boundary — Not started
- [TEL-101.md](TEL-101.md) — Implement rolled character creation — Not started
- [TEL-102.md](TEL-102.md) — Implement point-allocation character creation — Not started
- [TEL-103.md](TEL-103.md) — Implement daily-seed character creation — Not started
- [TEL-104.md](TEL-104.md) — Implement initial player setup and world-seed selection — Not started
- [TEL-105.md](TEL-105.md) — Implement treasure acquisition and loot resolution — Not started
- [TEL-106.md](TEL-106.md) — Preserve Legacy knowledge across character replacement — Not started
- [TEL-107.md](TEL-107.md) — Add the Core Alpha vertical-slice integration proof — Not started
- [TEL-108.md](TEL-108.md) — Add deterministic developer debug commands — Not started

## Core Alpha coverage audit

The audit compares §26 Character Creation, §48 Recommended First Vertical
Slice, and §51 Definition of Core Alpha with the existing ledger and current
implementation evidence:

- **Character creation:** no existing ticket owned the common boundary or any
  of the three required modes; TEL-100–TEL-103 are separate because each mode
  has different validation and determinism decisions.
- **Initial player setup and deterministic world-seed selection:** no existing
  ticket owned selecting/persisting the seed and creating a ready-at-inn game;
  TEL-104 owns that boundary.
- **Dungeon entry, generation, mapping, hidden geography, descent, return,
  carried/secured wealth, fight, flee, threat, features, suspend/resume, and
  renderer independence:** already represented by existing TEL-010–TEL-016,
  TEL-020–TEL-025, TEL-030–TEL-045, TEL-050–TEL-056, and TEL-090–TEL-093
  tickets. No duplicate extensions were created.
- **Monster appearance:** TEL-030 and TEL-031 own the content/runtime contract
  and deterministic configured trigger. No content-expansion ticket is added.
- **Treasure found underground:** item schemas and carried gold exist, but no
  existing ticket owns loot resolution into unsecured expedition state;
  TEL-105 owns that gap.
- **Feature and monster journal observations:** the existing TEL-051,
  TEL-054, and TEL-055 tickets own the observation pipeline and knowledge
  categories; no duplicate ticket is added.
- **Knowledge between Legacy characters:** existing legacy tickets do not own
  the death-to-new-character profile handoff required by §51; TEL-106 owns
  that narrow gap.
- **First vertical-slice content:** §48 is a recommendation, not a separate
  §51 checklist item. Per scope, no production content-expansion ticket is
  created. TEL-107 is an integration/evidence ticket using fixtures and does
  not add the recommended content counts.
- **Developer debug commands and deterministic test mode:** the existing
  harness only accepts `--seed` and `--deterministic`; §43–§44 are not
  owned by an existing TEL ticket. TEL-108 owns the tooling surface without
  moving authority into tooling.
- **Depth ecology, broad content counts, and other post-alpha systems:** not
  required to close the §51 checklist and deliberately not ticketed here.

## Definition of ledger completion

The ledger is complete when every listed task has a focused implementation, tests proportional to its domain behavior, and evidence against its acceptance criteria. Creating these documents does not implement gameplay.
