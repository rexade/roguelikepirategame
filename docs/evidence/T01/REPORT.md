# T01 Developer Handoff

Status: accepted after Lead review. Assignee: Codex (developer). Date: 2026-09-18.
Owner assignment: implement task 01 using dev-task.

## Inputs and Repository Boundary

T00 is accepted in its report. D01-D06 qualifications and delegated D07 apply.
No Git commit exists; Git resolves to C:/Users/henri, above the workspace.
No parent repository files are staged or modified. Input SHA-256 snapshots:

| Input | SHA-256 |
| --- | --- |
| docs/TARGETS.md | 2A3AB9BD55D639C311FBB2AC201CFD25959D167B198C3E34894F2FBB40EDDA99 |
| docs/evidence/T00/REPORT.md | 119B108BA0A94EBABFE4C4DAE5D29AF3D2B905FBF5789D1F932CDC7822D8A91F |
| docs/decisions/001-engine-and-rendering.md | 45E9F672CAC1C355D124C586F46B9DC72A0ACD20FBAD90AF1AEB002EBBC03377 |

T01 reserves Packages, ProjectSettings and bootstrap assets until accepted handoff.
No shared gameplay contracts exist or are introduced. T02 remains waiting.

## Environment Discovery

`git rev-parse --show-toplevel` returned `C:/Users/henri`.
`git rev-parse HEAD` failed because no HEAD revision exists.
`Get-FileHash` produced the input hashes above.
No applicable AGENTS.md was found in the workspace or its ancestors.
No Unity editor was found in the standard installation location or Hub's
configured `D:/unity` location. `C:/Program Files/Unity Hub` is an empty directory;
attempting its Unity Hub.exe failed with CommandNotFoundException.
The existing machine license was accepted by the editor for restore, build and tests.

## Acceptance Results

Implementation includes pinned manifest/editor, Unity-generated lockfile/settings,
Bootstrap and WaterTest scenes, UI Toolkit menu/theme, HDRP High256 water,
native 1080p reference settings, and editor/player serializer smoke checks.
No gameplay framework, progression or shared gameplay contracts are included.

| T01 acceptance criterion | Developer result | Evidence |
| --- | --- | --- |
| Fresh checkout opens and builds a runnable Windows player | Passed | clean-build.log, clean-checkout.json, final-player.log, final-*.png |
| Editor/manifest/lockfile recorded; UI supports keyboard/mouse | Passed | ProjectVersion.txt, manifest/lockfile, package-inventory.json, ui-player-tests.xml, final mouse smoke |
| No gameplay framework or progression implementation | Passed, source inspection | source-files.txt and authored bootstrap source/scenes |
| Reproducible handoff with results and remaining limitations | Complete | This report and BUILD.md; Lead review remains pending |

Checks performed:

- Installer signature: Valid, signer Unity Technologies SF. Initial installer exit 0.
- Package restore: passed; exact graph in package-inventory.json and lockfile.
- Initial compilation: failed on internal HDRP global-settings type and then a
  missing namespace; both corrected. Later setup compiled and exited 0.
- Configuration and editor JSON round-trip: passed in build.log.
- Final asset metadata pairing: 15 authored asset files checked, zero missing
  .meta files; all authored subdirectories also have metadata.
- Serialized settings: ForceText (2), Visible Meta Files, activeInputHandler 1,
  1920x1080, linear color, two enabled build scenes, water enabled at 256.
- Initial Windows build: failed, 59 errors. Preserved in build.log. No runnable
  build is claimed from that attempt.
- VerifyCleanCheckout.ps1 PowerShell syntax: passed.
- First isolated clean-checkout Windows build: passed, exit 0, zero build errors.
- Final isolated clean-checkout Windows build: passed, exit 0, zero errors,
  reported size 203,505,804 bytes. No substantive tracked-source diff after import
  and build; Unity rewrote line endings and generated temporary test-resource metadata.
