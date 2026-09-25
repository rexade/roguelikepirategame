# T00: Define targets and visual acceptance

Status: done
Assignee: Codex, acting as tech/test lead
Role: Tech lead with game owner
Depends on: none

## Assignment

Define an evidence-traceable target sheet that T01 can build against and T02 can
evaluate. This is documentation and decision gathering, not engine implementation.
Follow the [dispatch rules](README.md), [context](../../CONTEXT.md),
[architecture](../ARCHITECTURE.md), [invariants](../INVARIANTS.md),
[dependencies](../DEPENDENCIES.md), and [rendering ADR](../decisions/001-engine-and-rendering.md).

Readiness: accepted on 2026-09-18. D01-D06 retain their qualifications. The later
owner instruction "you choose engine and create tasks" delegates D07; the lead
selects Unity 6 LTS/HDRP. T01 is ready for assignment, not started.

Owned outputs when executed: docs/TARGETS.md and docs/evidence/T00/REPORT.md, plus
supporting reference material under docs/evidence/T00/. Draft targets and an interim
report are complete and accepted. Prototype rendering gates remain unevaluated.
The lead owns synchronized changes to this packet, the board, backlog, dependencies,
and ADR if a decision changes their meaning. No runtime contracts are changed here.

## Investigation Findings

Historical snapshot before the owner's D01-D07 response. The earlier interpretation
of engine authority is superseded by D07 in TARGETS.md. Current decisions follow below.

Inspected the workspace on 2026-09-18. The project contains planning documents and
local skills, with no Assets/, Packages/, ProjectSettings/, target sheet, or T00
evidence report. No applicable AGENTS.md was found in the project or inspected
ancestor directories. This is a document-snapshot investigation, not a code review
against a known project commit. Git resolves to a parent repository outside this
project; do not treat that repository as permission to inspect/change unrelated files.

| Classification | Finding and source | Consequence |
| --- | --- | --- |
| Confirmed requirement | Excellent water, convincing lighting/shadows, practical art: CONTEXT.md, User requirements | Define visible criteria and content-production effort |
| Confirmed requirement | Fixed map, shared hub unlocks, ship progression: CONTEXT.md, User requirements | Preserve direction; do not redesign gameplay in T00 |
| Superseded authority claim | Earlier ADR treated Unity/HDRP as Lead-selected | Owner D07 corrects this: proposal only, explicit owner approval required |
| Proposed defaults | Windows single-player, both input methods: CONTEXT.md, Proposed design defaults | Label as proposals until resolved for the first slice |
| Provisional budget | 1080p, median <=16.7 ms, p95 <=20 ms: TASKS.md, T02 | Not a measured result or proof of sustained 60 FPS |
| Missing input | Benchmark hardware, access, asset budget, concrete visual references, acceptable art effort | Gather or explicitly record as unresolved; do not manufacture answers |
| Unverified | Water appearance, performance, package compatibility, production workflow | T01/T02 provide actual implementation evidence later |

The previous packet did not specify a target-sheet schema or failure checks. It
also did not distinguish a reference benchmark from minimum supported hardware.
This revision fixes those specification gaps. No rendering gate has been evaluated.

## Required Inputs and Decisions

Use the responses and later delegation in TARGETS.md. Earlier Lead-only authority
was withdrawn; the subsequent explicit owner instruction now delegates the engine
choice to the lead. Unity 6 LTS/HDRP is selected. Do not reopen D01-D06 or turn
their prototype/workflow targets into hard release requirements.

| Decision | Current state | Resolution needed for acceptance |
| --- | --- | --- |
| Platform/play mode/input | D01 approved | Windows single-player, keyboard/mouse; controller beyond prototype |
| Benchmark machine | D02 approved, specs observed | This workstation; protocol and measurement responsibility in TARGETS.md |
| Minimum hardware | D06 approved deferral | Revisit after representative slice measurements |
| Performance | D02 prototype targets approved | Report misses/bottlenecks; no automatic visual downgrade |
| Spending/art | D03 approved | No paid assets/tools; simple low-poly and pixel-inspired materials |
| Production effort/camera | D04/D05 qualified approval | Workflow targets and adjustable camera baseline, not hard product rules |
| Engine/renderer | D07 resolved under owner delegation | Unity 6 LTS/HDRP; exact versions belong to T01 |

Required decisions are now resolved; T00 is accepted and T01 is ready. Future
incompatible decisions must update affected packets. Death penalties, economy balancing, map size, and final
release hardware claims are outside this decision set.

## Work

1. Inventory existing decisions and required missing inputs using the table above.
   For each target record value, classification (requirement/decision/proposal/open),
   source, decision authority, date, and downstream consumer. Do not infer agreement
   from silence or upgrade an assumption because it appears in multiple documents.
