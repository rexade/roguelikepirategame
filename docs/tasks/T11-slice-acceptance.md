# T11: Verify and accept the integrated playable slice

Status: waiting
Assignee: unassigned
Role: QA engineer with lead integration
Depends on: T09, T10

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Accepted two-hub travel and multi-region loading plus the integrated slice build.

Owned paths: Assets/_Game/Tests/T11/; Assets/_Game/Scenes/Tests/T11/; docs/evidence/T11/; production composition by lead only

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Have the lead integrate travel and streaming; verify travel between hubs in different regions.
2. Review invariant coverage and run relevant regression scenarios; assign discovered fixes to their owning subsystem.
3. Repeat the agreed benchmark with combat/salvage/UI/transitions and capture daylight/dusk/rough-water readability.
4. Add a diagnostic stress fixture with 12 enemy ships, 100 live projectiles, and 100 pickups; record results separately from the representative route.

## Acceptance

Rules: INV-01 through INV-18; GATE-01, GATE-02, GATE-03.
Cases from [the backlog](../TASKS.md): AC-01 through AC-18 plus cross-region travel, keyboard/mouse navigation, and stress capture.

- [ ] Required cases pass with evidence; no progression-blocking defects remain.
- [ ] Target hardware/settings/revision and measured frame-time distribution, memory and stalls are recorded; no fabricated passes.
- [ ] Game owner reviews visual quality; lead records acceptance or remaining blocking defects.
- [ ] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: New features, unowned subsystem rewrites, or treating diagnostic stress counts as final content caps.

Deliver: Self-contained acceptance report, reproduction instructions, evidence, remaining risks and explicit release/gate verdict.

Record results in docs/evidence/T11/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.
