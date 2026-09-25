param([ValidateSet('Before','Tests','Build','Regression','Rules')][string]$Mode = 'Tests')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../../..").Path
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $PSScriptRoot + '/' + $Mode + '.log"'))
if ($Mode -eq 'Build') { $arguments += @('-executeMethod', 'PirateGame.Tests.T06.Editor.RegionAuthoring.BuildR1', '-quit') }
else {
    $assembly = if ($Mode -eq 'Regression') { 'PirateGame.T04.Tests' } elseif ($Mode -eq 'Rules') { 'PirateGame.T03.Tests' } else { 'PirateGame.T06.Tests' }
    $platform = if ($Mode -eq 'Rules') { 'EditMode' } else { 'PlayMode' }
    $arguments += @('-runTests', '-testPlatform', $platform, '-assemblyNames', $assembly, '-testResults', ('"' + $PSScriptRoot + '/' + $Mode + '.xml"'))
    if ($Mode -eq 'Before') { $arguments += @('-testFilter', 'PirateGame.Tests.T06.SalvageTests.StartupHudAndFreshReloadAreNeutral') }
}
$process = Start-Process 'D:/u6-t01/Editor/Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $arguments
$process.WaitForExit()
if ($Mode -ne 'Build') {
    [xml]$result = Get-Content "$PSScriptRoot/$Mode.xml"
    Write-Output "Result=$($result.'test-run'.result), passed=$($result.'test-run'.passed), failed=$($result.'test-run'.failed)"
    if ($Mode -eq 'Before' -and [int]$result.'test-run'.failed -eq 1) { exit 0 }
    if ($result.'test-run'.result -ne 'Passed' -or [int]$result.'test-run'.passed -eq 0) { throw 'Tests failed or empty' }
}
if ($process.ExitCode -ne 0) { throw "Unity $Mode exit $($process.ExitCode)" }
