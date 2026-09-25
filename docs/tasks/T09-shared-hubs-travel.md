# T09: Integrate shared hubs and fast travel

Status: review
Assignee: Lead (Claude Opus 5.5), 2026-09-25
Role: Systems engineer
Depends on: T08

## Assignment

When this task is assigned and its prerequisites are accepted, complete the work
below within the stated scope. Follow the [dispatch rules](README.md) and read
[context](../../CONTEXT.md), [architecture](../ARCHITECTURE.md),
[invariants](../INVARIANTS.md), and [dependencies](../DEPENDENCIES.md).
This prepared ticket does not itself authorize starting implementation now.

Inputs: Durable campaign transitions, hub registry, travel rules, and single-region arrival adapter.

Contract follow-up from T03 review: r1 has no discovery/activation command. Before
pickup, request a Lead-owned, tested extension for activation and its save/error
semantics. Do not mutate snapshots or replace the session owner to activate hubs.

Owned paths: Assets/_Game/Gameplay/Hubs/; Assets/_Game/UI/Travel/; Assets/_Game/Content/World/Hubs/; Assets/_Game/Scenes/Tests/T09/; Assets/_Game/Tests/T09/; docs/evidence/T09/

These are planned paths, not existing assets. Record accepted input revisions at
pickup. Shared contracts, settings, input assets, and production scenes remain
single-owner; coordinate changes as specified in the dispatch rules.

## Work

1. Create a second hub and later travel unlock using two hubs in one isolated region fixture.
2. Build activation and travel UI against existing rules and shared bank/upgrade queries.
3. Keep input locked until arrival readiness; implement retry/reload feedback for destination-load failure.
4. Submit second-hub placement to lead for production integration; avoid modifying T10's scenes or shared save contracts.

## Acceptance

Rules: INV-01, INV-03, INV-08, INV-09, INV-17.
Cases from [the backlog](../TASKS.md): AC-07, AC-08, AC-18.

- [x] Both hubs show identical shared progression; activation remains local.
- [x] Travel at sea or to inactive hubs is rejected without mutation.
- [x] Save/reload keeps the destination and safe hub; failed arrival never spawns a second player or resumes incomplete-world input.
- [x] Handoff report includes changed files, exact checks/results, reproduction
  steps, evidence locations, and unresolved defects. Unrun checks are labeled.

The activation extension (`CampaignSession.ActivateHub`) and all checks are in
[the T09 report](../evidence/T09/REPORT.md). Owner review is pending.

## Scope and Handoff

Out of scope: Region streaming, separate per-hub banks, new death rules, or direct save-file access.

Deliver: Hub/travel prefabs, fixture, AC-07/08/18 results. Input to T11; cross-region travel is verified there.

Record results in docs/evidence/T09/REPORT.md when executed. No evidence or
completion is claimed by this ticket. Move to review only when the deliverables
and required checks are complete; the lead records acceptance.
