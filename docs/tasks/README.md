# Modern Telengard implementation task ledger

This ledger is derived from the ordered TEL tickets in `docs/modern-telengard-spec.md` §49. The Core Alpha, first-vertical-slice content, and playable-Godot-client extension tickets below are project-local implementation tasks, not additions or changes to the source specification. Numeric gaps in the specification-defined series remain intentional.

## Rules

- The specification and repository `AGENTS.md` are authoritative.
- These are implementation tasks, not gameplay decisions. Undefined formulas, balancing, and policies remain `CONFIGURATION/TUNING DECISION REQUIRED`.
- Every task preserves renderer-independent authoritative simulation, deterministic replay, explicit save DTOs/migrations, content separation, and documented non-goals.
- Status is `Not started` until implementation and verification evidence exist.
- This ledger is the current authority for TEL-ticket status. `docs/BUILD_STATUS.md` is append-only verification history; it does not override this ledger when historical evidence and current status differ.
- For TEL-110–TEL-128 selection, also follow `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`; the content and Godot-client tracks may progress in parallel when explicit dependencies are satisfied.
- Production-art tickets must not be introduced or selected until `docs/gates/ART-PRODUCTION-READY.md` has passing evidence.

## Ordered ledger

### Foundation

- [TEL-001.md](TEL-001.md) — Create GameState domain model — Implemented and verified
- [TEL-002.md](TEL-002.md) — Implement deterministic RNG service — Implemented and verified
- [TEL-003.md](TEL-003.md) — Implement command dispatcher — Implemented and verified
- [TEL-004.md](TEL-004.md) — Implement domain event bus — Implemented and verified
- [TEL-005.md](TEL-005.md) — Implement save/load DTO system — Implemented and verified
- [TEL-006.md](TEL-006.md) — Add deterministic simulation test harness — Implemented and verified

### Dungeon

- [TEL-010.md](TEL-010.md) — Define dungeon coordinate types — Implemented and verified
- [TEL-011.md](TEL-011.md) — Implement procedural floor layout generator — Implemented and verified
- [TEL-012.md](TEL-012.md) — Add connectivity validation — Implemented and verified
- [TEL-013.md](TEL-013.md) — Implement tile visibility — Implemented and verified
- [TEL-014.md](TEL-014.md) — Implement fog-of-war map — Implemented and verified
- [TEL-015.md](TEL-015.md) — Implement stairs and floor transitions — Implemented and verified
- [TEL-016.md](TEL-016.md) — Persist discovered map state — Implemented and verified

### Expedition

- [TEL-020.md](TEL-020.md) — Implement ExpeditionState — Implemented and verified
- [TEL-021.md](TEL-021.md) — Implement inn state — Implemented and verified
- [TEL-022.md](TEL-022.md) — Implement carried gold — Implemented and verified
- [TEL-023.md](TEL-023.md) — Implement secured gold — Implemented and verified
- [TEL-024.md](TEL-024.md) — Implement expedition completion — Implemented and verified
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
- [TEL-081.md](TEL-081.md) — Implement Legacy death — Implemented and verified
- [TEL-082.md](TEL-082.md) — Implement Adventure death — Implemented and verified
- [TEL-083.md](TEL-083.md) — Persist dead hero records — Implemented and verified
- [TEL-084.md](TEL-084.md) — Implement graves — Implemented and verified
- [TEL-085.md](TEL-085.md) — Implement heirlooms — Implemented and verified

### Presentation

- [TEL-090.md](TEL-090.md) — Create presentation-state adapter — Implemented and verified
- [TEL-091.md](TEL-091.md) — Build Modern renderer prototype — Implemented and verified
- [TEL-092.md](TEL-092.md) — Build Terminal renderer prototype — Implemented and verified
- [TEL-093.md](TEL-093.md) — Verify renderer-independent save compatibility — Implemented and verified

## Core Alpha extensions (project-local)

These tickets close requirements that are necessary for the §51 Core Alpha
checklist but are not owned by an existing §49 ticket. They are intentionally
narrow implementation extensions and remain `Not started` until implemented
and verified.

