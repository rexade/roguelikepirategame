# T01: Create the minimal engine project

Status: done
Assignee: Codex (developer)
Role: Engine engineer
Depends on: T00

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: accepted docs/TARGETS.md and docs/evidence/T00/REPORT.md, plus ADR 001.
Assigned by the owner on 2026-09-18: "U are dev so use dev skill implement task 01".
Input snapshot hashes and verification are recorded in docs/evidence/T01/REPORT.md.
T01 reserves its package/settings/bootstrap assets until accepted handoff.
D07 is resolved by the owner's explicit delegation; build the Unity 6 LTS/HDRP
prototype. Pin a supported editor patch and its compatible package versions here.

Selected pins: Unity 6000.3.24f1 (4e7b9b5b6244); HDRP/Core RP/Shader Graph/VFX
Graph 17.3.0; Input System 1.20.0; Test Framework 1.6.0; Newtonsoft JSON 3.2.2;
built-in UI Toolkit from the pinned editor. The generated packages-lock.json
records transitive versions. Build/restore verification is tracked in the report.

Owned paths: Packages/; ProjectSettings/; .gitignore; Assets/_Game/Bootstrap/; Assets/_Game/Scenes/Bootstrap.unity; Assets/_Game/Scenes/Tests/T02/; docs/BUILD.md; docs/evidence/T01/

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Create the minimal Unity project and pin a supported editor plus compatible HDRP, input, and test packages.
2. Select and verify one UI implementation and a supported JSON serializer for later tasks; record dependency versions and obligations.
3. Configure visible asset metadata, text serialization, version-control exclusions, and a minimal bootstrap plus water-enabled visual test scene.
4. Document restore and Windows build steps in docs/BUILD.md. Hand the visual test scene to T02 after acceptance.
5. Use the Windows 1080p reference workstation and keyboard/mouse scope in TARGETS.md.
   Keep ray tracing/upscaling off in the reference configuration; do not lower water
   quality to conceal a target miss. Purchase no assets or tools.
6. Confirm repository boundaries before configuring version control: Git currently
   resolves above this project. Keep project metadata/exclusions inside this workspace;
   do not stage or modify the parent repository's unrelated files.

## Acceptance

Rules: INV-18, GATE-02.
Cases from [the backlog](../TASKS.md): Clean-checkout build and keyboard/mouse-navigation smoke check.

- [x] Fresh checkout opens without project errors and produces a runnable Windows build.
- [x] Manifest/lockfile and editor version are recorded; selected UI supports keyboard/mouse navigation.
- [x] No gameplay framework or progression implementation is included.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Full game systems, production art, paid dependencies, or claiming the visual gate passed.

Deliver: Build log, version inventory, runnable build location, and reproduction steps. Unlocks T02.

Developer verification is recorded in docs/evidence/T01/REPORT.md, including a
successful clean-checkout Windows build, focused Windows-player keyboard test,
and standalone mouse/rendering smoke. Checked boxes record developer results,
not Lead acceptance. Subsequent Lead review accepted T01 on 2026-09-18; see the
report's Lead Acceptance Review. T02 is ready for assignment.
