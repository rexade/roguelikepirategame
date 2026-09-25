# T04 Developer Handoff

Assignee: Codex (gameplay engineer). Date: 2026-09-18.
Accepted output: T04-r2. Status: done; T04-R1 closed by independent Lead review.

Latest developer submission: [T04-r2 / R1 handoff](#r1-developer-handoff---t04-r2),
accepted; see R1 Lead Review below. The original r1 handoff and Lead findings below are
historical evidence and remain intact; they do not represent acceptance of r2.

## Input and Output Identity

Accepted prerequisites: T03-r1 and T02's provisional visual/performance approval.
T03 source manifest SHA-256:
`EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F`.
T02 source manifest SHA-256:
`6E60AA8FEDBFB3BCFC0E0C0549D194CD8B08F894A12C5B2BF8698F3D4D2CE4E8`.
Unity: `6000.3.24f1`, `D:/u6-t01/Editor/Unity.exe`.
No project Git HEAD exists. Output identities are recorded in `source-files.json`
and `build-files.json`; no commit is claimed. The T03 contract document has its
previously recorded Lead annotation; all inherited asset hashes are preserved.

Output source manifest (37 files) SHA-256:
`71FD2CB818275B77886B22AD3ED48D3001F2DD5DCF3C61280FE8DA7FEA6EFCE2`.
Output build manifest SHA-256:
`D8E5B89CF0791B8FBDC5E585108A9DCB5EBC237F2D3475F1E374293AD90439EA`.

## Delivered Behavior and Changed Paths

- `Assets/_Game/Gameplay/Ships/`: constrained Rigidbody motor and fixed-step
  session adapter. Hull `speed` comes from computed T03 stats. Forward/reverse
  acceleration, coasting drag, steering, braking, continuous collision, normalized
  planar aim, pause suspension with velocity restoration, and post-physics tick
  publication are implemented.
- `Assets/_Game/Gameplay/Input/`: local Input System action map, keyboard/mouse
  adapter, frame-to-tick edge retention, input suppression on pause/focus loss.
- `Assets/_Game/Presentation/Ships/`: bounded model-only bob, pitch and roll.
- `Assets/_Game/Presentation/Camera/`: time-correct exponential follow, fixed
  60-degree pitch, accepted 50-degree perspective field of view in the fixture.
- `Assets/_Game/Prefabs/Ships/PlayerCutter.prefab`: reusable physics body, motor,
  simulation adapter, stable hardpoints and nested accepted T02 cutter visuals.
- `Assets/_Game/Scenes/Tests/T04/`: isolated HDRP scene, in-memory test session,
  standalone capture harness and Editor authoring/build helpers. T02's visual-only
  shoreline meshes and pier planks gain static colliders in this scene only.
- `Assets/_Game/Tests/T04/`: six PlayMode cases, including two collision speeds
  and the flattened sphere mesh used for shoreline collision.
- Matching Unity metadata and scoped assembly definitions; `docs/evidence/T04/`;
  task packet and dispatch-board assignment/status records.

No shared contract, package, project setting, existing assembly definition, input
asset, production scene or accepted art asset was changed. Scoped assemblies stay
inside the assigned paths, following the T03 precedent. No shared reservation was
needed; T07 can proceed independently.

## Input Map

| Control | Intent |
| --- | --- |
| W / S | Forward / reverse throttle |
| A / D | Left / right yaw |
| Space | Brake to rest |
| Mouse position | Aim on the body's horizontal plane from WeaponOrigin |
| Left mouse held | Fire intent |
| Right mouse pressed | One ability intent per press |
| E pressed | One interaction intent per press |
| Escape | Toggle session pause in the isolated fixture only |

Fire/ability/interaction are published intents; weapon effects and salvage are
successor tasks. The prefab deliberately does not own a camera, physical input
adapter or campaign. The scene composes these. Controller support is deferred.
Shared input-asset integration request: Lead should mirror these bindings in the
eventual shared gameplay map and arbitrate gameplay/UI focus with T07. The local
map needs no shared input-asset edit to exercise this task.

## Acceptance Results

| Requirement | Result and evidence |
| --- | --- |
| Island collision and keyboard/mouse | Passed automated checks: thin static barrier at 8 and 50 m/s and the shoreline mesh at 50 m/s; W/D, mouse fire, mouse planar aim, ability/interaction edges and focus suppression through Input System. Scene also has shoreline/pier colliders. Manual owner feel testing is not claimed. |
| Acceleration, turn, brake and XZ/yaw | Passed all three collision test cases: forward travel, collision stop, yaw change, frozen Y, brake to rest. |
| INV-14 / AC-14 | Passed model-amplitude and render LOD-bias isolation checks; hitbox and gameplay hardpoint unchanged. Standalone comparisons use bob amplitudes 0 and 0.5 m. No water sample or visual-quality value enters motor/clock calculations. |
| 30/60/120 rendering | Passed: 202 common measured ticks and 211 common offline-footage ticks each had zero recorded position difference between cadences; all six runs reached tick 350 at the same collision stop. Offline footage is separately labeled and is not a performance benchmark. |
| Simulation pause and published clock | Passed PlayMode and standalone checks; body position and session Tick stay frozen, then ticks resume. Actual weapon cooldown integration remains T05. |
| Handoff | Commands, conventions, evidence, limitations and integration steps recorded here. |

`playmode-results.xml`: six passed, zero failed/skipped. `Tests.log`: final run.
`source-check.json`: 86 inherited asset entries unchanged, no missing file metadata.
The source preservation check compares both accepted T02 and T03 manifests.

Measured frame timing, including capture/initial-render overhead:

| Requested FPS | Median frame ms | P95 frame ms | Mean frame ms |
| --- | --- | --- | --- |
| 30 | 33.3334 | 33.3343 | 37.3042 |
| 60 | 16.6667 | 16.6672 | 19.0404 |
| 120 | 8.3334 | 8.3340 | 9.7239 |

These short runs verify movement across render cadences, not sustained performance
targets. Initial-render and screenshot stalls lower average FPS; no uninterrupted
120 FPS claim is made. Final stop is X=6.99999332, Z=18.6, aim=(1,0,0), with body
and clock stable while paused in every run. The wall's south face is Z=21 and the
2.4 m hull half-length accounts for that stopping position.

All three final MP4s fully decoded successfully. Frame counts are 211/421/840 at
30/60/120 FPS, 1280x720. Sampled images were nonblank and changed during the route;
rendered stills were visually inspected for the ship, water, follow framing and
collision stop. `video-validation.json` records exact clip hashes and checks.

## Reproduction and Evidence

From the repository root with no editor using the project:

```powershell
./docs/evidence/T04/RunUnity.ps1 -Mode Tests
./docs/evidence/T04/RunUnity.ps1 -Mode Build
./docs/evidence/T04/RunCaptures.ps1
./docs/evidence/T04/RunCaptures.ps1 -Footage
./docs/evidence/T04/AnalyzeCaptures.ps1
./docs/evidence/T04/ValidateVideos.ps1
./docs/evidence/T04/VerifySources.ps1
```

Author mode is for a missing T04 scene only and refuses to overwrite it.
`RefineCollision` applies the delivered island/pier collision to an earlier T04
scene. Normal verification uses the already authored scene, not Author mode.
Build mode explicitly selects only the T04 scene and does not edit build settings.

Standalone: `Builds/T04/ShipControls.exe`. Open it normally for interactive play,
or open `Assets/_Game/Scenes/Tests/T04/ShipControls.unity` and enter Play Mode.
Steer into the shoreline or the long grey test wall north of the starting ship;
brake, reverse, turn, aim and toggle Escape while moving. Observe the hull collider
and hardpoints in the Scene view while changing child bob amplitude.

Capture hardware: NVIDIA GeForce RTX 2070 SUPER, Direct3D 11, driver 32.0.16.1664,
Windows, 1280x720, inherited HDRP configuration, v-sync off, 50 Hz physics.
The hidden automated player explicitly submits an HDRP render request each frame
to its render texture; otherwise Windows suppresses hidden-window rendering.
Interactive play renders normally. Measured runs use actual frame deltas; offline
footage uses `Time.captureDeltaTime` to exercise fixed 30/60/120 simulation-frame
cadences while capturing every rendered frame. Its wall-clock deltas include JPEG
readback/encoding and must not be interpreted as achieved gameplay frame rates.

Evidence locations:

- `Build.log`, `RefineCollision.log`, `Tests.log`, `playmode-results.xml`.
- `measured-30/`, `measured-60/`, `measured-120/`: frame CSV, four stills, pause/aim
  results, player logs for the final rendered runs.
- `footage-30/`, `footage-60/`, `footage-120/`: JPEG sequences, MP4 clips, frame CSV,
  pause/aim results and player logs. These are local artifacts, not remote links.
- `capture-summary.json`, `source-check.json`, source/build manifests.

Development failures resolved: the initial camera namespace shadowed Unity Camera
in inherited code, so the new namespace was renamed; injected input initially
needed explicit headless-editor focus configuration; the 50 m/s brake test needed
the configured stopping duration; hidden-window framebuffer reads produced blank
stills until explicit render requests were added. `fps-*` directories are those
invalid initial captures and are not visual or performance acceptance evidence.

## Integration Conventions and Limits

1. Instantiate one PlayerCutter; inject the existing campaign via
   `ShipSimulation.Bind(session)` after collision/arrival restoration. Session
   fixed delta must equal Unity's fixed delta; mismatch throws without changing
   project settings. Reapply `motor.Configure(session.ShipStats())` when computed
   hull stats change. Acceleration/braking/turn tuning is local authored data.
2. Add a ShipKeyboardMouse adapter and inject its simulation and aim camera.
   Add ShipFollowCamera to the gameplay camera and target the physics root.
   Disable the input adapter during UI ownership and clear pending intent. The
   fixture's MemoryStore/ReadyArrival and automatic embark are never production
   save/arrival implementations; T08 supplies those ports.
3. Root axes are +Z forward, +X starboard, +Y up. Root BoxCollider is 1.8 x 1 x
   4.8 m, centered at zero. Root Y is fixed. WeaponOrigin is (0, 0.6, 2.4);
   InteractionOrigin is (0, 0, 0). Both are direct gameplay-root children. Visible
   hull/sails live on a separate child with no colliders or rigidbodies. VFX can
   follow that child; collision and weapon targeting must use the stable origins.
4. Fixture colliders use the existing Default layer and its existing collision
   matrix. Lead request for production: reserve Ship and Island/World layers with
   mutual collision, no water-surface collision, and query masks that omit visual
   meshes/VFX. Do not put gameplay colliders beneath the bobbing transform. Static
   island meshes may be non-convex; moving ship colliders must be primitive/convex.
5. T05 subscribes to TickStarted for intent handling and contributes damage with
   AddDamage during that step, including physics collision callbacks. Publication
   occurs after physics through WaitForFixedUpdate. Do not install a second
   CompleteTick caller. Producers running later coroutines must instead contribute
   during the documented step. Zero-damage ticks are normal until T05 integration.
6. Pause through CampaignSession.SetPaused; motor/input observe IsPaused, including
   arrival/transition locks and docked state. The body becomes kinematic and saves
   velocity for resumption; camera/bobbing may remain visual while rules are paused.
   Disabling the simulation freezes the body and clears input. Resume/save/arrival
   velocity restoration beyond this pause behavior belongs to T08 integration.

Not run: physical-device human playthrough, owner camera/handling approval,
integrated combat/cooldowns, persistent arrival, production input/UI arbitration,
long performance soak or other hardware. This task does not change T02's provisional
visual limitations. No independent Lead or owner acceptance is claimed.

## Lead Review

2026-09-18: changes requested; T04-r1 is not accepted. T05/T06 remain waiting.

### Blocking Finding

**P1: An in-flight physics step can lose lethal damage when a callback locks the
session.** `Assets/_Game/Gameplay/Ships/ShipSimulation.cs:72` clears `stepped`
before `CompleteTick` acknowledges the step; lines 75-78 merely log rejection.
The next FixedUpdate resets `damage` at line 56. A TickStarted or collision
callback can pause the session or perform an interaction whose save fails. The
session then rejects CompleteTick as Paused/Busy, so accumulated damage never
reaches authoritative health. The physical step may already have moved the body.

Reproduction: start an embarked 100-health session, collect 100 damage with
AddDamage during TickStarted, then SetPaused(true). PublishTicks reports Paused
and leaves tick 0 / health 100. Unpause and take another step: tick 1 / health 100,
still AtSea. Replacing pause with a failing CollectLoot commit reports SaveFailed
then Busy and produces the same lost damage after RetrySave.

The executable review probe links the unchanged production ShipSimulation,
Core/Application and existing fake-port fixtures, with minimal Unity/motor stubs:

`dotnet run --project docs/evidence/T04/lead-probe/LeadProbe.csproj`

Observed output (exit 0 means the probe reproduced the defect, not a passing
acceptance test):

```text
Ship tick: Paused
pause: after lethal step tick=0, health=100
After resume: tick=1, health=100, lifecycle=AtSea
Pickup: SaveFailed
Ship tick: Busy
save failure: after lethal step tick=0, health=100
After resume: tick=1, health=100, lifecycle=AtSea
```

Required correction: define/enforce a coherent step boundary for pause and
persistent interaction commands. Do not discard an unacknowledged tick or allow
new physics to overwrite its damage. Defer locks/commands or preserve and resolve
the pending step with explicit failure handling; coordinate any required T03
contract changes with the Lead. Add real Unity regressions for mid-step pause and
failed pickup saves, including lethal damage, retry, once-only damage/outcome and
body/snapshot agreement. Recheck queued docking's damage-first ordering. This is
the delivered bridge's tick/lock handling, not a request to implement T05 combat.

### Independent Checks

- Reran Unity 6000.3.24f1 with -batchmode -nographics -runTests -testPlatform
  PlayMode -assemblyNames PirateGame.T04.Tests. Exit 0: 6 passed, 0 failed/skipped.
  Results: lead-playmode-results.xml and lead-playmode.log. Existing tests do not
  cover the blocking in-flight lock sequence.
- Verified 37 T04 asset manifest entries and 283 build entries: zero mismatches.
  Verified 49 T02 and 37 T03 inherited asset entries: zero mismatches. The T03
  contract-document annotation is excluded, as already disclosed in its review.
- Independently recomputed CSV cadence comparisons: 202 measured and 211 offline
  common ticks, maximum recorded XZ difference 0 in both groups. This verifies
  the existing straight-line capture, not all steering/input paths or performance.
- Inspected motor/input, simulation, camera/bobbing, fixtures/authoring, capture
  driver and tests. Viewed measured-60/frame-0003.jpg: nonblank ship, ocean,
  islands and collision wall; no owner handling/visual approval inferred.

### Acceptance and Limits

Movement/collision, basic keyboard/mouse input, model-only bobbing and between-step
pause checks passed within the existing automated coverage. Tick publication under
mid-step locks fails the required simulation/pause integration boundary and must
be corrected before releasing the ship adapter to T05/T06.

The new control-flow probe is not a Unity scheduler/physics test. No new standalone
build, captures, full-motion inspection or physical-device playthrough was run.
No claim is made for actual weapon cooldowns, persistence recovery or owner feel
approval. Production code, assets, settings and existing tests were not edited;
only review evidence/probe and status documents changed. Source/build manifests
identify the reviewed r1 submission; no project commit is claimed.

## R1 Developer Handoff - T04-r2

Task: T04-R1. Assignee: Codex (gameplay engineer). Date: 2026-09-18.
Status: review, not accepted. The original Lead finding above awaits independent
closure against this revision. T05/T06 remain waiting; T07 is unaffected.

### Identity and Scope

Pickup verified the T03-r1 and T04-r1 manifest hashes recorded above and every
listed asset against its manifest: zero mismatches. Required Unity installation
was available at `D:/u6-t01/Editor/Unity.exe`, version `6000.3.24f1`.
There is no project Git revision; manifests identify the submission.

- Source: [R1/source-files.json](R1/source-files.json), 39 files, SHA-256
  `1EBA894A94BE245D9015710F23E8A198FD5427E6E5D51AB9F34B5426871DF558`.
- Build: [R1/build-files.json](R1/build-files.json), SHA-256
  `C8C530540E6A67E2EA601C177F02F24B162656EA3B8B67BF016704E3620C3CC9`.
  Standalone: `Builds/T04-r2/ShipControls.exe`. Original `Builds/T04` is retained.
- [R1/source-check.json](R1/source-check.json): all 86 inherited T02/T03 assets
  unchanged, zero missing metadata. Existing GUIDs were preserved.

Changed runtime paths: `Gameplay/Ships/ShipSimulation.cs`, `ShipMotor.cs` under
`Assets/_Game`; fixture pause callers `Scenes/Tests/T04/ShipTestScene.cs` and
`ShipCapture.cs`; fixture `Editor/ShipTestAuthoring.cs` gains a build destination
argument so the original player is preserved. Added `Tests/T04/TickLockTests.cs`
and its metadata. Existing six T04 tests are unchanged. Evidence runners accept
revision-specific output paths; task/dispatch status and this handoff are updated.
No shared contracts, Core/Application, T03 tests, input, camera, art, scene,
prefab, project settings, packages or assembly definitions changed.

### Delivered Protocol

Full consumer instructions: [R1/PROTOCOL.md](R1/PROTOCOL.md). This supersedes the
original handoff's instruction to pause directly through CampaignSession.

ShipSimulation retains an in-flight step until the authoritative tick or outcome
acknowledges processing. Paused/Busy rejection retains damage and one captured
physical result, freezes subsequent physics, and retries publication without
replaying motor input. SaveFailed after tick processing and invalid docking do
not reapply damage. Session.RetrySave retains the identical outcome candidate.

Use `ShipSimulation.SetPaused` and `ShipSimulation.CollectLoot` from gameplay
callbacks. Ordering is damage/docking, deferred pickup, requested pause. A lethal
step cannot credit a deferred pickup. Queue acknowledgement is not save success;
LastTickResult/LastPickupResult expose command results and DrainEvents reports
commits. Direct mid-step session locks are unsupported for integration, but their
publication rejection now retains the step until resume/retry. The supported
pickup wrapper also ensures the save contains the already processed tick.

Component disable and whole-GameObject deactivation retain pending work. Re-enable
the same adapter to finish it. Bind rejects replacement while a step is pending;
destroying/unloading that adapter is unsupported. The motor preserves speed and
disables interpolation while suspended to avoid reactivation from a stale visual
pose. No shared API change or Lead-owned contract reservation was required.

### Verification

All tests used actual Unity PlayMode/physics with the production motor/session and
a local fake store. New lock regressions do not manually advance the publication
coroutine, use reflection, or call Physics.Simulate. A physical box collision
executes OnCollisionEnter; separate variants use TickStarted.

| Check | Developer result and evidence |
| --- | --- |
| Failure before fix | [R1/before.xml](R1/before.xml), [log](R1/before.log): original six pass; all four new lethal pause/pickup tests fail after resume/retry, from TickStarted and collision. Exit 2 is the expected failure reproduction. Exact baseline test source: [before-TickLockTests.cs.txt](R1/before-TickLockTests.cs.txt). Original Lead probe/findings remain unchanged. |
| R1-C01 | Passed: SupportedPauseOrdering covers 10/100 damage, both callback types, pause before/after AddDamage; health 90 or one Sunk outcome, stable pause and correct resume. Direct pause variants also retain damage. |
| R1-C02 | Passed: SupportedPickupOrdering covers the same eight combinations, failed save and two failing retries, identical candidate, atomic cargo/source accounting, duplicate request rejection and damage-first ordering. Lethal resolution rejects the deferred pickup. Direct failed pickups also retain nonlethal/lethal damage. |
| R1-C03 | Passed: failed Sink/Dock candidates remain identical through repeated retry; no event before commit, exactly one afterward, fixed health/tick/body while save-locked. These final tests use the save lock without an additional explicit pause. |
| R1-C04 | Passed: queued dock plus lethal damage selects Sunk, banks no cargo, with successful and failed saves. Invalid zone/speed reports its error while retaining tick 1/health 90; resumed ticks do not reapply damage. |
| R1-C05 | Passed: unlocked XZ/speed agreement within 0.001 m and 0.001 m/s, retained-body freeze, stored speed preserved over pause, clock and position stable during locks. New standalone pause checks also pass. |
| R1-C06 | Passed: TickStarted and actual collision coverage; component and GameObject disable/re-enable retain the pending step without overwrite. Rebinding while pending throws. Lifecycle limits are in PROTOCOL.md. |
| R1-C07 | Passed: [R1/verified.xml](R1/verified.xml), [log](R1/verified.log): 40/40 T04 PlayMode tests, including original six. [R1/t03.xml](R1/t03.xml), [log](R1/t03.log): 33/33 unchanged T03 EditMode tests. No failed/skipped tests in these final runs. |
| R1-C08 | Passed: [R1/Build.log](R1/Build.log), separate revised standalone; measured 30/60/120 runs compare 202 common ticks with maximum recorded XZ difference 0 m, below 0.01 m. All finish at tick 350, X=6.99999332, Z=18.6, with position/clock stable during pause. |
| R1-C09 | Developer handoff complete. Independent Lead review/acceptance is pending; this submission does not release successors. |

Measured evidence: `R1/measured-30/`, `R1/measured-60/`, `R1/measured-120/`, and
[R1/capture-summary.json](R1/capture-summary.json). Windows, RTX 2070 SUPER,
1280x720, inherited HDRP, v-sync off, 50 Hz physics. Median frame times were
33.3334/16.6667/8.3334 ms; mean times including startup/capture overhead were
37.3192/18.8153/9.5662 ms. These short runs are cadence checks, not a sustained
performance claim. The new measured-60 final still was visually inspected and
shows the rendered ship, water, islands and wall. Framing/art did not change.
Original r1 full-motion footage is reused only as presentation evidence; it is
not labeled as r2 behavior evidence. No new offline footage was required or run.

Intermediate logs remain for audit: `after-initial` passed the ten initial tests;
`after-expanded` exposed reactivation pose restoration and test callback timing;
`after` additionally exposed an incorrectly paused test precondition for cargo;
`after-final` passed 40 tests before strengthening save-lock checks to exclude an
extra explicit pause. `verified.xml` is the final test suite for the manifest above.

### Reproduction and Integration

From the repository root, with no editor using the project:

```powershell
./docs/evidence/T04/R1/RunTests.ps1 -Label verified
./docs/evidence/T04/R1/RunTests.ps1 -Label t03 -Rules
./docs/evidence/T04/RunUnity.ps1 -Mode Build -OutputDirectory "$PWD/docs/evidence/T04/R1" -BuildPath Builds/T04-r2/ShipControls.exe
./docs/evidence/T04/RunCaptures.ps1 -OutputDirectory "$PWD/docs/evidence/T04/R1" -BuildPath Builds/T04-r2/ShipControls.exe
./docs/evidence/T04/AnalyzeCaptures.ps1 -OutputDirectory "$PWD/docs/evidence/T04/R1" -MeasuredOnly
./docs/evidence/T04/VerifySources.ps1 -OutputDirectory "$PWD/docs/evidence/T04/R1" -BuildDirectory Builds/T04-r2
```

Use a new test label/output directory to retain this submission's logs on reruns.
The failing baseline was run with `RunTests.ps1 -Label before` before runtime
edits, against the verified r1 source identities; current code is expected to pass.

T05/T06 must adopt the linked protocol after Lead acceptance. T07 needs no API
migration. T08 must still integrate real persistence/arrival and boundary-only
checkpoint capture. Known integration limits: no crash-recovery guarantee for
unsupported direct mid-step persistent calls; no destruction/scene-transfer
support with an unpublished step; no actual weapon/cooldown integration, human
handling approval or performance soak claimed. No known failing developer
acceptance check remains. Lead must independently review r2 and close the original
finding before accepting T04.

## R1 Lead Review - 2026-09-18

Outcome: accept T04-r2 and close T04-R1. The original lost-damage blocker is
resolved in the reviewed scope. No remaining blocking defect identified.
This supersedes earlier pending/change-request statuses without deleting history.
Parent T04 is done; T05/T06 are ready and unassigned, not started. T07 remains ready.
The reusable ship prefab transfers to Lead ownership under the dispatch rules.

### Independent Verification

- Inspected the revised simulation/motor, callback protocol and all new lock
  regressions. Pending publication retains step identity, damage and physical
  state; processed ticks/outcomes are acknowledged independently of save success.
  Deferred pickup and pause ordering preserves damage-first resolution.
- Ran `./docs/evidence/T04/R1/RunTests.ps1 -Label lead-review`: Unity exit 0,
  40 passed, 0 failed. Evidence: R1/lead-review.xml and R1/lead-review.log.
- Ran `./docs/evidence/T04/R1/RunTests.ps1 -Label lead-t03 -Rules`: Unity exit 0,
  33 passed, 0 failed. Evidence: R1/lead-t03.xml and R1/lead-t03.log.
- Verified all 39 r2 source entries and 283 build entries: zero hash mismatches.
  Verified all 49 T02 and 37 T03 inherited asset entries: zero mismatches.
  Against the original T04 source manifest, only the five declared existing
  runtime/fixture source paths changed; the new test and metadata are in r2.
- Inspected preserved baseline XML: original six tests passed and four new
  lethal-lock cases failed. This is inspected developer baseline evidence, not
  an independent rerun of obsolete production code.
- Independently recomputed the revised 30/60/120 CSV comparisons: 202 common
  ticks, maximum recorded XZ difference 0 m. All three pause-result files report
  stable position and clock. Source/build identity matches the r2 handoff hashes.

### Acceptance Mapping

| Checks | Lead result |
| --- | --- |
| R1-C01/C02 | Passed Unity pause/pickup ordering cases with lethal/nonlethal damage, both callback sources, both orders, failure and retry |
| R1-C03/C04 | Passed failed Sink/Dock candidate retries, once-only outcomes, damage-first docking and invalid docking without replay |
| R1-C05/C06 | Passed physical/state agreement, retained-step freeze, component/GameObject disable/re-enable and rebind guard |
| R1-C07 | Passed expanded T04 and unchanged T03 suites; original six T04 cases retained |
| R1-C08 | Accepted build manifest/build evidence and independently checked revised capture data; no new Lead build or captures run |
| R1-C09 | Protocol and scope reviewed; independent acceptance recorded here |

### Integration Limits

T05/T06 must use R1/PROTOCOL.md: ShipSimulation.SetPaused/CollectLoot for
mid-step requests; damage producers finish during TickStarted/collision collection;
only this adapter publishes ticks. RetrySave handles processed failed candidates,
not physics replay. Unsupported direct mid-step persistent calls are protected
against lost live damage but are not a coherent crash-save protocol. Never destroy
or transfer an adapter with a pending step; T08 must coordinate that boundary.

No owner handling approval, new full-motion visual review, real persistence,
integrated weapons/cooldowns, performance soak or other-hardware testing is claimed.
T02's provisional visual limitations remain. This review used the current workspace
and recorded manifests, not a clean import or project commit. Only evidence and
status/consumer documentation were updated; no production or test-source edits.
