# T02 Developer Handoff

Status: done, provisionally accepted by the owner with documented limitations.
Assignee: Codex (developer). See the final owner disposition below; earlier pending
gate statements record the handoff/review state before approval.
Date: 2026-09-18. Assignment: user requested implementation of task 02.

## Inputs and Ownership

T01 was accepted by Lead at snapshot
`172bcab89cc5de8021d713527b906d4700f42854`. `inputs.json` records the actual
workspace target sheet, accepted report, package pins, HDRP asset, inherited scene
and executable SHA-256 hashes before implementation. No parent Git commit exists.
The project still resolves to the enclosing user-directory Git repository; this
task does not stage or commit that repository.

Editor: Unity 6000.3.24f1; HDRP 17.3.0; reference hardware and D01-D06 remain as
approved in TARGETS.md. No shared contract, package, project setting or Bootstrap
source change is authorized or needed. Camera overrides, volume profile, art and
test-scene composition live in T02-owned paths.

## Delivered Behavior

- A standalone ocean test scene with cutter, island, offshore islet, rocks, pier,
  harbor light, independent dummy attack marker and a temporary 60-second route.
- HDRP water with absorption/refraction, shallow shelf, caustics, foam decals,
  stern wake, shadows, sky/reflection probe and a closed hull exclusion mesh.
- Daylight, dusk and rough conditions selected by command line. No production
  combat, progression, controls or fluid solver is implemented.
- Reusable ship/island prefabs and variants, flat materials and procedural visual
  bob/pitch/roll. WORKFLOW.md describes authoring and reuse.
- Per-frame standalone CSV collection, environment metadata, separate offline
  route footage and repeatable PowerShell capture/summary commands.

The final foam stamp is `Presentation/Water/FoamStamp.shader`, a small appearance
shader feeding HDRP's existing WaterDecal atlas. It implements the pinned 17.3
`Foam` pass and green/blue surface/deep output channels, with an explicit triangle
UV and a reusable soft footprint texture. The engine sample rendered box stamps
but not its sphere/texture modes in this fixture; those unsuccessful captures
are retained. The engine package was not patched. This shader's tag/pass/channel
contract must be checked on any future HDRP upgrade. It is not a fluid solver or
replacement water renderer.

## Route and Camera

`OceanProof.Route` fixes the route in meters and seconds:
0-15 idle at (7,-9); 15-30 travel north to (7,12), 1.4 m/s;
30-45 semicircle about (16,12), radius 9 m, approximately 1.885 m/s;
45-60 return from (25,12) to (7,-9), approximately 1.844 m/s.
The route is a visual fixture with instantaneous heading changes at segment joins,
not production ship steering. Only its root translates in XZ and rotates in yaw.
The collider remains on that root; the visible child alone bobs/tilts. The attack
marker uses stable root aim, independent of waves and model motion.

D05 starts and remains at 60 degrees downward. Perspective FOV is 50 degrees;
camera offset is (0,49,-28.29016) m. The ship occupies approximately 8-9% of the
1080p image height in the harbor captures. No camera-angle adjustment was made.
Lighting/exposure iterations corrected clipped island highlights and dark dusk
water; these were visual refinements, not performance-driven quality reductions.

## Reproduction

Build with the editor closed, from the project root:

```powershell
$p = Start-Process D:/u6-t01/Editor/Unity.exe -WindowStyle Hidden -Wait -PassThru -ArgumentList '-batchmode','-quit','-projectPath',(Get-Location).Path,'-executeMethod','PirateGame.Presentation.Water.Editor.OceanProofAuthoring.Build','-logFile',"$PWD/docs/evidence/T02/rebuild.log"
if ($p.ExitCode -ne 0) { throw 'Build failed' }
./docs/evidence/T02/RunProof.ps1 -Mode benchmark -RunName repeat-01
./docs/evidence/T02/Summarize.ps1 -CaptureDirectory ./docs/evidence/T02/repeat-01-benchmark
./docs/evidence/T02/RunProof.ps1 -Mode footage -RunName repeat-01
```

Run names must be new; the runner refuses to overwrite an existing evidence
directory. `OceanProofAuthoring.Create` was one-time authoring and refuses an
already authored scene. `Refine` is the recorded authoring iteration, not a normal
build step. Normal builds consume serialized assets and call `Verify` first.

The interactive player must have a visible window. Hidden launches were found to
suppress rendering despite runInBackground; black captures from those diagnostic
attempts are retained and are not visual evidence or performance runs.

