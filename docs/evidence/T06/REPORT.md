# T06 developer handoff

Task: T06, first region and salvage. Assignee: Codex (world engineer).
Output revision: T06-r1. Status: review (changes requested). Owner visual disposition: pending.
This report records implementation evidence, not independent acceptance.

## Inputs and changed paths

Accepted inputs: T03-r1 contracts and T04-r2 ship adapter/step protocol.
T03 source manifest SHA-256:
`EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F`.
All implementation entries match. The sole changed entry is CONTRACTS.md, whose
Lead acceptance annotation is explicitly documented in the T03 review.
T04-r2 source manifest SHA-256:
`1EBA894A94BE245D9015710F23E8A198FD5427E6E5D51AB9F34B5426871DF558`.
All 39 entries match. Accepted build manifest:
`C8C530540E6A67E2EA601C177F02F24B162656EA3B8B67BF016704E3620C3CC9`.
The project has no dedicated Git revision; source/build manifests identify outputs.

Output [source-files.json](source-files.json), 50 entries, SHA-256:
`D5EB4B8D05F3C7F8B19928E8A264C7C9DF42C820898BC77A27CCAB1D066C9BFB`.
Output [build-files.json](build-files.json), SHA-256:
`16506CA631D00656E2B82A6F01E6E1E1BDD6669B42E4E748C2848C77AF95FCDF`.
Build: `Builds/T06/Salvage.exe`. Source validation found no missing metadata;
[input-check.json](input-check.json) records the inherited-source comparison.

Changed assets, code and matching Unity metadata are confined to:

- `Assets/_Game/Content/World/FirstRegion/`: authored geography, explicit ID rows,
  docking data, route, salvage bundles and landmark materials.
- `Assets/_Game/Gameplay/World/Salvage/`: region recreation, authoritative entity
  capture/restore and nearest-source input adapter.
- `Assets/_Game/Prefabs/World/`: reusable barrel, wreck and complete region.
- `Assets/_Game/Scenes/Regions/FirstRegion.unity`: authored region scene.
- `Assets/_Game/Scenes/Tests/T06/`: isolated playable scene, composition, authoring,
  global content validation and standalone capture driver.
- `Assets/_Game/Tests/T06/`: focused PlayMode verification.
- `docs/evidence/T06/`: handoff, runner, manifests, logs and captures.
- Dispatch/status records: T06 packet, tasks README, TASKS and DEPENDENCIES.

New assemblies are task-local. No existing shared assembly, package, settings,
input asset, ship prefab, production composition or contract implementation changed.

## Delivered behavior

Homeward Reach has a home harbor, three fixed islands with collision and distinct
landmarks, three barrels, two wrecks, two encounter markers, and a short outward/
return route. FirstRegion.asset is the authoritative authored data. The scene and
prefab provide geometry; the region component recreates salvage from expedition
records. ID strings are independent of names, paths and Unity instance IDs.

E uses the existing T04 Interact edge and interaction hardpoint. A whole bundle
fits or returns CargoFull without changing either ledger. ShipSimulation queues
mid-step pickups after damage/docking and before pause. Source views query the
session ledger; queued or failed saves never hide or deplete them prematurely.
Scene recreation cannot introduce missing sources or refill depleted sources.

Shared contract changes: none. The scoped Lead-owned integration proposal and
affected consumers (T08/T10) are in [INTEGRATION.md](INTEGRATION.md). The test-only
catalog/store do not become production campaign ownership or durable storage.

## Verification

Unity 6000.3.24f1, `D:/u6-t01/Editor/Unity.exe`, Windows standalone, HDRP 17.3.0.
Commands run from the repository root:

```powershell
./docs/evidence/T06/RunUnity.ps1 -Mode Author
./docs/evidence/T06/RunUnity.ps1 -Mode Refine
./docs/evidence/T06/RunUnity.ps1 -Mode Tests
./docs/evidence/T06/RunUnity.ps1 -Mode Regression
./docs/evidence/T06/RunUnity.ps1 -Mode Rules
./docs/evidence/T06/RunUnity.ps1 -Mode Build
./docs/evidence/T06/RunCaptures.ps1
./docs/evidence/T06/VerifySources.ps1
```

Author is a one-time asset creation command and refuses existing outputs. Refine
preserves metadata while updating T06 landmarks and removing inherited pier art.
The runner rejects empty test runs; initial discovery exposed an Editor-only
assembly setting, which was corrected before the passing PlayMode run.

