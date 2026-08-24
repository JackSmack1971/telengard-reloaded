# Test guidance

Tests verify Telengard behavior and architecture; they do not invent product rules.

- Existing test stack is xUnit. Reuse it; do not add another framework for convenience.
- Prefer small deterministic headless tests over graphical/integration setup when simulation behavior is sufficient.
- New domain behavior should cover the relevant subset of: valid path, invalid/boundary path, invariant, emitted events, persistence, deterministic replay, and dependency direction.
- For random/generation behavior, assert durable properties and replay equality; avoid brittle snapshots of incidental implementation details unless the exact output is a compatibility contract.
- For save behavior, test explicit DTO/version boundaries and round-trip/migration semantics.
- Do not weaken an invariant assertion merely to make an implementation pass.
- Do not read `bin/`, `obj/`, or `TestResults/` as test inputs.
- Never run bare `dotnet`; use repo-root `./eng/dotnet.ps1` or `./eng/verify.ps1`.