Benchmark: native 1920x1080, D3D11, 100% render scale, no VSync/frame cap, no
dynamic resolution/upscaling/ray tracing, High256 water. Each condition warms for
60 seconds, then captures three 60-second routes. CSV wall-frame intervals include
route stalls. CPU/GPU timings overlap and must not be summed. Missing timing is
-1. Memory fields are Unity allocated/reserved memory, not total process or VRAM.
The development build enables FrameTimingManager without changing shared settings.
An `overhead` mode retains wall-frame collection but disables extra diagnostic
recorders and memory queries for a separate warmed 60-second comparison.

Footage uses the same camera/scene/settings at 1920x1080, an explicit render target,
30 fixed simulation steps per second, JPEG 95 intermediates and H.264 CRF18 output.
It is an offline motion/readability capture, not a real-time FPS demonstration.
Screenshot readback/encoding never runs during benchmark capture.

To preview the route interactively, launch `Builds/T02/OceanProof.exe`; it loops
daylight by default. Add `-t02-condition dusk` or `-t02-condition rough` for the
other conditions. This fixture has no production steering/combat controls.
To validate a new three-condition recording, run
`./docs/evidence/T02/ValidateFootage.ps1 -CaptureDirectory ./docs/evidence/T02/repeat-01-footage`.

## Verification and Gates

### Acceptance Checklist

| Check | Developer result / remaining review |
| --- | --- |
| Gameplay-camera ship, marker, hull and coherent art | Sampled final frames in all three conditions retain readable ship/marker, visible wake and no obvious water on deck. Full-motion owner/Lead review remains pending; sampling does not establish absence of transient defects. |
| Approved prototype median/p95 targets | Passed all nine final-revision routes, with raw per-frame timing retained. |
| Lead measurements and owner visual/production dispositions | Not yet performed. GATE-01/02/03 remain pending; T03 stays waiting. |
| D05 camera baseline and adjustments | Passed: fixed 60-degree pitch, approximately 8-9% ship height; no pitch adjustment. Lighting/reflection corrections documented. |
| AC-14 temporary visual driver | Passed automated root/collider/aim isolation checks; production comparison deferred to T04 as specified. |
| Reusable art and effort | Base/variant prefabs and recorded automated demonstration delivered. Full human workflow timing and owner suitability are not claimed passed; see WORKFLOW.md. |
| Build and navigation regression | Passed corrected standalone build and existing keyboard integration test (1/1). |
| Handoff and reproducibility | Passed: report, scripts, raw measurements, workflow, final identities and validated videos delivered. |

### Final R2 Measurements

Corrected final-build benchmark completed: all nine routes met the approved prototype
median <=16.7 ms / p95 <=20 ms targets. These are not release requirements or
measurements of future production gameplay.

| Condition/run | Frames | Median ms | p95 ms | p99 ms | Max ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Daylight 1 | 12741 | 4.648 | 5.767 | 6.295 | 7.446 |
| Daylight 2 | 12652 | 4.662 | 5.827 | 6.375 | 8.258 |
| Daylight 3 | 12641 | 4.687 | 5.825 | 6.387 | 8.395 |
| Dusk 1 | 12671 | 4.660 | 5.824 | 6.359 | 8.070 |
| Dusk 2 | 12688 | 4.659 | 5.784 | 6.331 | 8.317 |
| Dusk 3 | 12684 | 4.649 | 5.799 | 6.348 | 8.339 |
| Rough 1 | 12671 | 4.666 | 5.787 | 6.320 | 7.218 |
| Rough 2 | 12660 | 4.662 | 5.806 | 6.354 | 7.871 |
| Rough 3 | 12627 | 4.687 | 5.853 | 6.424 | 8.383 |

All runs had zero frames >33.3 ms. CPU-frame medians were 4.647-4.683 ms and
GPU medians 4.495-4.543 ms, with valid CPU/GPU samples throughout. The separate
render-thread recorder was unavailable and is -1 in CSV / null in summaries;
no render-thread result is invented. Main-thread timing is available. CPU-frame
timing includes waits, so similar CPU/GPU values alone do not prove a CPU-bound
workload. No target miss requires a bottleneck/quality disposition.

Peak Unity allocated memory was 175,285,711 bytes (~167.2 MiB); reserved memory
peaked at 365,064,192 bytes (~348.2 MiB). These exclude process/driver/GPU memory
outside Unity's memory accounting. All route spikes are retained. No runtime
error or warning appeared in the three benchmark player logs.

Evidence: `final-r2-benchmark/summary.json`, nine per-condition/run CSVs, each
condition's environment.json/player.log/process.json, and startup.json. The
reference machine is Ryzen 7 5800X3D / RTX 2070 SUPER / 32 GiB / Windows 11;
player logs record driver 32.0.16.1664 (NVIDIA 616.64). Other existing desktop
applications were not terminated; no other Unity editor/player or video encoder
ran during these measurements. No screenshots were taken during these runs.