| Acceptance / invariant | Result | Evidence |
| --- | --- | --- |
| AC-04 / INV-05: full cargo, indivisible bundle | Passed | FullCargoRejectsWholeBundle: 3-unit bundle, 2-unit hold, same snapshot/source |
| INV-05: duplicate callbacks and atomic depletion | Passed | DuplicateCallbacksCreditOnce; FailedSaveKeepsSourceAndCargoUntilRetry |
| AC-05 entity recreation / INV-11 | Passed | RecreateAndReloadKeepDepletedWreck: destroy objects, construct session from snapshot, recreate; wood/iron credited once |
| Missing entities cannot reset loot | Passed | MissingEntityDoesNotSeedFreshLoot; stale restore rejected |
| AC-15 / INV-12: duplicate and invalid references | Passed | DuplicateIdsReportBothRegions; InvalidReferencesFailBeforePlay; editor pre-play/build validation |
| INV-10/11: stable identities/geography and collision | Passed | AssetRenameDoesNotChangeIdentityOrGeography; AuthoredRouteHasHullClearanceAndIslandCollision |
| T04-r2 protocol | Passed | InteractionQueuesThenCommitsAfterDamageAndPause: pending has no credit; damage and pickup commit before pause; recreation retains depletion |
| T06 PlayMode suite | Passed, 10 tests | Tests.xml / Tests.log |
| T04 regression | Passed, 40 tests | Regression.xml / Regression.log |
| T03 rules regression | Passed, 33 tests | Rules.xml / Rules.log |
| Standalone build | Passed | Build.log; Builds/T06/Salvage.exe |
| Standalone sailing route and visual inspection | Passed developer check, 9/9 waypoints | captures/route-result.txt, route.csv, harbor.png, region.png |
| Required handoff/reproduction | Delivered | This report and INTEGRATION.md |

The route geometry check uses engine collision with a 3-metre swept radius along
every segment and a downward collision query on every island. Asset rename is
tested on a detached clone; serialized IDs are explicit fields and do not derive
from the asset name. Captures are developer visual checks, not owner approval.

## Remaining scope and review

Durable saves, streamed unload/reload, random encounter selection, banking UI,
production composition and full expedition progression are not run here; they
belong to T08/T10 and later integration. The isolated memory store cannot prove
filesystem crash recovery. The fixture uses the accepted water setup and does
not reopen or claim to resolve T02's documented rendering limitations.

Manual keyboard sailing and owner aesthetic acceptance are not claimed. The
fixture exposes the existing keyboard bindings and explicit cargo-full feedback;
reproduction and route coordinates are in INTEGRATION.md. Scene-only render
captures omit the IMGUI HUD. No performance benchmark or minimum hardware claim
is made by these captures.

The final standalone capture ran on NVIDIA GeForce RTX 2070 SUPER at 1280x720
with HDRP. The capture driver submitted steering/throttle/interaction intents to
the existing ship simulation, followed all nine authored waypoints, braked near
home, and exited 0. No route teleportation or replacement physics was used.
The trace reaches tick 2100 before its final approach; it is route verification,
not a performance sample. The inspected overview shows all three islands and
home harbor, with collected barrels absent and rejected wreck bundles intact.
No known blocking defect remains in the assigned scope.

Lead action: review T06-r1 and merge the scoped definition/composition proposal
when integrating T08. Do not mark T06 done or release T08 on this developer report
alone. Preserve existing accepted T03/T04/T05 review records.

## Lead Review - 2026-09-19

Disposition: changes requested; T06 is not accepted and T08 remains waiting.
The developer's no-known-blocker statement above is superseded by this review.

### Finding F01 - P2: uninitialized pickup result breaks the startup HUD

`Assets/_Game/Scenes/Tests/T06/SalvageFixture.cs:67-68` reads
`interaction.LastResult.IsPending` before a pickup request has initialized it.
`SalvageInteraction.cs:14` declares an uninitialized reference-type `RuleResult`;
neither fixture Start nor interaction initialization assigns a neutral result.
OnGUI therefore dereferences null whenever it runs after session binding but
before the first interaction. This prevents the cargo/status box from drawing
and can emit repeated exceptions on an ordinary fresh launch.

