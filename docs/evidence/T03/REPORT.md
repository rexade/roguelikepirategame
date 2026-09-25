# T03 Developer Handoff

Status: done. Assignee: Codex (gameplay architect). Date: 2026-09-18.
Output contract/source revision: **T03-r1**. Lead review: **accepted**.
The Lead review below supersedes historical pending statements in the developer
handoff. T04/T07 are ready; no downstream task has been started.

## Input and Output Identity

The owner assigned task 03 after T02's provisional acceptance of GATE-01/02/03.
T02's visual/workflow limitations remain in force; this task makes no new visual
or performance claim. Accepted inputs were verified from the existing report:

- T02 `source-files.json` SHA-256:
  `6E60AA8FEDBFB3BCFC0E0C0549D194CD8B08F894A12C5B2BF8698F3D4D2CE4E8`.
- T02 `build-files.json` SHA-256:
  `809ACC3A15A75EF2F9E8EBE26F1A2371E784598248BE4611DEDA4AD20F000187`.
- Accepted T02 game assembly SHA-256 recorded by its handoff:
  `46E436A75B8E4CF9B5FAE02C42EC7B9B9E96077289EB5692B12C7AEB508B8B84`.
- Pinned Unity 6000.3.24f1 at `D:/u6-t01`, existing package lock and architecture,
  invariants, dependency plan and task acceptance cases.

The project has no Git HEAD; the enclosing user-directory repository was not
initialized, staged or changed. Output is identified by `source-files.json`,
38 source/metadata/contract entries, SHA-256:
`EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F`.
The manifest identifies the source exercised by the final test run plus the
published contract document; report/status files are outside that manifest.

## Changed Paths

- `Assets/_Game/Core/`: immutable values, definitions, state records, loadout/stat
  rules and engine-free `PirateGame.Core.asmdef`.
- `Assets/_Game/Application/`: `CampaignSession`, ports and Core assembly reference.
- `Assets/_Game/Content/Definitions/`: ScriptableObject-to-immutable adapters,
  before-play/build validation and scoped runtime/Editor assembly definitions.
- `Assets/_Game/Tests/T03/`: 33 NUnit cases, reusable rule catalog, fake ordered
  storage and fake arrival, with scoped Editor test assembly.
- Matching Unity metadata, including newly needed Content/Tests folder metadata.
- `docs/CONTRACTS.md`: exact r1 signatures, ownership and consumer protocol.
- `docs/evidence/T03/`: this report, runners, engine-free compile fixture, logs,
  NUnit XML, source manifest and preservation/boundary check results.
- `docs/tasks/T03-rules-contracts.md`, `docs/tasks/README.md`: assignment, input
  identities and synchronized review status.

The manifest enumerates individual source files. All 49 files in the accepted
T02 source manifest still match their hashes. Existing shared settings, package
manifests, Bootstrap, production scenes and previous assembly definitions were
not edited. Unity generated metadata for the new assets; no GUIDs were replaced.

## Delivered Behavior

One injected session owns campaign and expedition state. Commands validate and
prepare immutable snapshots before ordered storage calls. Failed commits freeze
the candidate, retain the previous committed state and publish no success;
retry submits the identical candidate. Commit events are consumed from a queue.
World arrival is separately retryable and input remains locked until readiness.

Docking queues until the fixed-tick damage total is processed. Lethal damage wins;
one expedition receives one outcome. Whole-bundle loot and depletion commit
together; weighted capacity and all resource arithmetic are checked. Sinking
retains purchases, bank and ownership and permits free embark with computed full
health. Shared upgrades, travel prerequisites, loadout validation and the specified
stat formula are implemented in engine-free C#.

Definition adapters copy all values; runtime changes cannot mutate authored rows.
Duplicate world identities report both origins. Ports specify input intent,
simulation time, entity/expedition capture and restore, save revisions and arrival.
Captured checkpoints cannot refill loot, change cargo, resurrect defeated entities
or discard world history. Unknown required content returns validation errors.

These are initial T03 contracts, not revisions to an accepted gameplay contract.
The new Core assembly/asmref follow the existing dependency plan's explicit
Core/Application co-location rule; the new adapter/test assemblies stay inside
owned paths. No extra settings/package permission was required. Lead acceptance
of r1 and subsequent shared-contract ownership transfer remain pending.

## Acceptance Results

