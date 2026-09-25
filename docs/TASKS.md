# Delivery and Delegation Plan

T00 and T01 are complete and accepted. Unity/HDRP bootstrap and T03 shared rules
are implemented and accepted. T02 is owner-approved for now with documented visual/workflow
limitations; technical measurement review passed. Other roles describe future ownership. Estimates are
rough focused person-days, excluding learning/setup delays and art revisions.

The [invariant specification](INVARIANTS.md) is required reading for every task.
Task IDs are stable. Preparing these briefs does not start implementation.
Individual [assignment packets and the dispatch board](tasks/README.md) are ready
for delegation. T00-T05, T04-R1 and T05-R1 are done; T06 needs changes; T07 is ready;
other successors are waiting.
The [dependency plan](DEPENDENCIES.md) defines the task graph, required handoffs,
module boundaries, and conditions for parallel work.

## Ready Queue

- T00 is accepted: D01-D06 qualifications stand and D07 selects Unity/HDRP under
  the owner's explicit delegation. T01's build passed Lead review.
- T02 has verified measurement evidence and owner approval for now of GATE-01/02/03,
  with visual/workflow limitations retained in its report's Owner Disposition.
- T03-r1, T04-r2 and T05-r2 are accepted. T05 owner combat/HUD approval is recorded.
  T06 (with R1) and T07 passed Lead technical review 2026-09-25 (owner visual pending).
- T08 and T09 were implemented by the Lead on 2026-09-25 and are in review: the
  game is playable end to end from `Builds/Game/PiratePrototype.exe`.
- [T06-R1](tasks/T06-R1-startup-hud.md) is submitted by Codex for review: fix the initial
  salvage HUD state and provide regression/visible-HUD evidence. This is parent
  remediation, not a new milestone or successor authorization.
- [T05-R1](tasks/T05-R1-player-combat-state.md) is closed; R1-C01 through R1-C09
  are accepted, including owner visual disposition.
  It is parent T05 remediation, not a new milestone or successor authorization.
- [T04-R1](tasks/T04-R1-tick-lock-coordination.md) is closed; independent review
  accepted R1-C01 through R1-C09. T05/T06 must follow the linked step protocol.
- Other successors wait for their listed dependencies. T09 also needs the
  Lead-owned activation contract extension recorded in its packet.

## Invariant Coverage and Evidence

The primary owner establishes the rule; integration tasks verify it across systems.
All checks below are future acceptance work, not passing test reports.

| Task | Required rules/gates | Required handoff evidence |
| --- | --- | --- |
| T00 | GATE-01, GATE-02, GATE-03 | Target sheet: hardware/budget, reference framing, art-effort criteria, open decisions |
| T01 | INV-18, GATE-02 | Pinned versions, build instructions, clean-checkout build log |
| T02 | INV-14, GATE-01, GATE-02, GATE-03 | Camera footage, benchmark, asset workflow, explicit gate verdicts |
| T03 | INV-01 through INV-09, INV-12, INV-13, INV-18 | Contracts, rule tests, content validation, fake-storage failure tests |
| T04 | INV-14 | Input/collision scene, pause checks, footage at 30/60/120 render FPS |
| T05 | INV-12, INV-13, INV-15 | Duplicate hit/death, pool-reuse and swept-hit tests, loadout examples |
| T06 | INV-05, INV-10, INV-11, INV-12 | ID validation, full-cargo/repeated-pickup tests, authored route |
| T07 | INV-01, INV-03, INV-04, INV-08, INV-13 | Unaffordable/duplicate purchase and invalid-loadout tests, keyboard/mouse recording |
| T08 | INV-01 through INV-08, INV-11, INV-16, INV-17, INV-18 | Expedition tests, resume fixture, filesystem fault/recovery results |
| T09 | INV-01, INV-03, INV-08, INV-09, INV-17 | Shared-hub and travel tests, destination-load failure check |
| T10 | INV-10, INV-11, INV-12, INV-16 | Traversal, encounter resume and depleted-loot restoration tests |
| T11 | All invariants and gates | Coverage review, regression results, performance capture, remaining defects |

## Critical Acceptance Scenarios