This is a source-confirmed defect, not a claimed runtime reproduction. A hidden,
non-batch standalone smoke run (`lead-startup.log`) initialized successfully but
did not log the exception; it does not prove that a visible IMGUI repaint ran.
Reproduce with a visible fresh launch of `Builds/T06/Salvage.exe`, without pressing
E: the initial cargo/status HUD must render without exceptions.

Required correction: explicitly represent/handle the no-interaction state before
reading result members. Add startup/no-input regression coverage and visible-player
HUD evidence; retain pending, success and CargoFull feedback. Do not require a
first interaction to make the HUD usable. Existing render-target screenshots omit
IMGUI and the current automated tests do not exercise this startup HUD path.

### Independent Verification and Limits

- Reran pinned Unity 6000.3.24f1 checks: T06 PlayMode 10/10, T04 PlayMode
  40/40, T03 EditMode 33/33. See `lead-T06.xml`, `lead-T04.xml`, `lead-T03.xml`
  and corresponding logs. All 83 passed; this does not clear F01.
- Verified all 50 T06 source and 297 build manifest entries, 39 accepted T04-r2
  source entries, 37 T03 implementation entries and 53 accepted T05-r2 source
  entries without hash mismatches. The historical T03 CONTRACTS.md annotation
  is excluded as documented above. Review identity is the manifests above,
  not a dedicated Git revision.
- AC-04/INV-05, entity-level AC-05/INV-11 and AC-15/INV-12 have passing isolated
  checks; stable IDs/geography and route collision checks also pass. Inspected
  developer route evidence reports 9/9 waypoints; the route was not independently
  rerun. Durable/streamed continuity remains T08/T10 scope.
- Owner visual disposition and manual keyboard sailing remain unverified.
  No new owner approval, performance result or durable-save result is inferred.
- Review changed documentation and generated review evidence only; no production
  code, assets, settings or test sources were modified. No successors were started.

Return for review after F01 is corrected with evidence. T07 remains independently
ready; T05 acceptance and existing T02 limitations are unchanged.

Remediation planning 2026-09-19: [T06-R1](../../tasks/T06-R1-startup-hud.md) is
prepared, ready and unassigned. It covers F01, regression and visible-HUD evidence.
No implementation, new test result or acceptance is claimed by packet creation.

## T06-R1 Developer Handoff - 2026-09-19

Assignee: Codex (world/gameplay engineer). Output: T06-r2 / T06-R1 correction.
Status: submitted for independent Lead review. Owner visual disposition pending.
This section supersedes the remediation planning status above, not the historical
Lead review of r1. F01 closure and parent acceptance remain Lead decisions.

### Identity and Scope

Inputs are accepted T03-r1/T04-r2 and reviewed T06-r1 identified above.
The original T06 source/build manifest hashes still match the packet identities.
[R1/input-check.json](R1/input-check.json) accounts for all 50 old T06 source
entries: only SalvageFixture.cs, RegionAuthoring.cs and SalvageTests.cs changed.
All 297 original build entries remain identical. All 39 accepted T04 and 53
accepted T05-r2 source entries match; 37 T03 implementation entries match, with
the previously disclosed CONTRACTS.md Lead annotation as the sole difference.
No dedicated project Git revision exists.

Final source: [R1/source-files.json](R1/source-files.json), 52 entries, SHA-256
`D9962E16969B8F7CF3DB0186BF5E27FB68A54C177E7EF268AADCD68F6BA772E7`.
Final build: [R1/build-files.json](R1/build-files.json), 297 entries, SHA-256
`E13D2EB98EB1F1D10AB7F64ECC66142B34EF0C89C14870D4ED7DA1F5D6BA7F51`.
Player: `Builds/T06-R1/Salvage.exe`. Original r1 build/evidence retained.

Changed paths: the three T06 files above; new task-local HudCapture.cs and metadata;
docs/evidence/T06/R1/ runners, evidence and reproduction notes; this report and
INTEGRATION.md; T06/T06-R1 packets, dispatch board, TASKS.md and DEPENDENCIES.md.
Existing asset GUIDs are preserved. No shared contract change was required.

### Delivered Behavior

OnGUI calls HudText for its actual box contents. That method treats null
LastResult as a neutral status and reads cargo from the authoritative session.
No startup pickup is issued. Pending still displays Collecting, successful
pickup displays committed cargo, and CargoFull/no-source error feedback remains.
Presentation does not mutate state or consume commit events.

