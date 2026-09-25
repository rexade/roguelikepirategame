# Integrated-slice benchmark (T11). Requires a development player built with:
#   ./tools/RunUnity.ps1 -Mode Method -Name build-bench -Method 'PirateGame.Composition.Editor.WorldAuthoring.BuildGame' -Extra @('-build-output','Builds/Benchmark/PiratePrototype.exe','-development')
# Runs daylight, dusk and rough (warm-up lap + 3 recorded laps each) and the
# diagnostic stress workload at 1920x1080, VSync off, then summarizes the CSVs.
param(
    [string]$Output = 'docs/evidence/T11/benchmark',
    [string[]]$Conditions = @('daylight', 'dusk', 'rough'),
    [int]$Runs = 3,
    [switch]$SkipStress
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/..").Path
$player = Join-Path $project 'Builds/Benchmark/PiratePrototype.exe'
if (-not (Test-Path $player)) { throw "Build the development benchmark player first: $player" }
$out = Join-Path $project $Output
New-Item -ItemType Directory -Force $out | Out-Null
$jobs = @()
foreach ($c in $Conditions) { $jobs += ,@($c, $false) }
if (-not $SkipStress) { $jobs += ,@('daylight', $true) }
foreach ($job in $jobs) {
    $condition = $job[0]; $stress = $job[1]
    $label = if ($stress) { "stress-$condition" } else { $condition }
    $saves = Join-Path $env:TEMP ('pirate-bench-' + [Guid]::NewGuid().ToString('N'))
    $arguments = @('-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080',
        '-save-dir', ('"' + $saves + '"'), '-seed', '1234', '-benchmark', ('"' + $out + '"'),
        '-condition', $condition, '-runs', $(if ($stress) { '2' } else { "$Runs" }),
        '-logFile', ('"' + (Join-Path $out "$label-player.log") + '"'))
    if ($stress) { $arguments += '-stress' }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $player -PassThru -ArgumentList $arguments
    if (-not $process.WaitForExit(900000)) { $process.Kill(); Write-Output "$label TIMEOUT" }
    Write-Output ("{0}: exit {1} in {2:0}s" -f $label, $process.ExitCode, $watch.Elapsed.TotalSeconds)
}
Write-Output "Summarize with: python3 tools/summarize_benchmark.py $Output"
