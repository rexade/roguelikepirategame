# Engine Bootstrap Build

T01 passed Lead review. See [the evidence report](evidence/T01/REPORT.md)
for verified build/test results. This does not establish visual-gate acceptance.

## Toolchain

Use Unity **6000.3.24f1**, revision **4e7b9b5b6244**, Windows x64.
The editor was selected from Unity's [official release page](https://unity.com/releases/editor/whats-new/6000.3.24f1).
Unity [lists 6.3 LTS support through December 2027](https://unity.com/releases/unity-6/support).
Use a valid Unity license; no paid tools or assets are required by this project.
The local editor installation is `D:/u6-t01`. Use a short editor installation path:
the Windows installer silently omitted HDRP files when the full extracted path
exceeded 260 characters beneath this workspace's `.tools` directory. Install this
same version through Unity Hub on other workstations, checking path lengths.

UI selection: built-in UI Toolkit, version tied to the editor. Its default runtime
event system supports the new Input System without a scene EventSystem; see
[Unity's input documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Runtime-Event-System.html).
Use native focus/navigation and button activation. Controller acceptance is deferred.

Serializer selection: Unity's `com.unity.nuget.newtonsoft-json` 3.2.2
([official package documentation](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)).
Later persistence work must use explicit DTOs, keep serializer dependencies out of
domain rules, validate data, and leave polymorphic type-name deserialization disabled.
T01 does not define a save schema or implement persistence.

| Dependency | Pin | Source |
| --- | --- | --- |
| HDRP, Core RP, Shader Graph, VFX Graph | 17.3.0 | Bundled editor packages |
| Input System | 1.20.0 | Unity package registry |
| Test Framework | 1.6.0 | Bundled editor package |
| Newtonsoft JSON | 3.2.2 | Unity package registry |
| UI Toolkit | 6000.3.24f1 | Built into editor/runtime |

`Packages/packages-lock.json` records the complete resolved dependency graph.
Unity package `LICENSE.md` files identify the Unity Companion License; preserve
those files and package `Third Party Notices` when redistributing package content.
The JSON package's notices include MIT-licensed Newtonsoft.Json and Json.Net.Unity3D;
preserve their copyright/permission notices with distributions containing them.
The editor/runtime remains subject to the user's Unity license. No third-party
asset-store content is included. Recheck licensing/attribution at distribution time.

## Windows Build

From PowerShell in the project root, with the editor closed:

```powershell
$unity = 'D:/u6-t01/Editor/Unity.exe'
$project = (Get-Location).Path
$process = Start-Process -FilePath $unity -WindowStyle Hidden -PassThru -ArgumentList @(
    '-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'PirateGame.Bootstrap.Editor.ProjectSetup.BuildWindows',
    '-logFile', ('"' + $project + '/docs/evidence/T01/rebuild.log"')
)
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity build failed: $($process.ExitCode)" }
```

Output: `Builds/Windows/PiratePrototype.exe` plus its adjacent data/DLL directories.
Copy the entire output directory together. This is a Windows x64 Mono development
build using the editor's bundled Windows support; IL2CPP and Visual Studio are not
required for this bootstrap. Do not use `-nographics` for rendering verification.
`ProjectSetup.Validate` runs configuration and editor JSON smoke checks without
building. The development player also logs a JSON DTO round-trip result on startup.
`CreateInitialAssets` was the one-time authoring command and refuses to overwrite
existing scenes. Normal restores/builds consume the checked-in serialized assets.

## Source and Restore

Keep `Assets` with every `.meta`, `Packages` including the generated lockfile,
and `ProjectSettings` together. Never transfer `Library`, `Temp`, or local editor
installation files. Open the folder in the exact pinned editor and let package
restore and asset import finish before running checks. Internet access is needed
for an uncached restore. Do not upgrade packages when opening the project.

The enclosing Git repository currently resolves to `C:/Users/henri` and has no HEAD.
T01 does not initialize, stage, or change that parent repository. The verification
script creates a separate test repository from the Unity source files under
`.tools`, commits that snapshot, and clones it with no `Library` directory:

```powershell
./docs/evidence/T01/VerifyCleanCheckout.ps1 -RunName T01-verification-repeat
```

Use a new RunName for each run. The script records the fixture commit, checkout
location, editor, exit status and build log under `docs/evidence/T01`. The fixture
commit identifies only the tested Unity source snapshot, not a project/parent
repository commit or Lead acceptance. It restores the exact pinned packages and
builds without re-running the one-time scene authoring command.

## Reference Configuration

Windows x64, 1920x1080, linear color space, HDRP native resolution, ray tracing off,
dynamic resolution/upscaling off, VSync off, no frame cap. Reference workstation:
Ryzen 7 5800X3D, RTX 2070 SUPER 8 GiB, approximately 32 GiB RAM, Windows 11.
Keep water quality unchanged when evaluating performance. The T02 owner measures
and reviews the target protocol; T01 does not claim visual or performance acceptance.

## Smoke Check

For the automated keyboard integration check, close the editor and run:

```powershell
$project = (Get-Location).Path
$process = Start-Process -FilePath 'D:/u6-t01/Editor/Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList @(
    '-batchmode', '-projectPath', ('"' + $project + '"'),
    '-runTests', '-testPlatform', 'StandaloneWindows64',
    '-testResults', ('"' + $project + '/docs/evidence/T01/ui-player-tests.xml"'),
    '-logFile', ('"' + $project + '/docs/evidence/T01/ui-player-tests.log"')
)
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity tests failed: $($process.ExitCode)" }
```

Keep the test player focused when it opens. The test allows 30 seconds to acquire
focus, then sends keyboard device events through the Input System and checks
Tab, Shift+Tab, Enter, and navigation in both directions. An unfocused batch-mode
Editor is not a valid runtime UI navigation target. This test does not substitute
for the standalone mouse and rendering smoke check below.

Start the standalone build. Confirm the menu is readable at 1920x1080 and initial
focus is visible. Use Tab and Shift+Tab to change focus, then activate Open ocean
with Enter. Confirm the water scene opens. Activate Back with the keyboard and
repeat both transitions using mouse clicks. Use Quit to close the player.
Record screenshots and player log; report errors or missing water as failures.
No ship controls, progression, combat, or save behavior exists at this stage.