| Case | Setup/action | Expected result | Owner |
| --- | --- | --- | --- |
| AC-01 | Cargo has 7 wood; dock callback fires twice | Bank increases by 7; expedition clears once | T03/T08 |
| AC-02 | Lethal hit and docking request in same tick | Sink outcome; no cargo banked | T03/T08 |
| AC-03 | Bank has 5; upgrade costs 6 | Rejected; balance and tier unchanged | T03/T07 |
| AC-04 | Bundle weighs 3; cargo has 2 free units | No transfer; source remains intact | T03/T06 |
| AC-05 | Loot wreck, checkpoint, unload/return, reload save | Wreck depleted; cargo credited once | T06/T08/T10 |
| AC-06 | Sink with empty bank and upgraded loadout | Ownership persists; embark possible without payment | T03/T08 |
| AC-07 | Buy tier at A, travel to activated B | Same bank/tier/unlocks; B becomes safe hub | T07/T09 |
| AC-08 | Travel at sea or to inactive hub | Rejected; no mutation | T03/T09 |
| AC-09 | Fail docking save before replacement | Prior save usable; outcome frozen; retry banks once | T08 |
| AC-10 | Crash after replacement before UI success | Reload committed result; no repeated effects | T08 |
| AC-11 | Resume damaged ship with cooldown and cargo | Health, cooldown, cargo/world match checkpoint | T08 |
| AC-12 | Corrupt main save or unsupported schema | Recover valid backup; preserve originals; report error if unrecoverable | T08 |
| AC-13 | Reuse projectile; duplicate collision callbacks | New owner; one new hit; no stale damage | T05 |
| AC-14 | Change water quality/bobbing amplitude | Rules, hitboxes, aim unchanged | T02/T04 |
| AC-15 | Duplicate authored ID in another region | Validation identifies both offending objects | T03/T06/T10 |
| AC-16 | Equip instance twice or in incompatible slot | Harbor and embark both reject | T03/T07 |
| AC-17 | Older checkpoint finishes after newer purchase | Cannot overwrite committed purchase snapshot | T08 |
| AC-18 | Travel committed; destination load fails | Input locked; retry/reload reaches saved hub; one player | T09 |

## Assignment Packet

Include this filled-out block with the relevant brief when assigning work:

```text
Task: <stable ID and title>
Status: ready | in progress | blocked | review | done
Owner: <one accountable contributor>
Prerequisites: <accepted outputs and exact contract revision>
Read: CONTEXT.md, docs/ARCHITECTURE.md, docs/INVARIANTS.md, docs/DEPENDENCIES.md
Owned paths/assets: <exclusive write scope>
Required invariants and acceptance cases: <IDs from tables>
Deliverables: <code/assets/docs and verification evidence>
Out of scope: <brief exclusions>
Shared changes needed: <lead-owned contracts/scenes/settings, or none>
Handoff: <changed files, check results, build/repro steps, unresolved defects>
```

Done means outputs exist, required checks pass, evidence is attached, and the lead
accepts integration. Visual gates also require the game owner's visual review.
Report checks as passed, failed, or not run with reasons. A stub or standalone test
scene alone does not complete an integration task.

## Milestones

1. Visual proof: T00-T02. Establish that the game's defining visual is achievable.
2. Expedition slice: T03-T08. Complete one departure, loot, combat, and return loop.
3. Progression proof: T09-T11. Demonstrate shared hubs, resume, and region continuity.

Only the small engine bootstrap precedes the visual gate. Do not build the full
progression codebase while the rendering decision is unproven.

## Task Index

| ID | Task | Owner role | Dependencies | Estimate |
| --- | --- | --- | --- | --- |
| T00 | Confirm targets and reference criteria | Tech lead + game owner | None | 0.5 |
| T01 | Create minimal engine project | Engine engineer | T00 | 0.5-1 |
| T02 | Prove ocean and art direction | Rendering/technical artist | T01 | 2-4 |
| T03 | Establish runtime contracts and state rules | Gameplay architect | T02 accepted | 1-2 |
| T04 | Ship controls and camera | Gameplay engineer | T03 | 1-3 |
| T05 | Combat and equipment | Combat engineer | T04 | 2-4 |
| T06 | Authored region and salvage | World engineer | T04 | 2-3 |
| T07 | Harbor economy and progression | Systems engineer | T03 | 2-3 |
| T08 | Save/resume and expedition integration | Persistence engineer + lead | T05, T06, T07 | 2-4 |
| T09 | Shared hubs and fast travel | Systems engineer | T08 | 1-2 |
| T10 | Region loading and voyage variation | World engineer | T08 | 2-4 |
| T11 | Slice quality and performance gate | QA + lead | T09, T10 | 2-3 |

