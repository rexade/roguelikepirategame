param(
    [string]$Unity = 'D:/u6-t01/Editor/Unity.exe',
    [string]$RunName = 'T01-verification'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if ($RunName -notmatch '^[A-Za-z0-9-]+$') { throw 'Use a simple verification folder name.' }
$verification = Join-Path $projectRoot ".tools/$RunName"
if (Test-Path -LiteralPath $verification) { throw 'This verification run already exists; use a new RunName.' }
$source = Join-Path $verification 'source'
$checkout = Join-Path $verification 'checkout'
New-Item -ItemType Directory -Path $source -Force | Out-Null
foreach ($path in @('Assets', 'Packages', 'ProjectSettings', '.gitignore')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $path) -Destination $source -Recurse
}

# The commit belongs only to this disposable test fixture, never the user's parent repository.
git -C $source init --quiet
if ($LASTEXITCODE -ne 0) { throw 'Could not initialize verification fixture.' }
$gitRoot = (git -C $source rev-parse --show-toplevel).Trim()
if ($gitRoot.Replace('/', '\') -ne $source.Replace('/', '\')) { throw 'Unexpected Git boundary.' }
git -C $source add --all
if ($LASTEXITCODE -ne 0) { throw 'Could not stage verification snapshot.' }
git -C $source -c user.name='T01 Verification' -c user.email='t01-verification@localhost' commit --quiet -m 'T01 source snapshot for clean-checkout verification'
if ($LASTEXITCODE -ne 0) { throw 'Could not record verification snapshot.' }
$revision = (git -C $source rev-parse HEAD).Trim()
git clone --quiet --no-local $source $checkout
if ($LASTEXITCODE -ne 0) { throw 'Could not clone verification snapshot.' }
if (Test-Path (Join-Path $checkout 'Library')) { throw 'Checkout unexpectedly contains an import cache.' }
$record = [ordered]@{ revision = $revision; source = $source; checkout = $checkout; editor = $Unity }
$record | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'clean-checkout.json') -Encoding UTF8
$log = Join-Path $PSScriptRoot 'clean-build.log'
$process = Start-Process -FilePath $Unity -WindowStyle Hidden -PassThru -ArgumentList @(
    '-batchmode', '-quit', '-projectPath', ('"' + $checkout + '"'),
    '-executeMethod', 'PirateGame.Bootstrap.Editor.ProjectSetup.BuildWindows',
    '-logFile', ('"' + $log + '"')
)
$process.WaitForExit()
$record['exitCode'] = $process.ExitCode
$record | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'clean-checkout.json') -Encoding UTF8
if ($process.ExitCode -ne 0) { throw "Clean build failed: $($process.ExitCode). See $log" }
Write-Output "Clean-checkout build passed: $revision"
Write-Output (Join-Path $checkout 'Builds/Windows/PiratePrototype.exe')
