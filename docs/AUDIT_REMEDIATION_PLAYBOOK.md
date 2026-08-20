---
document_type: audit-remediation-playbook
audience: downstream-coding-agents
repository: JackSmack1971/telengard-reloaded
baseline_commit: 34c7a4d00d8b7588869875f34edee1c1adfcdeaf
audit_date: 2026-08-18
status: active
execution_policy: one-remediation-packet-per-focused-change
---

# Audit Remediation Playbook for Coding Agents

## 0. Purpose

This document translates the 2026-08-18 repository audit into bounded, executable remediation work for downstream AI coding agents.

It is intentionally written as an implementation control document, not as a narrative audit report. Treat each `AUD-*` section as a work packet with explicit scope, evidence, constraints, tests, and completion criteria.

The audit baseline is commit `34c7a4d00d8b7588869875f34edee1c1adfcdeaf` (`Persist dead hero records (TEL-083)`). Before changing code, verify the current branch has not already remediated the packet. If the implementation has moved, preserve the intent and re-anchor evidence to the current code rather than blindly applying file/line assumptions from the baseline.

## 1. Authority and repository contracts

Read these before implementing any packet:

- `docs/modern-telengard-spec.md`
- `docs/ARCHITECTURE.md`
- `docs/INVARIANTS.md`
- `docs/DEVELOPMENT.md`
- `docs/BUILD_STATUS.md`
- `CONTRIBUTING.md`
- the relevant `docs/tasks/TEL-*.md` files for affected systems

The following constraints are non-negotiable unless a packet explicitly requires a compatibility change:

1. Authoritative gameplay state remains owned by the simulation.
2. State-changing commands are validated before mutation.
3. Presentation code does not become authoritative.
4. Equal seed/version/initial-state/command inputs must reproduce equal authoritative outcomes.
5. Hidden-information boundaries remain intact.
6. Carried and secured wealth semantics remain intact.
7. Runtime objects do not become the persistence contract; saves continue to use explicit DTOs and migrations.
8. Renderer independence remains intact.
9. Avoid unrelated refactors. A remediation packet is not permission to redesign neighboring systems.

## 2. Agent execution protocol

For every packet:

1. **Refresh evidence.** Inspect the named files and search for all relevant call sites. Do not assume the audit snapshot is still current.
2. **State the compatibility impact before editing.** Explicitly decide whether the change affects deterministic vectors, generator version, simulation version, save schema, event contracts, or public APIs.
3. **Write or update failing tests first when practical.** Tests must prove the defect or missing invariant, not merely increase coverage.
4. **Make the smallest production change that closes the packet.** Preserve public interfaces unless the packet requires a deliberate contract change.
5. **Run focused checks, then the repository gate.** Use repository scripts rather than ad-hoc substitutes.
6. **Inspect the diff for scope creep.** Remove unrelated cleanup.
7. **Update documentation/version records when behavior or compatibility changes.** Do not claim compatibility if deterministic vectors changed.
8. **Record completion evidence.** A packet is complete only when its acceptance criteria are demonstrably satisfied.

Canonical verification commands:

```powershell
./eng/doctor.ps1
./eng/dotnet.ps1 format Telengard.sln --verify-no-changes
./eng/dotnet.ps1 build Telengard.sln --configuration Release
./eng/dotnet.ps1 test Telengard.sln --configuration Release --no-restore
./eng/verify.ps1 -Mode Full
```

If the environment blocks `eng/doctor.ps1`, record the exact cause. Do not convert an environment failure into a claimed pass.

## 3. Priority and dependency map

Use this default order unless the current repository state makes a different order safer:

```text
P0 simulation integrity
  AUD-001 canonical deterministic RNG encoding
  AUD-002 defensive ownership of authoritative collections

P1 state identity / lifecycle
  AUD-005 unique deterministic expedition identity   <- review AUD-001 compatibility first
  AUD-006 enforce dungeon lifecycle guards
  AUD-007 harden save trust-boundary validation      <- review AUD-002 invariants first

P1 engineering controls (can proceed in parallel)
  AUD-003 update .NET 8 security patch baseline
  AUD-004 enforce verification in GitHub Actions

P2 failure semantics / quality-gate clarity
  AUD-008 remove event-observer commit ambiguity
  AUD-009 reconcile verification vs coverage gates
```

