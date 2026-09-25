$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../../..").Path
Set-Location $project
$checks = foreach ($path in @('docs/evidence/T03/source-files.json','docs/evidence/T04/R1/source-files.json','docs/evidence/T05/R1/source-files.json','docs/evidence/T06/source-files.json','docs/evidence/T06/build-files.json')) {
    $rows = Get-Content $path -Raw | ConvertFrom-Json
    $changed = @($rows | Where-Object { !(Test-Path $_.path) -or (Get-FileHash $_.path).Hash -ne $_.sha256 } | Select-Object -ExpandProperty path)
    [ordered]@{ manifest = $path; hash = (Get-FileHash $path).Hash; entries = $rows.Count; changed = $changed }
}
$checks | ConvertTo-Json -Depth 5 | Set-Content "$PSScriptRoot/input-check.json"
$roots = @('Assets/_Game/Content/World/FirstRegion','Assets/_Game/Gameplay/World/Salvage','Assets/_Game/Prefabs/World','Assets/_Game/Scenes/Tests/T06','Assets/_Game/Tests/T06')
$files = @($roots | ForEach-Object { Get-ChildItem $_ -File -Recurse; Get-Item ($_ + '.meta') })
$files += Get-Item 'Assets/_Game/Scenes/Regions/FirstRegion.unity','Assets/_Game/Scenes/Regions/FirstRegion.unity.meta'
if (@($files | Where-Object { $_.Extension -ne '.meta' -and !(Test-Path ($_.FullName + '.meta')) }).Count) { throw 'Missing metadata' }
$files | Sort-Object FullName | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length }
} | ConvertTo-Json -Depth 4 | Set-Content "$PSScriptRoot/source-files.json"
if (Test-Path 'Builds/T06-R1/Salvage.exe') {
    Get-ChildItem 'Builds/T06-R1' -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length }
    } | ConvertTo-Json -Depth 4 | Set-Content "$PSScriptRoot/build-files.json"
}
$checks | ConvertTo-Json -Depth 5
Get-FileHash "$PSScriptRoot/source-files.json"
