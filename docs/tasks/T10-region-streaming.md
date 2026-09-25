# T10: Implement region continuity and voyage variation

Status: waiting
Assignee: unassigned
Role: World engineer
Depends on: T08

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Real snapshots, accepted identity/capture/restore hooks, and T08 scene coordinator.

Owned paths: Assets/_Game/Gameplay/World/Streaming/; Assets/_Game/Gameplay/World/Encounters/; Assets/_Game/Content/World/NeighborRegion/; Assets/_Game/Scenes/Regions/NeighborRegion.unity; Assets/_Game/Scenes/Tests/T10/; Assets/_Game/Tests/T10/; docs/evidence/T10/

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Add a neighboring region and load-ahead/unload behavior through existing scene-lifecycle contracts.
2. Restore expedition entities from data; keep the player outside unloaded collision and snapshot state before retiring scenes.
3. Select encounters with seeded region tables and persist selected outcomes/RNG while keeping geography fixed.
4. Use dedicated region fixtures; request shared coordinator changes through lead and record streaming stalls.

## Acceptance

Rules: INV-10, INV-11, INV-12, INV-16.
Cases from [the backlog](../TASKS.md): AC-05 with streaming/resume, AC-15; encounter persistence and new-expedition reset.

- [ ] Crossing, returning, and save/reload do not duplicate loot/enemies.
- [ ] New expeditions vary encounters without moving islands; campaign flags survive resets.
- [ ] Duplicate IDs fail validation; region-readiness failure is handled without entering incomplete collision.
- [ ] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Fast-travel UI, large map, procedural islands, or floating origin without measured need.

Deliver: Region scene, loading/encounter components, restore tests and stall captures. Input to T11.

Record results in docs/evidence/T10/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.

