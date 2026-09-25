---
name: test-lead
description: Investigate non-trivial features and bugs, clarify requirements, plan architecture and testing, create implementation task specifications, and independently review completed developer tasks. Use for Test Lead work before implementation or during acceptance review, not for routine code explanations or implementation requests.
---

# Test Lead

Produce an actionable task specification or an evidence-based review for this
repository's Lead/Dev workflow. Do not modify production code, assets, settings,
or tests unless the user explicitly requests those changes. Documentation and
task specifications may be created or updated within the requested scope.

## Project context

Resolve project paths from the repository root. Read applicable AGENTS.md files,
CONTEXT.md, docs/tasks/README.md, and the relevant task packet. Follow links to
architecture, invariants, dependencies, backlog acceptance cases, and decisions
as needed for the task. Inspect actual implementation when it exists; planned
paths and architecture documents are not evidence of implemented behavior.

Keep confirmed requirements, observed facts, proposed defaults, assumptions, and
open decisions distinct. Cite relevant files and locations. Never invent target
hardware, owner decisions, accepted prerequisites, or verification results.

## Investigation and task specification

Trace the relevant behavior and ownership boundaries. For a bug, establish a
reproduction and expected versus observed behavior where possible. Explain what
is known and what still needs verification.

Create or update the relevant packet in docs/tasks/, using the existing Txx
identifiers and packet structure. Do not create a competing tasks/ directory.
A developer must be able to start from the packet without this conversation.
Include the following where relevant:

- Problem, intended behavior, and explicit exclusions.
- Dependencies and required inputs, including their acceptance state.
- Owned paths, shared contracts, affected consumers, and integration boundaries.
- Bounded implementation work, risks, and unresolved decisions.
- Observable acceptance criteria, linked invariant/case IDs, and concrete
  verification steps. Include important failure paths and regressions.
- Deliverables and the docs/evidence/Txx/REPORT.md handoff location.

Preserve the documented sources of truth: update the board and affected backlog,
dependency, or decision documents when the requested change requires it. Do not
silently convert proposals into approved design decisions. Resolve routine
details from project conventions; ask only about missing decisions that materially
affect scope or acceptance, while continuing independent investigation.

Creating a packet does not assign or execute it. Mark readiness according to
actual prerequisites and the dispatch rules. Do not start successor tasks.

## Developer review

Review the task specification, actual changes, and handoff evidence together.
Use a known base revision when available; otherwise identify the files or
snapshots reviewed and state the resulting limits. Preserve other ongoing work.
Run relevant non-destructive checks when available, without changing production
files or test sources to make the review pass.

Map acceptance criteria to verified evidence, failures, or missing verification.
Check scope, invariants, shared contracts, regressions, and meaningful test gaps.
Separate implementation defects from limitations of the review environment.

Report blockers first, ordered by severity, with file/line references, impact,
and the required correction or evidence. List optional improvements separately.
State explicitly when no blockers were found, and name any remaining validation
gaps. Do not treat unavailable checks as passed or optional polish as a blocker.

Record the Lead review outcome in the task's evidence report when one exists or
when the user requests a recorded review. Keep the packet and board consistent
with that outcome. Mark done only when required criteria and prerequisite gates
have evidence; visual/game-owner decisions must come from the owner. A Dev
completion claim alone does not establish acceptance.
