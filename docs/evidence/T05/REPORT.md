# T05 Developer Handoff

Status: done. Assignee: Codex (combat engineer). Date: 2026-09-18.
Accepted output: **T05-r2**. Technical Lead review passed; owner visual approval recorded.
T05/T05-R1 are complete. Earlier pending statements and r1 findings are historical;
see the final Owner Disposition for acceptance.

## Identity and Scope

Assigned by the owner as "implement t05". Accepted inputs were available:

- T04-r2 source manifest SHA-256:
  `1EBA894A94BE245D9015710F23E8A198FD5427E6E5D51AB9F34B5426871DF558`.
- T04-r2 build manifest SHA-256:
  `C8C530540E6A67E2EA601C177F02F24B162656EA3B8B67BF016704E3620C3CC9`,
  standalone `Builds/T04-r2/ShipControls.exe`.
- Accepted T03-r1 contracts; historical source manifest:
  `EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F`.
  Its later Lead annotations are not mistaken for unchanged developer documentation.
- Accepted T02 ocean/art, subject to the recorded provisional visual limitations.
- Unity 6000.3.24f1 at `D:/u6-t01/Editor/Unity.exe`.

Pickup checked 408 inherited asset/build entries with zero mismatches.
Final preservation check verifies all 125 inherited T02/T03/T04 asset entries.
No project Git HEAD exists; the enclosing user repository was not modified.

Output source manifest: [source-files.json](source-files.json), 53 asset/metadata
entries, SHA-256:
`E3FB7C370D2069C269B199D9B3B0E15AF12AEA4F33232BE02387E560CFE0F8CA`.

Output build manifest: [build-files.json](build-files.json), SHA-256:
`A84CE5D312040B2C2AA5C0C3F001CCEB65D3D90BD1914C5F9684AFD09C172F58`.
Runnable player: `Builds/T05/Combat.exe`.

Changed paths:

- `Assets/_Game/Content/Combat/`: authored rule/behavior catalogs, detached
  immutable specs and two enemy sail materials.
- `Assets/_Game/Gameplay/Combat/`: bounded targets, swept/reusable shots,
  simulation consumer and expedition capture/restore.
- `Assets/_Game/Gameplay/AI/`: enemy motor/state-machine and entity state port.
- `Assets/_Game/Prefabs/Combat/`: Raider and Gunner prefabs.
- `Assets/_Game/Scenes/Tests/T05/`: ocean combat scene, fixture-only session,
  presentation, content validation, authoring/build and capture helpers.
- `Assets/_Game/Tests/T05/`: 15 focused PlayMode tests and scoped assembly.
- Matching Unity-generated metadata; no existing GUIDs changed.
- `docs/evidence/T05/`, this packet and the dispatch board.

Existing shared contracts, input assets, settings, manifests, assembly definitions,
production scenes and the shared PlayerCutter prefab were not edited. New scoped
assemblies live inside owned paths. No shared-contract changes were made or
accepted. See [INTEGRATION.md](INTEGRATION.md) for exact consumer instructions.

## Delivered Behavior

Two owned alternative directed weapons: cannon (30 damage/1.1 s) and repeater
(8 damage/0.22 s), each with authored speed, range and sweep radius. The shared
loadout rules select the weapon before embark and reject refitting at sea.
The optional computed damage-scale hull stat consumes existing modifier rules.

Brace reduces collected player damage by 75% for 2 seconds, with a 6-second
cooldown. Timers advance only from T04 TickStarted. The existing adapter remains
the sole tick publisher; damage finishes during its collection phase.

Raider pursues; gunner maintains distance and retreats when approached. Both use
ShipMotor, stable hardpoints and engine obstacle casts. Initial/repeated enemy
reloads are authored. Enemy bodies suspend between accepted physics steps.

Non-piercing shots sweep the complete travel segment, handle initial overlaps,
sort hit distance, respect blockers and retire before damage callbacks. Pool reuse
resets owner/team, position, direction, damage, range and generation. Old-generation
callbacks and repeat hits cannot apply another hit. Health is bounded; enemy death
emits once and disables collision. Restoring death emits no new event.

Capture retains player cooldowns/brace, enemy health/mode/pose/motion/cooldown,
active shots and generation, while preserving existing expedition/world ledgers.
A versioned context EntityState fits the accepted capture contract without changing
Core. Validation occurs before restoration mutates live combat. Fresh-session
restore is covered, including a defeated enemy and live shots.