Startup before scene telemetry was 4.433 s daylight, 4.004 s dusk and 4.025 s
rough. Each then had a separate 60-second warm-up, excluded from the route CSVs.
Per-condition process wall time was approximately 244.5-245.0 seconds.

The final separately warmed daylight `overhead` comparison recorded 12,689 frames:
median 4.657 ms, p95 5.793 ms, p99 6.313 ms, max 7.194 ms, zero >33.3 ms.
This falls within the range of the three instrumented daylight runs. It is a run-to-run comparison,
not proof of zero overhead; no causal cost is resolved at this variation level.
It isolates the added recorders/memory queries, not intrinsic development-player
or enabled-engine-timing overhead. No overhead was subtracted from any result.
See `final-r2-overhead/summary.json` and its raw CSV/environment/process log.

Successful build logs contain AC-14 visual-driver verification: bobbing
0 versus 2 m, swell amplitude 0 versus 1 and disabled ripples leave root matrix,
collider size and marker matrix unchanged at five route points. 6001 route samples
remain in XZ. Production rules/hit resolution do not exist yet; the T04 comparison
remains explicitly out of scope.

GATE-01 owner visual disposition: pending. GATE-02 Lead measurement review and
owner disposition: pending. GATE-03 owner workflow disposition: pending. No gate
acceptance is inferred from compilation, screenshots or developer judgment.
T03 remains waiting.

### Visual Evidence

Final clips: `final-r2-footage/daylight/daylight.mp4`,
`final-r2-footage/dusk/dusk.mp4`, and `final-r2-footage/rough/rough.mp4`.
Each covers the same full 60-second route, including idle, departure, turn and
return, at 1080p/30 fps. Raw JPEGs remain beside the clips.

`final-r2-footage/video-validation.json` records successful complete decoding of
all 1800 frames in each clip, correct 1920x1080/30 fps/60-second format, no black
frames, and 1799 changed adjacent-frame pairs per clip. The validation pipeline
exited 0 at approximately 16:40 CEST; video SHA-256 hashes and full signal logs
are retained. These pixel tests establish nonblank motion, not aesthetic approval.

Developer inspection sampled daylight frames 0000/0900/1200/1650, dusk frames
0000/0450/1200/1650, and rough frames 0000/0450/1200/1650. Ship and marker are
readable, the moving samples show stern foam, and no obvious water through the
deck was seen in those samples. Shallow shelves differ visibly from open water.
Dusk samples retain depth contrast and the corrected reflection boundary.
These are sampled-image observations plus automated motion checks, not a claim
that every frame was visually reviewed or that shadow flicker is ruled out.
Owner/Lead moving-footage review is still required.

The offline capture begins with a residual stern-foam patch from warmup, which
fades while idle; this does not represent ship movement. The fixture's abrupt
heading changes and repeated shoreline foam are disclosed visual limitations.
FFmpeg reports a deprecated JPEG pixel-format conversion warning during encoding;
the clips are subsequently decoded for integrity checks.

## Diagnostic History

- author.log: failed compilation because minimal packages omit ScreenCapture.
  Capture now uses the existing Texture2D image-conversion API.
- author-2.log: failed compilation on an internal HDRP resource type. Authoring
  now loads the pinned package's shader asset through AssetDatabase.
- author-3.log: successful authoring. Subsequent numbered build/refine logs retain
  the visual iterations and AC-14 checks.
- first-look-smoke and render-target-smoke: black hidden-window captures, invalid
  for visual acceptance. visible-smoke and final-look-smoke retain lighting tests.
- route-check-footage: interrupted PNG capture while investigating the absent
  wake; partial frames are diagnostic only, not complete delivered footage.
- foam-check-footage: complete daylight diagnostic with visibly square foam.
  delivery-footage/final-footage: superseded or interrupted capture attempts,
  not final evidence. foam-strength-smoke also retained the absent textured wake.
  stamp-smoke verified the corrected appearance shader. `verified-footage` is
  the pre-reflection-fix set. Final recordings use `final-r2-footage`, the same
  corrected build measured by `final-r2-benchmark`.

## Output Identity and Changed Paths

Final corrected build log: `build-reflection.log`, 203,596,608 reported bytes.
Executable: `Builds/T02/OceanProof.exe`; keep its adjacent data/DLL/notices folders.
The executable is Unity's generic launcher, so the game assembly and data-file
inventory identify the game revision, not the launcher hash alone.

