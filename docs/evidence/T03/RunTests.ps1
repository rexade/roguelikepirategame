param([string]$Unity = 'D:/u6-t01/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$evidence = $PSScriptRoot
$process = Start-Process -FilePath $Unity -WindowStyle Hidden -PassThru -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'PirateGame.T03.Tests',
    '-testResults', ('"' + $evidence + '/editmode-results.xml"'),
    '-logFile', ('"' + $evidence + '/editmode.log"')
)
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity tests failed: $($process.ExitCode)" }
if (!(Test-Path "$evidence/editmode-results.xml")) { throw 'No Unity result XML was produced.' }
[xml]$results = Get-Content "$evidence/editmode-results.xml"
if ($results.'test-run'.result -ne 'Passed') { throw "Unity tests: $($results.'test-run'.result)" }
Write-Output ("Unity tests passed: " + $results.'test-run'.passed)