The dedicated scene reuses the accepted ocean and camera with distinct enemy sails,
friendly/hostile shot colors and brace ring. Fixture HUD includes health, cooldowns
and enemy bars. It remains isolated from production Bootstrap composition.

## Acceptance Results

| Requirement | Result and evidence |
| --- | --- |
| Duplicate hits and death once | Passed: AC13_DuplicateHitsDeathOnceAndNewOwnerOnReuse; EntityRestoreValidatesBeforeMutationAndDoesNotEmitDeathAgain |
| Fast shots hit crossed targets | Passed: SweptShotHitsCrossedThinTarget; wall/friendly filtering and initial overlap/range expiry tests |
| Correct owner and no stale history on reuse | Passed: AC13 test covers second owner/team/weapon/damage/direction/range, old-generation callback rejection and one new hit |
| Loadout changes behavior | Passed: DockedLoadoutSelectsDifferentWeaponAndAtSeaRefitRejects; both standalone loadout captures |
| Cooldowns pause with simulation | Passed: PauseFreezesShotsCooldownsAbilityAndEnemyMotor; brace, shot and body state remain stable |
| Defensive ability | Passed: BraceReducesCollectedDamageAndCooldownPreventsReactivation |
| Capture/restore combat state | Passed: CaptureCheckpointRestoreRetainsShotsTimersEnemyAndRejectsMalformedAtomically; FreshSessionRestoresLiveProjectilesAndDefeatedEnemy |
| Accepted T04 lock protocol | Passed: retained tick/disabled adapter, failed checkpoint retry and lethal failed Sink tests; no replay, one Sink publication |
| Two enemy archetypes | Passed: EnemyArchetypesPursueAndRetreatUsingMotor plus rendered captures |
| Immutable/valid authored data | Passed: DefinitionsDetachAndRejectUnknownOrInvalidContent; scene catalog validates before build |
| Combat readability on accepted ocean | Developer inspection passed for sampled gameplay-camera frames: distinct shot colors, brace ring, enemy silhouettes and defeat visibility. Owner visual gate pending |
| Handoff and reproducibility | Complete: report, integration guide, scripts, manifests, XML/logs, CSV/frame/video captures |

Final test results:

- T05 PlayMode: **15 passed, 0 failed**, [Tests.xml](Tests.xml), [Tests.log](Tests.log).
- T04 regression: **40 passed, 0 failed**, [Regression.xml](Regression.xml),
  [Regression.log](Regression.log). Run after initial T05 implementation; final
  T05-local cleanup/fixture refinements do not modify T04.
- T03 EditMode: **33 passed, 0 failed**, [Rules.xml](Rules.xml), [Rules.log](Rules.log).
- Standalone development build: **passed**, Unity exit 0, [Build.log](Build.log).
- Source preservation/metadata: **passed**, [source-check.json](source-check.json).

The first combat run exposed two teardown failures after enemy bodies were
destroyed before the world. Suspend now tolerates destroyed bodies; reruns pass.
Visual iteration corrected a shoreline spawn, capture aiming and enemy reload
timing. Final review removed invisible colliders from defeated ships and obsolete
projectile visuals after restore. Final tests/build/captures include these fixes.

## Reproduction and Evidence

Run from the project root with no other Unity editor using this project:

```powershell
./docs/evidence/T05/RunUnity.ps1 -Mode Tests
./docs/evidence/T05/RunUnity.ps1 -Mode Regression
./docs/evidence/T05/RunUnity.ps1 -Mode Rules
./docs/evidence/T05/RunUnity.ps1 -Mode Build
./docs/evidence/T05/RunCaptures.ps1
./docs/evidence/T05/VerifySources.ps1
```

The scripts launch the pinned Unity editor hidden and wait for its exit. Tests use
PlayMode except Rules (EditMode). Builds explicitly include only Combat.unity and
do not edit shared build settings. Author creates the initial fixture only when
the scene is absent; Refine records the final spawn positions and reload values.
Do not rerun Author over existing assets.

For manual play, open `Assets/_Game/Scenes/Tests/T05/Combat.unity` and enter Play,
or run `Builds/T05/Combat.exe`. WASD sails/turns, Space brakes, left mouse fires,
right mouse activates brace, Escape pauses. Pass `-t05-repeater` to the standalone
for the alternate owned loadout. The fixture uses memory storage, not durable saves.

