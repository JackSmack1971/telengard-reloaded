# Modern Telengard implementation task ledger

This ledger is the current human-readable authority for TEL ticket status. It is
derived from the ordered TEL tickets in `docs/modern-telengard-spec.md` plus
project-local extension tickets for Core Alpha, representative content, the
Godot client, repository engineering, and the Five-Floor MVP Demo.

## Current product priority

The immediate milestone is the **Five-Floor MVP Demo** defined in
[`../MVP_DEMO.md`](../MVP_DEMO.md).

The next implementation sequence is intentionally:

```text
TEL-129 → TEL-130 → TEL-131 → TEL-132
```

That sequence composes the already-built simulation/content/client foundations
into one legitimate fixed-seed floor-1-through-floor-5 Godot playthrough.

Until TEL-132 passes, TEL-126 save/resume breadth, TEL-127 full Playable Godot
Vertical Slice acceptance, TEL-128 Art Production Ready, and production asset
work are **post-MVP**. They remain valid tickets but must not pre-empt the MVP
sequence unless repository evidence proves one is a hard prerequisite or the
user explicitly changes the milestone.

## Status/provenance rules

- The specification and repository `AGENTS.md` are authoritative for long-term
  product intent and repository contracts.
- This file is authoritative for TEL IDs, titles, and status.
- `docs/tasks/index.json` is the generated compact scheduling projection of this
  ledger plus `index-overrides.json`.
- `docs/BUILD_STATUS.md` is append-only verification history and does not
  override this ledger or current implementation evidence.
- Ticket status must describe reality; planning/documentation alone never marks
  behavior implemented.
- Undefined formulas, balance, tuning, and policies remain
  `CONFIGURATION/TUNING DECISION REQUIRED` unless intentionally supplied as
  explicit replaceable demo configuration.
- Production-art tickets remain blocked until `docs/gates/ART-PRODUCTION-READY.md`
  passes.

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

### Core Alpha extensions (project-local)

These tickets close requirements needed by the original Core Alpha checklist
without changing the source specification.

- [TEL-100.md](TEL-100.md) — Define character creation command boundary — Implemented and verified
- [TEL-101.md](TEL-101.md) — Implement rolled character creation — Implemented and verified
- [TEL-102.md](TEL-102.md) — Implement point-allocation character creation — Implemented and verified
- [TEL-103.md](TEL-103.md) — Implement daily-seed character creation — Implemented and verified
- [TEL-104.md](TEL-104.md) — Implement initial player setup and world-seed selection — Implemented and verified
- [TEL-105.md](TEL-105.md) — Implement treasure acquisition and loot resolution — Implemented and verified
- [TEL-106.md](TEL-106.md) — Preserve Legacy knowledge across character replacement — Implemented and verified
- [TEL-107.md](TEL-107.md) — Add the Core Alpha vertical-slice integration proof — Implemented and verified
- [TEL-108.md](TEL-108.md) — Add deterministic developer debug commands — Implemented and verified

### Recommended First Vertical Slice content extensions (project-local)

The representative production content pack is complete for the current MVP:
one Upper Ruins band covering floors 1–5, eight monsters, first-slice encounter
and loot tables, representative items/spells, and the four named dungeon
features.

- TEL-109 — Establish the data-driven vertical-slice content pack — Implemented and verified
- [TEL-110.md](TEL-110.md) — Author the floors 1–5 dungeon biome and band data — Implemented and verified
- TEL-111 — Author the first-slice monster roster — Implemented and verified
- TEL-112 — Author first-slice encounter ecology tables — Implemented and verified
- TEL-113 — Author the first-slice item roster — Implemented and verified
- TEL-114 — Author first-slice loot tables — Implemented and verified
- TEL-115 — Author the first-slice spell roster — Implemented and verified
- TEL-116 — Author the four first-slice dungeon feature definitions — Implemented and verified

### Playable Godot client extensions (project-local)

These remain the broader production-shaped client track. Status below has been
reconciled against ticket acceptance criteria and current implementation evidence
rather than older umbrella-plan snapshots.

- [TEL-120.md](TEL-120.md) — Build playable Godot application host and bootstrap — Implemented and verified
- [TEL-121.md](TEL-121.md) — Implement Godot input-to-command and simulation-clock bridge — In progress
- [TEL-122.md](TEL-122.md) — Implement Godot client session and scene flow — In progress
- [TEL-123.md](TEL-123.md) — Expand production presentation contract and asset registry — Implemented and verified
- [TEL-124.md](TEL-124.md) — Build first-slice dungeon and content graybox presentation — In progress
- [TEL-125.md](TEL-125.md) — Build playable HUD and first-slice interaction flows — In progress
- [TEL-126.md](TEL-126.md) — Integrate Godot save, suspend, resume, and session lifecycle — Not started
- [TEL-127.md](TEL-127.md) — Verify playable Godot vertical slice — Not started
- [TEL-128.md](TEL-128.md) — Verify Art Production Ready gate — Not started

TEL-121, TEL-122, and TEL-124 all have substantial implemented automated/runtime
evidence but retain `In progress` status because their required broader
manual/interactive acceptance has not been fully recorded. Their implemented
pieces are available to the MVP; finishing that broader acceptance must not
pre-empt TEL-129–TEL-132 unless integration exposes a concrete defect. TEL-125
likewise remains in progress for its broader HUD/interaction acceptance.

### Five-Floor MVP Demo extensions (project-local, current priority)

These tickets close the integration gap between the implemented systems above
and an actual five-floor playable Godot demo. They do not replace TEL-126–128;
they create a smaller product checkpoint that must pass first.

- [TEL-129.md](TEL-129.md) — Compose deterministic floors 1–5 Godot session — Implemented and verified
- [TEL-130.md](TEL-130.md) — Compose first-slice encounters, features, and treasure into Godot demo — Blocked
- [TEL-131.md](TEL-131.md) — Close MVP demo setup and combat playthrough path — Blocked
- [TEL-132.md](TEL-132.md) — Verify fixed-seed five-floor MVP demo — Blocked

### Repository engineering tickets

- [TEL-117.md](TEL-117.md) — Harden coverage and mutation tooling scope — Implemented and verified
- [TEL-118.md](TEL-118.md) — Reconcile audit remediation status and documentation provenance — Implemented and verified
- [TEL-119.md](TEL-119.md) — Generate audit status fields from the canonical ticket/exec-plan ledger — Implemented and verified

## Current milestone dependency view

The project no longer needs additional foundational simulation or representative
content work before pursuing the MVP. Existing client implementations whose
broader manual acceptance is still open may be consumed by the MVP without
making those older tickets the scheduling priority. The critical path is:

```text
implemented Core + content + usable Godot graybox foundations
                    │
                    ▼
               TEL-129
        floor-aware hosted session
                    │
                    ▼
               TEL-130
     encounters + features + treasure
                    │
                    ▼
               TEL-131
       demo setup + combat closure
                    │
                    ▼
               TEL-132
       real five-floor Godot gate
                    │
                    ▼
          FIVE-FLOOR MVP COMPLETE
                    │
                    ▼
       TEL-126 → TEL-127 → TEL-128
```

## Definition of ledger completion

A ticket is complete only when its implementation and required verification
evidence satisfy its acceptance criteria. Documentation that describes planned
work does not implement that work.