## Task Briefs

### T00: Targets and reference criteria

Record engine preference, minimum target CPU/GPU/RAM, resolution, input devices,
asset budget, and desired visual references. Owner decisions are Windows single-player,
keyboard/mouse first, and 1080p prototype timing targets. Unity/HDRP is selected under explicit owner delegation. Engine setup must not
claim hardware support without a specified machine. Define what acceptable water
looks like from gameplay distance and select a reference camera framing.

Acceptance: the [T00 packet](tasks/T00-targets.md) defines T00-C01 through T00-C07:
traceable decisions, accessible benchmark hardware, reproducible performance
protocol, visual matrix, art/input/spend criteria, resolved required inputs, and
review evidence. Distinguish benchmark hardware from minimum supported hardware,
which may remain explicitly deferred. T00 specifies gates; it does not pass them.
T00 targets and report are accepted; this does not pass rendering gates. Do not purchase assets as part of this task.

### T01: Minimal engine project

Pin a supported Unity 6 LTS editor and compatible HDRP packages. Enable the existing
engine water system and input package. Create only bootstrap and visual-test scenes;
configure version control exclusions, text serialization, and visible metadata.

Acceptance: a fresh checkout opens without errors and produces a Windows build;
exact editor/package versions and reproducible build steps are documented.

### T02: Ocean and art proof

Own Presentation/Water and the visual-test scene. Place a simple ship, a shoreline,
rocks, and a harbor light under the intended camera. Integrate waves, shallow/deep
water color, foam, wake, shadows, and controlled reflections. Demonstrate motion
using a temporary test driver, not production combat or progression systems.

Acceptance: gameplay-camera footage in daylight, dusk, and rough water; a readable
ship silhouette and dummy attack marker; no obvious water-through-hull artifacts;
cohesive simple art; a standalone performance report on T00 hardware. Measure after
warmup across a 60-second path. Provisional target: median frame <=16.7 ms and
p95 <=20 ms at 1080p, with spikes separately reported. Record memory and CPU/GPU
timing. The game owner reviews visual quality; the lead reviews performance.

Decision: report measurements/bottlenecks on target misses, without automatic
visual downgrade. Workflow overruns trigger analysis/revision, not automatic game
failure. Owner/lead record gate disposition; never report a missed target as met.
Engine changes require explicit owner approval. Do not mark passed based
on screenshots or editor FPS alone. This gate is outstanding.

### T03: Contracts and rules

Own Core/Application and the initial content definitions. Establish the state
records and commands in ARCHITECTURE.md, stable IDs, lifecycle ownership, and stat
calculation order. Agree the narrow cross-task contracts before parallel coding.

Acceptance: tests prove cargo banking, sinking, duplicate outcome rejection,
upgrade validation, and loadout slot rules. Runtime state never mutates definition
assets. Publish method/data signatures for downstream tasks in the repository.

Deliver contracts for Embark, CollectLoot, RequestDock, ResolveSink, PurchaseUpgrade,
SetLoadout, and FastTravel, plus save candidate/commit coordination and read-only
query snapshots. Specify request IDs, expected state, errors, and success events.
Implement rules without scene dependencies and test transitions with fake storage.
T08 supplies real storage; early tests do not establish durability. T03 owns
initial travel rules; T09 integrates them with hubs and scenes.
Also publish the gameplay clock/input intent, entity capture/restore, and world
arrival contracts listed in DEPENDENCIES.md so downstream tasks can work independently.

### T04: Ship controls and camera

Own Gameplay/Ships and Presentation/Camera. Implement the constrained physics
motor, input actions, camera follow, collision, and visual-only bobbing.

