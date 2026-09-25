# Batch-mode Unity runner for tests, authoring methods and builds.
# Usage:
#   ./tools/RunUnity.ps1 -Mode EditMode [-Assemblies 'A;B'] [-Filter 'Name']
#   ./tools/RunUnity.ps1 -Mode PlayMode [-Assemblies 'A;B']
#   ./tools/RunUnity.ps1 -Mode Method -Method 'Namespace.Type.Method'
# Output goes to Logs/runs/<Name>.log (+ .xml for tests). Close the editor first.
param(
    [Parameter(Mandatory = $true)][ValidateSet('EditMode', 'PlayMode', 'Method')][string]$Mode,
    [string]$Name = '',
    [string]$Method = '',
    [string]$Assemblies = '',
    [string]$Filter = '',
    [string[]]$Extra = @(),
    [string]$Unity = 'D:/u6-t01/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/..").Path
if ($Name -eq '') { $Name = $Mode }
$out = Join-Path $project 'Logs/runs'
New-Item -ItemType Directory -Force $out | Out-Null
$log = Join-Path $out "$Name.log"
$xml = Join-Path $out "$Name.xml"
if (Test-Path $xml) { Remove-Item $xml -Force }
$arguments = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $log + '"'))
if ($Mode -eq 'Method') {
    $arguments += @('-executeMethod', $Method, '-quit')
} else {
    $arguments += @('-nographics', '-runTests', '-testPlatform', $Mode, '-testResults', ('"' + $xml + '"'))
    if ($Assemblies -ne '') { $arguments += @('-assemblyNames', ('"' + $Assemblies + '"')) }
    if ($Filter -ne '') { $arguments += @('-testFilter', ('"' + $Filter + '"')) }
}
$arguments += $Extra
$watch = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $Unity -WindowStyle Hidden -PassThru -ArgumentList $arguments
$process.WaitForExit()
$seconds = [int]$watch.Elapsed.TotalSeconds
$errors = @(Select-String -Path $log -Pattern 'error CS\d+|Scripts have compiler errors|Exception:' -ErrorAction SilentlyContinue | Select-Object -First 25)
foreach ($e in $errors) { Write-Output ("LOG: " + $e.Line.Trim()) }
if ($Mode -ne 'Method') {
    if (-not (Test-Path $xml)) { Write-Output "NO RESULTS (exit $($process.ExitCode), ${seconds}s). See $log"; exit 1 }
    [xml]$result = Get-Content $xml
    $run = $result.'test-run'
    Write-Output "Tests: result=$($run.result) total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped) (${seconds}s)"
    foreach ($case in $result.SelectNodes('//test-case[@result="Failed"]')) {
        Write-Output ("FAILED: " + $case.fullname)
        $message = $case.failure.message.'#cdata-section'
        if ($message) { Write-Output ("  " + ($message -split "`n" | Select-Object -First 6) -join "`n  ") }
    }
    if ($run.result -notlike 'Passed*') { exit 1 }
} else {
    Write-Output "Method $Method exit=$($process.ExitCode) (${seconds}s). Log: $log"
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }
}