- Game assembly SHA-256:
  `46E436A75B8E4CF9B5FAE02C42EC7B9B9E96077289EB5692B12C7AEB508B8B84`.
- `source-files.json` SHA-256:
  `6E60AA8FEDBFB3BCFC0E0C0549D194CD8B08F894A12C5B2BF8698F3D4D2CE4E8`.
- `build-files.json` SHA-256:
  `809ACC3A15A75EF2F9E8EBE26F1A2371E784598248BE4611DEDA4AD20F000187`.

Earlier identity manifests are retained with the `pre-reflection-` prefix.

Changed paths:

- Assets/_Game/Presentation/Water/: temporary driver, foam appearance shader and
  Editor authoring/build/verification tools, with metadata.
- Assets/_Game/Art/Prototype/: hull/coast meshes, materials, foam footprint,
  copied-and-owned volume profile, base and variant prefabs, with metadata.
- Assets/_Game/Scenes/Tests/T02/WaterTest.unity: test composition and camera
  overrides; original scene metadata/GUID retained.
- docs/evidence/T02/: this handoff, workflow, reproducible scripts and evidence.
- docs/tasks/T02-ocean-proof.md and docs/tasks/README.md: assignment/status.

`metadata-check.json` found no missing metadata among 24 checked source assets.
`shared-boundary-check.json` compares 55 package/settings/Bootstrap files with
the accepted T01 source snapshot, normalizing CRLF/LF, and found no changes.
`script-syntax.json` records zero parser errors for all three evidence scripts.

## Known Limitations

The shore foam has a repeated lobed pattern, rocks use simple block forms, and
the route has instantaneous heading changes at segment joins. These are visible
prototype limitations for the owner to assess, not claimed final art polish.
The custom foam stamp depends on HDRP 17.3's atlas contract as described above.
The return menu is intended only when Bootstrap is included in a build.
No production simulation, release/minimum hardware, controller, or human asset
authoring-speed acceptance is claimed. Automated variant timings exclude setup
and shared rendered QA; owner workflow suitability remains an open gate.

## Integration

Keep assets and their metadata together. Builds/T02 is a separate test player;
the accepted T01 Builds/Windows output is preserved. WaterTest keeps its inherited
scene GUID and remains at the existing T02 scene path. Its Back/Quit menu is
preserved for builds containing Bootstrap and hidden only in the direct T02
player. The production Bootstrap scene is not edited. This visual fixture is
not a replacement gameplay scene.

Independent Lead review: recorded below. Owner review: not yet recorded.

## Reflection Correction and Navigation Regression

Inspection of the dusk turn at 40 seconds revealed a rectangular dark boundary
in far water. The boundary matched the original 120x60x120 m reflection volume.
The T02-owned probe was expanded to 500x100x500 m with a 40 m blend distance;
the comparison at the same 40-second route pose no longer shows that rectangle.
See verified-footage/dusk/dusk-1200.jpg before and reflection-smoke/dusk/dusk.png
after. The corrected build log is build-reflection.log. No water quality or
shared setting was reduced. Final-r2 captures measure and record this revision;
the earlier `measured-benchmark`, `measured-overhead` and `verified-footage`
directories remain labeled pre-fix comparisons, not final-revision evidence.

The existing Bootstrap keyboard integration test ran in a focused Windows player
against the corrected scene: 1 passed, 0 failed; editor exit 0. It checks initial
focus, Tab, Shift+Tab, Enter, Open ocean, Back, and focus restoration. Evidence:
navigation-tests.xml and navigation-tests.log. No test assertions were changed.

Exact regression invocation uses the same editor/project arguments as the build,
with `-runTests -testPlatform StandaloneWindows64 -testResults
docs/evidence/T02/navigation-tests.xml -logFile
docs/evidence/T02/navigation-tests.log` and without `-quit` or `-executeMethod`.
The editor-managed test player was closed before the final-r2 benchmark began.

## Lead Review - 2026-09-18

Outcome: technical evidence reviewed; T02 remains in review, not accepted. T03
remains waiting. No blocking runtime/code defect was identified in the reviewed
source. The following acceptance gaps are distinct from implementation failures.

### Acceptance Blockers

1. GATE-01 has no owner visual disposition. The three final-r2 clips are the
   reviewable output, but neither successful compilation nor timing results decide
   whether this meets the owner's primary water-quality requirement. Review the
   complete clips and record accept/revise, including shoreline foam repetition,
   turn discontinuities, readability and any transient hull/shadow artifacts.
