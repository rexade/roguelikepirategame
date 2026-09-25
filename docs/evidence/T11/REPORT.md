# T11 Report: Integrated Slice Verification

Task: [T11](../../tasks/T11-slice-acceptance.md). Run 2026-09-25 by the Lead (Claude
Opus 5.5) on the integrated build that contains T08, T09 and T10. Status: **review**.
The game owner's visual/gameplay review (GATE-01) and final acceptance are pending;
nothing below claims them.

## Build and Environment

- Source: project Git repository, T11 commits after `c909f1a`.
- Measured player: `Builds/Benchmark/PiratePrototype.exe`, Windows x64 Mono
  **development** build (FrameTimingManager enabled, like the T02 benchmark).
  The playable release build is `Builds/Game/PiratePrototype.exe`.
- Hardware (from `*-environment.json`): AMD Ryzen 7 5800X3D, NVIDIA GeForce RTX
  2070 SUPER (8 GB), 32 GB RAM, Windows 11 26200, D3D11.
- Settings: 1920x1080 windowed, VSync off, no frame cap, HDRP settings as
  committed (ray tracing off, dynamic resolution off). Unity 6000.3.24f1.
- Render-setting change made during T11: HDRP `maximumWaterDecalCount` 48 -> 96,
  shore foam re-spaced (fewer, larger stamps) and the water decal region returned
  to 150 m. Reason: the production archipelago exceeded 48 visible water decals,
  so HDRP silently dropped foam and logged a warning every frame in development
  builds. After the change the warning count in a full lap is 0.

## Representative Route (GATE-02 re-run with real systems)

`tools/RunBenchmark.ps1` launches the player once per condition. An autopilot
sails the validated T06 outward-and-return route (dock -> north past encounter 01
-> north-east near encounter 02 and the Galewater border -> south past the wreck
-> home) with real physics, AI, combat (autofire at the nearest enemy in range),
salvage views, HUD, weather preset and region streaming (Galewater loads near the
border and unloads on the way back), then docks with the normal assisted docking.
The first lap is warm-up; three further laps are recorded per condition, each
from embark to the committed Dock. Benchmark voyages use a 2000-point hull so every
lap finishes the same route (documented deviation; rendering and simulation load
are unchanged). Per-frame times include every frame; nothing is discarded.

Nine recorded laps (three per condition), each ~40 s from embark to the committed
Dock, all completing the route with no stranding:

| Run | Frames | Median ms | p95 ms | p99 ms | Max ms | >33.3 ms | CPU med | GPU med | Peak alloc MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| daylight 1 | 9424 | 4.222 | 5.083 | 5.525 | 21.558 | 0 | 4.245 | 4.055 | 208.4 |
| daylight 2 | 9470 | 4.204 | 5.071 | 5.438 | 21.241 | 0 | 4.224 | 4.033 | 208.4 |
| daylight 3 | 9402 | 4.243 | 5.113 | 5.527 | 22.185 | 0 | 4.261 | 4.066 | 208.5 |
| dusk 1 | 9493 | 4.191 | 5.054 | 5.510 | 20.448 | 0 | 4.215 | 4.020 | 208.6 |
| dusk 2 | 9492 | 4.190 | 5.073 | 5.508 | 22.730 | 0 | 4.205 | 4.011 | 208.6 |
| dusk 3 | 9503 | 4.194 | 5.033 | 5.460 | 23.062 | 0 | 4.215 | 4.010 | 208.6 |
| rough 1 | 9372 | 4.241 | 5.130 | 5.552 | 19.757 | 0 | 4.269 | 4.080 | 209.2 |
| rough 2 | 9411 | 4.225 | 5.079 | 5.513 | 21.959 | 0 | 4.240 | 4.051 | 209.2 |
| rough 3 | 9447 | 4.208 | 5.007 | 5.364 | 21.467 | 0 | 4.194 | 4.022 | 209.2 |

All nine runs meet the D02 prototype targets by a wide margin (median ~4.2 ms,
p95 ~5.1 ms). Every run has six frames above 12 ms at the same route moments:
Galewater streaming in (~15 s; region load 8.6-16.9 ms in `*-laps.txt`), its ships
being populated (~18 s), Galewater being retired with its capture checkpoint
(~28 s), the 10-second periodic checkpoints (e.g. ~38 s) and the docking commit
(~40 s). These are synchronous save/stream costs of 16-23 ms; none exceeded
33.3 ms. Moving saves off the frame would need a contract change (commits are
deliberately synchronous in r1). No exceptions and no water-decal warnings in any
player log. CSVs: `docs/evidence/T11/benchmark/<condition>-run<n>.csv`, summary in
`summary.md` / `summary.json`.

Targets (D02, prototype benchmark only): median <= 16.7 ms, p95 <= 20 ms.
CPU and GPU medians overlap in time and must not be added.

## Diagnostic Stress Workload

`-stress` multiplies Homeward's encounters to 12 ships, adds 100 generated
barrels (pickups) and makes enemy guns fire 20x faster, with a 500000-point hull.
This is a diagnostic load, not a content cap. Results are reported separately:

