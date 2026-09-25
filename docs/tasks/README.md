# Delegation Dispatch

Prepared task packets for assignment to a human contributor or coding agent.
T00 is complete and accepted. The owner delegated D07; Unity 6 LTS/HDRP is selected.
T01 passed Lead review. T02 is owner-approved for now with documented visual and
workflow limitations; its technical measurement review passed. T03-r1 passed Lead
review. T04-r2 is accepted and T04-R1 closed. T05-r2 is accepted following technical
review and owner visual approval; T05-R1 is closed.
T06 (with R1) and T07 passed Lead technical review on 2026-09-25; owner visual
dispositions remain pending. On the owner's 2026-09-25 instruction to "implement
all you can", the Lead (Claude Opus 5.5) implemented T08, T09 and T10 and ran the
T11 verification (tests, integrated benchmark, stress run); all are in review
awaiting the owner's hands-on play and visual review.
D01-D06 qualifications remain in force.

## Board

| Packet | Role | Status | Direct prerequisites |
| --- | --- | --- | --- |
| [T00: Define targets and visual acceptance](T00-targets.md) | Tech lead with game owner | done | None |
| [T01: Create the minimal engine project](T01-engine-bootstrap.md) | Codex (engine developer) | done | T00 |
| [T02: Prove water, lighting, and affordable art](T02-ocean-proof.md) | Codex (developer) | done (approved for now) | T01 |
| [T03: Implement shared rules and publish contracts](T03-rules-contracts.md) | Codex (gameplay architect) | done | T02 |
| [T04: Build ship controls and gameplay camera](T04-ship-controls.md) | Codex (gameplay engineer) | done | T03 |
| [T04-R1: Fix in-flight tick/lock coordination](T04-R1-tick-lock-coordination.md) | Codex (gameplay engineer) | done | T03 accepted; T04-r1 review finding (parent remediation) |
| [T05: Implement weapons, ability, and enemy ships](T05-combat.md) | Codex (combat engineer) | done | T04 |
| [T05-R1: Synchronize player combat read state](T05-R1-player-combat-state.md) | Codex (combat engineer) | done | T03/T04 accepted; T05-r1 review finding (parent remediation) |
| [T06: Author the first region and salvage loop](T06-world-salvage.md) | Codex (world engineer) | done (technical; owner visual pending) | T04 |
| [T06-R1: Fix startup salvage HUD](T06-R1-startup-hud.md) | Codex (world/gameplay engineer) | done | T03/T04 accepted; T06-r1 F01 (parent remediation) |
| [T07: Build harbor economy and loadout UI](T07-harbor-progression.md) | Codex (systems/UI engineer) | done (technical; owner visual pending) | T03 |
| [T08: Integrate durable saves and the expedition loop](T08-persistence-integration.md) | Lead (Claude Opus 5.5) | review | T05, T06, T07 |
| [T09: Integrate shared hubs and fast travel](T09-shared-hubs-travel.md) | Lead (Claude Opus 5.5) | review | T08 |
| [T10: Implement region continuity and voyage variation](T10-region-streaming.md) | Lead (Claude Opus 5.5) | review | T08 |
| [T11: Verify and accept the integrated playable slice](T11-slice-acceptance.md) | Lead (Claude Opus 5.5) | review (owner review pending) | T09, T10 |

## Dispatch Order

1. Preserve T02's provisional gate acceptance and limitations recorded in its report.
2. T03-r1 is accepted; use its [Lead review](../evidence/T03/REPORT.md) and recorded input identities for pickup.
3. T05-r2 is accepted and T05-R1 closed. T06-R1 and T07-r1 are submitted for review.
   T05/T06 consume the accepted R1 step protocol linked in their packets.
4. Assign T08 after T05, T06, and T07 are accepted.
5. After T08, assign T09 and T10 independently, then T11 after both are accepted.

The [dependency graph](../DEPENDENCIES.md) defines required outputs. Starting a
prerequisite is not sufficient. All three T02 gate reviews require acceptance before
T03 starts. Per D02/D04, report and review target misses; do not automatically reduce
quality, fail the product, or claim a missed target was met.

## Pickup Instructions

Give the assignee the packet path and these instructions:

> Read the packet and its linked project documents. Verify that prerequisites are
> accepted and accessible. Complete only this task's scope, preserve its invariants,
> run its acceptance checks, and write the required handoff report. Report missing
> inputs or incompatible contracts to the lead instead of inventing replacements.
> Do not start successor tasks or expand scope.

At actual assignment, the lead fills in the assignee, changes status to in progress,
records accepted input revisions/builds, and reserves shared assets. If Git is not
available, record identifiable input snapshots/build versions without inventing a
commit hash. Waiting tasks stay unstarted until their prerequisites are satisfied.

## Ownership Rules

- The packet's paths define its write scope. Their parent directories are not
  blanket permission to change another task's assets.
- Matching .meta files belong to the corresponding asset owner. Preserve GUIDs.
- T01 initially owns package/settings/bootstrap setup. On handoff, shared settings,
  manifests, assembly definitions, input assets, and Bootstrap pass to the lead.
- T03 establishes shared definitions/contracts. After acceptance, changes to them
  go through the lead with an affected-consumer list and updated fixtures.
- Each contributor owns their task test scene and prefabs. The lead owns production
  composition. T06's initial region and T04's ship prefab transfer to lead ownership
  at acceptance; later tasks submit components/prefabs or scoped change requests.
- T09 and T10 use separate test scenes. Neither directly edits the other's scenes,
  shared save schema, or scene coordinator. T11 joins their behavior with the lead.
- All Unity asset moves/renames preserve metadata. Never resolve scene conflicts by
  discarding another contributor's edits.

## Handoff Format

The future docs/evidence/Txx/REPORT.md contains:

1. Task, assignee, input revisions, output revision/build, and changed paths.
2. Delivered behavior and any contract changes accepted by the lead.
3. Each required acceptance check: passed, failed, or not run, with evidence/reason.
4. Exact build/test commands or editor steps and the target hardware/settings.
5. Evidence file locations, known defects, and integration instructions.
6. Lead review outcome; visual tasks also record the game owner's gate decision.

Large builds/video captures may live outside source control; record their accessible
locations. Do not create empty evidence files that imply checks were performed.
A task moves through ready -> in progress -> review -> done. Waiting means unmet
prerequisites; blocked means assigned work cannot continue and records the cause.

## Source of Truth

[Task backlog](../TASKS.md) owns milestones, acceptance-case definitions, and estimates.
[Dependencies](../DEPENDENCIES.md) owns ordering and contract handoffs.
[Invariants](../INVARIANTS.md) owns behavioral rules. These packets supply bounded
execution instructions. Update the board and affected documents together when scope
or dependencies change; ask the lead to resolve contradictions before implementation.
