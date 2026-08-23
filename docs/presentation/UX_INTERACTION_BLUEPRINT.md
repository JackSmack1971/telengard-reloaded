# Godot UX and interaction blueprint

## Purpose

Define the interaction and navigation contract for the playable Godot vertical
slice before production UI art is created. This document describes behavior and
state flow, not final styling.

## Principles

The client must remain:

- keyboard-first;
- controller-first;
- configurable;
- contextual;
- low-clutter;
- explicit about known rules;
- deliberately uncertain only where the game intends mystery.

Godot input produces intent. The simulation validates and resolves that intent.
UI widgets never apply gameplay results directly.

## Required client states

The first playable client must account for these user-visible states:

```text
startup/title
new game / load game
character creation
inn / preparation
dungeon exploration
context interaction
combat/encounter
inventory/equipment
spell selection / quick cast
journal/map
pause/settings
suspend/save/load
death / Legacy replacement
return-to-inn summary
```

These may be separate scenes, overlays, or state-driven views, but their
presentation structure must not create a second authoritative game-state
machine.

## Command mapping rule

Every gameplay action must map to a documented simulation command or a clearly
presentation-only action.

Examples:

| User intent | Ownership |
|---|---|
| move north/south/east/west | simulation command |
| attack/defend/flee/cast/use | simulation command |
| interact with feature | simulation command |
| equip/unequip | simulation command |
| start/finish expedition | simulation command |
| open map/journal | presentation only |
| move UI focus | presentation only |
| change window scale | presentation only |
| save/suspend/load | explicit persistence/application boundary |

If a requested UX action has no valid command/application boundary, do not
implement it by mutating state from Godot. Create or assign the missing owning
TEL work.

## Input contexts

Use explicit input contexts so the same physical button can have contextual
meaning without ambiguous command dispatch:

- `GLOBAL` — pause, settings, accessibility;
- `INN` — prepare, inventory, spells, enter dungeon;
- `EXPLORATION` — movement, interact, map, journal, quick-cast;
- `COMBAT` — attack, defend, flee, spell/item/context actions;
- `MENU` — focus/navigation/confirm/cancel;
- `DEATH` — review outcome, Legacy replacement flow where applicable.

The active context is presentation state derived from authoritative state plus
open UI overlays. It must not determine gameplay legality; the simulation still
validates commands.

## Controller and keyboard parity

A slice is not complete if its required action is reachable only by mouse.
Every required first-slice gameplay action must have keyboard and controller
navigation.

Mouse support is allowed but is not the sole required path.

## Focus and modal rules

- Every modal has a deterministic initial focus target.
- Cancel/back behavior is defined for every modal.
- Gameplay input does not leak through a modal that intentionally captures it.
- Closing a presentation-only modal must not alter authoritative state.
- Commands that advance simulation must make that transition explicit.

## Information hierarchy

The HUD should prioritize information needed for the central decision to
continue deeper or retreat:

- HP / immediate survivability;
- spell/resource capacity needed for immediate decisions;
- carried versus secured wealth distinction;
- threat classification during encounters;
- current floor/location context;
- contextual action availability;
- deliberately qualitative danger/atmosphere cues rather than raw hidden danger.

Secondary detail belongs in inventory, journal, map, or contextual panels.

## Mystery rule

The interface must distinguish:

```text
unknown because the player has not learned it
```

from:

```text
known by the simulation/player but omitted by the UI
```

Do not expose hidden stats to make UI implementation easier. Do not hide
legitimately known interaction instructions merely to manufacture mystery.

## Required wireframe coverage before production UI art

Before the Art Production Ready gate, placeholder wireframes must cover:

1. startup/new/load flow;
2. all three character-creation modes;
3. inn/preparation;
4. dungeon HUD and automap access;
5. feature interaction;
6. encounter/combat actions and threat communication;
7. inventory/equipment;
8. spells/quick cast;
9. journal;
10. pause/settings;
11. suspend/save/resume;
12. death/Legacy replacement;
13. return-to-inn result/secured-progress feedback.

## Accessibility baseline

The first playable slice should preserve space for:

- remappable controls;
- readable text scaling;
- full-pause accessibility behavior where supported by simulation time modes;
- clear focus indication;
- alternatives to color-only information where practical.

Specific accessibility settings may be split into later tickets, but the UI
architecture must not make them structurally difficult.

## Acceptance evidence for UX tickets

For Godot-visible interaction work, record:

- input path exercised;
- command/application boundary invoked;
- resulting authoritative state/event evidence;
- keyboard path;
- controller path when the ticket requires controller acceptance;
- screenshots or concise manual observations when practical;
- confirmation that presentation-only navigation did not mutate GameState.