- [TEL-100.md](TEL-100.md) — Define character creation command boundary — Implemented and verified
- [TEL-101.md](TEL-101.md) — Implement rolled character creation — Implemented and verified
- [TEL-102.md](TEL-102.md) — Implement point-allocation character creation — Implemented and verified
- [TEL-103.md](TEL-103.md) — Implement daily-seed character creation — Implemented and verified
- [TEL-104.md](TEL-104.md) — Implement initial player setup and world-seed selection — Implemented and verified
- [TEL-105.md](TEL-105.md) — Implement treasure acquisition and loot resolution — Implemented and verified
- [TEL-106.md](TEL-106.md) — Preserve Legacy knowledge across character replacement — Implemented and verified
- [TEL-107.md](TEL-107.md) — Add the Core Alpha vertical-slice integration proof — Implemented and verified
- [TEL-108.md](TEL-108.md) — Add deterministic developer debug commands — Implemented and verified

## Recommended First Vertical Slice content extensions (project-local)

These tickets populate only the §48 representative slice: floors 1–5, one
dungeon biome, 8–12 monsters, 10–15 items, 6–8 spells, and the four named
features. They author data against the existing `Telengard.Content` schemas;
they do not add fifty-floor content, move rules into renderers, or reimplement
the already-owned feature, journal, wealth, inn, or death mechanics.

- TEL-109 — Establish the data-driven vertical-slice content pack — Implemented and verified
  - Define the external content files, catalog/loader boundary, validation, and content-version handling for the slice.
  - Keep authored definitions separate from simulation logic and presentation; do not add renderer-owned content rules.
- [TEL-110.md](TEL-110.md) — Author the floors 1–5 dungeon biome and band data — Implemented and verified
  - Provide one biome covering only floors 1–5, with data references for its encounter, feature, and loot ecology.
  - Keep unresolved pacing, weights, and balance as `CONFIGURATION/TUNING DECISION REQUIRED`.
- TEL-111 — Author the first-slice monster roster — Implemented and verified
  - Provide 8–12 distinct monster definitions using families, traits, behaviors, actions, resistances, vulnerabilities, spawn rules, and loot references.
  - Require ecological differences; do not fill the roster with stat-scaled variations of one monster.
- TEL-112 — Author first-slice encounter ecology tables — Implemented and verified
  - Provide deterministic, data-driven floor-band encounter tables for floors 1–5 that reference the TEL-111 monster IDs.
  - Keep encounter selection and validation in the existing simulation/content boundary, not in renderer code.
- TEL-113 — Author the first-slice item roster — Implemented and verified
  - Provide 10–15 item definitions using the existing categories, properties, identification, affix, curse, and depth-rule fields.
  - Keep item-instance creation and treasure resolution in the existing item/expedition tickets.
- TEL-114 — Author first-slice loot tables — Implemented and verified
  - Provide data-driven loot tables that reference TEL-113 item IDs and the existing carried-wealth/treasure boundary.
  - Do not invent a permanent drop-rate, value, or loss formula where the specification is silent.
- TEL-115 — Author the first-slice spell roster — Implemented and verified
  - Provide 6–8 spell definitions with targeting, effects, interactions, costs, and discovery descriptions for the existing spell system.
  - Leave balance and undefined spell formulas configurable.
- TEL-116 — Author the four first-slice dungeon feature definitions — Implemented and verified
  - Provide data definitions and outcomes for exactly fountain, altar, pit, and teleporter using the existing generic feature and knowledge contracts.
  - Do not duplicate the already-implemented feature resolvers or encode feature behavior in a renderer.

## Playable Godot client extensions (project-local)

TEL-091 is the visual Modern renderer prototype and TEL-093 proves renderer
separation/save compatibility. The tickets below own the separate transition to
a complete playable Godot application and the later art-production handoff.
Follow `docs/presentation/GODOT_CLIENT_BLUEPRINT.md` and the active umbrella
ExecPlan.

The client track intentionally overlaps the content track: TEL-120–TEL-123 may
proceed when their dependencies are satisfied while TEL-110–TEL-116 are still
being authored. TEL-124 onward declares the content dependencies needed for
real first-slice integration. Numerical order alone does not determine next
slice eligibility.

