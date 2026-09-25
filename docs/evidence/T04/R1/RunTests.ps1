param([string]$Label = 'after', [switch]$Rules)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../../..").Path
$platform = if ($Rules) { 'EditMode' } else { 'PlayMode' }
$assembly = if ($Rules) { 'PirateGame.T03.Tests' } else { 'PirateGame.T04.Tests' }
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'),
    '-runTests', '-testPlatform', $platform, '-assemblyNames', $assembly,
    '-testResults', ('"' + $PSScriptRoot + '/' + $Label + '.xml"'),
    '-logFile', ('"' + $PSScriptRoot + '/' + $Label + '.log"'))
$process = Start-Process 'D:/u6-t01/Editor/Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $arguments
$process.WaitForExit()
[xml]$result = Get-Content "$PSScriptRoot/$Label.xml"
Write-Output "Exit $($process.ExitCode): $($result.'test-run'.passed) passed, $($result.'test-run'.failed) failed"
if ($process.ExitCode -ne 0 -or $result.'test-run'.result -ne 'Passed') { throw 'Unity tests failed' }
