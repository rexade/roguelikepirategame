param([ValidateSet('Author','Refine','Tests','Build','Regression','Rules')][string]$Mode = 'Tests')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $PSScriptRoot + '/' + $Mode + '.log"'))
if ($Mode -eq 'Author') { $arguments += @('-executeMethod', 'PirateGame.Tests.T06.Editor.RegionAuthoring.Create', '-quit') }
elseif ($Mode -eq 'Build') { $arguments += @('-executeMethod', 'PirateGame.Tests.T06.Editor.RegionAuthoring.Build', '-quit') }
elseif ($Mode -eq 'Refine') { $arguments += @('-executeMethod', 'PirateGame.Tests.T06.Editor.RegionAuthoring.Refine', '-quit') }
else {
    $assembly = if ($Mode -eq 'Regression') { 'PirateGame.T04.Tests' } elseif ($Mode -eq 'Rules') { 'PirateGame.T03.Tests' } else { 'PirateGame.T06.Tests' }
    $platform = if ($Mode -eq 'Rules') { 'EditMode' } else { 'PlayMode' }
    $arguments += @('-runTests', '-testPlatform', $platform, '-assemblyNames', $assembly, '-testResults', ('"' + $PSScriptRoot + '/' + $Mode + '.xml"'))
}
$process = Start-Process 'D:/u6-t01/Editor/Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $arguments
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Mode exit $($process.ExitCode)" }
if ($Mode -notin @('Author','Refine','Build')) {
    [xml]$result = Get-Content "$PSScriptRoot/$Mode.xml"
    if ($result.'test-run'.result -ne 'Passed' -or [int]$result.'test-run'.passed -eq 0) { throw "Tests: $($result.'test-run'.result), passed=$($result.'test-run'.passed)" }
    Write-Output "Passed $($result.'test-run'.passed) tests"
}