- Standalone mouse smoke: passed. Open ocean and Back changed scenes; Quit exited
  the process. The water rendered and changed between captures. Runtime JSON
  round-trip passed at 1920x1080 on D3D11; player-mouse.log contains no runtime errors.
- Headless Editor keyboard test: failed. Diagnostic input checks showed discarded
  keyboard events without Game-view focus; allowing background device input did
  not bypass UI Toolkit's separate unfocused-navigation suppression. The focused
  Windows-player test is the required replacement environment, not a relaxed assertion.
- Focused Windows-player keyboard integration: passed, 1 test / 0 failures, editor
  exit 0. Checks initial focus, Tab, Shift+Tab, Enter, ocean scene entry, Back,
  and restored focus. See ui-player-tests.xml and ui-player-tests.log.
- Final delivered-build smoke: passed startup, mouse Open ocean, rendered ocean,
  Back, and Quit. The process exited after Quit. final-player.log confirms the
  runtime JSON check, pinned version, RTX 2070 SUPER, D3D11 and 1920x1080, with no
  runtime errors. final-bootstrap.png, final-ocean.png and final-back.png show the
  exact delivered build.

## Installation Failure and Recovery

The workspace-local editor installer silently omitted files exceeding MAX_PATH.
For example, RayTracingIndirectDiffuse_APVOff.raytrace's full path had 265
characters and was missing; the adjacent 254-character .hlsl file existed.
The water subgraph's 262-character .meta path was also missing. Missing HDRP
resource references caused import errors and a duplicate null-resource key (0)
in HDRPBuildData shader processing during the first build.

The same installer completed successfully at D:/u6-t01 (exit 0). Its corresponding shader path
is 205 characters and the previously missing shader and metadata are present.
The failed generated Library cache was preserved under .tools/T01-failed-import
so restoration can run without reusing incomplete copied packages. No external
package source was patched and no rendering feature was weakened to hide errors.

The repaired fresh restore compiled without the missing-resource errors. Its scene
validation caught missing serialized UI panel references; the authoring code and
both scenes were corrected. `finalize.log` records exit 0 with the configuration
and JSON checks passed. No gameplay or rendering quality was changed to hide a failure.

## Output and Changed Paths

Final test-fixture source commit: `172bcab89cc5de8021d713527b906d4700f42854`.
This is an isolated verification snapshot, not a commit in the parent repository.
`clean-checkout.json` records its source/clone locations and build result.
The earlier fixture was `0df0165ab07a044bb131baf2f136f371f02d3cca`; its build
metadata/log are retained as clean-checkout-initial.json and clean-build-initial.log.
Unity's generated HDRP default-profile/global-settings normalization is retained
in the final source snapshot, together with the completed keyboard test.

Runnable output: `C:/Users/henri/Documents/roguelikepirategame/Builds/Windows/PiratePrototype.exe`.
The complete adjacent output directory is required. It was copied from the final
clean checkout; `build-files.json` records every delivered file's SHA-256 and size.
Executable SHA-256: `30D4B41A85303ABF83C686F06B75AB1681471C79B9DC7FE530EAD09099FDF63D`.
Game assembly SHA-256: `7D0A2555A18CB4D6FE3F8A8F2CCF0D3BB11F88F30F1B0BDBAA94A141F69630C2`.
Package notices are included in `Builds/Windows/ThirdPartyNotices`.

- `.gitignore`: Unity/build/tool exclusions, including temporary performance-test resources.
- `Packages/manifest.json`, `Packages/packages-lock.json`: editor-compatible dependency pins.
- `ProjectSettings/`: Unity-generated Windows, graphics, quality, editor, input and build settings.
- `Assets/_Game/Bootstrap/`: menu, development JSON probe, editor setup/build checks,
  UI theme, panel asset, HDRP assets/default volume profile, lighting profiles/material,
  and a keyboard integration test assembly excluded from ordinary player builds.
