# Telengard.Godot

Godot 4 .NET presentation boundary. This module is intentionally separate
from Telengard.Core and is not required by headless simulation tests.

Add scenes, renderer adapters, audio, and Godot InputMap bindings here only
after the simulation contracts exist. Do not put authoritative game logic in
Godot nodes or callbacks.
