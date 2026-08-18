# Contributing to Modern Telengard

Modern Telengard is pre-alpha and prioritizes a deterministic,
renderer-independent simulation. Before proposing a change, read:

- [`docs/modern-telengard-spec.md`](docs/modern-telengard-spec.md)
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/INVARIANTS.md`](docs/INVARIANTS.md)
- [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)
- [`docs/BUILD_STATUS.md`](docs/BUILD_STATUS.md)

## Development workflow

1. Create a focused branch and link the relevant TEL task or issue.
2. Keep authoritative state and rules in the simulation; presentations submit
   commands and consume state/events.
3. Preserve deterministic replay, hidden-information boundaries, carried versus
   secured wealth, and explicit save/version contracts.
4. Keep content definitions separate from simulation logic.
5. Update tests and documentation when behavior or contracts change.

Use the repository-local SDK wrapper from PowerShell. Do not invoke bare
`dotnet` for repository work:

```powershell
./eng/doctor.ps1
./eng/dotnet.ps1 restore Telengard.sln
./eng/verify.ps1 -Mode Full
```

For focused checks, use `./eng/dotnet.ps1` with the required `build`, `test`,
or `format` arguments. Do not install a global SDK or modify `.dotnet/` as
part of an ordinary change.

## Pull requests

Describe the intent, scope, non-goals, determinism impact, save impact,
command/event impact, and verification performed. Keep unrelated cleanup out
of the change. Pull requests use squash merging so each accepted change has a
single reviewable commit; the pull-request template lists the required checks.

Run the full verification gate before requesting review. If verification is
blocked by the environment, report the exact command and failure instead of
claiming a pass.

## Reporting problems

Use the repository issue templates for reproducible bugs, feature proposals,
and support questions. Report suspected vulnerabilities privately according to
the [security policy](SECURITY.md).