Do not batch all packets into one change. Preferred unit: one packet per focused branch/PR, except tightly coupled save/version updates that cannot be reviewed safely in isolation.

## 4. Machine-readable remediation index

```yaml
remediations:
  - id: AUD-001
    severity: high
    priority: P0
    status: closed
    verified_commit: 1c1a1262
    area: determinism
    compatibility_sensitive: true
  - id: AUD-002
    severity: high
    priority: P0
    status: closed
    verified_commit: d976c4c4
    area: authoritative-state
    compatibility_sensitive: false
  - id: AUD-003
    severity: high
    priority: P1
    status: open
    area: toolchain-security
    compatibility_sensitive: false
  - id: AUD-004
    severity: high
    priority: P1
    status: closed
    verified_commit: 3957084
    area: repository-controls
    compatibility_sensitive: false
  - id: AUD-005
    severity: medium
    priority: P1
    status: closed
    verified_commit: 9cbd2021
    area: expedition-identity
    compatibility_sensitive: true
  - id: AUD-006
    severity: medium
    priority: P1
    status: closed
    area: command-validation
    compatibility_sensitive: false
  - id: AUD-007
    severity: medium
    priority: P1
    status: closed
    verified_commit: 1474e1b2
    area: save-validation
    compatibility_sensitive: true
  - id: AUD-008
    severity: medium
    priority: P2
    status: open
    area: event-delivery
    compatibility_sensitive: true
  - id: AUD-009
    severity: low
    priority: P2
    status: open
    area: quality-gates
    compatibility_sensitive: false
```

**Status authority (current):** Until TEL-119 is implemented, this machine-readable index is the manually maintained authority for current AUD-packet status, priority, severity, and compatibility sensitivity. Completed exec plans provide packet evidence and commit provenance; `docs/BUILD_STATUS.md` is append-only verification history; `docs/gates/AUDIT-P0.md` is a historical snapshot. TEL-119 specifies the future transition to a ticket/exec-plan ledger with generated views, but its planned authority is not yet active.

---

# AUD-001 — Canonicalize deterministic RNG seed encoding

**Severity:** High  
**Priority:** P0  
**Primary risk:** Cross-environment deterministic replay failure and ambiguous stream namespaces.

## Audit evidence

Inspect:

- `src/Telengard.Core/Rng/DeterministicRng.cs`
- `tests/Telengard.Architecture.Tests/DeterministicRngTests.cs`
- every `CreateStream(...)` caller
- deterministic fixed-vector tests in generation, encounter, feature, item, and progression systems

At the audit baseline, stream seed material is assembled as text and hashed. The world seed is appended through ordinary numeric formatting, which is culture-sensitive, and stream/scope fields are separated by a NUL delimiter even though input strings are not guaranteed to exclude embedded NUL characters.

Two distinct logical input tuples can therefore map to the same serialized byte sequence, and numeric text can differ across process cultures.

## Required outcome

Define one canonical, culture-independent, unambiguous byte encoding for RNG stream derivation.

The encoding must:

- encode numeric values without current-culture formatting;
- distinguish field boundaries even when strings contain arbitrary UTF-8 text;
- preserve input order;
- define byte order explicitly for numeric lengths/values;
- be stable across supported .NET runtimes and operating-system cultures;
- reject invalid null/empty inputs at the same or stricter boundary as today.

Preferred shape: length-prefixed UTF-8 fields plus fixed-width numeric fields using an explicitly selected byte order. Do not rely on a delimiter that can occur in payload text.

## Compatibility rule — MUST resolve before implementation

Changing the seed encoding will change existing fixed RNG vectors. Inventory all consumers before choosing version changes.

At minimum:

- if floor-generation output changes, advance the generator compatibility version according to repository conventions;
- if replay-significant non-generation outcomes change under the same simulation inputs, assess whether the simulation compatibility version must also advance;
- update any fixed-vector assertions intentionally, never by blindly accepting new values;
- document why old and new vectors differ.

