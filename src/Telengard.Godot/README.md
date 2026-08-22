# Telengard.Godot

Godot 4 presentation boundary. This module is intentionally separate from
Telengard.Core and is not required by headless simulation tests.

`ModernRenderer.tscn` is the TEL-091 visual prototype. Its script accepts a
dictionary made from the immutable `ModernRenderFrame` projection and draws a
map, discovered features, player marker, atmosphere/light cue, and HUD/combat
panel. The host is responsible for adapting the C# projection into that
dictionary and for submitting user intent as simulation commands.

The Godot node has no authoritative state and does not resolve commands,
generate randomness, or call simulation mutators. Headless tests therefore
verify the projection and event-cue contract; running Godot is an optional
manual presentation check.
