# Source code guidance

These rules apply under `src/` in addition to the root contract.

## Boundaries
- `Telengard.Core` owns renderer-independent simulation, commands/events, deterministic RNG, and domain state.
- `Telengard.Save` owns explicit DTOs and migrations; it may depend on simulation contracts but must not make serialization the runtime model.
- `Telengard.Content` is the content-definition boundary; content data must not become a hidden second rules engine.
- `Telengard.Terminal` and `Telengard.Godot` are presentation boundaries; they must not own authoritative gameplay state.
- Do not create reverse dependencies from Core into Save, Terminal, Godot, or renderer/UI concepts.

## C#/.NET
- Target `net8.0` and C# 12 as configured by repository files.
- Preserve nullable correctness and warnings-as-errors; fix causes rather than suppressing warnings.
- Prefer simple, explicit domain types and existing patterns over speculative abstractions.
- Modern syntax is welcome when it improves clarity, but do not mechanically rewrite unrelated code to primary constructors, collection expressions, or other style forms.
- There is currently no `Directory.Packages.props`; preserve the repository's actual package-management convention unless a dedicated task changes it.

## API and behavior changes
- Keep new public surface minimal. Before changing an existing public type/member/event/save shape, find all callers and tests and verify the change is required by the task/spec.
- Do not introduce gameplay formulas or tuning constants not defined by the spec/ticket. Keep unresolved tuning explicit/configurable.
- Any randomness affecting authoritative behavior must use the deterministic RNG infrastructure and receive determinism tests.

## Verification
- Never run bare `dotnet`; use `../eng/dotnet.ps1` or repo-root `./eng/dotnet.ps1`.
- Run focused tests while iterating.
- Before completing source changes, run repo-root `./eng/verify.ps1 -Mode Full` unless the task is explicitly documentation-only or verification is impossible; if impossible, report exactly what did not run and why.
