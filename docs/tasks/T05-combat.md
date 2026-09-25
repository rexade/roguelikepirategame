# T05: Implement weapons, ability, and enemy ships

Status: done
Assignee: Codex (combat engineer)
Role: Combat engineer
Depends on: T04

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Accepted ship motor/hardpoints and T03 equipment/stat/capture contracts.

Readiness 2026-09-18: T04-r2 accepted, R1 closed. Record source/build identities
from [Lead review](../evidence/T04/REPORT.md#r1-lead-review---2026-09-18) at pickup.
Required [step protocol](../evidence/T04/R1/PROTOCOL.md): produce damage during
TickStarted/collision collection, use the adapter pause entry point, and never
install a second tick publisher. No physics replay after save failure. The shared
ship prefab is Lead-owned; submit scoped changes rather than editing it independently.
Assigned by the owner on 2026-09-18. Input: T04-r2 source manifest
`1EBA894A94BE245D9015710F23E8A198FD5427E6E5D51AB9F34B5426871DF558`, build
`C8C530540E6A67E2EA601C177F02F24B162656EA3B8B67BF016704E3620C3CC9`
(`Builds/T04-r2/ShipControls.exe`); accepted T03-r1 contracts and T02 ocean.
No project Git HEAD; output will be identified by a source manifest.

Owned paths: Assets/_Game/Gameplay/Combat/; Assets/_Game/Gameplay/AI/; Assets/_Game/Content/Combat/; Assets/_Game/Prefabs/Combat/; Assets/_Game/Scenes/Tests/T05/; Assets/_Game/Tests/T05/; docs/evidence/T05/

These paths now contain the T05-r1 submission. Accepted input revisions are recorded
above. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Implement two alternative directed weapons, one active ability, and two enemy archetypes using existing motor and authored definitions.
2. Implement swept projectile collision and bounded hit/death resolution; reset all state on pool reuse if pooling is used.
3. Expose health, cooldowns, and enemy state through the accepted capture/restore contract.
4. Verify combat readability in a dedicated scene with the accepted ocean.

## Acceptance

Rules: INV-12, INV-13, INV-15.
Cases from [the backlog](../TASKS.md): AC-13 plus swept hits, cooldown pause, death-once, and loadout behavior.

- [x] Duplicate hit callbacks cannot multiply damage and death emits once.
- [x] Fast shots hit crossed targets; reused projectiles have correct new ownership and no stale history.
- [x] Loadouts change behavior as defined; paused simulation freezes cooldowns; capture/restore retains combat state. Public player read-state correction passed independent T05-r2 review.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Boarding, crews, character combat, new ability language, economy, or editing the shared ship prefab directly.

Deliver: Combat prefabs/definitions, capture fixtures, test results and footage. Input to T08; coordinate ship changes with T04's owner.

Results are recorded in docs/evidence/T05/REPORT.md. Deliverables and required
developer checks are complete; the Lead records independent acceptance.

Developer submission 2026-09-18: T05-r1 is ready for Lead review. See
[handoff](../evidence/T05/REPORT.md): 15 T05 tests, 40 T04 regressions and 33 T03
rules passed; standalone build and both loadout captures completed. Checkboxes
record developer verification, not Lead acceptance. Owner visual decision pending.

Lead review 2026-09-18: changes requested. Independent T05 15/15, T04 40/40,
T03 33/33 passed; source/build preservation checks passed. R1 (P2): player
CombatTarget health/defeat state remains stale after damage followed by pause or
sinking because refresh occurs only on the next tick. Correct the public read
state and define player outcome notification semantics; add Unity regressions
for pause, lethal damage, failed Sink/retry and restore. See REPORT.md's Lead
Review for reproduction and correction requirements. Owner combat/HUD visual
disposition remains pending. T05 stays in review; T08 waiting; T06/T07 unaffected.

Delegation packet: [T05-R1: Player combat read state](T05-R1-player-combat-state.md).
Implemented as T05-r2. Independent review 2026-09-18 passed R1-C01 through C08
and closed the P2 defect; 20 T05, 40 T04 and 33 T03 tests passed. Owner combat/HUD
visual disposition (R1-C09) remains pending, so T05/R1 stay in review. No further
code changes requested. See REPORT.md's R1 Lead Review; T08 remains waiting.

Owner approval 2026-09-18: "approved" closes the remaining combat/HUD visual
disposition. T05-r2 and T05-R1 are accepted and done; prior pending statements
are historical. See REPORT.md's Owner Disposition. T08 still awaits accepted
T06 and T07; no successor implementation is started by this approval.