Do not preserve a flawed encoding solely to keep current vectors unless the defect is instead closed by an equally unambiguous backward-compatible encoding strategy.

## Required tests

Add tests that prove:

1. the same logical stream produces the same sequence under at least two materially different `CultureInfo` values;
2. changing culture does not alter the stream hash/vector;
3. `name="a\0b", scopes=[]` does not collide with `name="a", scopes=["b"]` or equivalent delimiter-boundary cases;
4. empty scope lists and multiple scopes remain distinguishable where semantically distinct;
5. fixed deterministic vectors are explicit for the post-remediation compatibility version;
6. affected generation/replay tests pass with the intended version semantics.

## Completion criteria

- Canonical encoding is explicit in code and tests.
- Cross-culture test passes.
- Delimiter-collision test passes.
- Version impact is documented and correct.
- Full repository verification passes.

## Do not

- Do not use JSON serialization as the seed encoding unless the exact canonical JSON byte contract is explicitly fixed and tested.
- Do not use `ToString()` on deterministic numeric inputs without `InvariantCulture`.
- Do not silently update fixed expected vectors without documenting the compatibility reason.

---

# AUD-002 — Defensively own authoritative-state collections

**Severity:** High  
**Priority:** P0  
**Primary risk:** Authoritative state can be mutated outside the command/event boundary through retained mutable collection references.

## Audit evidence

Inspect at minimum:

- `src/Telengard.Core/Simulation/GameState.cs`
- state records referenced by `GameState`
- collection-bearing types in Core
- existing safe examples such as `src/Telengard.Core/combat/MonsterInstance.cs`

At the audit baseline, multiple `IReadOnlyList<T>` state properties accept caller-owned collections without making a defensive copy. `IReadOnlyList<T>` does not make the underlying collection immutable. A caller can retain a `List<T>`, assign it into state, then mutate the list later without executing a simulation command or emitting an event.

## Required outcome

No externally retained mutable collection reference may be able to mutate authoritative simulation state after construction/assignment.

For authoritative collections:

- copy incoming enumerables/lists on assignment or construction;
- expose immutable/read-only snapshots;
- reject null elements where null is not a domain value;
- preserve ordering where ordering is part of equality, replay, save, or presentation behavior;
- preserve intentional value semantics.

Use a consistent pattern across state types. Prefer immutable collections or copied arrays/read-only wrappers over ad-hoc per-property behavior.

## Scope discovery

Search Core for:

```text
IReadOnlyList<
IReadOnlyCollection<
IEnumerable<
init;
Array.Empty
[]
```

Classify each collection as:

- authoritative state requiring defensive ownership;
- immutable configuration already safely copied;
- transient local data not stored in state.

Do not modify transient/configuration types merely for stylistic consistency.

## Required tests

For each distinct storage pattern, add a representative aliasing test:

```text
1. create mutable source List<T>
2. assign it into authoritative state
3. mutate source list
4. assert authoritative state did not change
```

Also test any setter/constructor null-element and duplicate rules that are part of the domain contract.

## Completion criteria

- No known authoritative collection aliases caller-owned mutable storage.
- Aliasing tests fail on the old behavior and pass on the fix.
- Existing save round-trip and equality tests remain correct.
- No unrelated public API redesign.
- Full repository verification passes.

---

# AUD-003 — Move the .NET 8 SDK pin to a current supported security patch

**Severity:** High (toolchain/security)  
**Priority:** P1  
**Primary risk:** Fresh clones intentionally install an obsolete .NET 8 SDK patch.

## Audit evidence

Inspect:

- `global.json`
- `docs/DEVELOPMENT.md`
- `eng/dotnet.ps1`
- `eng/doctor.ps1`
- any workflow introduced by `AUD-004`

The audit baseline pins SDK `8.0.100` and documents installation of exactly that SDK.

## Required outcome

Use a currently supported .NET 8 SDK security patch and keep bootstrap documentation synchronized with `global.json`.

**Important:** The audit-time current patch is not a permanent instruction. At implementation time, verify the current supported .NET 8 patch from official Microsoft/.NET sources. Do not use a third-party version table as the authority.

## Implementation requirements

