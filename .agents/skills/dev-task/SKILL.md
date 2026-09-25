---
name: dev-task
description: Implement an assigned repository task packet, verify its acceptance criteria, and produce the developer handoff for Lead review. Use when asked to implement or fix a specified task in docs/tasks/; not for open-ended investigation, task planning, or independent acceptance review.
---

# Dev Task

Complete the assigned task within its documented scope and provide reproducible
verification evidence for the Lead. The user's request to implement a named task
is its assignment; do not request the same authorization again.

## Pickup

Resolve paths from the repository root. Read applicable AGENTS.md files, the
specified packet, docs/tasks/README.md, and its linked context and contracts.
If the task is not identified and cannot be inferred, ask which packet to use;
do not select the next backlog item on your own.

Verify that required inputs exist and prerequisites have been accepted. A ready
packet or a planned path does not prove that an engine project, contract, or
accepted build exists. If required inputs are missing, report the precise blocker
and continue only work independent of it. Do not invent replacement contracts or
bypass rendering and owner-decision gates.

At pickup, keep packet and dispatch-board status consistent with the assignment
and record identifiable input revisions/builds and the assignee. Use available
snapshot identifiers when Git is unavailable; never invent commit hashes.

## Implementation

Implement only the assigned scope and acceptance criteria. Follow existing
project patterns and the packet's owned paths. Parent directories do not grant
ownership of other tasks' files. Preserve concurrent edits and Unity asset GUIDs;
keep .meta files with their assets when moving or renaming them.

Shared settings, manifests, assembly definitions, contracts, input assets, and
production composition follow the dispatch ownership rules. When a required
change falls outside the assignment, document the proposed change, affected
consumers, and why it is needed for the Lead; continue independent work. Honor
any explicit scope expansion already authorized by the user.

Do not weaken acceptance criteria or modify tests merely to conceal a failure.
Do not start successor tasks or introduce unrelated refactors. If the task spec
conflicts with an invariant or accepted contract, identify the conflict for the
Lead instead of silently choosing a new design.

## Verification and handoff

Run the task's required checks and targeted regression checks appropriate to the
changed behavior. Record exact commands or editor steps and distinguish passed,
failed, and not run. If Unity, hardware, or another required tool is unavailable,
state which criteria remain unverified and why. Never infer runtime or visual
success from static inspection.

Write docs/evidence/Txx/REPORT.md using the dispatch handoff format. Include input
and output revisions/builds, changed paths, delivered behavior, authorized
contract changes, results for each acceptance criterion, reproduction steps,
evidence locations, known defects, and integration instructions. Create evidence
only for work actually performed; retain existing Lead review records and clearly
identify which revision each review covers.

Move the packet and board to review when deliverables and required checks are
complete under the dispatch rules. Otherwise record remaining work or the blocker
accurately. Do not mark your own work done or invent Lead/owner acceptance.
Finish with a concise handoff naming the report, verification results, and any
issues requiring Lead attention.