| Run | Frames | Median ms | p95 ms | p99 ms | Max ms | >33.3 ms | Peak live shots | Ships loaded | Pickups |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| stress 1 | 10647 | 5.110 | 6.701 | 7.769 | 29.763 | 0 | 56 | 18 | 111 |
| stress 2 | 11601 | 4.713 | 6.553 | 7.719 | 143.878 | 2 | 56 | 18 | 111 |

The stress lap loiters for 15 s inside the escorted pair's reach. It reached 12
Homeward ships (18 with Galewater streamed), 111 pickups and at most 56 live
projectiles, not the 100 the packet names: ships only fire while the player is in
their engagement range, and even at 33x fire rate that capped the concurrent count.
Run 2 had one 144 ms main-thread stall (plus one 48 ms frame) at the allocation
high-water mark (~224 MB) while CPU/GPU frame times stayed ~4.5 ms, which is
consistent with a garbage-collection pause. Finding for follow-up: reduce per-frame
allocations (LINQ in the director's view updates and presenter). The representative
route shows no such stall.

## Cross-Region Travel

PlayMode `CrossRegionTravelTests.TravelBetweenHomewardAndStormwatchStreamsEachDestination`:
buy Navigator's Charts, sail into Galewater, claim and moor at Stormwatch
(Homeward retires), fast-travel home through the harbor screen (the arrival waits
for Homeward to stream back in), travel back to Stormwatch, then reload: docked at
Stormwatch with Galewater loaded, one shared bank throughout.

## Invariant and Acceptance-Case Coverage

| Rule / case | Automated evidence |
| --- | --- |
| INV-01 one campaign owner | `WorldAuthoring.Validate` (exactly one GameDirector); director owns the only session; all harbor views, HUD and travel read it (T07/T08/T09 tests) |
| INV-02 one expedition, one outcome | T03 rules; T08 `AC01_AC02_…`, `AC09_…`; T04 tick protocol |
| INV-03 validate before mutation | T03; T07 purchase/loadout; T08 `AC17_…`; T09 activation/travel rejections |
| INV-04 explicit accounting | T03; T08 full loop (banked amounts equal cargo) |
| INV-05 indivisible pickups | T03; T06 `FullCargoRejectsWholeBundle`, HUD CargoFull |
| INV-06 docking banks once | T03 `AC01_…`; T08 `AC09_…`, `AC10_…` |
| INV-07 sinking keeps a playable campaign | T08 `AC06_…`, `SinkingLosesCargoKeepsProgressAndReturnsHome` |
| INV-08 shared hub progression | T07; T09 `AC07_…`; T11 cross-region travel |
| INV-09 travel cannot bypass risk | T09 `AC08_…`, `AC18_…` |
| INV-10 stable geography | T10 `GeographyAndSalvageNeverMoveBetweenVoyages`; T06 route |
| INV-11 scene lifetime ≠ entity lifetime | T06 recreation; T08 resume; T10 `LeavingAndReturning…`, `ResumeInsideGalewater…` |
| INV-12 valid, immutable definitions | T03 definitions; T10 `AC15_…`; build/play validation hooks |
| INV-13 owned, compatible loadouts | T03; T07 `AC16_…`; T05 loadout tests |
| INV-14 presentation cannot change simulation | T02/T04 AC-14 checks; weather/audio/wake/presenter take no rule input (code review) |
| INV-15 bounded combat effects | T05 duplicate-hit, pool reuse, swept-hit (AC-13) |
| INV-16 coherent snapshots | Checkpoint-before-pickup/activation; T08 resume; T10 capture before retiring a region |
| INV-17 recoverable save failures | T08 `AC09`, `AC10`, `AC12` (x3) |
| INV-18 rules independent of rendering | `PirateGame.Core` and `PirateGame.Persistence` compile with `noEngineReferences` |
| AC-01…AC-18 | All mapped above; AC-13 (T05), AC-14 (T02/T04), AC-05 with streaming (T10), AC-07/AC-18 across regions (T09, T11) |

Test totals at this revision: EditMode 69/69, PlayMode 83/83 (152 tests).

## Gate Status

| Gate | Status |
| --- | --- |
| GATE-01 ocean/art quality at gameplay scale | Owner review pending. Standalone screenshots for daylight, dusk and rough seas are in `docs/evidence/T08/captures-audio-weather/` and `docs/evidence/T10/captures/`. |
| GATE-02 measured performance | Measured above on the reference machine; see the target comparison. |
| GATE-03 practical asset production | Unchanged since T02 (approved for now); all new art is generated from the T02 kit by `WorldAuthoring`. |

## Known Limitations and Remaining Work

- No owner playtest yet; all play evidence is automated (scripted intents and
  test relocations). Physical keyboard/mouse play is the next step.
- The T01 standalone keyboard test was updated for the new title menu but needs a
  focused standalone test player to run.
- Enemy AI is simple (pursue / keep range with short obstacle casts); ships can
  still bump islands.
- No map screen, no controller support (deferred by D01), placeholder primitive
  art for barrels/wrecks, procedural sound only.