Capture hardware: Windows workstation, NVIDIA GeForce RTX 2070 SUPER, Direct3D11,
1280x720. Each capture has 330 frames encoded as an 11-second, 30 FPS MP4.
These are offline fixed-capture-delta sequences, not performance measurements.

| Capture | Recorded result |
| --- | --- |
| [cannon.mp4](cannon.mp4) | 14 total player/enemy shots; raider defeated at frame 71, gunner at 178; player ends at 64 health |
| [repeater.mp4](repeater.mp4) | 50 total player/enemy shots; raider defeated at frame 61, gunner at 152; player ends at 64 health |

Each weapon directory contains frame-0000.jpg through frame-0329.jpg, combat.csv,
result.txt and player.log. Both final player logs contain no error/exception match.
Final inspected samples: cannon/frame-0045.jpg, repeater/frame-0060.jpg and
repeater/frame-0270.jpg. Earlier iteration samples were also inspected.
The capture render request excludes screen-space OnGUI, so these recordings
establish battlefield readability, not HUD visual acceptance. The MP4s were encoded;
full-motion manual playback was not performed.

## Limits and Lead Review

No known failing automated check. Remaining scope limits:

- Owner combat/handling approval, HUD visual acceptance, manual mouse playthrough,
  dusk/rough-water combat review, performance soak and other-hardware checks are
  not run. T02's accepted-for-now limitations remain.
- T08 must compose production components and coordinate coherent captures with
  at-sea writes. No real persistence, economy, salvage or successor work is claimed.
- Player momentum is reconstructed from saved scalar speed/yaw, with zero angular
  velocity; exact lateral/reverse solver momentum is not in the accepted T03 state.
- Enemy steering is local avoidance, not complex island pathfinding. No audio or
  elaborate impact/death effects; dead hulls disappear and no salvage is spawned.
- Content state version 1 rejects unknown versions. Future version migration is
  a coordinated persistence/content change, not silent substitution.

Lead: independently review T05-r1, its scope and evidence, then record acceptance
or requested changes here. Owner visual decision remains pending. The developer
has moved T05 to review, not done; T08 remains waiting for its accepted prerequisites.

## Lead Review - 2026-09-18

Outcome: changes requested; T05-r1 remains in review, not accepted. T08 remains
waiting. T06/T07 readiness is unaffected. No production or existing test source
was edited during review.

### R1 - Stale Player Combat Read State (P2)

CombatTarget.cs:32 forwards player damage without updating its Health. That is
appropriate for collecting authoritative damage, but CombatWorld.cs:87 refreshes
the player target only at the beginning of the next TickStarted. Once the damaged
step is paused, save-locked or resolves Sunk, there may be no next combat tick.
The public target then indefinitely exposes stale Health/Defeated state. Its Died
event is never emitted for the delegated player path. The fixture HUD hides this
by reading CampaignSession rather than CombatWorld.Player.

This is a read-state/publication defect, not lost authoritative damage: the
session correctly subtracts health and resolves sinking. Consumers following
the documented combat read-state interface nevertheless see a healthy player
after damage or sinking, and cannot rely on generic target death notifications.

Independent data-path probe:

`dotnet run --project docs/evidence/T05/lead-probe/LeadProbe.csproj`

It links unchanged CombatTarget and Core/Application with existing fake ports;
Unity component calls are stubbed. It reproduces the actual ordering (refresh,
collect, CompleteTick, pause), not Unity physics/scheduler behavior. Exit 0 means
the defect reproduced, not acceptance passed:

```text
damage=10, lifecycle=AtSea, sessionHealth=90, targetHealth=100, targetDefeated=False, targetDeaths=0
damage=100, lifecycle=Docked, sessionHealth=resolved, targetHealth=100, targetDefeated=False, targetDeaths=0
```

Required correction: synchronize or query player combat read state at the
authoritative processed boundary, including paused and failed-outcome-save states,
without duplicating damage or bypassing T04 publication. Define player death
notification semantics explicitly: once-only committed outcome notifications must
not be fabricated before save acknowledgement or repeated on retry/restore. If
Died is enemy-only, document and expose the supported player outcome path rather
than implying uniform target events. Keep health/death state correct independently
of when the next tick runs. Add real Unity tests asserting both session and public
player-target state after nonlethal damage+pause, lethal damage, failed Sink/retry,
and fresh-session restore; preserve brace reduction and enemy death-once behavior.
Coordinate any shared API change with the Lead.

