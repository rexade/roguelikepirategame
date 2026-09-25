param([string]$Run = 'visible-final')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../../..").Path
$output = Join-Path $PSScriptRoot $Run
if (Test-Path $output) { throw 'Use a new output directory to preserve the recorded run.' }
New-Item -ItemType Directory -Path $output | Out-Null
# The task explicitly requires a visible standalone HUD check.
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720', '-t06-hud-output', ('"' + $output + '"'), '-logFile', ('"' + $output + '/Player.log"'))
$process = Start-Process "$project/Builds/T06-R1/Salvage.exe" -WindowStyle Normal -PassThru -ArgumentList $arguments
$process.WaitForExit()
"ExitCode=$($process.ExitCode)" | Set-Content "$output/exit.txt"
if ($process.ExitCode -ne 0) { throw "Player exit $($process.ExitCode)" }