- update `global.json` intentionally;
- update documented local provisioning commands to the same version or derive them from `global.json` where practical;
- ensure `eng/doctor.ps1` detects unacceptable version drift strongly enough that an obsolete SDK is not silently treated as a normal environment;
- preserve the repository-local SDK workflow unless a separate architecture decision changes it;
- run the full build/test gate on the new SDK;
- record any analyzer/compiler behavior changes caused by the SDK update.

## Completion criteria

- Fresh-clone instructions install a supported .NET 8 patch.
- `global.json`, docs, doctor output, and CI agree on the intended SDK policy.
- Full verification passes on the updated SDK.

---

# AUD-004 — Enforce the verification gate in GitHub Actions

**Severity:** High (process/repository controls)  
**Priority:** P1  
**Primary risk:** Repository quality controls are self-attested rather than server-enforced.

## Audit evidence

Inspect:

- `.github/`
- `.github/pull_request_template.md`
- `eng/verify.ps1`
- `eng/coverage.ps1`
- `CONTRIBUTING.md`

At the audit baseline, there is no `.github/workflows` verification workflow, while the PR template asks contributors to manually check `./eng/verify.ps1 -Mode Full`.

## Required outcome

Add a GitHub Actions workflow that runs the canonical repository verification on pull requests and protected-branch pushes.

## Implementation requirements

- Prefer a Windows runner initially because the repository's canonical scripts and local SDK bootstrap are PowerShell-oriented.
- Provision the SDK in a way consistent with the repository-local SDK contract, or explicitly document why CI uses an equivalent supported path.
- Run `./eng/verify.ps1 -Mode Full` rather than reimplementing its steps in YAML unless a specific step cannot run in CI.
- Use least-privilege workflow permissions.
- Add concurrency cancellation for superseded PR runs where appropriate.
- Do not add secrets for ordinary build/test verification.
- Keep mutation testing out of the ordinary PR gate unless runtime cost is known and intentionally accepted.

Repository settings may need a maintainer action after the workflow merges: mark the workflow/check as a required status check for `main`. The workflow file alone cannot guarantee branch-protection configuration.

## Required tests/evidence

- workflow syntax is valid;
- a PR run executes the same full verification gate expected locally;
- a deliberately failing test/build would fail the check;
- successful gate reports a successful commit/PR check.

## Completion criteria

- `.github/workflows/...` exists and runs on PRs.
- Canonical full verification is server-executed.
- CONTRIBUTING/PR documentation no longer implies manual execution is the only enforcement.
- Required-status-check configuration is documented if it cannot be committed to the repository.

---

# AUD-005 — Guarantee unique deterministic expedition identities

**Severity:** Medium  
**Priority:** P1  
**Primary risk:** Two sequential expeditions can receive the same `ExpeditionId` when seed, player ID, and simulation tick are unchanged.

## Audit evidence

Inspect:

- `src/Telengard.Core/world/generation/DungeonWalking.cs`
- `src/Telengard.Core/Simulation/GameState.cs`
- `src/Telengard.Save/Dto/GameStateSaveDto.cs`
- `src/Telengard.Save/Migrations/SaveMigrations.cs`
- `tests/Telengard.Architecture.Tests/ExpeditionStateTests.cs`
- `tests/Telengard.Architecture.Tests/Phase2AcceptanceTests.cs`
- dead-hero persistence added by TEL-083

At the audit baseline, expedition identity is derived from `WorldSeed`, `SimulationTick`, and `Player.Id`. The acceptance flow can complete an expedition and start another without advancing `SimulationTick`, recreating the same identifier.

## Required outcome

Sequential expeditions for the same character must receive distinct IDs while deterministic replay of the same authoritative history must reproduce the same sequence of IDs.

## Preferred design

Introduce an authoritative deterministic expedition sequence/nonce whose value is part of persisted/replay-relevant state, then derive the ID from stable state including that sequence.

Alternative designs are acceptable only if they prove both properties:

1. uniqueness for distinct sequential expeditions;
2. exact reproducibility after save/load and deterministic replay.

Do not use `Guid.NewGuid()`, wall-clock time, process randomness, or machine-specific input.