- `Assets/_Game/Scenes/Bootstrap.unity` and `Assets/_Game/Scenes/Tests/T02/WaterTest.unity`.
- Corresponding `.meta` files, including necessary parent folder metadata.
- `docs/BUILD.md`, this evidence directory, T01 packet and dispatch-board status.

The water scene contains the stock HDRP ocean preset, directional light, global
volume, a camera pitched 60 degrees down, and one scale-reference cube. It is a
bootstrap rendering fixture, not a completed T02 art or ocean-quality demonstration.
The UI offers Open ocean, Back and Quit. No campaign, progression, gameplay rules,
save system, ship controller or successor task has been implemented.

## Reproduction and Evidence

All PowerShell commands run from `C:/Users/henri/Documents/roguelikepirategame`.
The exact build command is in `docs/BUILD.md`; the one-time authoring used the same
batch-mode invocation with `-executeMethod PirateGame.Bootstrap.Editor.ProjectSetup.CreateInitialAssets`.
The corrective authoring invocation used `ProjectSetup.FinalizeBootstrap` and log
`docs/evidence/T01/finalize.log`. Normal builds run only `ProjectSetup.BuildWindows`.

Final clean-checkout invocation:
`./docs/evidence/T01/VerifyCleanCheckout.ps1 -RunName T01-verification-final`.
The script copies only Assets, Packages, ProjectSettings and .gitignore into a
new local test repository, records a fixture commit, clones it with no Library,
and runs the pinned editor's Windows build. It never stages parent-repository files.

The exact standalone test command is in BUILD.md, under Smoke Check. It uses
`-runTests -testPlatform StandaloneWindows64` and writes ui-player-tests.xml.
The keyboard test ran in a focused Windows player on the reference workstation;
events entered through InputSystem.QueueStateEvent, not direct button callbacks.
Desktop automation's injected key presses did not affect the ordinary player;
physical owner keyboard confirmation was requested but not received. Neither is
reported as a manual keyboard pass. The successful player integration test
provides the keyboard verification; desktop mouse input provides the mouse check.

Reference: Windows x64 Mono development player, D3D11, 1920x1080, linear color,
native resolution, ray tracing/upscaling/dynamic resolution off, VSync off,
uncapped frame rate. HDRP water simulation is High256. Hardware is the accepted
T00 workstation; GPU/driver capture is `gpu-inventory.txt` (RTX 2070 SUPER 8 GiB,
driver 616.64). No performance threshold or visual gate is assessed by this task.

Evidence files:

- `import.log`, `setup.log`, `setup-final.log`: early compilation/setup attempts.
- `build.log`: initial failed build, retained for the installation-path diagnosis.
- `installation-path-check.json`: missing 265-character path versus valid 205-character path.
- `restore-final.log`: repaired fresh import and the subsequently corrected panel-reference finding.
- `finalize.log`: successful configuration and editor serializer validation.
- `package-inventory.json`: exact resolved versions and dependency sources.
- `clean-build.log`, `clean-checkout.json`: isolated checkout verification.
- `VerifyCleanCheckout.ps1`: executable reproduction script.
- `player-mouse.log`: development build startup/configuration and serializer result.
- `ocean-mouse.png`, `ocean-later.png`, `bootstrap-mouse-back.png`: observed mouse
  navigation and water rendering captures from the first clean-checkout build.
- `ui-tests-unfocused.xml`: retained failed headless Editor diagnostic result.
- `ui-player-tests.xml`, `ui-player-tests.log`, `player-keyboard-test.log`: successful
  focused Windows-player keyboard test and development startup result.
- `source-files.txt`: exact tracked file list of the final Unity source fixture.
- `asset-metadata-check.json`: final authored-file metadata check.
- `build-files.json`: final delivered output inventory and hashes.
- `final-player.log`, `final-bootstrap.png`, `final-ocean.png`, `final-back.png`:
  final clean-checkout player startup, rendering and mouse navigation evidence.

