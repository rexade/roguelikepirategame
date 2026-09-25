# T08: Integrate durable saves and the expedition loop

Status: waiting
Assignee: unassigned
Role: Persistence engineer with lead integration
Depends on: T05, T06, T07

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Accepted combat, world/salvage, harbor UI, and their state fixtures; T03 commit/arrival ports.

Owned paths: Assets/_Game/Persistence/; Assets/_Game/Tests/T08/; Assets/_Game/Scenes/Tests/T08/; docs/evidence/T08/; Bootstrap and production scene edits by lead only

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Implement versioned DTO snapshots, selected serializer, ordered writes, validated replacement/backup, and recoverable load errors.
2. Capture coherent campaign/expedition state and restore without cooldown, health, or loot resets.
3. Implement real commit and single-region arrival coordination; lead assembles bootstrap and production scene.
4. Complete departure -> combat/salvage -> docking/sinking -> upgrade -> departure, including pause/quit checkpoints.

## Acceptance

Rules: INV-01 through INV-08, INV-11, INV-16, INV-17, INV-18.
Cases from [the backlog](../TASKS.md): AC-01, AC-02, AC-05, AC-06, AC-09, AC-10, AC-11, AC-12, AC-17; integrated AC-03/04.

- [ ] Full loop works and upgrades affect the next sailing expedition.
- [ ] Inject failures before replacement and crashes after replacement; old/new valid snapshots recover without duplicate banking.
- [ ] Older checkpoint cannot overwrite a newer transition; failed resolution stays frozen/retryable.
- [ ] Corrupt/unsupported saves remain preserved; resume and death do not strand the player.
- [ ] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Streaming, fast-travel UI, cloud saves, anti-cheat, or guarantees beyond checkpoint recovery.

Deliver: Playable loop, save schema/fixtures, filesystem fault evidence, real arrival adapter and build steps. Unlocks T09 and T10.

Record results in docs/evidence/T08/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.