2. Create docs/TARGETS.md with sections for scope, decision register, benchmark
   machine/access, performance protocol, visual matrix, art workflow/budget, and
   unresolved/deferred items. Every required field needs a value or explicit open state.
3. Define the future T02 protocol: standalone build, recorded revision/settings,
   warmup rule, reproducible 60-second route, sample count, and treatment of loading
   stalls. Record resolution, render scale, VSync/frame-cap policy and dynamic
   resolution/upscaling policy so runs are comparable. Measure raw frame times and
   report median/p95 plus spikes, CPU/GPU timings, and memory. Set the pass thresholds;
   specify which additional metrics are diagnostic rather than inventing limits.
4. Define a daylight/dusk/rough-water matrix at the intended gameplay camera. For
   each row specify observable checks: ship/attack-marker visibility, foam/wake
   continuity, shallow/deep distinction, water-through-hull defects, shadow stability,
   and coherent simple art. Reference a location/timecode when using external media,
   or an explicit written framing description. Name the owner who will judge T02.
5. Define GATE-03 evaluation: repeatable steps/tools for one simple ship and island,
   permitted purchased assets, time/effort limit and who performs/reviews the workflow.
   Describe what T02 must demonstrate; do not create prototype assets in this task.
6. Resolve required inputs, record remaining deferrals and reasons, and reconcile
   affected planning documents through the lead. If the engine/platform changes,
   update ADR/task scope before T01. Write the handoff with a criterion-by-criterion
   result and a clear accepted/pending recommendation for Lead review.

## Acceptance

Rules: GATE-01, GATE-02, GATE-03. T00 defines their criteria; only subsequent work
can pass the gates. Gameplay AC-01 through AC-18 do not apply to this task.

- [x] T00-C01: All target fields have classification, source and authority; user
  requirements are preserved and proposals are not presented as owner decisions.
- [x] T00-C02: A named benchmark machine has exact required specs and a credible
  access/measurement plan. Minimum hardware is separately specified or deferred.
- [x] T00-C03: Performance protocol includes prototype targets and reproducible
  capture conditions, with analysis/review of misses rather than automatic downgrades; no results
  or sustained-60-FPS claims are inferred from median/p95 thresholds.
- [x] T00-C04: All three visual conditions have observable checks and recorded
  game-owner agreement on the criteria to judge in T02, not a claim of visual approval.
- [x] T00-C05: Input scope, spending policy and measurable art-effort workflow targets are
  recorded; the GATE-03 evaluator and future demonstration are identified.
- [x] T00-C06: Required decisions are resolved; any permitted deferral states its
  owner, consequence and later task. Packet/board/backlog/ADR are consistent.
- [x] T00-C07: REPORT.md lists evidence and checks, unresolved issues, and Lead outcome.
  T01 stays waiting until acceptance; GATE-01/02/03 stay unevaluated at T00 completion.

## Verification Procedure

1. Review every target-sheet row against its cited source/decision. An unsupported
   confirmation is a failed T00-C01 even if the proposed value seems reasonable.
2. Check benchmark fields and access plan. A GPU family alone, unavailable machine,
   or unapproved substitution cannot satisfy T00-C02. Hardware inventory, if later
   collected, is an observed fact rather than evidence the owner selected that target.
3. Have a reader reconstruct the intended T02 capture using only the target sheet.
   Missing settings, thresholds, warmup or route definition fail T00-C03. This is a
   protocol review, not an engine benchmark; no executable tests exist for T00.
4. Walk through the visual matrix and effort criteria with their designated reviewer.
   A generic instruction to make the water excellent is insufficient; an example
   showing unreadable markers or excessive art effort must have a defined fail path.
5. Audit failure cases: absent owner input, conflicting performance/quality goals,
   unknown budget, inaccessible reference, and changed platform. Record open/conflict
   state and retain the T01 dependency instead of substituting fabricated acceptance.
6. Resolve local document links, compare scope and status with board/backlog, and
   confirm that no engine files, assets, tests, settings, or later tasks were changed.
   Report each T00-Cxx as passed, failed, or not run with its evidence/reason.

## Scope and Handoff

Out of scope: Engine installation/setup, package version selection, purchases,
benchmarks, generated assets, gameplay implementation, or passing rendering gates.
T01 pins compatible versions; T02 creates and measures the prototype. Do not require
T02 screenshots as a T00 prerequisite and thereby introduce a dependency cycle.

Consumers: T01 uses accepted platform/engine/input/spend constraints; T02 uses the
hardware, protocol, visual matrix and workflow limits; T11 reuses the agreed budget.
T00's acceptance authorizes no unsupported final-release hardware claim.

Deliver: docs/TARGETS.md and docs/evidence/T00/REPORT.md, with decision sources and
criterion results. The report records target-definition acceptance and remaining
prototype validation limits. T01 is unlocked for assignment, not executed.

Record results in docs/evidence/T00/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.
