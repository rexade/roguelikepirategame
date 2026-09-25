param([ValidateSet('Author','Tests','Build','RefineCollision')][string]$Mode = 'Tests',
    [string]$OutputDirectory = $PSScriptRoot, [string]$BuildPath = 'Builds/T04/ShipControls.exe')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $OutputDirectory + '/' + $Mode + '.log"'))
if ($Mode -eq 'Author') { $arguments += @('-executeMethod', 'PirateGame.Tests.T04.Editor.ShipTestAuthoring.Create', '-quit') }
elseif ($Mode -eq 'Build') { $arguments += @('-executeMethod', 'PirateGame.Tests.T04.Editor.ShipTestAuthoring.Build', '-t04-build-output', ('"' + $BuildPath + '"'), '-quit') }
elseif ($Mode -eq 'RefineCollision') { $arguments += @('-executeMethod', 'PirateGame.Tests.T04.Editor.ShipTestAuthoring.RefineCollision', '-quit') }
else { $arguments += @('-runTests', '-testPlatform', 'PlayMode', '-assemblyNames', 'PirateGame.T04.Tests', '-testResults', ('"' + $OutputDirectory + '/playmode-results.xml"')) }
$process = Start-Process 'D:/u6-t01/Editor/Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $arguments
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Mode exit $($process.ExitCode)" }
if ($Mode -eq 'Tests') {
    [xml]$result = Get-Content "$OutputDirectory/playmode-results.xml"
    if ($result.'test-run'.result -ne 'Passed') { throw "Tests: $($result.'test-run'.result)" }
    Write-Output "Passed $($result.'test-run'.passed) tests"
}
