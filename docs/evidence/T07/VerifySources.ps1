$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$roots = @('Assets/_Game/UI/Harbor', 'Assets/_Game/UI/Loadout', 'Assets/_Game/Content/Progression', 'Assets/_Game/Scenes/Tests/T07', 'Assets/_Game/Tests/T07')
$files = @($roots | ForEach-Object { Get-ChildItem (Join-Path $project $_) -Recurse -File })
$missing = @($roots | ForEach-Object {
    @(Get-Item (Join-Path $project $_)) + @(Get-ChildItem (Join-Path $project $_) -Recurse) | Where-Object {
        $_.Extension -ne '.meta' -and !(Test-Path ($_.FullName + '.meta'))
    } | ForEach-Object FullName
})
if ($missing.Count) { throw "Missing metadata: $($missing -join ', ')" }
$accepted = Get-Content "$project/docs/evidence/T03/source-files.json" -Raw | ConvertFrom-Json
$inherited = @($accepted | Where-Object { $_.path -like 'Assets*' })
$changed = @($inherited | Where-Object { (Get-FileHash (Join-Path $project $_.path)).Hash -ne $_.sha256 })
if ($changed.Count) { throw "T03 sources changed: $($changed.path -join ', ')" }
$files += $roots | ForEach-Object { Get-Item ((Join-Path $project $_) + '.meta') }
$files += Get-Item "$project/Assets/_Game/UI.meta"
$manifest = @($files | Sort-Object FullName -Unique | ForEach-Object {
    [pscustomobject]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length }
})
$manifest | ConvertTo-Json -Depth 5 | Set-Content "$PSScriptRoot/source-files.json"
$build = @(Get-ChildItem "$project/Builds/T07" -Recurse -File | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length }
})
$build | ConvertTo-Json -Depth 5 | Set-Content "$PSScriptRoot/build-files.json"
[pscustomobject]@{ output = 'T07-r1'; sourceFiles = $manifest.Count; buildFiles = $build.Count; inheritedT03Files = $inherited.Count; changedInheritedFiles = $changed.Count; missingMetadata = $missing.Count } | ConvertTo-Json | Set-Content "$PSScriptRoot/source-check.json"
Get-Content "$PSScriptRoot/source-check.json"
Get-FileHash "$PSScriptRoot/source-files.json", "$PSScriptRoot/build-files.json"
