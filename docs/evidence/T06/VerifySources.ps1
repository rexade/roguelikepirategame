$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$roots = @('Assets/_Game/Content/World/FirstRegion', 'Assets/_Game/Gameplay/World/Salvage', 'Assets/_Game/Prefabs/World', 'Assets/_Game/Scenes/Tests/T06', 'Assets/_Game/Tests/T06')
$files = @($roots | ForEach-Object { Get-ChildItem (Join-Path $project $_) -File -Recurse })
$files += @($roots | ForEach-Object { Get-Item (Join-Path $project ($_ + '.meta')) })
$files += Get-Item "$project/Assets/_Game/Scenes/Regions/FirstRegion.unity", "$project/Assets/_Game/Scenes/Regions/FirstRegion.unity.meta"
$manifest = @($files | Sort-Object FullName | ForEach-Object { [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length } })
$manifest | ConvertTo-Json -Depth 4 | Set-Content "$PSScriptRoot/source-files.json"
$missing = @($files | Where-Object { $_.Extension -ne '.meta' -and !(Test-Path ($_.FullName + '.meta')) })
if ($missing.Count) { throw "Missing metadata: $($missing.FullName)" }
$checks = foreach ($path in @('docs/evidence/T03/source-files.json', 'docs/evidence/T04/R1/source-files.json')) {
    $rows = Get-Content (Join-Path $project $path) -Raw | ConvertFrom-Json
    $changed = @($rows | Where-Object { (Get-FileHash (Join-Path $project $_.path)).Hash -ne $_.sha256 } | Select-Object -ExpandProperty path)
    [ordered]@{ manifest = $path; hash = (Get-FileHash (Join-Path $project $path)).Hash; entries = $rows.Count; changed = $changed }
}
$checks | ConvertTo-Json -Depth 4 | Set-Content "$PSScriptRoot/input-check.json"
if (Test-Path "$project/Builds/T06") {
    Get-ChildItem "$project/Builds/T06" -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length }
    } | ConvertTo-Json -Depth 4 | Set-Content "$PSScriptRoot/build-files.json"
}
Write-Output "Source entries=$($manifest.Count), missing metadata=$($missing.Count)"
Get-FileHash "$PSScriptRoot/source-files.json"
