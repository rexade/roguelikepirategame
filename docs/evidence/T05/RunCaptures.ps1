param([string]$Revision = '')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
foreach ($weapon in @('cannon','repeater')) {
    $root = if ($Revision) { Join-Path $PSScriptRoot $Revision } else { $PSScriptRoot }
    $output = Join-Path $root $weapon
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $arguments = @('-batchmode','-screen-width','1280','-screen-height','720','-t05-output',('"' + $output + '"'),'-logFile',('"' + $output + '/player.log"'))
    if ($weapon -eq 'repeater') { $arguments += '-t05-repeater' }
    $build = if ($Revision) { "Builds/T05/$Revision/Combat.exe" } else { 'Builds/T05/Combat.exe' }
    $process = Start-Process (Join-Path $project $build) -WindowStyle Hidden -PassThru -ArgumentList $arguments
    if (!$process.WaitForExit(180000)) { Stop-Process -Id $process.Id; throw "Capture timed out: $weapon" }
    if ($process.ExitCode -ne 0 -or !(Test-Path "$output/result.txt")) { throw "Capture failed: $weapon" }
    & ffmpeg -y -loglevel error -framerate 30 -i "$output/frame-%04d.jpg" -c:v libx264 -pix_fmt yuv420p "$root/$weapon.mp4"
    if ($LASTEXITCODE -ne 0) { throw 'Video encoding failed' }
    Get-Content "$output/result.txt"
}
