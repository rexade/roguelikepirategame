param([Parameter(Mandatory=$true)][string]$CaptureDirectory)
$ErrorActionPreference = 'Stop'
function Percentile($values, $p) {
    $sorted = @($values | Sort-Object)
    if (!$sorted.Count) { return $null }
    $sorted[[Math]::Max(0,[Math]::Ceiling($sorted.Count*$p)-1)]
}
$summary = foreach ($file in Get-ChildItem $CaptureDirectory -Recurse -Filter '*-run*.csv') {
    $rows = @(Import-Csv $file.FullName)
    $frames = @($rows | ForEach-Object { [double]::Parse($_.frame_ms,[Globalization.CultureInfo]::InvariantCulture) })
    $cpu = @($rows | ForEach-Object { [double]$_.cpu_frame_ms } | Where-Object { $_ -gt 0 })
    $gpu = @($rows | ForEach-Object { [double]$_.gpu_frame_ms } | Where-Object { $_ -gt 0 })
    $median = Percentile $frames 0.5
    $p95 = Percentile $frames 0.95
    [pscustomobject]@{
        source=$file.FullName; frames=$frames.Count; durationSeconds=($frames | Measure-Object -Sum).Sum/1000
        medianMs=$median; p95Ms=$p95; p99Ms=(Percentile $frames 0.99); maxMs=($frames | Measure-Object -Maximum).Maximum
        framesOver33_3ms=@($frames | Where-Object { $_ -gt 33.3 }).Count
        cpuMedianMs=(Percentile $cpu 0.5); gpuMedianMs=(Percentile $gpu 0.5); cpuSamples=$cpu.Count; gpuSamples=$gpu.Count
        mainThreadMedianMs=(Percentile @($rows | ForEach-Object { [double]$_.main_thread_ms } | Where-Object { $_ -ge 0 }) 0.5)
        renderThreadMedianMs=(Percentile @($rows | ForEach-Object { [double]$_.render_thread_ms } | Where-Object { $_ -ge 0 }) 0.5)
        peakAllocatedBytes=($rows | ForEach-Object { [double]$_.allocated_bytes } | Measure-Object -Maximum).Maximum
        peakReservedBytes=($rows | ForEach-Object { [double]$_.reserved_bytes } | Measure-Object -Maximum).Maximum
        targetsMet=($median -le 16.7 -and $p95 -le 20)
    }
}
$summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $CaptureDirectory 'summary.json')
$summary | Format-Table @{l='Run';e={Split-Path $_.source -Leaf}},frames,medianMs,p95Ms,p99Ms,maxMs,gpuMedianMs,targetsMet
