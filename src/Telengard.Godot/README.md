# Telengard.Godot

Godot 4 presentation boundary. This module is intentionally separate from
Telengard.Core and is not required by headless simulation tests.

`ModernRenderer.tscn` starts the small `Telengard.GodotHost` .NET process. That
host loads the repository content pack, creates a deterministic authoritative
`GameState` through the existing Core setup boundary, and emits a JSON
`ModernRenderFrame` projection. Godot passes that projection to the visual
renderer; it does not own or mutate authoritative state.

Build the host before launching this project:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/dotnet.ps1 build tools/Telengard.GodotHost/Telengard.GodotHost.csproj --configuration Debug
```

The Godot node has no authoritative state and does not generate randomness or
call simulation mutators. The external host is the composition/transport
boundary: it translates intents into Core commands, resolves stable spell IDs
through the loaded content pack, and injects explicit combat tuning through
`--gameplay-config`. The gameplay config JSON must provide
`attack_damage`, `flee_success_chance`,
`trivial_maximum_level_difference`, `deadly_minimum_level_difference`, and
`known_monster_definition_ids`; no host default is supplied because those
values remain `CONFIGURATION/TUNING DECISION REQUIRED` until product tuning is
authored.

Headless tests verify the projection and command-delegation contract; launching
Godot verifies the external host and content/bootstrap boundary. This
infrastructure does not by itself satisfy TEL-125's broader UI acceptance or
the TEL-127 playable-slice gate.
