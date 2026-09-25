param([string]$OutputDirectory = $PSScriptRoot, [switch]$MeasuredOnly)
$ErrorActionPreference = 'Stop'
$prefixes = if ($MeasuredOnly) { @('measured') } else { @('measured', 'footage') }
$summaries = foreach ($prefix in $prefixes) {
    $runs = @{}
    foreach ($fps in @(30, 60, 120)) {
        $folder = Join-Path $OutputDirectory "$prefix-$fps"
        $rows = @(Import-Csv "$folder/frames.csv")
        $runs[$fps] = @{}
        foreach ($row in $rows) { $runs[$fps][[int]$row.tick] = $row }
        $times = @($rows | ForEach-Object { [double]$_.deltaSeconds } | Sort-Object)
        $result = Get-Content "$folder/result.txt" -Raw
        if ($result -notmatch 'pausePositionStable=True' -or $result -notmatch 'pauseTickStable=True') { throw "Pause failed: $folder" }
        if (Select-String "$folder/player.log" -Pattern 'Exception|ReadPixels was called|error CS' -Quiet) { throw "Player errors: $folder" }
        [ordered]@{ kind = $prefix; requestedFPS = $fps; frames = $rows.Count;
            averageDelta = ($times | Measure-Object -Average).Average;
            medianDelta = $times[[int][Math]::Floor($times.Count / 2)];
            p95Delta = $times[[int][Math]::Floor(($times.Count - 1) * 0.95)];
            lastTick = [int]$rows[-1].tick; finalX = [double]$rows[-1].x; finalZ = [double]$rows[-1].z }
    }
    $count = 0; $maximum = 0.0
    foreach ($tick in $runs[30].Keys) {
        if (!$runs[60].ContainsKey($tick) -or !$runs[120].ContainsKey($tick)) { continue }
        $count++
        foreach ($fps in @(60, 120)) {
            $dx = [double]$runs[30][$tick].x - [double]$runs[$fps][$tick].x
            $dz = [double]$runs[30][$tick].z - [double]$runs[$fps][$tick].z
            $maximum = [Math]::Max($maximum, [Math]::Sqrt($dx*$dx + $dz*$dz))
        }
    }
    if ($count -lt 50 -or $maximum -gt 0.01) { throw "Cadence comparison failed: $prefix, $count ticks, $maximum m" }
    [ordered]@{ kind = "$prefix-comparison"; commonTicks = $count; maximumPositionDifference = $maximum }
}
$summaries | ConvertTo-Json -Depth 5 | Set-Content "$OutputDirectory/capture-summary.json"
Get-Content "$OutputDirectory/capture-summary.json"
