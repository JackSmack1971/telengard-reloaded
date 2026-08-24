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
./eng/dotnet.ps1 build Telengard.sln --configuration Debug
```

The Godot node has no authoritative state and does not resolve commands,
generate randomness, or call simulation mutators. Headless tests verify the
projection and event-cue contract; launching Godot verifies the external host
and content/bootstrap boundary.
