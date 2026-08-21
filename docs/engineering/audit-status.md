# Audit status projections

`docs/audit-status.json` is the canonical machine-readable ledger for audit
packet status and provenance. Each packet has ticket metadata and may have an
exec-plan metadata record. For every field, a non-null ticket value takes
precedence; an exec-plan value fills the field only when the ticket value is
absent. Exec-plan paths must be under `docs/exec-plans/active/` or
`docs/exec-plans/completed/`, and their declared status must match that
location. A closed packet must carry a verified commit; an unresolved packet
must carry an explicit unresolved explanation.

`eng/audit-status.ps1` renders the derived machine-readable sections in
[the audit remediation playbook](../AUDIT_REMEDIATION_PLAYBOOK.md) and
[the P0 gate](../gates/AUDIT-P0.md). The sections are delimited by
`BEGIN/END GENERATED: audit-status` markers. Human-authored rationale,
historical baselines, acceptance evidence, and explanatory notes remain
outside those markers.

Use these commands from the repository root:

```powershell
./eng/audit-status.ps1 -Mode Generate
./eng/audit-status.ps1 -Mode Check
```

`Generate` is the explicit local update command. `Check` is read-only and is
the stale-generation gate used by the repository workflow; it reports the
affected document/section and never overwrites a pull request worktree.