### Visual Acceptance Gap

The owner has not recorded combat readability/appearance acceptance. Both MP4s
are available for review; capture omits OnGUI, so it does not establish HUD
readability. Record owner disposition after reviewing moving combat and the HUD,
or explicit acceptance of those disclosed limitations. Do not infer approval from
successful tests or from T02's water approval.

### Independent Verification

- Unity 6000.3.24f1 current-workspace reruns: T05 PlayMode 15/15, T04 PlayMode
  40/40 and T03 EditMode 33/33 passed, each exit 0. Evidence: lead-T05.xml/log,
  lead-T04.xml/log and lead-T03.xml/log. Existing tests check authoritative player
  health/outcomes but miss R1's stale public player-target state.
- Verified 53 T05 source entries and 291 build entries against their manifests:
  zero mismatches. All 125 inherited T02/T03/T04 asset entries match; T03's
  historical contract-document annotation was excluded as previously recorded.
- Inspected combat/AI/catalog code, tests, capture/restore and integration notes,
  fixture presentation and Editor validation. No shared-contract edit found in
  the verified asset set. Reviewed against manifests, not a project commit.
- Fully decoded cannon.mp4 and repeater.mp4 using ffmpeg -v error -i <clip>
  -f null NUL: both exit 0. Inspected cannon/frame-0045.jpg and
  repeater/frame-0060.jpg: ships, distinct sails and brace ring are visible; the
  repeater sample also shows opposing shot colors. This is sampled inspection,
  not full-motion owner review or a HUD check.

### Acceptance Summary and Limits

Existing projectile sweep, reuse, enemy death-once, loadout, pause and snapshot
fixtures pass. Exposed player read state needs the R1 correction and regression
coverage before technical acceptance. The visual disposition remains separately
pending. No new standalone build/captures, manual device playthrough, performance
soak, durable storage or production integration was run. Original scope limits
remain; the new probe is supplemental review evidence, not a Unity test substitute.

## R1 Developer Handoff - 2026-09-18

Task: T05-R1, Codex (combat engineer). Output: T05-r2, technical submission for
independent Lead review. Parent T05 is not accepted. Owner visual disposition is
separately pending; no successor task was started.

### Inputs, Outputs and Scope

Accepted T03-r1 and T04-r2 were verified against their reports. Input T05-r1
source manifest E3FB7C370D2069C269B199D9B3B0E15AF12AEA4F33232BE02387E560CFE0F8CA
matched all 53 entries; build manifest
A84CE5D312040B2C2AA5C0C3F001CCEB65D3D90BD1914C5F9684AFD09C172F58 matched all
291 entries at pickup. There is no project Git revision; these are snapshot IDs.
The original finding, probe, build, manifests, test logs and videos are preserved.

- Revised source: [R1/source-files.json](R1/source-files.json), 53 entries,
  SHA-256 C477591F827B6838D1E76385F37F0EC65FC0A04E65A0E7255710F74D8B26868C.
- Revised build: [R1/build-files.json](R1/build-files.json), SHA-256
  9E3B36E163F25339D7D0450B2FCA1B2ABED9D7DB6CB19287AF2BF175CA206AEA.
  Executable: `Builds/T05/R1/Combat.exe`, Unity 6000.3.24f1, Windows x64 development.
- [R1/source-check.json](R1/source-check.json): all 125 inherited T02/T03/T04 asset
  entries unchanged; zero missing metadata. All original T05 .meta files preserved.
- [R1/final-audit.json](R1/final-audit.json): 53 source and 291 build entries
  match delivered manifests; both 330-frame CSVs have zero session/target health
  mismatches. Exactly the six declared asset source files differ from r1.

Changed asset paths: Gameplay/Combat/{CombatTarget,CombatWorld}.cs;
Scenes/Tests/T05/{CombatCapture,CombatView}.cs and Editor/CombatAuthoring.cs;
Tests/T05/CombatTests.cs (all beneath Assets/_Game). Evidence runners RunUnity.ps1,
RunCaptures.ps1 and VerifySources.ps1 now support revision-specific output;
INTEGRATION.md, this report, the packet and dispatch board are updated. New
evidence lives in docs/evidence/T05/R1/. No Core/Application, T03/T04, shared
contracts, settings, package, assembly, input, prefab or authored scene changes.

