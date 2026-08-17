# Development tooling

Reserved for deterministic debug commands, state dumps, replay inspection, and
test fixtures. Tooling consumes simulation contracts; it does not own state.

`Telengard.TestHarness` runs scripted dispatcher commands headlessly, can
round-trip through the explicit save boundary at checkpoints, and compares
stable final-save/event results for deterministic replay checks.
