# Prototype Targets

Status: accepted for prototype target definition; D07 resolved by owner delegation. Updated 2026-09-18.
Scope: T00 target definition only. No engine setup or rendering acceptance.

## Decision Register

Source: the game owner's explicit D01-D07 response in this conversation on
2026-09-18, recorded below, followed by "you choose engine and create tasks".
The later instruction delegates D07 to the lead, who selects Unity 6 LTS/HDRP.
This is explicit owner authorization, not retroactive approval of the earlier
Lead-only proposal. No visual/performance gate is passed by accepting targets.

| ID | Proposed value | Classification and authority | Consumer |
| --- | --- | --- | --- |
| D01 | Windows single-player; keyboard/mouse first; controller beyond prototype | Owner approved | T01, T04, T07, T11 |
| D02 | This workstation; 1080p, median <=16.7 ms, p95 <=20 ms | Owner approved as initial prototype benchmark only; report misses/bottlenecks, do not automatically reduce quality | T02, T11 |
| D03 | No paid assets/tools; simple low-poly geometry, pixel-inspired textures/materials, procedural motion where practical | Owner approved | T01, T02 |
| D04 | Repeatable ship variant <=2 hours; small island variant <=4 hours | Owner approved as workflow targets, not hard product requirements; analyze/revise overruns | T02, GATE-03 |
| D05 | Start around 60 degrees downward; ship roughly 8% of image height | Owner approved initial T02/GATE-01 camera baseline; testing may justify changes | T02, GATE-01 |
| D06 | Defer minimum/release hardware until representative slice measurements | Owner approved | T11 |
| D07 | Unity 6 LTS with HDRP and C# for prototype | Lead selection under explicit owner instruction: "you choose engine and create tasks" | T01, T02 |

Confirmed requirements remain those in [CONTEXT.md](../CONTEXT.md): excellent water,
convincing illumination/shadows, manageable art, a fixed archipelago, ship/hub
progression, and shared unlocks. These proposals do not replace those requirements.

## Observed Workstation

Read-only inventory on 2026-09-18 using Win32_Processor, Win32_ComputerSystem,
Win32_OperatingSystem, Win32_VideoController, and nvidia-smi:

| Field | Observed value |
| --- | --- |
| CPU | AMD Ryzen 7 5800X3D 8-Core Processor |
| GPU | NVIDIA GeForce RTX 2070 SUPER |
| Dedicated GPU memory | 8192 MiB, reported by nvidia-smi |
| Physical RAM | 31.9 GiB usable, approximately 32 GB installed |
| OS | Microsoft Windows 11 Home, version 10.0.26200 |

The owner selected this workstation as the prototype reference in D02. This is not
a benchmark result or a release requirement. Measurement responsibility: the lead
defines/reviews the protocol and the future developer runs the build on this
accessible workstation. Any need for manual owner interaction is coordinated when
the build exists; no unattended GUI capability is claimed here.

## Prototype Performance Protocol

- Standalone Windows build, 1920x1080, 100% render scale, ray tracing off,
  dynamic resolution and upscaling off, VSync off, frame cap off for timing capture.
- T02 records the approved renderer/quality configuration and GPU driver/build versions.
  Use the same settings and camera for all compared captures; any changed setting
  starts a newly labeled comparison. Capture profiling overhead separately.
- Warm up for 60 seconds, then run three captures of the same 60-second route per
  lighting/weather condition. Record per-frame timing rather than sampled FPS.
- Route: 0-15 seconds idle at harbor; 15-30 travel parallel to shore;
  30-45 turn in open water showing wake; 45-60 approach harbor and shoreline again.
  T02 fixes the exact path and movement speed and retains them with its test scene.
- Compare each run with initial median <=16.7 ms and p95 <=20 ms targets. Report p99, maximum,
  frames >33.3 ms, CPU/GPU timing, and memory as diagnostics. These thresholds
  do not prove that every frame achieves 60 FPS.
- Exclude initial load/warmup from steady-state timing but report their duration.
  Include stalls occurring during the route; do not silently discard spikes.
- T11 repeats the representative route with real systems and reports its separate
  stress workload. Minimum release hardware remains a later measured decision.

These are approved initial prototype targets, not release requirements. If missed,
report measurements and the identified bottleneck (or clearly labeled investigation
needed), without automatically reducing visual quality. Keep the gate under review
until the owner and lead record a disposition: optimize, revise a target, accept a
documented prototype limitation, or revisit the technical proposal. Never relabel
a missed target as met. Capture details above are the lead's test protocol; they
are not additional owner-approved product requirements. No measurements exist yet.