## Known Limitations

No unresolved T01 functional defect was observed in the completed checks.
Physical owner keyboard input was not observed; keyboard navigation is covered
by the focused Windows-player integration test, as distinguished above. Earlier
headless Editor test failures are retained and are not reported as passes.
The empty test-package Resources folder metadata generated during a fresh build
is not an authored asset and is not needed in source. No controller, IL2CPP,
other-OS, performance-threshold, visual/art gate, or gameplay test was run.
Those checks are deferred or outside T01. No visual or performance gate pass is claimed.

## Integration and Lead Review

Keep assets and metadata together, retain the exact editor/lockfile, and follow
BUILD.md. At acceptance, shared package/settings/bootstrap ownership passes to the
Lead; T02 receives its water scene. T02 remains waiting until that acceptance.
Lead should update the older planning summaries in docs/TASKS.md and
docs/DEPENDENCIES.md that still describe T01 as ready/no packages installed; these
documents are outside this packet's implementation-owned paths.
Lead review: accepted. See the independent review below; T02 is ready for assignment.

## Lead Acceptance Review - 2026-09-18

Outcome: accepted for T01. No blocking implementation defects or missing T01
acceptance evidence found. T02 is ready, unassigned; no successor work started.

Reviewed the actual C# sources, serialized scenes/settings, manifest/lockfile,
test source/XML, clean-build logs, runtime logs and final-ocean.png. The developer
snapshot is 172bcab89cc5de8021d713527b906d4700f42854; there is no parent-project
base commit. Comparison of every tracked snapshot file with the workspace found
no substantive differences after normalizing CRLF/LF. All 293 delivered files
matched build-files.json hashes. Package manifest pins match the lockfile, and
all inspected authored files/folders under Assets/_Game have their metadata.

Independent execution: invoked the pinned editor on the existing isolated checkout
with -batchmode -quit -executeMethod PirateGame.Bootstrap.Editor.ProjectSetup.BuildWindows,
using docs/evidence/T01/lead-build.log as a separate log. Exit 0; configuration/JSON
validation passed and build reported Succeeded with zero errors. This was a cached
rebuild of the developer's isolated checkout, not a second uncached restore. The
original clean-checkout evidence was inspected and its source identity verified.
The delivered Builds/Windows directory and production source were not modified.

| Acceptance criterion | Lead result | Evidence |
| --- | --- | --- |
| Clean checkout and runnable Windows player | Accepted | Verified snapshot identity, original fresh-build evidence, independent successful rebuild, delivered-file hashes and runtime evidence |
| Pinned dependencies and keyboard/mouse UI | Accepted | Manifest/lockfile/editor match; reviewed Input System test code and passing focused-player XML; mouse/navigation logs and captures |
| No gameplay/progression scope expansion | Accepted | Source inspection: bootstrap UI, serializer probes, authoring/build validation and focused test only |
| Reproducible handoff | Accepted | BUILD.md commands, preserved failed attempts, source/build identity and limitations are documented |

Keyboard and mouse interaction were not rerun manually during this Lead turn.
Acceptance uses the inspected focused-player integration test and developer mouse
evidence, tied to the verified source/build. This is not a new physical-keyboard
pass. No T02 visual/art/performance gate is inferred from the bootstrap screenshot.

Optional improvement (non-blocking): VerifyCleanCheckout.ps1 lines 32-41 use fixed
clean-checkout.json/clean-build.log output names despite distinct RunName folders.
A repeat can overwrite previous evidence. Use run-specific evidence filenames or
archive the previous pair when improving the verification tooling. This review
used a separate lead-build.log and preserved the developer's original records.

Ownership: shared settings, packages and Bootstrap transfer to the Lead. T02 may
take its water-test scene through the existing dispatch rules. Runtime quality,
minimum hardware, art workflow and gameplay remain later-task validation work.
