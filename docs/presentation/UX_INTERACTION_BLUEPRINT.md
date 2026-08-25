# Godot UX and interaction blueprint

## Purpose

Define the interaction and navigation contract for the Godot client before
production UI art is created. This document describes behavior and state flow,
not final styling.

The current project sequence matters:

1. **Five-Floor MVP Demo** — prove the central dungeon loop through floor 5 with
   normal keyboard input and representative authored interactions.
2. **Playable Godot Vertical Slice** — complete the broader first-slice UX,
   including full required keyboard/controller parity, persistence, death/Legacy
   flow, and second-expedition coverage.
3. **Art Production Ready** — freeze enough UX structure to justify systematic
   production UI/art work.

Do not pull stage-2 or stage-3 breadth ahead of TEL-129–TEL-132 unless it is a
real blocker for the MVP.

## Durable principles

The complete client should remain:

- keyboard-first;
- controller-capable with full parity by TEL-127;
- configurable;
- contextual;
- low-clutter;
- explicit about known rules;
- deliberately uncertain only where the game intends mystery.

Godot input produces intent. The simulation validates and resolves that intent.
UI widgets never apply gameplay results directly.

## MVP interaction surface

For the Five-Floor MVP Demo, the required user-visible path is intentionally
small:

```text
startup / demo start
inn / demo-ready authoritative state
enter dungeon
explore / move
use legitimate stairs through floors 1-5
encounter / combat action
feature interaction
treasure feedback
floor-5 end-of-demo state
```

The MVP must not require developer/debug commands. Keyboard input is sufficient
for TEL-132 acceptance; complete controller parity remains a TEL-127 requirement.

Fixed seed/demo character setup is allowed. Save/load, all character-creation
modes, complete inventory/equipment UX, death/Legacy replacement, and a second
expedition are post-MVP unless a specific one becomes necessary to unblock the
mandated five-floor route.

## Broader required client states

The later Playable Godot Vertical Slice must account for these user-visible
states:

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
| use stairs / change floor | simulation command/application composition around Core transition |
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
- `EXPLORATION` — movement, stairs, interact, map, journal, quick-cast;
- `COMBAT` — attack, defend, flee, spell/item/context actions;
- `MENU` — focus/navigation/confirm/cancel;
- `DEATH` — review outcome, Legacy replacement flow where applicable.

The active context is presentation state derived from authoritative state plus
open UI overlays. It must not determine gameplay legality; the simulation still
validates commands.

## Controller and keyboard parity

### MVP

Every action required by `docs/gates/FIVE-FLOOR-MVP-DEMO.md` must be reachable by
normal keyboard input. Mouse-only/debug-only paths fail the MVP.

Controller support may exist and should not regress, but complete controller
parity does not block TEL-132.

### TEL-127 full playable slice

A required first-slice action is not complete if it is reachable only by mouse
or keyboard. Every required gameplay action in the broader gate must have
keyboard and controller navigation.

Mouse support is allowed but is not the sole required path.

## Focus and modal rules

- Every required modal has a deterministic initial focus target.
- Cancel/back behavior is defined for every modal in the current milestone.
- Gameplay input does not leak through a modal that intentionally captures it.
- Closing a presentation-only modal must not alter authoritative state.
- Commands that advance simulation must make that transition explicit.

The MVP should avoid adding nonessential modal complexity. Build only the
interaction surface needed to make the five-floor route understandable and
playable.

## Information hierarchy

The HUD should prioritize information needed for the central decision to
continue deeper or retreat:

- HP / immediate survivability;
- spell/resource capacity needed for immediate decisions;
- carried versus secured wealth distinction where present;
- threat classification during encounters;
- current floor/location context;
- contextual action availability;
- deliberately qualitative danger/atmosphere cues rather than raw hidden danger.

For TEL-132, current floor, survival state, encounter/action state, and carried
progress must be sufficiently clear to complete the demo. Secondary detail
belongs in inventory, journal, map, or contextual panels and may remain
post-MVP.

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

Before the Art Production Ready gate, placeholder wireframes must eventually
cover:

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

This is a TEL-127/TEL-128 readiness list, not the TEL-132 MVP checklist. The MVP
should not implement missing entries solely to make this list look complete.

## Accessibility baseline

The final first-slice client should preserve space for:

- remappable controls;
- readable text scaling;
- full-pause accessibility behavior where supported by simulation time modes;
- clear focus indication;
- alternatives to color-only information where practical.

Specific accessibility settings may be split into later tickets, but MVP work
must not choose a structure that makes them needlessly difficult later.

## Acceptance evidence for UX tickets

For Godot-visible interaction work, record:

- input path exercised;
- command/application boundary invoked;
- resulting authoritative state/event evidence;
- keyboard path;
- controller path when the selected ticket/gate requires controller acceptance;
- screenshots or concise manual observations when practical;
- confirmation that presentation-only navigation did not mutate GameState.

For TEL-132 specifically, record the complete normal-input route from demo start
through the designated floor-5 completion state.
