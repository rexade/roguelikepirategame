# T04: Build ship controls and gameplay camera

Status: done
Assignee: Codex (gameplay engineer)
Role: Gameplay engineer
Depends on: T03

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
The owner assigned implementation on 2026-09-18.

Inputs: Hull/clock/input contracts plus accepted prototype rendering and framing.

T03-r1 accepted 2026-09-18; see ../evidence/T03/REPORT.md's Lead Review.
Accepted inputs and shared-asset reservations are recorded below.

Pickup 2026-09-18: accepted T03-r1 manifest
EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F,
accepted T02 assets, Unity 6000.3.24f1. No project Git HEAD exists.
No shared assets reserved. Bindings and new scoped assemblies remain T04-local.

Owned paths: Assets/_Game/Gameplay/Ships/; Assets/_Game/Gameplay/Input/; Assets/_Game/Presentation/Camera/; Assets/_Game/Presentation/Ships/; Assets/_Game/Prefabs/Ships/; Assets/_Game/Scenes/Tests/T04/; Assets/_Game/Tests/T04/; docs/evidence/T04/

Delivered assets now occupy these paths. Shared contracts, settings, input assets,
and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Implement constrained XZ/yaw physics, acceleration, turning, braking, and keyboard/mouse adapters; controller support is deferred beyond the prototype.
2. Create stable gameplay hardpoints and interaction origin; document collision-layer requirements for the lead.
3. Implement camera follow and child-model bobbing without altering gameplay aim/collision.
4. Deliver a reusable ship prefab and isolated collision/input test scene. Request shared input asset changes through the lead while T07 runs.

## Acceptance

Rules: INV-14.
Cases from [the backlog](../TASKS.md): AC-14; movement, collision, pause, and input verification.

- [x] Ship cannot cross island colliders; keyboard/mouse controls work.
- [x] At 30/60/120 render FPS movement remains stable and bobbing/quality settings do not change hitboxes or aim.
- [x] Simulation pause freezes motion and the published clock; T05 verifies actual weapon cooldowns. R1's mid-step lock defect is closed by the T04-r2 Lead review.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Combat, progression UI, new water implementation, or independent project-setting changes.

Deliver: Ship prefab, hardpoint/collision conventions, input map, frame-rate footage and test results. Unlocks T05 and T06.

Results are recorded in docs/evidence/T04/REPORT.md. Deliverables and required
developer checks are complete; the Lead records acceptance.

Developer delivery 2026-09-18: T04-r1 is ready for Lead review. Six PlayMode
tests passed; standalone measured and offline frame-cadence checks and clips are
recorded in [the report](../evidence/T04/REPORT.md), including capture overhead
and unrun owner/production checks. Acceptance checkmarks record developer
verification, not Lead acceptance. T05/T06 remain waiting.

## Lead Review - Changes Requested

2026-09-18: T04-r1 remains in review, not accepted. Existing 6/6 PlayMode tests
independently passed, but the review probe reproduced lost lethal damage when
pause or a failed pickup save locks the session during an in-flight step.

R1 (P1): correct ShipSimulation's tick/lock coordination so rejected publication
cannot discard accumulated damage or let physics advance incoherently. Add Unity
regressions for mid-step pause and failing interaction saves, retry, once-only
damage/outcomes and body/snapshot agreement; preserve damage-before-dock ordering.
Any T03 contract change requires Lead coordination. No combat implementation is
requested. Full reproduction and evidence: ../evidence/T04/REPORT.md, Lead Review.
T05/T06 remain waiting. T07 readiness is unaffected.

Delegation packet: [T04-R1: Tick/lock coordination](T04-R1-tick-lock-coordination.md).
R1 was assigned to Codex on 2026-09-18 and its T04-r2 delivery is now in review.
Developer verification: 40/40 T04 PlayMode, 33/33 T03 EditMode, revised standalone
build and measured 30/60/120 cadence/pause checks pass. The appended R1 handoff
records the new protocol and evidence. Independent Lead review is still required
before parent T04 acceptance; the original review finding is retained above.

Lead acceptance 2026-09-18: T04-r2 and T04-R1 accepted. Independent PlayMode
40/40 and T03 EditMode 33/33 passed; source/build identities and revised cadence
evidence checked. See REPORT.md's R1 Lead Review for limits. This supersedes prior
pending statuses. T05/T06 are ready, unassigned, not started. The reusable ship
prefab transfers to Lead ownership. Consumers must follow R1/PROTOCOL.md.