2. GATE-03 has no owner workflow disposition. WORKFLOW.md lines 47-69 explicitly
   exclude human learning/iteration and separate per-variant rendered QA from the
   subsecond automated timings. Those numbers cannot demonstrate the 2-hour/4-hour
   full workflow. Record a representative end-to-end trial or an explicit reviewed
   limitation/revision under D04, plus the owner's judgment of manageability.

The timing targets are workflow/prototype targets, not hard product requirements.
Neither gap means the game failed or authorizes automatic quality reduction.

### Independent Evidence Checks

- All 49 source-manifest entries and 291 delivered-build entries match their
  recorded hashes. All three final video hashes match video-validation.json.
  Manifest hashes match the final R2 identities in this report.
- Compared Packages, ProjectSettings and Bootstrap against the accepted T01 fixture
  with CRLF/LF normalization: no differences. No shared implementation changes found.
- Recomputed nearest-rank median/p95 directly from all nine CSVs without replacing
  developer summaries. Each run spans 60.001-60.004 seconds; all meet initial targets.
  Median range: 4.6479-4.6870 ms; p95 range: 5.7666-5.8529 ms; maximum: 8.3946 ms.
- Reviewed telemetry and capture code: benchmarks avoid image readback/encoding;
  unavailable render-thread timing is disclosed, memory is Unity-only, CPU/GPU
  overlap is acknowledged, and offline footage is not presented as real-time FPS.
- Independently decoded each complete final MP4 with ffmpeg -v error -i <clip>
  -f null NUL: all three exited successfully. This verifies decoding, not aesthetics.
- Inspected final daylight frames 0000/0900, dusk 0450/1200, rough 0000/1650.
  Ship/marker are readable in these samples; no obvious deck-water intersection
  was observed. The repetitive shoreline foam pattern is visible. This was sampled
  inspection, not full-motion visual clearance or a claim of no transient flicker.
- Reviewed the unchanged keyboard regression source and passing 1/1 result in
  navigation-tests.xml. Did not repeat physical keyboard input or benchmark runs.

### Gate Disposition

Fresh independent verification: copied Assets/Packages/ProjectSettings into
.tools/T02-lead-review without Library and invoked the pinned editor's
OceanProofAuthoring.Verify in batch mode. It compiled/imported and exited 0;
lead-verify.log records passing AC-14, 6001 XZ route samples, camera pitch and
fixture-reference checks. Verification-only mutations stayed in that disposable
copy; this was not a new standalone build or a new performance capture.

| Gate/check | Lead result | Remaining action |
| --- | --- | --- |
| GATE-01 | Pending owner/full-motion visual disposition | Review final clips; record accepted limitations or requested revisions |
| GATE-02 | Technical measurement review passed; all nine initial targets met | Owner gate disposition remains pending under the task packet |
| GATE-03 | Reuse demonstration exists; full human workflow not established | Owner workflow review and timed trial or explicit limitation/revision disposition |
| D05 | Initial camera configuration and sample framing consistent | No final-camera commitment inferred |
| AC-14 fixture | Passed independent fresh-import verification | Production simulation check still belongs to T04 |
| Scope/identity | Passed inspected boundaries and hashes | Preserve metadata and pin HDRP shader contract |

The code review is against the current source manifest and accepted T01 fixture,
not a parent-repository commit. No production code/assets/settings/tests were edited.
Only review evidence and planning/status documents were updated.

Optional visual refinement: replace the repeated isolated shoreline stamps with
less uniform foam coverage. This is review feedback, not a newly invented mandatory
feature; the owner decides whether the disclosed prototype limitation is acceptable.

## Owner Disposition - 2026-09-18

Following the Lead review, the owner stated: "approved for now".
Lead records T02 as done for prototype progression, with GATE-01/02/03 accepted
for now on the reviewed evidence and disclosed limitations. This supersedes the
pending dispositions above and releases T03 for assignment, not execution.

- GATE-01: provisional visual acceptance, including repetitive shoreline foam.
  This approval does not establish that a full-motion visual inspection occurred
  or that transient artifacts have been ruled out.
- GATE-02: accepted prototype measurements; all nine measured runs meet D02.
  No release-hardware or release-performance commitment is implied.
- GATE-03: accepted for now with human end-to-end workflow effort unverified.
  Automated generation times are not proof of the D04 human workflow targets.

Retain full-motion visual inspection and a representative human authoring trial
as follow-up validation when revisiting visual/art suitability for the slice.
These limitations do not block T03. D01-D06 qualifications remain unchanged;
this approval does not freeze the final camera, art, or water quality.
Accepted output is the final-r2 evidence and source/build manifests identified in
this report. No implementation or new verification was performed for this approval.