## Proposed Visual Matrix

Written reference framing: elevated perspective at roughly 60 degrees downward,
fixed heading, a ship around 8% of image height, nearby coast plus open sea in view.
The low-poly ship, island, and water share a deliberate palette/material response.
Pixel-inspired textures do not imply strict pixel-perfect screen rendering.

| Condition | Observable acceptance criteria |
| --- | --- |
| Daylight | Ship silhouette and attack marker stay readable through glare; shallow/deep water differ; shore foam and wake are visible; no obvious water through hull |
| Dusk | Harbor light and ship shadows feel spatially consistent; silhouette/marker remain readable; dark water retains depth contrast and visible wake; no unstable shadow flicker |
| Rough water | Wave motion/foam remain coherent while ship and marker stay readable; wake follows travel without visible breaks; shoreline/hull intersections do not produce obvious artifacts |

Check all conditions in motion from the gameplay camera at the target resolution,
including harbor departure, shore traversal, turning, and return. The game owner
judges overall water/art quality; the lead records defects and performance evidence.
An unreadable marker or obvious hull intersection is a failure requiring correction.
The initial camera baseline is owner-approved in D05; exact values may change with
visual/gameplay evidence. The matrix operationalizes the existing water/readability
requirements for review; it is not an owner claim that rendered quality has passed.
Record adjustments and reasons and compare labeled captures. GATE-01 remains unevaluated.

## Art Workflow and Budget

No paid assets/tools for the prototype. Reuse simple meshes/materials and animate
ship bobbing, recoil and sail motion procedurally. No skeletal character animation.
The T02 contributor documents steps/tools and logs initial setup separately from
repeatable content work. Following that setup, produce one simple ship variant in
<=2 hours and a small island variant in <=4 hours using reusable parts.
Count modeling, textures, import and in-engine adjustment in each variant's time.
The owner reviews the demonstration/instructions and whether the workflow is
practical for their skill level. D03 approves zero spending; D04 approves the times
as workflow targets only. An overrun triggers analysis/revision, not automatic game
failure. Record time and bottlenecks honestly and obtain a reviewed disposition
before closing GATE-03; do not silently change the numbers to claim success.

## Owner Answers

Source: owner response, 2026-09-18. Faithful transcription with ASCII punctuation:

> D01 - APPROVED. Windows single-player. Keyboard/mouse first. Controller support can be deferred beyond the prototype.

> D02 - APPROVED AS A PROTOTYPE BENCHMARK, NOT A RELEASE REQUIREMENT. Use this workstation at 1080p as the reference prototype machine. Median <=16.7 ms and p95 <=20 ms are good initial targets. If they are missed, report the measurements and bottleneck rather than automatically reducing visual quality. Minimum supported/release hardware remains a later decision.

> D03 - APPROVED. No paid assets/tools for the prototype. Use simple low-poly geometry, pixel-inspired textures/materials and procedural motion where practical.

> D04 - APPROVED AS WORKFLOW TARGETS, NOT HARD PRODUCT REQUIREMENTS. <=2 hours for a repeatable simple ship variant and <=4 hours for a small island variant are useful targets for evaluating whether the art pipeline is manageable. Exceeding them should trigger analysis/revision of the workflow, not automatically fail the game itself.

> D05 - APPROVED AS THE INITIAL PROTOTYPE CAMERA TARGET. Start around 60 degrees downward with the ship roughly 8% of image height. Treat this as the baseline for T02/GATE-01, not an immutable final-camera decision. Visual/gameplay testing may justify adjustment.

> D06 - APPROVED. Defer minimum supported hardware until the representative slice has measured performance data.

> D07 - NOT APPROVED AS A LEAD-OWNED DECISION. Engine/renderer choice is a game-owner/product/technical decision and must not become authoritative solely because the Lead selected it during planning. Do not treat ADR-001 as confirmed until I explicitly approve the engine choice

## Subsequent D07 Resolution

Later owner instruction on 2026-09-18:

> you choose engine and create tasks

Decision: Unity 6 LTS with HDRP and C#, selected by the lead under that explicit
delegation. Existing water/lighting support fits the primary visual requirement;
T02 must measure its cost on the reference machine. D01-D06 qualifications stand.
The earlier D07 response is preserved as history, not an unresolved current veto.
Exact supported editor and compatible packages are T01 deliverables.

## Open and Deferred

T00 target definition is accepted and T01 is ready for assignment. Controller
support remains deferred beyond the prototype; minimum release hardware awaits
representative slice measurements. Exact versions are T01 work. Actual gate
verification remains T02 work; no prototype performance or visual pass is claimed.
