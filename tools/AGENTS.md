# Tooling guidance

Tooling exists to make the simulation legible and verifiable; it must not become an authority for gameplay.

- `Telengard.TestHarness` consumes simulation/save contracts and may drive scripted commands, checkpoints, replay, state/event inspection, and deterministic comparisons.
- Keep tool output stable and concise enough for automated/agent use.
- Prefer machine-readable or line-stable output for new diagnostic commands when practical.
- Never duplicate gameplay rules in the harness to predict what Core should do; assert against Core outputs instead.
- Any tool that mutates state must do so through the same public command/simulation boundaries used by other consumers.
- Never run bare `dotnet`; use repo-root `./eng/dotnet.ps1`.
