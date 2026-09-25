$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$roots = @('Assets/_Game/Core', 'Assets/_Game/Application', 'Assets/_Game/Content/Definitions', 'Assets/_Game/Tests/T03')
$files = @($roots | ForEach-Object { Get-ChildItem (Join-Path $project $_) -Recurse -File })
$missing = @($roots | ForEach-Object {
    $root = Get-Item (Join-Path $project $_)
    @($root) + @(Get-ChildItem $root.FullName -Recurse) | Where-Object {
        $_.Extension -ne '.meta' -and !(Test-Path ($_.FullName + '.meta'))
    } | ForEach-Object FullName
})
if ($missing.Count -ne 0) { throw "Missing Unity metadata: $($missing -join ', ')" }
$inherited = Get-Content "$project/docs/evidence/T02/source-files.json" -Raw | ConvertFrom-Json
$changed = @($inherited | Where-Object {
    (Get-FileHash (Join-Path $project $_.path) -Algorithm SHA256).Hash -ne $_.sha256
} | ForEach-Object path)
if ($changed.Count -ne 0) { throw "Accepted T02 files changed: $($changed -join ', ')" }
$imports = @(Get-ChildItem "$project/Assets/_Game/Core", "$project/Assets/_Game/Application" -Recurse -Filter '*.cs' |
    Select-String -Pattern 'using\s+(Unity|PirateGame\.Content|PirateGame\.Persistence|PirateGame\.UI)')
if ($imports.Count -ne 0) { throw 'Engine or adapter imports found in rules.' }
$assembly = Get-Content "$project/Assets/_Game/Core/PirateGame.Core.asmdef" -Raw | ConvertFrom-Json
if (!$assembly.noEngineReferences -or $assembly.references.Count -ne 0) { throw 'Invalid Core assembly boundary.' }
$files += Get-Item "$project/docs/CONTRACTS.md"
$files += $roots | ForEach-Object { Get-Item ((Join-Path $project $_) + '.meta') }
$files += Get-Item "$project/Assets/_Game/Tests.meta", "$project/Assets/_Game/Content.meta"
$manifest = @($files | Sort-Object FullName -Unique | ForEach-Object {
    [pscustomobject]@{ path = $_.FullName.Substring($project.Length + 1); sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash; bytes = $_.Length }
})
$manifest | ConvertTo-Json -Depth 5 | Set-Content "$PSScriptRoot/source-files.json"
[pscustomobject]@{
    output = 'T03-r1'; files = $manifest.Count; missingMetadata = $missing.Count;
    inheritedT02FilesVerified = $inherited.Count; changedInheritedFiles = $changed.Count;
    noEngineReferences = $assembly.noEngineReferences; forbiddenImports = $imports.Count
} | ConvertTo-Json | Set-Content "$PSScriptRoot/source-check.json"
Get-Content "$PSScriptRoot/source-check.json"
Get-FileHash "$PSScriptRoot/source-files.json" -Algorithm SHA256