The new startup test loads the real scene twice and verifies null initial result,
neutral zero-cargo text, intact sources, identical snapshot and retained embark
event. Another test checks no-source/full-cargo HUD and recreation; the existing
pending/damage/pause test now checks collecting and committed HUD text too.

### Acceptance Evidence

| Check | Developer result | Evidence |
| --- | --- | --- |
| R1-C01 | Passed: visible fresh launch, neutral zero cargo, no input or fake pickup | [startup.jpg](R1/visible-repeat/startup.jpg), startup-ready.txt and Player.log in visible-repeat/; no error/exception matches |
| R1-C02 | Passed: fails before null guard; passes after, including second scene load | [BEFORE.md](R1/BEFORE.md), Before.xml/log (1 failure, NullReferenceException), Tests.xml/log (12 passed) |
| R1-C03 | Passed: real HUD text path covers pending, committed success, CargoFull and no-source | InteractionQueuesThenCommitsAfterDamageAndPause and HudReportsNoSourceAndFullCargoWithoutMutation in Tests.xml; retained-event assertions |
| R1-C04 | Passed: visible five wood/Cargo full, intact six-unit wreck, barrels remain depleted after recreation | [cargo-full.jpg](R1/visible-final/cargo-full.jpg), cargo-ready.txt (valid=True), Player.log, exit.txt (0) in visible-final/ |
| R1-C05 | Passed: T06 12/12, unchanged T04 40/40, unchanged T03 33/33; final build exit 0 | R1/Tests.xml/log, Regression.xml/log, Rules.xml/log, Build.log and source/build/input manifests |
| R1-C06 | Developer handoff delivered; independent F01 closure and owner disposition not run | This report maps every check; Lead/owner decisions remain pending |

### Reproduction and Environment

Pinned Unity 6000.3.24f1 at D:/u6-t01/Editor/Unity.exe; HDRP 17.3.0;
Windows standalone, NVIDIA GeForce RTX 2070 SUPER, 1280x720 windowed client.
Exact commands and capture sequence are in [REPRODUCTION.md](R1/REPRODUCTION.md):

```powershell
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Before
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Tests
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Regression
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Rules
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Build
./docs/evidence/T06/R1/RunPlayer.ps1
./docs/evidence/T06/R1/RunPlayer.ps1 -Run visible-repeat
./docs/evidence/T06/R1/VerifySources.ps1
```

Before was run on the extracted original status expression without its null guard.
The initial expanded test setup failure and initial evidence-driver overshoot are
retained and explained in BEFORE.md and REPRODUCTION.md. Final tests supersede
Tests-initial.xml/log; final visible route supersedes visible/. The capture-driver
correction was compiled and exercised in the final build; gameplay/test sources
are identical to the passing 85-test run.

Actual-window images were captured through Windows.Graphics.Capture using the
computer-use skill's sky.get_window_state and saved unchanged. They include
IMGUI and window chrome. The route used injected throttle/interaction intents
through ShipSimulation, without teleportation. It reached Z=78, attempted the
wreck pickup once at an idle boundary and recreated sources while paused.
No manual keyboard sailing, performance measurement, durable storage validation
or owner aesthetic approval is claimed. Existing T02 visual limitations remain.

No known blocking gameplay defect remains in this correction's verified scope.
Lead must independently review T06-r2 and close F01. Parent T06 stays review
(changes requested) until accepted, including owner visual disposition or explicit
acceptance of disclosed limitations. T08 remains waiting; no successor was started.

## Lead Review - 2026-09-25 (T06-R1)

Reviewer: Lead (Claude Opus 5.5), on the owner's 2026-09-25 instruction to
implement as much as possible. Reviewed the R1 source (`SalvageFixture.HudText`
null guard, `StartupHudAndFreshReloadAreNeutral` regression) and the recorded
actual-window evidence (`R1/visible-final/cargo-full.jpg`: "Wood 5  Iron 0  Cargo
full"). Independent re-run on the baseline snapshot (Git `5093da9`): T06 12/12,
T04 40/40 PlayMode and T03 33/33 EditMode passed.

Outcome: F01 closed; T06 technically accepted. The salvage components are now
integrated in production by T08 (generated wreck salvage was added there, with
`collectOnInteractIntent` keeping this fixture's behaviour). Owner visual
disposition of the T06 fixture art remains pending; production uses the T02 island
kit on the same authored footprints instead of the fixture cylinders.
