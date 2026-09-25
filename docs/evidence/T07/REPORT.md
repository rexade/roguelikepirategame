# T07 Developer Handoff

Status: review. Assignee: Codex (systems/UI engineer). Date: 2026-09-19.
Output revision: T07-r1. Lead review and owner visual decision: pending.

## Inputs and Scope

Owner assignment: "implement t07". Accepted inputs: T03-r1 and T01's UI Toolkit
choice on Unity 6000.3.24f1 (`D:/u6-t01/Editor/Unity.exe`). T03 source manifest
SHA-256: `EF99CD14263CE24B26E06ACC4CA4450872C8A1F1727F36747764125A8DF0314F`.
This workspace is inside an unrelated parent Git repository; there is no project
commit identity. `source-files.json` and `build-files.json` identify this delivery.

- Source manifest: 41 entries, SHA-256 `AF79222C0D9A023E82CE725BD626949A1F5D90221DB6CEDA72D98FB04537C1E7`.
- Build manifest: 303 entries, SHA-256 `5C16FAF230B221897171A1FEC5CBF95AA93A3B6D6A5B79A0C79A3EDB733CEC8B`.
- All 37 inherited T03 asset/metadata entries match the accepted manifest.
- Missing metadata: 0. Source/build manifests exclude evidence and status documents.

No shared contracts, packages, settings, input actions, accepted assets, or production
composition changed. Scoped assembly definitions live under owned T07 paths.
T02's existing visual limitations remain in force.

## Changed Paths

- `Assets/_Game/UI/Harbor/`: injected harbor view, stylesheet, panel settings and prefab.
- `Assets/_Game/UI/Loadout/`: draft selection view and shared-rule validation.
- `Assets/_Game/Content/Progression/`: immutable catalog adapter asset with two upgrades.
- `Assets/_Game/Scenes/Tests/T07/`: isolated scene, fixture ports, authoring/build tool,
  opt-in final-state recorder and keyboard verification route.
- `Assets/_Game/Tests/T07/`: rule/UI integration checks.
- Matching Unity metadata and new `Assets/_Game/UI.meta`.
- `docs/evidence/T07/`: checks, logs, captures, manifests and this report.
- Task packet, dispatch board and `docs/DEPENDENCIES.md`: pickup and handoff status.

## Delivered Behavior

One scene fixture creates a CampaignSession and injects it into the prefab.
The UI reads authoritative bank, tiers, unlocks and computed ship stats. It sends
purchases, loadout commits and embark through application commands. It does not
implement affordability, ownership, stat formulas, or bank mutations.

The fixture begins with 11 wood and 2 iron, explicitly granted in the initial
snapshot before session creation. Storehouse costs 5 wood, unlocks `storehouse`
and adds 5 cargo capacity. Reinforced hull costs 6 wood and 2 iron and adds 50%
health. Starter cannon, heavy cannon (+20 flat health) and dash are owned.
These values are fixture content, not a replacement production/combat catalog.

Equipment selections are drafts. Shared validation rejects incompatible or
duplicate instances. Apply commits a validated loadout; Reset discards the draft.
Embark refuses unapplied changes, then uses the application's departure validation.
At sea the harbor actions lock. Failed saves show the previous committed values;
Retry resubmits the application's existing pending candidate. Arrival errors keep
input locked and offer arrival retry. The UI never drains the owner's event queue.

## Verification

Unity EditMode integration tests: **9 passed, 0 failed, 0 skipped**. These instantiate
the delivered prefab, attach its UI Toolkit panel, focus controls and dispatch
navigation submissions. OS input and standalone visual checks are recorded separately.

| Acceptance / invariant | Result | Evidence |
| --- | --- | --- |
| AC-03 / INV-03,04: five wood cannot buy a six-wood upgrade | Passed | `AC03_FiveCannotBuySixAndNothingChanges`; same snapshot, balance and visible error |
| INV-08: valid purchase deducts/grants once; repeated tier changes nothing | Passed | `PurchaseCommandsGrantOnceAndRefreshBothViews` |
| AC-16 / INV-13: duplicate, incompatible and unowned instances rejected | Passed | both `AC16_*` cases plus `UnownedEquipmentRejectedBySharedCommand` |
| INV-13: computed stats use equipment and upgrades | Passed | heavy cannon plus hull yields 180 health at embark; storehouse yields 15 cargo |
| AC-07 shared-query portion / INV-01,08 | Passed | `TwoHarborViewsReadSameCampaignWithoutOwningCopies`; one owner, matching bank/tier/unlock queries |
| INV-03: failed save preserves committed values, retry grants once | Passed | `SaveFailureShowsCommittedStateAndRetryGrantsExactlyOnce` |
| Arrival failure and unapplied draft block departure/input | Passed | `ArrivalFailureKeepsEmbarkLockedUntilRetry`, `PendingDraftRequiresApplyThenEmbarksWithSharedStats` |
| Standalone Windows development build | Passed | `Build.log`; pinned Unity, Direct3D 11, RTX 2070 SUPER |
| UI at 1280x720, keyboard and mouse | Passed with keyboard method qualification | `keyboard-check.txt`, `keyboard-interaction.mp4`, `invalid-loadout.png`, `embarked.png` |
| Handoff, source preservation and identities | Complete | source/build manifests, `source-check.json`, this report |