Acceptance: keyboard/mouse works; controller is deferred beyond the prototype; ship motion is stable at different
render frame rates; islands cannot be crossed; waves cannot tilt the gameplay
collider or aim; camera framing keeps nearby threats and pickups visible.

### T05: Combat and equipment

Own Gameplay/Combat and Gameplay/AI. Deliver two alternative weapons, one active
ability, and two basic enemy behaviors using authored definitions.

Acceptance: valid loadouts change play; cooldowns and damage are reliable; fast
shots cannot pass through targets; reused projectiles do not retain hit state;
death emits one outcome. Confirm readable attacks over the actual water.

### T06: Region and salvage

Own Content/World, Gameplay/World, and the first region scene. Author a home harbor,
three islands, fixed wrecks, barrels, sea lanes, and encounter markers. No large
world or procedural island generator.

Acceptance: salvage enters cargo exactly once; full cargo has defined feedback;
object IDs are unique and stable; unloading/recreating an entity cannot reset its
expedition loot state. A short outward-and-return route is navigable.

### T07: Harbor progression

Own harbor/loadout UI and economy integration using T03's rules. Add bank display,
loadout editing, one hub upgrade, one ship upgrade, and embark validation. Shared
rules remain owned by T03's maintainer rather than duplicated in UI code.

Acceptance: unaffordable purchases are rejected; valid purchases deduct once;
equipment cannot occupy illegal slots; upgrades visibly change the next expedition.
UI works with keyboard/mouse and remains readable at 1280x720.
T07 verifies upgrade effects through computed stats and query fixtures; T08 verifies
the resulting behavior in the integrated expedition.

### T08: Persistence and full expedition

Own Persistence and lifecycle integration in Bootstrap. Implement the versioned
save snapshot, backup recovery, suspend/resume, and atomic outcome commits.

Acceptance: fresh game -> embark -> fight -> salvage -> dock -> upgrade -> embark
works. Also prove sinking preserves permanent progress, resume preserves cargo and
depleted loot, duplicate docking cannot duplicate resources, and a simulated write
failure preserves a recoverable save. A player cannot become permanently stranded.

### T09: Shared hubs and travel

Own hub activation/travel rules and the travel UI. Add a second small harbor and
one later travel unlock; read the existing shared bank and upgrade state.
Place both hubs in the existing region for isolated acceptance. T11 combines this
with T10's region loading to verify travel across regions.

Acceptance: visiting either hub shows the same progression; locked/undiscovered
destinations are rejected; travel is unavailable at sea; save/reload retains the
destination and global unlocks. No cargo escape through fast travel.

### T10: Region continuity and variation

Own region loading and encounter selection. Add a neighboring region, load ahead
of traversal, persist expedition entity state, and choose encounters using seeded
tables while retaining the fixed island layout.

Acceptance: crossing and returning does not duplicate enemies or loot; reloading
preserves selected encounters; a new expedition can vary encounters without moving
islands; docking and sinking reset only the documented expedition state. Record
streaming stalls. Do not implement floating origin unless measured precision needs it.

### T11: Slice gate

Own integration evidence and defect triage. Re-run the agreed benchmark with real
combat, salvage, UI, and region transitions. Add a stress scene with 12 enemy ships,
100 live projectiles, and 100 pickups as an initial diagnostic workload, not a final
content cap. Verify daylight/dusk readability, keyboard/mouse navigation, and saves.

Acceptance: documented hardware/settings, footage, frame-time distributions,
critical tests passing, and no progression-blocking defects. Record achieved
results and remaining limitations; do not report provisional targets as achieved.

## Delegation Rules

After T03, T04 and T07 can proceed independently. After T04, T05 and T06 can run in
parallel while T07 continues. T09 and T10 can run in parallel after T08.

Each assignment includes its task ID, dependencies, owned directories, agreed
contracts, acceptance criteria, and expected verification evidence. One owner
edits a shared Unity scene, prefab, package manifest, or project setting at a time.
Contributors work in separate test scenes and submit prefabs for integration.

The lead owns Bootstrap, shared contract changes, final scene composition, and
milestone acceptance. A contributor proposing a contract change records affected
consumers before others integrate it. Avoid multiple agents editing shared assets.
No extra abstraction, paid dependency, multiplayer work, or map expansion is part
of a task unless its brief is updated.
