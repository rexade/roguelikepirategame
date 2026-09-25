param([switch]$SkipBuild, [string]$Revision = '')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$output = if ($Revision) { Join-Path $PSScriptRoot $Revision } else { $PSScriptRoot }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$preserved = 0
foreach ($manifest in @('docs/evidence/T02/source-files.json','docs/evidence/T03/source-files.json','docs/evidence/T04/R1/source-files.json')) {
    foreach ($file in (Get-Content (Join-Path $project $manifest) | ConvertFrom-Json)) {
        if ($file.path -notlike 'Assets*') { continue }
        if ((Get-FileHash (Join-Path $project $file.path)).Hash -ne $file.sha256) { throw "Inherited file changed: $($file.path)" }
        $preserved++
    }
}
$roots = @('Assets/_Game/Gameplay/Combat','Assets/_Game/Gameplay/AI','Assets/_Game/Content/Combat','Assets/_Game/Prefabs/Combat','Assets/_Game/Scenes/Tests/T05','Assets/_Game/Tests/T05')
$files = foreach ($root in $roots) {
    Get-ChildItem (Join-Path $project $root) -File -Recurse | ForEach-Object {
        if ($_.Extension -ne '.meta' -and !(Test-Path ($_.FullName + '.meta'))) { throw "Missing metadata: $_" }
        [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length }
    }
    $meta = Get-Item (Join-Path $project ($root + '.meta'))
    [ordered]@{ path = $meta.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $meta.FullName).Hash; bytes = $meta.Length }
}
$files | ConvertTo-Json -Depth 4 | Set-Content "$output/source-files.json"
if (!$SkipBuild) {
    $buildRoot = if ($Revision) { "Builds/T05/$Revision" } else { 'Builds/T05' }
    $buildFiles = Get-ChildItem (Join-Path $project $buildRoot) -File -Recurse | ForEach-Object {
        [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName).Hash; bytes = $_.Length }
    }
    $buildFiles | ConvertTo-Json -Depth 4 | Set-Content "$output/build-files.json"
}
[ordered]@{ inheritedAssetsUnchanged = $preserved; outputFiles = @($files).Count; missingMetadata = 0 } | ConvertTo-Json | Set-Content "$output/source-check.json"
Get-Content "$output/source-check.json"