## Save/version impact

If a persisted sequence/nonce is added, advance the save schema and add a forward migration. Choose a migration default that cannot silently cause ID reuse on an already-completed historical expedition. Inspect what expedition identity state older saves retain before deciding the default.

Coordinate this packet with `AUD-001` if expedition-ID hashing is also moved to a canonical encoder.

## Required tests

- two sequential expeditions without a tick advance receive different IDs;
- replaying the same two-expedition history produces the same ordered pair of IDs;
- save/load between expeditions preserves the next identity deterministically;
- migration from the prior save version has a documented deterministic result;
- dead-hero records continue to reference the correct expedition.

## Completion criteria

- No sequential ID reuse under supported flows.
- Replay/save determinism preserved.
- Save/version impact handled explicitly.
- Full verification passes.

---

# AUD-006 — Enforce active-dungeon lifecycle guards for movement and floor transitions

**Severity:** Medium  
**Priority:** P1  
**Primary risk:** Dungeon-only commands can mutate state while the player is logically at the inn or not on an active expedition.

## Audit evidence

Inspect:

- `src/Telengard.Core/world/generation/DungeonWalking.cs`
- `src/Telengard.Core/world/generation/FloorTransition.cs`
- `src/Telengard.Core/meta/GameSuspension.cs`
- `src/Telengard.Core/combat/Attack.cs`
- movement/floor-transition tests

At the audit baseline, `Move` and `FloorTransitionResolver.Apply` validate position/alive/combat state but do not require `Expedition.Active == true` and do not reject `Inn.IsAtInn == true`. Other dungeon-only resolvers already enforce active expedition state.

## Required outcome

Dungeon movement and floor transitions must reject logically impossible lifecycle states before mutation or event emission.

## Implementation guidance

Prefer a small shared validation helper only if it genuinely reduces duplicated lifecycle rules without creating a generic validation framework. Otherwise use explicit guards in each resolver.

Required conditions for ordinary dungeon movement/floor changes should include at least:

- active expedition;
- player is not at the inn/safety boundary;
- player is alive;
- no incompatible active combat state;
- supplied layout matches authoritative position/floor.

Do not invent new balance rules or time costs in this packet.

## Required tests

- movement while `Expedition.Active == false` is rejected with state unchanged;
- movement while `Inn.IsAtInn == true` is rejected;
- floor transition under those states is rejected;
- valid active-expedition movement still works;
- no map discovery/event is produced by rejected commands.

Update any existing test that currently constructs an inactive fresh state and expects movement to succeed. That test is protecting the defect and must be rewritten around a valid entered-dungeon state.

## Completion criteria

- Invalid lifecycle states cannot move/reveal map/change floors.
- Rejections happen before mutation/events.
- Valid expedition behavior is unchanged.
- Full verification passes.

---

# AUD-007 — Harden save loading as a trust boundary

**Severity:** Medium  
**Priority:** P1  
**Primary risk:** Syntactically valid JSON can materialize invalid authoritative state or escape the intended save-format exception contract.

## Audit evidence

Inspect:

- `src/Telengard.Save/SaveGameSerializer.cs`
- `src/Telengard.Save/Migrations/SaveMigrations.cs`
- `src/Telengard.Save/Dto/GameStateSaveDto.cs`
- `src/Telengard.Core/Simulation/GameState.cs`
- `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`

The current save layer has substantial validation, but the audit identified gaps such as null elements inside position collections and scalar/cross-field states that gameplay resolvers separately reject.

## Required outcome

After deserialization and migration, no invalid save may be admitted as authoritative `GameState` merely because its JSON shape is valid.

## Validation classes to review

At minimum classify and enforce:

### Structural

- required state boundaries present;
- required collections present after migration;
- no null collection elements where null is not a domain value;
- enum values defined;
- required IDs/strings satisfy constructor contracts.

### Scalar domain ranges

Examples to review, based on actual domain semantics:

- nonnegative gold/counters;
- valid levels/hit points/maxima;
- valid dungeon floors/coordinates where constrained;
- nonnegative simulation/expedition ticks where required.

### Cross-field invariants

Examples to verify against current design:

