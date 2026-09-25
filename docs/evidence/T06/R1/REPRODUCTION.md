# T06-R1 checks

From the project root, pinned Unity at D:/u6-t01/Editor/Unity.exe:

```powershell
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Tests
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Regression
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Rules
./docs/evidence/T06/R1/RunUnity.ps1 -Mode Build
./docs/evidence/T06/R1/RunPlayer.ps1
./docs/evidence/T06/R1/VerifySources.ps1
```

Use new evidence output paths on reruns to preserve this submission. RunPlayer
refuses an existing run directory (default visible-final). Before.md describes the failing-before
run. RunUnity uses PlayMode for T06/T04 and EditMode for T03; build uses the
task-local RegionAuthoring.BuildR1 method and writes Builds/T06-R1/Salvage.exe.

For ordinary manual use, launch that executable without evidence arguments.
Check the neutral HUD before pressing E, then follow INTEGRATION.md's route.

The evidence player uses -screen-fullscreen 0 -screen-width 1280 -screen-height
720 -t06-hud-output <absolute-output-directory> -logFile <output>/Player.log.
HudCapture disables keyboard input, pauses at the initial boundary, and writes
startup-ready.txt. Capture the real player window before adding continue.txt
to the output directory. The driver then submits throttle and interaction intents
through the accepted simulation. It stops at Z=78 near the wreck, pauses at a
completed boundary, then unpauses synchronously for one pickup attempt and pauses
again before another physics step. It recreates sources and writes cargo-ready.txt
with ledger assertions.
Capture the player window again, then add quit.txt to let it exit with its result.
The route does not teleport or claim manual keyboard sailing.

Recorded screenshots use the computer-use sky API (Windows.Graphics.Capture),
not Camera.Render or a render target. The returned window was selected by its
Builds/T06-R1/Salvage.exe application path; exactly one match was required. After
activation, sky.get_window_state captured the visible window including IMGUI and
title bar. Its returned JPEG bytes were saved unchanged as startup.jpg and
cargo-full.jpg. The client area is 1280x720; window images include chrome.

Final-build startup capture is visible-repeat/startup.jpg; CargoFull capture is
visible-final/cargo-full.jpg. The first attempt to activate the final-build
startup window was interrupted by detected user input, so a fresh process was
used for its startup image. No startup image is claimed for visible-final/.

Inspect startup-ready.txt, cargo-ready.txt, exit.txt and Player.log together with
the images. A screenshot proves the displayed state; the ledger trace and tests
establish bundle preservation and no refill after recreation.

The initial run in visible/ and Build-initial.log used a driver that checked
LastResult while injecting an interaction every fixed tick. It missed the
transient full-cargo feedback, sailed past the wreck, and exited 1 on its timeout.
It retained five wood and the intact wreck, but does not satisfy the CargoFull
HUD capture check. The revised driver and visible-final/ supersede this run.
The fixture fix and tests did not change during this driver correction.
