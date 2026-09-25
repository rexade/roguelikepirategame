param(
    [ValidateSet('benchmark','overhead','footage','smoke')][string]$Mode = 'benchmark',
    [ValidateSet('daylight','dusk','rough','all')][string]$Condition = 'all',
    [string]$RunName = (Get-Date -Format 'yyyyMMdd-HHmmss')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../../..").Path
$output = Join-Path $PSScriptRoot "$RunName-$Mode"
if (Test-Path $output) { throw "Evidence already exists: $output" }
New-Item -ItemType Directory -Path $output | Out-Null
$conditions = if ($Condition -eq 'all') { @('daylight','dusk','rough') } else { @($Condition) }
foreach ($weather in $conditions) {
    $destination = Join-Path $output $weather
    New-Item -ItemType Directory -Path $destination | Out-Null
    $arguments = @('-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-force-d3d11',
        '-t02-mode',$Mode,'-t02-condition',$weather,'-t02-output',('"' + $destination + '"'),
        '-logFile',('"' + $destination + '/player.log"'))
    $start = Get-Date
    # The interactive Unity player must be visible: hidden windows suppress rendering.
    $process = Start-Process -FilePath "$root/Builds/T02/OceanProof.exe" -WindowStyle Normal -PassThru -ArgumentList $arguments
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "$weather player failed: $($process.ExitCode)" }
    [pscustomobject]@{ condition=$weather; mode=$Mode; start=$start.ToString('o'); end=(Get-Date).ToString('o'); exitCode=$process.ExitCode } |
        ConvertTo-Json | Set-Content "$destination/process.json"
    if ($Mode -eq 'footage') {
        & ffmpeg -hide_banner -loglevel warning -framerate 30 -i "$destination/$weather-%04d.jpg" -c:v libx264 -crf 18 -pix_fmt yuv420p "$destination/$weather.mp4"
        if ($LASTEXITCODE -ne 0) { throw 'Video encoding failed' }
    }
}
Write-Output $output
