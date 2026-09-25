# T02: Prove water, lighting, and affordable art

Status: done (owner-approved for now, with documented limitations)
Assignee: Codex (developer)
Role: Rendering engineer / technical artist
Depends on: T01

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Accepted build, target sheet, HDRP configuration, and visual test scene.

Pickup: 2026-09-18, user assignment "Implement task 02". T01 accepted snapshot
172bcab89cc5de8021d713527b906d4700f42854; editor 6000.3.24f1 / HDRP 17.3.0.
Input hashes and build identity are recorded in docs/evidence/T02/inputs.json.

Developer handoff: 2026-09-18, docs/evidence/T02/REPORT.md. Corrected standalone
build, AC-14 checks, keyboard regression, nine benchmark routes and three validated
condition videos delivered. Lead and owner GATE-01/02/03 decisions remain pending;
full human workflow timing is not claimed passed. T03 remains waiting.

Lead review 2026-09-18: source/build/video identity and all nine CSV results verified;
technical measurement review passed. No blocking code defect identified. GATE-01
visual disposition, GATE-02 owner disposition, and GATE-03 workflow disposition
remained pending at review; see REPORT.md's Lead Review.

Owner disposition 2026-09-18: "approved for now" following that review. T02 is
accepted for prototype progression with GATE-01/02/03 limitations retained in
REPORT.md's Owner Disposition. Full-motion inspection and human workflow effort
are not newly verified by approval. T03 is ready, unassigned, and not started.
This disposition supersedes the historical pending handoff/review states above.

Owned paths: Assets/_Game/Presentation/Water/; Assets/_Game/Art/Prototype/; Assets/_Game/Scenes/Tests/T02/; docs/evidence/T02/

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Create one simple ship, shore, rocks, and harbor light at the intended gameplay-camera angle.
2. Integrate water color/depth, foam, wakes, shadows, and reflections using the selected water system; use a temporary motion driver.
3. Capture daylight, dusk, and rough-water footage including a dummy attack marker; record asset creation steps and effort.
4. Profile a warmed-up 60-second route in a standalone build on agreed hardware. Report frame-time distribution, CPU/GPU timing, memory, and stalls.

## Acceptance

Rules: INV-14, GATE-01, GATE-02, GATE-03.
Cases from [the backlog](../TASKS.md): AC-14 for the visual driver; production simulation comparison follows in T04.

- [x] Ship and attack marker remain readable; no obvious water-through-hull artifacts; art looks coherent from the gameplay camera. Accepted for now on sampled evidence with the documented full-motion review limitation.
- [x] Report against the approved prototype targets: 1080p median <=16.7 ms/p95 <=20 ms. On a miss report measurements/bottlenecks without automatic visual downgrade; these are not release requirements.
- [x] Lead records measurements and owner records visual/production dispositions. Workflow overruns trigger analysis/revision, not automatic game failure; misses remain honestly reported. Human workflow effort remains unverified and accepted as a limitation.
- [x] Start at the D05 camera baseline and record test-driven adjustments and reasons.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Production combat, economy, custom fluid solver, new rendering pipeline without an updated ADR.

Deliver: Footage, benchmark settings/results, asset workflow, gate verdicts, known defects. Unlocks T03 only when all gate reviews are accepted; an accepted limitation does not mean a missed numeric target was met.

Record results in docs/evidence/T02/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.
