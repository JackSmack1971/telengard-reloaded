# Codex model and reasoning policy

This is the repository policy for assigning model effort in the autonomous
workflow. Runtime model aliases and local agent profiles are deployment
configuration; this document does not require a particular provider name when
that alias is unavailable.

`high` is an escalation state, not a reviewer identity. Use the least effort
that can independently falsify the target, and raise effort only when the
evidence shows ambiguity, architectural risk, or a high-impact compatibility
boundary.

| Role | Model profile | Reasoning |
| --- | --- | --- |
| Parent selection with clear state | Terra-equivalent | medium |
| Parent architecture/product diagnosis | Sol-equivalent | medium/high |
| Well-specified implementation worker | Luna-equivalent | medium |
| Repository explorer | Terra-equivalent | low/medium |
| Correctness reviewer | Luna-equivalent | medium |
| Test reviewer | Luna-equivalent | low |
| Documentation/provenance reviewer | Luna-equivalent | low |
| Presentation reviewer | Luna-equivalent | medium |
| Architecture/determinism reviewer | Terra-equivalent | medium |
| Save-migration reviewer | Terra-equivalent | medium/high |
| Security/trust-boundary reviewer | Terra- or Sol-equivalent | medium/high |
| Final synthesis | parent | no extra agent |

## Application rules

- Do not globally default all subagents to the strongest model or `high` effort.
- The parent resolves ambiguity and establishes the plan before delegating
  well-specified work.
- Review lanes are selected by ticket risk and changed paths; their effort is
  set by the lane above and escalated only for a distinct high-impact target.
- A missing runtime profile is not permission to invent configuration. Use the
  closest available profile and record the deviation in the run handoff.
- Model choice never relaxes repository invariants, required verification, or
  independent review.
