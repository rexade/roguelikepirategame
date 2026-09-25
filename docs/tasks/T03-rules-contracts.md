# T03: Implement shared rules and publish contracts

Status: done
Assignee: Codex (gameplay architect)
Role: Gameplay architect
Depends on: T02

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Accepted visual gates and dependency-plan contract inventory.

Readiness 2026-09-18: T02 accepted for now by the owner with documented visual
and workflow limitations. See ../evidence/T02/REPORT.md, Owner Disposition, and
its final-r2 source/build manifests. Assigned by the owner on 2026-09-18.
Accepted input: source-files.json SHA-256
6E60AA8FEDBFB3BCFC0E0C0549D194CD8B08F894A12C5B2BF8698F3D4D2CE4E8;
build-files.json SHA-256
809ACC3A15A75EF2F9E8EBE26F1A2371E784598248BE4611DEDA4AD20F000187.
No project Git HEAD exists; use source manifests for revision identity.
T03 reserves its initial contracts/definitions and new Core assembly configuration
within its owned paths, following DEPENDENCIES.md's Core/Application assembly plan.
Existing shared assembly definitions, settings, packages and scenes are untouched.

Owned paths: Assets/_Game/Core/; Assets/_Game/Application/; Assets/_Game/Content/Definitions/; Assets/_Game/Tests/T03/; docs/CONTRACTS.md; docs/evidence/T03/

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Implement campaign/expedition state, lifecycle rules, resource accounting, loadout validation, and the specified stat formula without engine dependencies.
2. Publish signatures for Embark, CollectLoot, RequestDock, ResolveSink, PurchaseUpgrade, SetLoadout, FastTravel, queries, and errors.
3. Define gameplay clock/input intent, entity identity/capture/restore, ordered save candidate/commit, and world-arrival ports.
4. Create immutable authored definition adapters and validation. Use fake storage for transition tests; establish engine-independent assembly configuration with the lead.

## Acceptance

Rules: INV-01 through INV-09, INV-12, INV-13, INV-18.
Cases from [the backlog](../TASKS.md): AC-01, AC-02, AC-03, AC-04, AC-06, AC-08, AC-15, AC-16 at rule level.

- [x] Required rule cases pass, including same-tick sinking priority, duplicate outcomes, and full-cargo rejection.
- [x] Fake commit failure does not publish success; definitions are not mutated by runtime state.
- [x] Core/Application have no Unity/HDRP imports; docs/CONTRACTS.md identifies the reviewed contract revision.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

## Scope and Handoff

Out of scope: Real filesystem persistence, ship physics, UI, general scripting language, or claims of disk durability.

Deliver: Rule tests and exact contracts with fixtures; shared contracts transfer to lead ownership. Unlocks T04 and T07.

Record results in docs/evidence/T03/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.

Developer handoff 2026-09-18: T03-r1 is ready for Lead review. Unity EditMode
33/33 passed; engine-free build passed without warnings/errors. All 49 inherited
T02 source files retain their hashes. See [report](../evidence/T03/REPORT.md).
The checked boxes record developer verification, not Lead acceptance.
For the remaining combined checkbox, engine independence is verified;
docs/CONTRACTS.md publishes T03-r1 and records the Lead-reviewed revision as none
until the Lead reviews it. The acceptance requirement remains unchanged.

Lead review 2026-09-18: accepted T03-r1. Independent Unity EditMode run passed
33/33; engine-free build passed with no warnings/errors. No blocking defect found
in the scoped rules/contracts. See REPORT.md's Lead Review for identity, limits
and the T09 activation-contract follow-up. Earlier pending statements above are
historical. T04/T07 are ready, unassigned, and not started.
