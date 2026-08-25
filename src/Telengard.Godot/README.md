# Telengard.Godot

Godot 4 presentation/application boundary. This module is intentionally separate
from `Telengard.Core` and is not required by headless simulation tests.

## Current milestone

The immediate target is the
[`Five-Floor MVP Demo`](../../docs/MVP_DEMO.md), not the full TEL-127 client gate.
The Godot path already has host/bootstrap, input/session wiring, graybox dungeon
presentation, and partial HUD/combat interaction. The remaining MVP work is to
compose those foundations into one legitimate fixed-seed floor-1-through-floor-5
session.

The critical path is:

```text
TEL-129  floor-aware hosted session
  -> TEL-130  authored encounters/features/treasure in normal play
  -> TEL-131  demo-ready setup + combat closure
  -> TEL-132  real five-floor Godot acceptance
```

Save/resume breadth, full controller parity, complete death/Legacy UX, and
production art are post-MVP work.

## Host boundary

`ModernRenderer.tscn` starts the small `Telengard.GodotHost` .NET process. That
host loads the repository content pack, creates deterministic authoritative
`GameState` through existing Core setup boundaries, and emits a JSON
`ModernRenderFrame` projection. Godot passes that projection to the visual
renderer; it does not own or mutate authoritative state.

Build the host before launching this project:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/dotnet.ps1 build tools/Telengard.GodotHost/Telengard.GodotHost.csproj --configuration Debug
```

The Godot node has no authoritative gameplay state and does not generate
simulation randomness or call state mutators directly. The external host is the
composition/transport boundary: it translates intents into Core commands,
resolves stable content IDs through the loaded content pack, and supplies
explicit configuration required by Core resolvers.

Combat gameplay configuration remains explicit. The config JSON currently uses
`attack_damage`, `flee_success_chance`,
`trivial_maximum_level_difference`, `deadly_minimum_level_difference`, and
`known_monster_definition_ids`; no hidden host default should silently turn demo
tuning into permanent balance policy.

## MVP composition rules

For TEL-129–TEL-132:

- do not treat one retained floor-1 `FloorLayout` as the whole dungeon;
- movement uses the authoritative player's current deterministic floor layout;
- stairs invoke existing Core floor-transition rules with current/destination
  layouts;
- encounter tables, feature definitions/outcomes, and loot tables come from the
  production `ContentPack` and feed existing Core resolvers;
- Godot never owns encounter rolls, feature outcomes, loot outcomes, combat
  legality, or carried/secured wealth mutation;
- keyboard normal-input paths are sufficient for the MVP gate; and
- a real Godot run through floor 5 is required by TEL-132.

See
[`docs/gates/FIVE-FLOOR-MVP-DEMO.md`](../../docs/gates/FIVE-FLOOR-MVP-DEMO.md)
for the exact acceptance checklist.

## Verification boundaries

Headless tests verify projection, host-composition, and command-delegation
contracts. Godot-visible tickets also require the observation stated by their
acceptance criteria. A headless-only proof cannot pass TEL-132 because the MVP
is specifically a user-visible five-floor client milestone.

This infrastructure also does not by itself satisfy TEL-125's remaining broad UI
acceptance, TEL-126 persistence, TEL-127 full playable-slice breadth, or TEL-128
Art Production Ready. Those remain valid post-MVP work.
