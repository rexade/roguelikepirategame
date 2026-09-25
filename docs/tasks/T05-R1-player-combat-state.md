# T05-R1: Synchronize player combat read state

Status: done
Assignee: Codex (combat engineer)
Role: Combat engineer, with independent Lead acceptance
Parent: [T05](T05-combat.md), accepted as T05-r2
Priority: P2 acceptance blocker
Inputs: accepted T03-r1/T04-r2; reviewed, unaccepted T05-r1 and its Lead finding

Pickup 2026-09-18: verified T05-r1 source (53 entries) and build (291 entries)
against the manifests below, zero mismatches. Unity 6000.3.24f1 is available.
Input acceptance is recorded in the T03/T04 reports; no project Git revision.

Delivered 2026-09-18: T05-r2 technical handoff in
[REPORT.md](../evidence/T05/REPORT.md#r1-developer-handoff---2026-09-18).
Unity T05 20/20, T04 40/40, T03 33/33; revised build and both loadout smoke
checks passed. Revised moving clips and actual-window HUD evidence are in R1/.
Independent Lead review and owner visual disposition remain pending.

Lead review 2026-09-18: T05-r2 technical correction accepted; original P2 closed.
R1-C01 through R1-C08 passed. Independent reruns: T05 20/20, T04 40/40, T03 33/33.
R1-C09 owner combat/HUD disposition is still pending. Keep this task and parent
T05 in review; no further code change requested. See REPORT.md's R1 Lead Review.
Earlier pending technical-review statements describe the developer handoff state.

Owner disposition 2026-09-18: "approved" after clarification that only combat/HUD
visual approval remained. R1-C09 is satisfied; all R1 checks are accepted. T05-R1
and parent T05 are done. See REPORT.md's Owner Disposition; earlier pending
statements above are historical and no additional verification is implied.

This is remediation within T05, not a new milestone or a dependency on parent
acceptance. Preparing the packet does not assign or start implementation. Follow
the [dispatch rules](README.md), [context](../../CONTEXT.md),
[architecture](../ARCHITECTURE.md), [invariants](../INVARIANTS.md),
[contracts](../CONTRACTS.md) and [T04 protocol](../evidence/T04/R1/PROTOCOL.md).

## Problem and Reproduction

`CombatTarget.ApplyDamage` forwards player damage to the session's collector;
it correctly does not independently subtract authoritative health. However,
`CombatWorld.Tick` refreshes the target only before processing that tick's damage.
After publication followed by pause, save lock or sinking, the next tick may
never run. Public player Health/Defeated then remain stale indefinitely. The
delegated player path also never raises Died, despite the generic target API.
The fixture HUD bypasses the defect by reading the session directly.

The [Lead review](../evidence/T05/REPORT.md#lead-review---2026-09-18) records:

| Sequence from 100 health | Authoritative result | Incorrect exposed target |
| --- | --- | --- |
| Collect 10 damage, publish, pause | AtSea, health 90 | Health 100, not defeated |
| Collect 100 damage, publish Sunk | Docked, voyage resolved | Health 100, not defeated |

Run the original supplemental probe with:

```powershell
dotnet run --project docs/evidence/T05/lead-probe/LeadProbe.csproj
```

Exit 0 currently means it reproduced the defect, not a passing fix test. It links
production target/session code with stubbed Unity component calls. Real Unity
regressions are required; do not substitute this probe for scheduler verification.
Authoritative damage and sinking work already. Do not add a second damage owner.

T05-r1 source manifest SHA-256:
E3FB7C370D2069C269B199D9B3B0E15AF12AEA4F33232BE02387E560CFE0F8CA.
Build manifest SHA-256:
A84CE5D312040B2C2AA5C0C3F001CCEB65D3D90BD1914C5F9684AFD09C172F58.
See the parent handoff for accepted T03/T04 identities. Verify inputs at pickup;
use manifests, not an invented Git commit. The original 15 T05, 40 T04 and 33 T03
tests passed independent review but miss this player read-state failure.

## Ownership and Scope

Primary owned files: `Assets/_Game/Gameplay/Combat/CombatTarget.cs`,
`CombatWorld.cs`, related T05-local helpers, `Assets/_Game/Tests/T05/`,
`docs/evidence/T05/INTEGRATION.md` and `docs/evidence/T05/`.
Fixture-only view/runner changes under `Assets/_Game/Scenes/Tests/T05/` are allowed
when necessary to demonstrate corrected state and gather HUD evidence.

Core/Application, T03 contracts/tests, T04 ship adapter/motor and the shared ship
prefab remain Lead-owned. Before changing those paths, submit exact proposed API
semantics, affected consumers (T04/T05/T06/T07/T08), and regression coverage for
Lead approval. This packet does not reserve shared files or approve a new API.
Preserve GUIDs; no package/settings changes, combat rebalance, new weapons,
economy, real persistence, broad UI redesign or successor implementation.

## Required Behavior

1. Establish one authoritative source for exposed player health and defeat state.
   Refresh or query it after a processed tick, including pauses and save locks,
   without requiring another simulation tick. Define the completed-boundary timing
   so consumers do not depend on unspecified coroutine/subscriber order.
2. Preserve the distinction between processed damage, pending outcome persistence,
   and committed Sunk. Zero health may be exposed during a failed Sink save, but
   committed outcome success must wait for acknowledgement. Do not reset the
   defeated target to maximum health merely because the active expedition ended.
   Successful docking must not be mistaken for sinking.
3. Specify player notification semantics. Recommended: keep target Died explicitly
   enemy-only and use the session's committed Sink outcome as the player lifecycle
   notification. An alternative uniform target event must define timing and
   once-only behavior without becoming a second authoritative outcome. Document
   the selected API and consumers; no owner product decision is needed here.
4. Do not privately drain the session event queue and starve other consumers.
   Any event fan-out or shared publication API change requires Lead coordination.
   Do not award loot, mutate health, or resolve an expedition from presentation.
5. Restore exposes saved player health/defeat state before input resumes, with
   no new damage/death/success notification. Preserve brace reduction, enemy
   death-once, projectile reuse and T04's pending-step/retry protocol.

## Acceptance Checks

Rules: INV-01/02/03/14/15/16/18 at the existing task boundary; preserve AC-13.

| ID | Required observable result |
| --- | --- |
| R1-C01 | Add a real Unity regression that fails on r1: 10 damage then adapter pause in the same step. After publication, both session and exposed player health are 90, Defeated is false, and remain so while paused without another tick. Cover TickStarted and a physical collision callback. |
| R1-C02 | Lethal damage exposes health 0/Defeated true after processing, including after successful Sunk removes the active expedition. Exactly one committed Sunk outcome is available; successful docking is not classified as death. |
| R1-C03 | Failed Sink save exposes zero health without publishing committed success. Repeated failed retries freeze motion/cooldowns and do not repeat damage or notifications; successful retry publishes one outcome and leaves coherent defeated read state. |
| R1-C04 | Nonlethal damage plus a failed supported pickup/checkpoint save leaves exposed health equal to the processed session state while locked; retry cannot revert it to pre-damage health. |
| R1-C05 | Brace reduces 40 damage to 10 once: exposed and authoritative health both become 90. Multiple same-step hits aggregate correctly; no double subtraction from synchronization. |
| R1-C06 | Fresh-session restore of a damaged saved player exposes saved health before input resumes, emits no new outcome/death, and remains correct across pause/resume. Binding a new expedition composition does not inherit stale defeated state. |
| R1-C07 | Document and test the chosen player event contract: no premature committed notification, no repeats on retry/restore, and no loss of events for other consumers. Existing enemy death-once behavior remains unchanged. |
| R1-C08 | Expanded T05, unchanged T04 and T03 suites pass. Revised standalone builds and has a smoke check for both loadouts and corrected player state. Record new source/build identities and exact commands/results. |
| R1-C09 | Provide accessible moving combat and visible-HUD evidence for owner review, or explicitly identify reused clips and missing checks. Obtain a recorded owner visual disposition or explicit acceptance of disclosed limitations before parent T05 acceptance; developer/Lead tests cannot supply owner approval. |

Read-state assertions must inspect `CombatWorld.Player`, not merely the session
or the fixture HUD. Use real scheduler/physics callbacks for integration checks,
not reflection/manual coroutine advancement. State whether assertions run after
processed-tick publication or after save acknowledgement, and test both stages.

## Evidence and Handoff

Use Unity 6000.3.24f1. Preserve the original Lead finding, probe, logs, manifests
and build. Place new logs/XML, before/after evidence and revision manifests under
`docs/evidence/T05/R1/`; use a separate revised build directory. Existing runners
overwrite historical output names, so use revision-specific paths or extend the
evidence runners before rerunning them. Do not weaken existing assertions.

Record a failing-before/passing-after Unity regression, expanded suite results,
inherited-asset preservation checks and standalone smoke evidence. Reuse unchanged
visual footage only with clear revision/limitation labels; existing MP4 capture
omits OnGUI and cannot prove HUD readability. A normal-window capture or reviewed
manual playthrough can supply HUD evidence; no rendering-quality reduction is
authorized. Do not claim a manual/full-motion review that was not performed.

Append an R1 handoff to `docs/evidence/T05/REPORT.md`, mapping every check to
passed/failed/not-run evidence and linking updated consumer instructions. Technical
work may be delivered for Lead review while owner visual disposition is pending;
record it separately rather than marking it passed. A technical fix alone does
not mark parent T05 done. Lead acceptance of revised T05 plus the required owner
disposition releases its dependency; T08 still also requires T06 and T07.
T06/T07 can proceed independently. Do not start any successor task.
