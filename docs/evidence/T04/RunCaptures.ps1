param([switch]$Footage, [string]$OutputDirectory = $PSScriptRoot,
    [string]$BuildPath = 'Builds/T04/ShipControls.exe')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
foreach ($fps in @(30, 60, 120)) {
    $prefix = if ($Footage) { 'footage' } else { 'measured' }
    $output = Join-Path $OutputDirectory "$prefix-$fps"
    New-Item -ItemType Directory -Force $output | Out-Null
    $arguments = @(
        '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
        '-t04-fps', "$fps", '-t04-output', ('"' + $output + '"'), '-logFile', ('"' + $output + '/player.log"'))
    if ($Footage) { $arguments += '-t04-footage' }
    $process = Start-Process (Join-Path $project $BuildPath) -WindowStyle Hidden -PassThru -ArgumentList $arguments
    if (!$process.WaitForExit(240000)) { $process.Kill(); throw "Capture $fps timed out" }
    if ($process.ExitCode -ne 0 -or !(Test-Path "$output/result.txt")) { throw "Capture $fps failed" }
    Get-Content "$output/result.txt"
    if ($Footage) {
        $frameCount = @(Import-Csv "$output/frames.csv").Count
        & ffmpeg -y -v error -framerate $fps -i "$output/frame-%04d.jpg" -frames:v $frameCount -c:v libx264 -pix_fmt yuv420p "$output/ship-$fps.mp4"
        if ($LASTEXITCODE -ne 0) { throw 'Video encoding failed' }
    }
}
