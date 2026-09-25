# T04-R1: Preserve in-flight ticks across pause and save locks

Status: done
Assignee: Codex (gameplay engineer)
Role: Gameplay engineer, with Lead review of shared-contract changes
Parent: [T04](T04-ship-controls.md), accepted as T04-r2
Priority: P1 acceptance blocker
Inputs: accepted T03-r1; reviewed, unaccepted T04-r1; recorded Lead reproduction

This is remediation of T04, not a new milestone or a dependency on T04 acceptance.
Preparing this packet does not assign or start implementation. Follow the
[dispatch rules](README.md), [invariants](../INVARIANTS.md),
[contracts](../CONTRACTS.md), [architecture](../ARCHITECTURE.md),
[context](../../CONTEXT.md) and [dependency plan](../DEPENDENCIES.md).

## Problem and Evidence

`ShipSimulation.FixedUpdate` collects damage during a physics step.
`PublishTicks` clears `stepped` before calling `CampaignSession.CompleteTick`,
then only logs a rejected result. If a callback pauses the session or triggers
a failed pickup save, publication returns Paused/Busy. The next step resets
damage, losing it even though physics may already have advanced the body.

The [Lead review](../evidence/T04/REPORT.md#lead-review) reproduced both cases:
start at 100 health, collect 100 damage, acquire the lock during TickStarted,
publish, resume/retry, then step again. Actual r1 result: AtSea with health 100.
Expected: damage is resolved exactly once; a lethal step cannot resume alive.

Existing 6/6 PlayMode checks pass but do not cover mid-step locks. The isolated
probe demonstrates control flow, not Unity scheduling or physical correctness:

```powershell
dotnet run --project docs/evidence/T04/lead-probe/LeadProbe.csproj
```

Its current exit 0 means the bug reproduced. Do not count it as a passing fix test.

Input identity: T04-r1 source manifest SHA-256
71FD2CB818275B77886B22AD3ED48D3001F2DD5DCF3C61280FE8DA7FEA6EFCE2;
T03-r1 source manifest SHA-256
EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F.
The T03 contract document has a later Lead annotation; its asset hashes remain
the baseline. Verify actual inputs at pickup; do not invent a Git revision.

Pickup 2026-09-18: Codex verified both manifest hashes above and every listed
T03/T04 asset hash, with zero mismatches. Unity 6000.3.24f1 is available at
`D:/u6-t01/Editor/Unity.exe`. No shared-contract changes are required.

## Ownership and Dependencies

Primary write scope: `Assets/_Game/Gameplay/Ships/ShipSimulation.cs`, related
T04-local coordination helpers, `Assets/_Game/Tests/T04/`, and
`docs/evidence/T04/`. Change `ShipMotor.cs` or the T04 fixture only if needed
for physical suspension, coherent completion, or regression coverage.
Preserve metadata and unrelated input, camera, art and rendering behavior.

Core/Application, T03 fixtures and `docs/CONTRACTS.md` remain Lead-owned.
Before editing them, submit the exact proposed signatures/semantics, reason,
affected consumers (T04/T05/T06/T07/T08), and regression plan for Lead approval.
No shared-contract reservation or new API is approved by this packet. Local
investigation and tests can proceed without it. Do not weaken session locks,
save ordering or invariants to make the bridge pass.

T05/T06 cannot start until the revised T04 is accepted. T07 can proceed independently;
coordinate any shared-contract revision with its assignee. No settings, packages,
shared input assets, production scenes, combat or real storage implementation.

## Required Work

1. Reproduce the failure in real Unity PlayMode with the actual motor/session and
   fake storage. Exercise TickStarted and a physics collision callback, not only
   reflection or a manually advanced coroutine. Record failure on r1.
2. Define the step phases and ownership of pending damage, physics position/speed,
   pause requests and persistent interactions. Choose deferred commands/locks or
   another explicitly coordinated boundary that satisfies the checks below.
3. Ensure a begun step is neither discarded nor overwritten. Freeze subsequent
   physics while completion is pending; resolve its authoritative state once.
   Do not replay a physics step to recover a publication failure.
4. Distinguish rejection before tick processing from failure after processing:
   CompleteTick can advance state and prepare a Sink/Dock candidate before
   returning SaveFailed; invalid docking can also follow a valid simulation tick.
   Retrying those results must not apply damage twice or create another outcome.
5. Document the supported pause/interaction entry points and callback ordering
   for T05/T06. Route existing fixture pause calls through the chosen protocol
   if necessary; prohibit unsupported mid-step direct calls explicitly and ensure
   they fail safely, without silently losing damage.
6. Add regression coverage, rebuild the standalone fixture, and update the T04
   handoff with new source/build identities and exact verification results.

## Acceptance Checks

Rules: INV-02, INV-03, INV-14; preserve AC-01, AC-02 and AC-14.
These R1 IDs supplement, not replace, the parent T04 acceptance checks.

| ID | Required observable result |
| --- | --- |
| R1-C01 | Mid-step pause with 10 and 100 damage, requested before and after damage callbacks: apply damage once; nonlethal health becomes 90, lethal damage selects one Sunk outcome. Neither case silently resumes at 100 health. |
| R1-C02 | Pickup/save lock during a begun step: use a failing fake store, nonlethal and lethal damage, and repeated retry. Pending damage is not lost; cargo/source accounting is atomic, with no duplicate credit. Document interaction ordering relative to damage; a deferred pickup cannot credit a resolved voyage. |
| R1-C03 | Sink/Dock save failure after the tick was processed: retain the identical pending candidate, no additional physics/ticks while locked, no success event before commit, and exactly one outcome/event after retry. No second damage application. |
| R1-C04 | Queue docking and lethal damage in the same step: Sunk wins, cargo is not banked. Repeat with failed save/retry. Invalid zone/speed rejects docking without reapplying the already processed movement/damage tick. |
| R1-C05 | At unlocked AtSea boundaries, authoritative XZ position and planar speed match the Rigidbody within declared tolerances (initially 0.001 m and 0.001 m/s). While completion is pending the body cannot take another step. Pause/resume produces no unaccounted motion or clock advance. |
| R1-C06 | Cover locks requested from both TickStarted and an actual collision callback. Disable/re-enable the adapter with a pending step must not silently erase it or allow a later step to overwrite it; document the supported lifecycle. |
| R1-C07 | Existing T04 PlayMode and T03 EditMode suites remain passing; input edges, braking, collision, stable hardpoints and presentation isolation remain intact. |
| R1-C08 | Revised standalone builds; repeat measured 30/60/120 cadence/pause checks with no more than the existing 0.01 m common-tick difference. Record new build/source identities. Offline capture is not a performance claim. |
| R1-C09 | Handoff explains the new step protocol, retry semantics, any approved shared-contract changes, and each check's result. Lead independently reviews and accepts revised T04; developer success alone does not release successors. |

If the chosen protocol defers a lock until the current step completes, test that
behavior rather than requiring an unsafe immediate lock. If an unsupported direct
call is rejected, provide a passing test of the supported replacement and prove
the rejection itself cannot discard a begun step. Logging a warning is not recovery.

## Verification and Handoff

Use pinned Unity 6000.3.24f1. Run the expanded T04 PlayMode suite and T03 EditMode
suite, retaining XML/logs under a new R1 evidence subdirectory. Existing runners
write to historical filenames: archive r1 evidence before using them, or supply
new log/results paths to the same Unity command line. Preserve the original
Lead probe and findings; create a separate passing reproduction if needed.

Rebuild and rerun the T04 standalone cadence checks using the existing scripts
with archived original outputs or revision-specific destinations. Do not relabel
old captures/manifests as evidence of the fix. Additional visual footage is needed
only if framing/presentation changes; document reused visual evidence explicitly.

Append an R1 developer handoff to `docs/evidence/T04/REPORT.md` linking the new
artifacts, input/output hashes, failing-before/passing-after results, and consumer
instructions. Keep the previous Lead finding intact until independently closed.
Move this subtask to review when delivered; the Lead closes R1 and accepts the
parent T04 before marking T05/T06 ready. Do not start successor tasks.

Developer delivery 2026-09-18: T04-r2 is ready for independent Lead review.
Real Unity baseline reproduction failed all four new lethal-lock cases; revised
PlayMode suite passes 40/40, unchanged T03 suite passes 33/33. Revised standalone
build and measured 30/60/120 checks pass (202 common ticks, zero difference).
See [R1 handoff](../evidence/T04/REPORT.md#r1-developer-handoff---t04-r2) and
[consumer protocol](../evidence/T04/R1/PROTOCOL.md). No shared contracts changed.
R1-C09's independent acceptance remains pending; T05/T06 remain waiting.

Lead review 2026-09-18: accepted T04-r2; R1-C01 through R1-C09 satisfied at
the scope and evidence level recorded in REPORT.md's R1 Lead Review. Independent
runs: T04 40/40, T03 33/33. No remaining blocker found. Earlier pending statements
are historical. Parent T04 is accepted; T05/T06 are ready, not started.
