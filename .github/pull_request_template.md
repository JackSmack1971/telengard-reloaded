## Summary

<!-- What does this change accomplish? Link the TEL task or issue. -->

## Scope and non-goals

- Scope:
- Non-goals:

## Contract review

- [ ] Authoritative state remains in the simulation.
- [ ] State-changing input is validated before mutation.
- [ ] Determinism/replay behavior is preserved or covered by tests.
- [ ] Hidden information remains hidden until observed.
- [ ] Save/schema/version impact was reviewed, or this change has none.
- [ ] Carried and secured wealth semantics are preserved, or this change has none.

## Verification

Run `./eng/verify.ps1 -Mode Full` locally before requesting review when
possible. GitHub Actions runs the same canonical gate automatically on this
pull request and reports the `Full verification` check.

- [ ] Local `./eng/verify.ps1 -Mode Full` completed
- Focused checks:

## Documentation

- [ ] Documentation and task/status records were updated when needed.
- [ ] No unrelated files or generated artifacts are included.

Repository maintainers must configure the `Full verification` check as a
required status check for `main`; the workflow file cannot enable branch
protection by itself.