### Delivered Behavior

Player Health queries the bound session's published snapshot on every read;
Defeated derives from Health. Damage still goes only to T04's collector, including
brace reduction. Collection exposes the previous boundary, processed publication
exposes current damage immediately, and rejected publication retains prior state.
No coroutine/subscriber order, polling refresh or second simulation tick is needed.

After committed Sunk removes the expedition, the resolved-voyage outcome supplies
zero health, including after a new voyage begins. Failed Sink saves expose
processed zero health without claiming successful persistence. Successful docking
is healthy: after removal of expedition health it uses a documented maximum-health
terminal representation, not a newly persisted health value. New compositions bind
their own active expedition and immediately expose saved health before arrival.

Died is explicitly enemy-only. The existing session queue remains the source of
committed player Sink notifications; combat never drains it, adds fan-out, or
resolves outcomes. RestoreHealth rejects query-backed player targets; restore the
session first. The fixture HUD now consumes CombatWorld.Player. See the revised
[consumer contract](INTEGRATION.md#player-read-state-and-notifications-r2).
No shared API approval was required or assumed.

### Acceptance Evidence

All read-state checks inspect CombatWorld.Player. Real Unity fixed-step scheduling
and OnCollisionEnter are used; no reflection, manual coroutine advancement or
manual physics simulation substitutes for the scheduler.

| Check | Result and evidence |
| --- | --- |
| R1-C01 | Passed: R1_TickDamagePauseReadsProcessedStateWithoutAnotherTick and R1_PhysicalCollisionDamageAndPauseReadsProcessedState in delivered.xml. Two hits total 10, or one physical callback hits for 10, then adapter pause; after publication session and target are 90 and remain there without another tick. |
| R1-C02 | Passed: R1_CommittedSinkPersistsReadStateAndNewCompositionStartsHealthy and R1_DockingDoesNotNotifyOrExposePlayerDeath. Zero/defeated after acknowledged Sink, one queued Sink, docking healthy, fresh voyage healthy and old sunk target still defeated. |
| R1-C03 | Passed: LethalDamageWithFailedSinkPublishesOneOutcomeWithoutReplay. Processed zero before acknowledgement; three failed retries preserve candidate, position, cooldown and zero health; no committed events or Died. Successful retry immediately keeps zero and exposes exactly one Sink. |
| R1-C04 | Passed via supported idle-boundary captured checkpoint: R1_DamagedCheckpointFailureAndRetryKeepProcessedHealth. Damage 10, failed checkpoint, failed retry and successful retry all expose 90. |
| R1-C05 | Passed: BraceReducesCollectedDamageAndCooldownPreventsReactivation exposes authoritative/target 90 from 40 damage; R1_TickDamagePauseReadsProcessedStateWithoutAnotherTick aggregates 4+6 without double subtraction. |
| R1-C06 | Passed: FreshSessionRestoresLiveProjectilesAndDefeatedEnemy now saves damaged health and asserts 90 immediately on binding while arrival locked, restores, resumes and pauses without death/outcome events. New-composition coverage in R1-C02. |
| R1-C07 | Passed: sink failure/retry, fresh restore and docking tests assert the enemy-only Died contract and undrained session events. Original AC13_DuplicateHitsDeathOnceAndNewOwnerOnReuse and enemy restore death-once tests pass unchanged. |
| R1-C08 | Unity suites and build passed; standalone smoke/capture results recorded below. T05 20/20, unchanged T04 40/40, unchanged T03 33/33. |
| R1-C09 | Owner disposition pending. Revised footage/HUD inspection is recorded below; developer checks do not provide owner approval. Parent acceptance remains gated. |

### Reproduction and Test History

Run from the repository root in PowerShell:

```powershell
./docs/evidence/T05/RunUnity.ps1 -Mode Tests -Revision R1 -Label delivered
./docs/evidence/T05/RunUnity.ps1 -Mode Regression -Revision R1
./docs/evidence/T05/RunUnity.ps1 -Mode Rules -Revision R1
./docs/evidence/T05/RunUnity.ps1 -Mode Build -Revision R1 -Label Build-delivered
./docs/evidence/T05/RunCaptures.ps1 -Revision R1
./docs/evidence/T05/VerifySources.ps1 -Revision R1
```

Final suites: [delivered.xml](R1/delivered.xml) 20/20 PlayMode,
[Regression.xml](R1/Regression.xml) 40/40 PlayMode, [Rules.xml](R1/Rules.xml)
33/33 EditMode, each exit 0, with matching logs. Build-delivered.log records successful
standalone build, exit 0. The pinned editor is D:/u6-t01/Editor/Unity.exe.

Failing-before command was `RunUnity.ps1 -Mode Tests -Revision R1 -Label before`
against unchanged r1 combat implementation with expanded tests: [before.xml](R1/before.xml)
recorded 13 passed, 7 failed, exit 2. Six failures directly show stale health 100
instead of expected 90/0 (brace, fresh binding, failed Sink, committed Sink,
checkpoint and tick/pause). The seventh was a collision-fixture error: adding a
second collider when ShipMotor already requires one produced two callbacks.
That fixture now configures the existing collider and passes one physical callback.
Do not count its initial failure as a product-defect reproduction.

Intermediate after/verified/passed logs record compile errors while adapting the
HUD/capture (removed session local, wrong InputIntent namespace, unavailable
ScreenCapture module). These were fixed locally without dependency changes;
final.xml then passed 19/20 with only the duplicate-collider fixture failure.
verified-final.xml passed 20/20 before final capture/HUD changes; delivered.xml
reran 20/20 successfully after those fixture-only edits. T04/T03 source and tests
remained unchanged throughout.

### Standalone and Visual Evidence

Final RunCaptures.ps1 completed with exit 0 for both loadouts. Each runs a real
standalone tick that collects 10 damage and pauses, asserts target health 90 and
not defeated with no pending step, waits while paused and checks unchanged tick
and health, then resumes combat. Every recorded frame checks target health against
the session. CSV includes targetHealth/targetDefeated alongside session health.

| Evidence | Observed result |
| --- | --- |
| [R1/cannon.mp4](R1/cannon.mp4), cannon/result.txt and combat.csv | 330 frames, 11 seconds at 30 FPS; 14 shots, both enemies defeated, final player health 54. Paused and per-frame read-state checks passed. |
| [R1/repeater.mp4](R1/repeater.mp4), repeater/result.txt and combat.csv | 330 frames, 11 seconds at 30 FPS; 50 shots, both enemies defeated, final player health 54. Paused and per-frame read-state checks passed. |
| [R1/hud-defeated.png](R1/hud-defeated.png) | Actual standalone window, 1920x1080 capture including title bar, readable HULL 0 DEFEATED / CANNON READY / BRACE READY, EXPEDITION ENDED. Inspected through Windows computer-use; not a composited HUD. |

Capture hardware: NVIDIA GeForce RTX 2070 SUPER, Direct3D11, 1280x720 for the
offline clips. No water/rendering-quality reduction. Both final player logs have
no error/exception/ReadPixels match. Both MP4s fully decoded with exit 0 using
`ffmpeg -v error -i docs/evidence/T05/R1/<weapon>.mp4 -f null NUL`.
Sampled cannon/frame-0045.jpg shows ships and active brace; this is sampled visual
inspection, not full-motion manual playback or owner approval. Both videos use
render-target capture and omit OnGUI; the separate actual-window screenshot is
the HUD evidence. Original r1 clips remain accessible but are not the revised smoke.

The attempted framebuffer HUD capture produced black frames and ReadPixels
warnings despite passing gameplay checks. Its failed-hud-cannon.log,
failed-hud-repeater.log and failed-hud-frame.jpg are retained for diagnosis;
that capture path was removed, rebuilt and rerun through the supported render
target. The first actual-window check also exposed overlap with the inherited
bootstrap menu (hud-before.png). CombatView's fixture HUD moved to the upper-right;
the delivered screenshot verifies separation. The inherited menu remains visible.

For HUD reproduction, launch Builds/T05/R1/Combat.exe normally, observe the
upper-right player HUD, and allow enemies to sink the stationary player. Escape
uses the adapter pause path. The automated smoke covers nonlethal pause/retry;
the saved actual-window screenshot covers the terminal defeated presentation.
Only brief window inspection was performed; no manual mouse combat playthrough,
full-motion review, HUD review for every loadout/resolution, or owner visual
disposition is claimed. Owner review of the clips and HUD (or explicit acceptance
of these limitations) is still required before parent T05 acceptance.

### Review and Integration

Lead: review T05-r2 and the explicit resolved-Dock healthy representation, then
record independent disposition. Keep the existing Lead finding as historical r1
evidence. Use INTEGRATION.md to bind one combat composition per expedition; read
after processed publication (HasPendingStep false), and await committed Sink
events for lifecycle success. Do not call RestoreHealth on the bound player.
T08 remains waiting for accepted prerequisites. Existing durability, manual input,
performance, dusk/rough-water and other-hardware limitations remain unchanged.

## R1 Lead Review - 2026-09-18

Technical outcome: T05-r2 passes R1-C01 through R1-C08. The original P2 stale
player read-state defect is closed. No remaining blocking code defect found in
the reviewed remediation. R1-C09 owner visual disposition remains pending, so
T05 and T05-R1 stay in review, not done. T08 remains waiting for all prerequisites;
T06/T07 readiness is unaffected.

### Technical Disposition

The query-backed player reads the session's published health for its bound voyage,
and retains zero/defeated after that voyage resolves Sunk. It does not collect or
apply damage a second time, depend on another tick, or drain outcome events.
Enemy-only Died and session-owned committed Sink notifications are explicit.
Failed Sink saves expose processed zero health without claiming commit success.

The documented maximum-health representation after successful docking is accepted
as terminal read state for a disposed combat composition, not an at-sea heal or
a new persisted health rule. Consumers must create a new composition per voyage
and follow INTEGRATION.md's publication/outcome distinction. No shared API change
was needed or approved.

### Independent Evidence

- Reran `RunUnity.ps1 -Mode Tests -Revision R1 -Label lead-review`: 20/20 passed.
- Reran `RunUnity.ps1 -Mode Regression -Revision R1 -Label lead-t04`: 40/40 passed.
- Reran `RunUnity.ps1 -Mode Rules -Revision R1 -Label lead-t03`: 33/33 passed.
  All exited 0; XML/log pairs are in R1/ under those labels. Tests cover both
  real tick/collision publication, pause, brace/multiple hits, failed Sink/retry,
  checkpoint failure, docking, fresh restore and new-voyage read-state isolation.
- Verified all 53 revised source and 291 revised build manifest entries: zero
  mismatches. All 125 inherited T02/T03/T04 asset entries also match. The historical
  T03 contract annotation remains excluded as previously disclosed.
- Inspected the baseline XML: 13 passed, 7 failed. As the handoff explains, six
  failures concern stale state and one concerns the original collision fixture;
  that fixture failure is not counted as evidence of the production defect.
- Independently checked both 330-row revised combat CSVs: zero authoritative/
  exposed-health or defeat-flag mismatches. Fully decoded both revised MP4s
  using ffmpeg -v error -i <clip> -f null NUL, both exit 0.
- Inspected R1/hud-defeated.png: actual-window screenshot shows HULL 0 DEFEATED,
  EXPEDITION ENDED and the separate upper-right HUD without overlap with the
  inherited menu. This verifies the supplied image, not a new manual playthrough.

### Remaining Acceptance

R1-C09 evidence is available: [cannon](R1/cannon.mp4),
[repeater](R1/repeater.mp4), and [HUD](R1/hud-defeated.png).
The owner must record acceptance, requested changes, or explicit acceptance of
the disclosed visual-review limitations. No owner approval is inferred from
this technical review. No additional code change is requested by this review.

No new build/capture, full-motion aesthetic review, physical-device playthrough,
durability test or performance soak was performed. Tests ran in the current
workspace, not a clean import. Manifest identities, not a project commit, identify
the submission. Only review/status documentation was changed; production and
test-source files remain untouched.

## Owner Disposition - 2026-09-18

After being told that only combat/HUD visual approval remained, the owner stated
"approved". This records approval of T05-r2's combat/HUD presentation with the
disclosed review limitations and closes R1-C09. Combined with the independent
technical review above, T05 and T05-R1 are accepted and marked done.

Accepted source/build identities are the R1 manifests recorded in this report.
Approval does not assert that additional manual, full-motion, durability or
performance checks occurred. Existing scope limits and T02's provisional visual
limitations remain recorded. No production changes or new tests were performed
to record this decision. T06/T07 remain ready; T08 still requires their acceptance.
