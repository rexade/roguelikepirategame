# T06-R1: Make the salvage HUD safe before first interaction

Status: review
Assignee: Codex (world/gameplay engineer)
Role: World/gameplay engineer, with independent Lead acceptance
Parent: [T06](T06-world-salvage.md), review (changes requested)
Priority: P2 acceptance blocker
Inputs: accepted T03-r1/T04-r2; reviewed, unaccepted T06-r1 and finding F01

Developer submission: [T06-R1 handoff](../evidence/T06/REPORT.md#t06-r1-developer-handoff---2026-09-19).
Verified input/output manifests and all check results are recorded there.
Technical work is ready for independent review; F01 closure and owner visual
disposition remain pending. This does not accept parent T06.

This is parent remediation, not a new milestone or a dependency on T06 acceptance.
Preparing this packet does not assign or start implementation. Follow the
[dispatch rules](README.md), [context](../../CONTEXT.md),
[invariants](../INVARIANTS.md), [contracts](../CONTRACTS.md) and
[T04 step protocol](../evidence/T04/R1/PROTOCOL.md). Do not start successors.

## Problem and Inputs

The [Lead review](../evidence/T06/REPORT.md#lead-review---2026-09-19) identifies F01:
`SalvageFixture.OnGUI` reads `interaction.LastResult.IsPending` before the first
interaction. `SalvageInteraction.LastResult` is an uninitialized reference type.
Once the session is bound, a HUD repaint dereferences null instead of displaying
the initial cargo/status box. The existing 10 T06, 40 T04 and 33 T03 tests passed
independent review but do not cover this presentation path.

The finding is source-confirmed. The hidden standalone smoke log did not reproduce
the exception and does not establish that a visible IMGUI repaint occurred.
Reproduction to verify: launch the T06 standalone visibly or enter Play in its
Salvage scene, send no interaction input, and inspect the HUD and log.
Expected: initial cargo is readable with a neutral status and no exception.
Do not claim a runtime failure was observed until recorded.

T06-r1 source manifest SHA-256:
`D5EB4B8D05F3C7F8B19928E8A264C7C9DF42C820898BC77A27CCAB1D066C9BFB`.
Build manifest SHA-256:
`16506CA631D00656E2B82A6F01E6E1E1BDD6669B42E4E748C2848C77AF95FCDF`.
See [parent report](../evidence/T06/REPORT.md) for accepted input identities.
Verify inputs at pickup, preserve subsequent work, and record discrepancies.
There is no dedicated project Git revision; do not invent a commit identity.

## Ownership and Exclusions

Owned paths for this correction:

- `Assets/_Game/Scenes/Tests/T06/SalvageFixture.cs` and necessary T06-local HUD helpers.
- `Assets/_Game/Gameplay/World/Salvage/SalvageInteraction.cs` only if needed to define the initial result contract.
- `Assets/_Game/Tests/T06/` and necessary task-local evidence/build runners under
  `Assets/_Game/Scenes/Tests/T06/` and `docs/evidence/T06/`.
- Matching metadata, the T06 integration handoff, and task status records.

Core/Application, shared contracts, T04 adapter/motor, input assets, ship prefab,
packages/settings and accepted T05 implementation are not owned by this packet.
Submit any required shared change with consumers and tests for Lead approval first.
Preserve GUIDs. No region redesign, water changes, loot rebalance, broad UI rewrite,
banking, durable storage, streaming or T08 composition work.

## Required Correction

1. Handle no interaction yet explicitly. Preferred narrow default: render a neutral
   status when LastResult is null in the fixture. Initializing a neutral result is
   also acceptable if its public semantics are documented. Neither option may
   imply a committed pickup or require a dummy pickup at startup.
2. Keep cargo sourced from the authoritative session. Preserve collecting/pending,
   success, CargoFull and other existing error feedback. Pending is not committed
   success; presentation must not mutate cargo, source state or drain events.
3. Add a regression that fails on the reviewed implementation and exercises the
   status/HUD logic actually used at startup. A test that checks only a new helper
   without wiring it into OnGUI is insufficient. A headless status test alone is
   not evidence of a visible HUD repaint.
4. Rebuild the isolated T06 player and gather actual-window HUD evidence. Use
   revision-specific outputs; do not overwrite the reviewed r1 build or evidence.

## Acceptance Checks

Preserve INV-05/10/11/12, AC-04, entity-level AC-05 and AC-15 at the parent scope.

| ID | Observable result |
| --- | --- |
| R1-C01 | Fresh scene/player, after session binding and before any E input: initial cargo/status renders without null dereference, repeated exception or fake pickup. Capture actual-window evidence and inspect the startup log. |
| R1-C02 | Record a failing-before/passing-after automated regression for the no-result startup path used by OnGUI. Fresh scene reload also restores a safe initial display without requiring prior interaction. |
| R1-C03 | Verify the real HUD status path for pending, successful pickup, CargoFull and no-source error. Pending does not present committed success; success reads committed cargo. Tests may cover transient pending state that is difficult to capture visually. |
| R1-C04 | In the revised standalone, collect barrels at (0,8) and (0,38), then try the wreck at (0,82). HUD shows 5 wood and Cargo full; the six-unit wreck bundle remains intact. Supply visible HUD evidence, not render-target-only screenshots. Recreating sources must not refill collected barrels. |
| R1-C05 | Expanded T06, unchanged T04 and T03 suites pass with exact counts and logs. Revised standalone build passes. Source/build manifests identify the output and preservation checks account for accepted shared inputs. |
| R1-C06 | Handoff maps every check to evidence or explicitly not-run status. Independent Lead review closes F01; parent T06 acceptance also requires a recorded owner visual disposition or explicit acceptance of disclosed visual limitations. Neither automated tests nor developer claims supply owner approval. |

For R1-C04, the fixture has a 10-unit hold; barrels contribute 3 then 2 wood.
Use the [integration route](../evidence/T06/INTEGRATION.md#route-and-reproduction).
Record whether navigation was manual keyboard input or injected motor intents.
Do not claim manual sailing from an automated route. Keep missing owner/manual
checks distinct from a technical defect or a passed check.

## Evidence and Handoff

Use pinned Unity 6000.3.24f1. Preserve original r1 source/build manifests, logs,
captures and Lead review. Store new XML/logs, startup and CargoFull HUD images,
before/after regression evidence and manifests in `docs/evidence/T06/R1/`;
build to `Builds/T06-R1/`. Existing runners overwrite historical names, so adapt
their output paths before execution. Do not weaken existing assertions.

Append an R1 developer handoff to `docs/evidence/T06/REPORT.md`: assignee, verified
inputs, output identity, changed paths, exact commands, results per R1-C01-C06,
capture method, environment, known defects and remaining decisions. Update
INTEGRATION.md if the public result semantics or reproduction steps change.
Scene-only render captures omit IMGUI and cannot satisfy HUD visibility evidence.
Do not create placeholder evidence implying unperformed checks.

Submit technical work for independent Lead review even if owner disposition is
pending, labeling that gap. Do not mark parent T06 done on a developer handoff.
Only accepted revised T06 satisfies its T08 prerequisite; T08 also needs accepted
T07. T07 remains independently ready and accepted T05 remains unchanged.
