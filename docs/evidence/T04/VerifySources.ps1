param([string]$OutputDirectory = $PSScriptRoot, [string]$BuildDirectory = 'Builds/T04')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$preserved = 0
foreach ($task in @('T02', 'T03')) {
    foreach ($file in (Get-Content "$project/docs/evidence/$task/source-files.json" | ConvertFrom-Json)) {
        if ($file.path -notlike 'Assets*') { continue }
        $hash = (Get-FileHash (Join-Path $project $file.path) -Algorithm SHA256).Hash
        if ($hash -ne $file.sha256) { throw "Inherited file changed: $($file.path)" }
        $preserved++
    }
}
$roots = @('Assets/_Game/Gameplay/Ships', 'Assets/_Game/Gameplay/Input', 'Assets/_Game/Presentation/Ships', 'Assets/_Game/Presentation/Camera', 'Assets/_Game/Prefabs/Ships', 'Assets/_Game/Scenes/Tests/T04', 'Assets/_Game/Tests/T04')
$files = foreach ($root in $roots) {
    Get-ChildItem (Join-Path $project $root) -File -Recurse | ForEach-Object {
        if ($_.Extension -ne '.meta' -and !(Test-Path ($_.FullName + '.meta'))) { throw "Missing metadata: $_" }
        [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash; bytes = $_.Length }
    }
}
$files | ConvertTo-Json -Depth 4 | Set-Content "$OutputDirectory/source-files.json"
$buildFiles = Get-ChildItem (Join-Path $project $BuildDirectory) -File -Recurse | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash; bytes = $_.Length }
}
$buildFiles | ConvertTo-Json -Depth 4 | Set-Content "$OutputDirectory/build-files.json"
[ordered]@{ inheritedAssetsUnchanged = $preserved; outputFiles = @($files).Count; missingMetadata = 0 } | ConvertTo-Json | Set-Content "$OutputDirectory/source-check.json"
Get-Content "$OutputDirectory/source-check.json"
