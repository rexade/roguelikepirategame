$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$output = Join-Path $PSScriptRoot 'captures'
New-Item -ItemType Directory -Force $output | Out-Null
$arguments = @('-screen-width','1280','-screen-height','720','-screen-fullscreen','0', '-t06-output', ('"' + $output + '"'), '-logFile', ('"' + $output + '/Player.log"'))
$capture = Start-Process "$project/Builds/T06/Salvage.exe" -WindowStyle Hidden -PassThru -ArgumentList $arguments
if (!$capture.WaitForExit(210000)) { $capture.Kill(); throw 'T06 capture timed out' }
if ($capture.ExitCode -ne 0) { throw "Capture exit=$($capture.ExitCode)" }
Get-Content "$output/route-result.txt", "$output/capture.txt"