During development, tests exposed a missing panel-settings reference in the
generated prefab and scene. Both references and the authoring order were fixed,
and tests now assert attachment before submitting input. Initial harness attempts
used detached views and unnecessary Play Mode reloads; final tests run in EditMode
with the actual attached panel and frame-delayed focus/submission. No rule assertion
was weakened. An initial framebuffer recording missed the UI overlay; it was
removed in favor of external window recording. The final recorder only writes
the committed state and exits after departure.

Reproduction, from the project root:

```powershell
./docs/evidence/T07/RunUnity.ps1 -Action Test
./docs/evidence/T07/RunUnity.ps1 -Action Build
./docs/evidence/T07/VerifySources.ps1
```

`-Action Create` is a one-time asset-generation step and deliberately refuses to
overwrite existing T07 assets. Tests exercise the authored content and real UI
callbacks against fake storage/arrival. They do not prove disk durability.

Open `Assets/_Game/Scenes/Tests/T07/Harbor.unity` in the editor or run
`Builds/T07/Harbor.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0`.
Use Tab/Shift+Tab, Enter and arrow keys for controls, or the mouse.
The optional `-t07-capture` flag writes `interaction/final-state.txt` under this
evidence directory and exits two seconds after a successful embark or ten minutes.
It never injects commands/input. `-t07-keyboard` additionally enables the opt-in
Input System verification route, gated by `keyboard-start.txt` in this directory.
Remove that trigger before launching, focus the application, begin recording, then
create the trigger. The route logs device-state reception and UI/state assertions
in `keyboard-check.txt`. It uses three-frame held key states, not direct commands.

Final window recording command, after the 1280x720 harbor screen is visible:

```powershell
ffmpeg -y -hide_banner -loglevel warning -f gdigrab -framerate 15 -offset_x 320 -offset_y 191 -video_size 1280x720 -i desktop -t 35 -c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p docs/evidence/T07/keyboard-interaction.mp4
```

These offsets were measured from the observed window (outer origin 319,160).
Remeasure for a different desktop/window position. Keep the app visible and open
until recording finishes. The final video uses `-t07-keyboard` without auto-exit;
earlier title-based recordings returned stale frames and were discarded.

Standalone results: keyboard navigation, dropdown selection, apply, both purchases,
repeated-tier rejection and embark all passed through injected Input System device
states. The desktop automation tool's very short OS key presses did not drive Unity
reliably; physical-keyboard/OS injection is **not independently verified**. Mouse
clicks were delivered through the desktop tool: duplicate/incompatible selection
showed errors without mutation; Reset restored the committed equipment; an unapplied
heavy cannon blocked embark; Apply changed health to 120, hull purchase to 180,
storehouse purchase changed cargo to 15, and repeated purchase left both bank ledgers
at zero. Embark displayed AtSea, disabled harbor commands and started with 180 health.
Both routes completed revision 4. Player logs contain no exception/error entries.

Final evidence: `Test.log`, `editmode-results.xml`, `Build.log`,
`keyboard-check.txt`, `keyboard-player.log`, `mouse-player.log`,
`keyboard-final-state.txt`, `interaction/final-state.txt`,
`keyboard-interaction.mp4` (35 seconds, 1280x720, 15 fps),
`keyboard-selection.png`, `embarked-video.png`, `invalid-loadout.png`,
`embarked.png`, and both manifests plus `source-check.json`.
The PNGs from the video confirm that the final recording contains changing UI
states and the committed AtSea result, rather than a stale window surface.

The 1280x720 window and recorded frames were visually inspected: labels, menus,
status messages and focus outlines fit without clipping or overlap. No frame-time
or art-quality acceptance is claimed. The persistent footer records the last
command result; resetting a draft updates inline validity but retains that history.

## Integration and Limits

T08 should instantiate the Harbor prefab, call `Bind` after UIDocument is enabled,
and inject its existing single session, frozen catalog and encounter-plan factory.
Call Bind again after recreating/re-enabling the document. Replace the fixture ports
with production persistence and arrival; do not ship the fixture campaign grant or
compose a second owner. Production content IDs must be deliberately reconciled
with T05/T06 by the Lead. No production catalog change is requested by this task.

The isolated embark flow commits an expedition and displays its state; sailing,
real saves, travel UI, controller support and production scene integration remain
out of scope. T08 verifies actual sailing effects. T09 verifies travel to another
hub; T07 checks multiple harbor readers against one campaign owner.

Lead review: pending. Owner visual decision: pending. This report does not accept
its own submission or release T08's dependency gate.