- [TEL-120.md](TEL-120.md) — Build playable Godot application host and bootstrap — Implemented and verified
- [TEL-121.md](TEL-121.md) — Implement Godot input-to-command and simulation-clock bridge — Not started
- [TEL-122.md](TEL-122.md) — Implement Godot client session and scene flow — Not started
- [TEL-123.md](TEL-123.md) — Expand production presentation contract and asset registry — Not started
- [TEL-124.md](TEL-124.md) — Build first-slice dungeon and content graybox presentation — Not started
- [TEL-125.md](TEL-125.md) — Build playable HUD and first-slice interaction flows — Not started
- [TEL-126.md](TEL-126.md) — Integrate Godot save, suspend, resume, and session lifecycle — Not started
- [TEL-127.md](TEL-127.md) — Verify playable Godot vertical slice — Not started
- [TEL-128.md](TEL-128.md) — Verify Art Production Ready gate — Not started

### Presentation production gates

- `docs/gates/GODOT-PLAYABLE-SLICE.md` must pass using real first-slice content and placeholder/graybox presentation before the client is treated as a playable production-shaped vertical slice. TEL-127 owns this acceptance.
- `docs/gates/ART-PRODUCTION-READY.md` must pass before systematic final tiles, sprites, animations, VFX, UI-art, icon, or production-audio TEL batches are created. TEL-128 owns this separate acceptance.
- Concept art, style studies, UI wireframes, technical rendering tests, and explicit placeholders are allowed before Art Production Ready because they exist to validate the client and art direction rather than freeze production inventory.

## Repository engineering tickets

- [TEL-117.md](TEL-117.md) — Harden coverage and mutation tooling scope — Implemented and verified
- [TEL-118.md](TEL-118.md) — Reconcile audit remediation status and documentation provenance — Implemented and verified
- [TEL-119.md](TEL-119.md) — Generate audit status fields from the canonical ticket/exec-plan ledger — Implemented and verified

## Core Alpha and playable-slice coverage audit

The audit compares §26 Character Creation, §48 Recommended First Vertical
Slice, §51 Definition of Core Alpha, and the post-Phase-9 presentation gap with
the existing ledger and current implementation evidence:

- **Character creation:** TEL-100 owns the common boundary, while TEL-101–TEL-103 own the three required modes; each mode has different validation and determinism decisions.
- **Initial player setup and deterministic world-seed selection:** TEL-104 owns selecting/persisting the seed and creating a ready-at-inn game.
- **Dungeon entry, generation, mapping, hidden geography, descent, return, carried/secured wealth, fight, flee, threat, features, suspend/resume, and renderer independence:** represented by TEL-010–TEL-016, TEL-020–TEL-025, TEL-030–TEL-045, TEL-050–TEL-056, and TEL-090–TEL-093.
- **Treasure found underground:** TEL-105 owns loot resolution into unsecured expedition state.
- **Knowledge between Legacy characters:** TEL-106 owns the death-to-new-character profile handoff required by §51.
- **Core Alpha composition:** TEL-107 provides deterministic headless end-to-end evidence and explicitly leaves production content and renderer/client integration separate.
- **First vertical-slice content:** TEL-109–TEL-116 own the data-driven pack and representative authored content.
- **Playable Godot client:** TEL-090–TEL-093 prove renderer boundaries but do not provide an application host or full playable UI. TEL-120–TEL-127 own host/bootstrap, input/time, scene/session flow, production presentation identity, graybox world presentation, interaction UX, persistence, and playable-client acceptance.
- **Production-art readiness:** TEL-128 separately owns the Art Production Ready gate so unresolved visual direction or asset-pipeline policy cannot make TEL-127 ambiguously complete.
- **Production art:** intentionally not ticketed yet. Production batches are derived from the validated first-slice asset inventory only after TEL-128 passes.
- **Depth ecology, broad content counts, and other post-alpha systems:** remain outside the representative slice until separately planned.

## Definition of ledger completion

The ledger is complete when every listed task has a focused implementation, tests proportional to its domain behavior, and evidence against its acceptance criteria. Creating these documents does not implement gameplay or a playable client.
