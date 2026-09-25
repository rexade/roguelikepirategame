# T06: Author the first region and salvage loop

Status: done (technical acceptance 2026-09-25; owner visual disposition pending)
Assignee: Codex (world engineer)
Role: World engineer
Depends on: T04

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Navigable ship, interaction convention, fixed-world/entity-ID and loot contracts.

Readiness 2026-09-18: T04-r2 accepted, R1 closed. Record source/build identities
from [Lead review](../evidence/T04/REPORT.md#r1-lead-review---2026-09-18) at pickup.
Required [step protocol](../evidence/T04/R1/PROTOCOL.md): use ShipSimulation.CollectLoot
for mid-step pickup requests; pending acknowledges queueing, not a committed pickup.
Use the adapter pause entry point and respect retained steps and save retries.
The shared ship prefab is Lead-owned; submit scoped integration changes.
Assigned by the owner on 2026-09-18. Input: T03-r1 and accepted T04-r2;
T04-r2 source manifest `1EBA894A94BE245D9015710F23E8A198FD5427E6E5D51AB9F34B5426871DF558`,
build manifest `C8C530540E6A67E2EA601C177F02F24B162656EA3B8B67BF016704E3620C3CC9`.

Owned paths: Assets/_Game/Gameplay/World/Salvage/; Assets/_Game/Content/World/FirstRegion/; Assets/_Game/Prefabs/World/; Assets/_Game/Scenes/Regions/FirstRegion.unity; Assets/_Game/Scenes/Tests/T06/; Assets/_Game/Tests/T06/; docs/evidence/T06/

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Author a home harbor, three small islands, barrels, wrecks, encounter markers, and a short outward/return route.
2. Assign stable IDs and validate duplicates; implement whole-bundle cargo transfer with atomic source depletion.
3. Implement entity capture/recreation from expedition state in a test scene without a streaming system.
4. Supply hub/docking-zone data for integration; submit shared definition changes to the lead.

## Acceptance

Rules: INV-05, INV-10, INV-11, INV-12.
Cases from [the backlog](../TASKS.md): AC-04, AC-05 at entity-recreation level, AC-15; durable/streamed checks follow in T08/T10.

- [x] Full cargo leaves source intact; repeated pickups cannot duplicate resources.
- [x] Recreating a depleted source does not refill it; IDs survive asset rename and recreation.
- [x] Route is navigable and collision-ready; island positions remain fixed.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Large archipelago, procedural island generator, region streaming, banking UI, or filesystem saves.

Deliver: Region scene, world prefabs, ID validation, entity-state fixtures, route reproduction steps. Input to T08.

Record results in docs/evidence/T06/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.

Developer submission: [T06-r1 report](../evidence/T06/REPORT.md).
83 automated checks passed, standalone build passed, and the motor-driven route
completed 9/9 waypoints. The checked items record developer verification;
Lead disposition is recorded below; owner visual disposition remains pending.

Lead review 2026-09-19: changes requested. Independent reruns passed all 83
checks, but F01 identifies a null pickup result read in the startup HUD before
any interaction. Correct the idle state and supply startup/no-input regression
and visible HUD evidence. See [Lead review](../evidence/T06/REPORT.md#lead-review---2026-09-19).
The developer checks above do not constitute acceptance. Owner visual disposition
remains pending; T08 is not released.

Remediation packet: [T06-R1](T06-R1-startup-hud.md), submitted by Codex for review.
It bounds F01 correction, startup/status regressions and actual-window HUD evidence;
it does not require parent acceptance to start or authorize successor work.