- player and expedition carried gold agree during active expeditions;
- an active expedition is not simultaneously at the inn;
- combat presence is compatible with expedition/player lifecycle state;
- visited positions are also observed where required;
- floor history/deepest floor are internally coherent;
- dead/alive and hit-point combinations are valid for the current game-mode rules.

Do not invent constraints merely because they look aesthetically cleaner. Every validation rule must be grounded in an existing invariant, constructor rule, resolver guard, or specification requirement.

## Architecture guidance

Prefer one reusable authoritative invariant validator in Core if it can be introduced without circular dependencies or broad redesign. The Save project may call Core validation after DTO materialization and translate failures into `SaveFormatException`.

If a central validator is too broad for this packet, keep DTO validation explicit but add tests proving parity with critical runtime invariants.

Validation should occur **after migration** so old supported schemas can be normalized before current-state rules are enforced.

## Exception contract

Malformed or invalid save content must surface as `SaveFormatException` at the serializer boundary, not as incidental `NullReferenceException`, index exceptions, or other implementation leakage.

## Required tests

Add malformed-save cases for:

- `null` element in observed/visited-position arrays;
- negative or otherwise forbidden carried/secured gold;
- active expedition + at-inn contradiction;
- player/expedition carried-gold mismatch;
- at least one invalid cross-field combat/lifecycle state;
- any newly centralized scalar constraints.

Also retain migration and round-trip tests for all supported versions.

## Completion criteria

- Invalid JSON state shapes are rejected deterministically.
- Invalid cross-field authoritative states are rejected.
- Rejections emerge as `SaveFormatException` at the public serializer boundary.
- Supported old saves still migrate successfully.
- Full verification passes.

---

# AUD-008 — Define event-observer failure semantics after command commit

**Severity:** Medium  
**Priority:** P2  
**Primary risk:** A command can commit authoritative state and then throw because an observer fails, leaving the caller unable to distinguish “command failed” from “command committed; notification failed.”

## Audit evidence

Inspect:

- `src/Telengard.Core/Simulation/CommandDispatcher.cs`
- `src/Telengard.Core/Events/DomainEventBus.cs`
- `tests/Telengard.Architecture.Tests/CommandDispatcherTests.cs`
- presentation/test-harness consumers of the dispatcher/event bus

At the audit baseline, `CommandDispatcher` commits `CurrentState` before publishing events, and event handlers are invoked directly. A throwing subscriber can therefore make `Dispatch` throw after the state transition has already committed and can prevent later subscribers from seeing the event.

## Required outcome

A presentation/telemetry/observer failure must not create ambiguity about whether the authoritative simulation transition committed.

## Preferred semantics

Prefer isolation of observer failures from the command-return path:

```text
handler resolves command
-> authoritative state commits
-> command result is authoritative
-> event observers are notified
-> observer failures are reported through a separate diagnostics/error channel
```

Do not silently discard observer exceptions with no diagnostic path.

If repository architecture requires throwing on observer failure, introduce an explicit committed-state failure contract that makes retry safety unambiguous and still attempts appropriate observer delivery. Such a design is more complex and should be justified in documentation.

## Required behavior to decide and test

- Does one failing subscriber prevent later subscribers from receiving the event? Preferred answer: no.
- How are one or more subscriber failures surfaced diagnostically?
- Can a caller safely know that the command committed?
- Can a caller accidentally retry a committed gameplay action because observer delivery failed? Required answer: no under the documented API contract.

## Required tests

Add a test with:

1. subscriber A records receipt;
2. subscriber B throws;
3. subscriber C records receipt;
4. command mutates authoritative state.

Assert the intended post-remediation semantics explicitly, including committed state and observer-failure reporting.

## Completion criteria

- Commit status is never ambiguous to callers.
- Observer failure behavior is documented.
- Later observers are handled according to the documented policy.
- No silent diagnostic loss.
- Full verification passes.

---

# AUD-009 — Reconcile the ordinary verification gate with the 100% coverage gate

**Severity:** Low  
**Priority:** P2  
**Primary risk:** Repository documentation can imply a stronger gate than the command actually enforces.

## Audit evidence

