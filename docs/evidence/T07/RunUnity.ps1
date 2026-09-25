param([ValidateSet('Create','Test','Build')][string]$Action = 'Test', [string]$Unity = 'D:/u6-t01/Editor/Unity.exe', [string]$Filter = '')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$argsList = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $PSScriptRoot + '/' + $Action + '.log"'))
if ($Action -eq 'Test') {
    $argsList += @('-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'PirateGame.T07.Tests', '-testResults', ('"' + $PSScriptRoot + '/editmode-results.xml"'))
    if ($Filter) { $argsList += @('-testFilter', $Filter) }
} else {
    $argsList += @('-quit', '-executeMethod', ('PirateGame.Tests.T07.Editor.HarborAuthoring.' + $Action))
}
$process = Start-Process -FilePath $Unity -WindowStyle Hidden -PassThru -ArgumentList $argsList
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Action failed: $($process.ExitCode)" }
if ($Action -eq 'Test') {
    [xml]$result = Get-Content "$PSScriptRoot/editmode-results.xml"
    if ($result.'test-run'.result -ne 'Passed') { throw "Tests: $($result.'test-run'.result)" }
    Write-Output ("Passed: " + $result.'test-run'.passed)
}
Write-Output "Unity $Action completed."
