# T07: Build harbor economy and loadout UI

Status: done (technical acceptance 2026-09-25; owner visual disposition pending)
Assignee: Codex (systems/UI engineer)
Role: Systems/UI engineer
Depends on: T03

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: T03 bank/upgrade/loadout queries, definitions, errors, and fake storage; T01 UI choice.

T03-r1 accepted 2026-09-18; see ../evidence/T03/REPORT.md's Lead Review.
Prerequisite acceptance was verified at pickup; identities and reservations follow.

Assigned by the owner on 2026-09-19. Inputs: accepted T03-r1 (source manifest
EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F),
T01 UI Toolkit / Unity 6000.3.24f1. Output snapshot: T07-r1.
No shared assets reserved or changed; new scoped assemblies belong to T07 paths.
Production composition, input actions and shared definitions remain Lead-owned.

Owned paths: Assets/_Game/UI/Harbor/; Assets/_Game/UI/Loadout/; Assets/_Game/Content/Progression/; Assets/_Game/Scenes/Tests/T07/; Assets/_Game/Tests/T07/; docs/evidence/T07/

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Build bank, equipment, embark, one hub-upgrade and one ship-upgrade flow using application commands.
2. Use definition/query fixtures in a harbor test scene so combat and ship implementation are not prerequisites.
3. Show computed upgrade effects and validation errors; keep shared rules out of UI.
4. Support keyboard/mouse navigation; controller is deferred beyond the prototype. Coordinate shared input asset changes with the lead.

## Acceptance

Rules: INV-01, INV-03, INV-04, INV-08, INV-13.
Cases from [the backlog](../TASKS.md): AC-03, AC-16; AC-07 shared-query portion, plus duplicate purchase.

- [x] Unaffordable and repeated-tier purchases do not deduct resources; valid upgrade deducts/grants once.
- [x] Illegal or duplicate equipment assignment is rejected by shared validation.
- [x] Computed stats reflect upgrades; UI works at 1280x720 with keyboard/mouse. T08 verifies sailing effects.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Weapon components, duplicated economy logic, real saves, travel UI, or editing shared contracts independently.

Deliver: UI prefabs/views, progression definitions, interaction footage and rule integration tests. Input to T08.

Results are recorded in docs/evidence/T07/REPORT.md. Developer checks are complete
with the keyboard qualification below; the Lead records acceptance.

T07-r1 submitted 2026-09-19: 9 integration tests passed, Windows build passed,
1280x720 mouse checks and Input System keyboard route passed. Physical-keyboard
verification remains qualified as described in the report. Source manifest:
AF79222C0D9A023E82CE725BD626949A1F5D90221DB6CEDA72D98FB04537C1E7.
Lead review and owner visual decision are pending; this is not task acceptance.