Inspect:

- `eng/verify.ps1`
- `eng/coverage.ps1`
- `docs/test-quality-current-audit.md`
- `docs/mutation-hardening-report.md`
- `docs/DEVELOPMENT.md`
- `CONTRIBUTING.md`
- PR template and CI workflow after `AUD-004`

At the audit baseline, `eng/coverage.ps1` requires 100% line and branch coverage for its in-scope boundary, while `eng/verify.ps1 -Mode Full` does not invoke the coverage gate.

## Required outcome

Make gate names and documentation accurately describe what is enforced.

Choose one explicit model:

### Model A — Full verification includes coverage

Use this only if runtime and reliability are acceptable for every PR.

### Model B — Separate ordinary and exhaustive gates

Example semantic distinction:

```text
verify/full     = restore + format + Release build + Release tests
quality/coverage = verify/full + strict coverage
mutation         = slower explicit mutation-hardening workflow
```

Names may differ, but documentation must make the hierarchy unambiguous.

## Completion criteria

- No document claims `verify -Mode Full` enforces 100% coverage unless it actually does.
- CI uses the intended gate(s).
- Coverage remains explicit rather than silently weakened.
- Mutation prerequisites reference the correct coverage command.

---

# 5. Cross-packet regression matrix

Every remediation agent should run the focused tests for its packet and consider this matrix before declaring completion.

| Area | Minimum regression concern |
| --- | --- |
| Determinism | fixed vectors, replay harness, save/reload continuation |
| Dungeon generation | same seed + generator version produces intended geography |
| Expedition loop | enter, move, descend/ascend, retreat, complete, start another expedition |
| Death modes | Classic, Legacy, Adventure semantics unchanged except intentional identity/version work |
| Persistence | current round trip plus all supported migrations |
| Wealth | carried vs secured gold remains consistent |
| Knowledge/map | no discovery is produced by rejected commands; persistent map remains coherent |
| Combat | lifecycle and active-expedition guards remain consistent |
| Events | state is committed before committed-fact events; no hidden information leakage |
| Toolchain | zero-warning Release build and repository script behavior |

# 6. Versioning decision checklist

Before merging any packet that changes deterministic or persisted behavior, answer all of these in the PR/exec-plan documentation:

```yaml
version_impact:
  save_version_changed: yes|no
  simulation_version_changed: yes|no
  generator_version_changed: yes|no
  content_version_changed: yes|no
  deterministic_vectors_changed: yes|no
  old_save_migration_added: yes|no|not-applicable
  replay_compatibility_preserved: yes|no|version-bounded
```

A `no` answer must be defensible from tests and architecture, not assumed because a source edit looks small.

# 7. Completion evidence template for downstream agents

Use this structure in the packet's PR description, exec plan, or completion note:

```markdown
## Audit remediation

- Packet: AUD-00X
- Baseline inspected: <commit>
- Defect reproduced by: <test or exact reasoning>
- Production change: <short description>
- Compatibility impact: <save/simulation/generator/content/replay>
- Focused tests: <commands + result>
- Full gate: `./eng/verify.ps1 -Mode Full` -> <result>
- Coverage gate: <command/result/not-required with reason>
- Documentation updated: <files>
- Remaining related risk: <none or explicit follow-up>
```

# 8. Definition of audit remediation complete

The audit is not considered remediated merely because all current tests pass. It is complete when:

1. all `AUD-*` packets are implemented or explicitly closed with a documented technical rationale;
2. P0/P1 findings have regression tests that demonstrate the original failure mode;
3. deterministic compatibility changes are versioned and documented;
4. save trust-boundary invariants are enforced after migration;
5. server-side CI enforces the intended repository verification gate;
6. the SDK bootstrap uses a supported security patch;
7. state collections cannot be externally mutated through aliasing;
8. sequential expedition identity is deterministic and unique;
9. invalid lifecycle commands cannot mutate dungeon state;
10. observer failures cannot make command commit status ambiguous;
11. quality-gate documentation exactly matches executable behavior.

After all packets are closed, change this document's front-matter `status` from `active` to `completed`, update each machine-readable packet status, and record the final verification commit/PR references.