| Requirement | Result | Evidence |
| --- | --- | --- |
| AC-01 / INV-02,06: bank seven wood once | Passed | `AC01_DoubleDockBanksSevenOnce` |
| AC-02 / INV-02: damage before dock, one outcome | Passed | `AC02_LethalDamageWinsOverQueuedDockAndFailureFreezesSink` |
| AC-03 / INV-03,04,08: cannot afford six with five | Passed | `AC03_UnaffordableUpgradeLeavesBankAndTierUnchanged` |
| AC-04 / INV-04,05: three-unit bundle with two free | Passed | `AC04_WholeBundleRejectsWithoutDepletionAndDuplicateCannotCredit`; mixed-weight and overflow cases |
| AC-06 / INV-07: empty-bank sinking preserves playable upgraded ship | Passed | `AC06_SinkWithEmptyBankPreservesUpgradedLoadoutAndFreeEmbark` |
| AC-08 / INV-09: reject at-sea/inactive travel | Passed | `AC08_TravelAtSeaOrInactiveDestinationChangesNothing` |
| AC-15 / INV-12: duplicate ID diagnostics identify both regions | Passed | `AC15_DuplicateAuthoredIdentityReportsBothRegions` |
| AC-16 / INV-13: shared owned/compatible loadout validation | Passed | `AC16_HarborAndEmbarkUseTheSameOwnedCompatibleLoadoutValidation`; malformed loadout case |
| INV-01 rule boundary: shared state and read-only copies | Passed at rule level | shared upgrade/travel test; immutable snapshot test; Bootstrap singleton integration remains T08 |
| INV-03: no partial publication, ordered revisions, retry | Passed with fake storage | dock, purchase, embark and loot failure tests; stale checkpoint/reentrancy test |
| INV-06: authored docking gates and overflow | Passed | radius/speed boundary cases, wrong-region case, banking overflow |
| INV-08,09: shared tiers/unlocks and post-commit arrival failure | Passed at rule level | shared progression and arrival-failure tests |
| INV-12: definitions detached; missing/duplicate refs rejected | Passed | adapter/immutability, invalid-reference and numeric definition cases |
| INV-13: finite flat-plus-percent formula and lifetime | Passed | stat calculation, restored cooldown/modifier state and sinking tests |
| INV-18: no engine/HDRP dependency in Core/Application | Passed | engine-free .NET build, Unity assembly reference assertion, source/asmdef checks |
| Published contracts and fixtures | Delivered for review | `docs/CONTRACTS.md` identifies T03-r1 and explicitly states no Lead-reviewed revision yet |
| Handoff evidence and reproduction | Complete | this report and files below |

## Verification and Reproduction

Target: existing Windows workstation, Unity 6000.3.24f1, .NET SDK 9.0.100.
Tests are synchronous EditMode rules and in-memory authored adapter tests; no
graphics/GPU benchmark, scene launch or player interaction is needed for T03.

Run from the project root with no other editor instance using this project:

```powershell
./docs/evidence/T03/RunTests.ps1
dotnet build docs/evidence/T03/EngineFree.csproj --nologo
./docs/evidence/T03/VerifySources.ps1
```

`RunTests.ps1` launches the pinned editor hidden with `-batchmode -nographics
-runTests -testPlatform EditMode -assemblyNames PirateGame.T03.Tests`, records
the log/XML here, waits for exit and rejects a failed/missing result. The Core
compile targets .NET Standard 2.1, has no Unity reference or package dependency,
and treats warnings as errors. `VerifySources.ps1` verifies metadata, assembly
boundaries and accepted T02 source hashes, then emits the source manifest.

Final results:

- Unity EditMode: **33 passed, 0 failed, 0 skipped**, exit 0. NUnit test duration
  0.1147357 seconds; this excludes editor startup/import.
- Engine-free build: **passed, 0 warnings, 0 errors**.
- Source check: **38 manifest entries, 0 missing metadata, 49 unchanged inherited
  source files, 0 forbidden imports, noEngineReferences=true**.

During development the first Unity compile exposed a namespace collision with
existing `UnityEngine.Application` and an ambiguous Unity EntityId import in one
test. Both were corrected inside T03 paths. The subsequent 28-case run passed;
the final expanded 33-case run above covers the delivered revision. The first
source-verification script run exposed PowerShell JSON array nesting; the script
was corrected and rerun successfully. No test assertion was weakened to pass.

Evidence files:

- `editmode-results.xml`, `editmode.log`: final Unity execution and every case.
- `engine-free-build.log`: final standalone compilation output.
- `source-files.json`, `source-check.json`: output identity and preservation checks.
- `RunTests.ps1`, `VerifySources.ps1`, `EngineFree.csproj`: reproduction tools.
- `bin/`, `obj/`: ignored local compiler products, not source deliverables.

## Limits and Integration

No known failing T03 rule checks. Not run: real filesystem failure/recovery,
standalone player/visual tests, physical docking/combat or production scene
single-owner integration. Those belong to T04-T11 and are not established by
fake stores, an engine-free compile or EditMode results.

R1 intentionally uses synchronous commit acknowledgements and commits pickups;
disk latency/batching must be evaluated by T08. Request/outcome history is retained
without pruning. The full snapshot graph is immutable, but the owner itself is
single-threaded. Optional slots, rolled item stats and physics replay are not
part of this initial contract. No new production catalog or playable feature is
wired into Bootstrap by this rule-layer task.

Lead integration steps:

1. Review T03-r1 and this evidence; record the accepted contract revision and
   accept/change the task status. This report does not approve its own work.
2. Transfer shared contracts/definitions to Lead ownership. Update the planning
   document summaries that still describe T03 as ready/unassigned when accepting.
3. T04 consumes the session clock/input/lifecycle and hull contracts; T07 consumes
   bank, loadout, upgrade queries/commands and the executable catalog/fake ports.
4. T08 composes one session, supplies storage and world arrival, validates loaded
   snapshots, restores entities, then releases the arrival lock. Use the capture
   checkpoint overload at frozen simulation boundaries; preserve depleted and
   unloaded entity history. Respect the separate commit/readiness results.
5. Keep T04/T07 waiting until this review is accepted; do not infer acceptance
   from the presence of source files or successful developer checks.

## Lead Review

Accepted T03-r1 on 2026-09-18. No blocking defect found in the reviewed task scope.
Shared contracts/definitions transfer to Lead ownership. T04 and T07 are ready
for independent assignment, not started. T02's provisional limitations remain.

### Independent Verification

- Read all delivered Core/Application rules, state/value records, ports, authored
  adapters and Editor validation, both test suites and fixtures, against the task,
  invariants, architecture and downstream consumer packets.
- Verified all 38 T03 manifest entries before review documentation edits, and all
  49 inherited T02 source entries. Both had zero mismatches. T03 manifest SHA-256
  was EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F.
- Ran the pinned Unity editor with -batchmode -nographics -runTests
  -testPlatform EditMode -assemblyNames PirateGame.T03.Tests against this project.
  Exit 0: 33 passed, 0 failed, 0 skipped. Independent outputs:
  lead-editmode-results.xml and lead-editmode.log. This was a current-workspace
  rerun, not a clean-import or standalone-player check.
- Ran `dotnet build docs/evidence/T03/EngineFree.csproj --nologo`: exit 0,
  zero warnings/errors. Inspected noEngineReferences and assembly wiring and
  searched Core/Application sources for forbidden imports; none found.

### Acceptance Mapping

| Requirement | Lead disposition |
| --- | --- |
| AC-01/02/03/04/06/08/15/16 | Existing fixtures independently passed; code review supports the scoped banking, sinking, accounting, travel, identity and loadout behavior |
| Save failure and immutable definitions | Fake-failure/retry, reentrancy and detached-adapter cases passed; real disk durability is not claimed |
| INV-18 and published revision | Engine-free build and assembly test passed; T03-r1 accepted and recorded in CONTRACTS.md |
| Handoff and scope | Evidence sufficient for rule-layer acceptance; all inherited T02 manifest files unchanged |

### Integration Follow-up and Limits

R1 exposes no command to discover/activate a previously inactive hub, while T09
requires activation UI. The Lead must provide a tested contract extension before
T09; direct snapshot mutation or replacing the campaign owner is not an acceptable
workaround. This is outside T03's enumerated command set and does not block T04/T07.

No new regression cases were added during review. Existing tests do not establish
actual disk recovery, physical combat/docking, scene singleton ownership, production
Editor catalog-asset hook behavior, or integrated runtime performance. Those remain
downstream validation. Review is against the source manifest, not a project commit.
Only review/status documentation changed; production source/assets/settings/tests
were not edited. CONTRACTS.md's acceptance annotation and follow-up change its hash
after the developer manifest verification; that manifest remains the historical
tested submission identity, not a claim that the review annotation was test input.
