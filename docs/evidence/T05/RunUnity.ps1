param([ValidateSet('Author','Refine','Tests','Build','Regression','Rules')][string]$Mode = 'Tests', [string]$Revision = '', [string]$Label = '')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot/../../..").Path
$output = if ($Revision) { Join-Path $PSScriptRoot $Revision } else { $PSScriptRoot }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$name = if ($Label) { $Label } else { $Mode }
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $output + '/' + $name + '.log"'))
if ($Revision) { $arguments += @('-t05-build', ('"Builds/T05/' + $Revision + '/Combat.exe"')) }
if ($Mode -eq 'Author') { $arguments += @('-executeMethod', 'PirateGame.Tests.T05.Editor.CombatAuthoring.Create', '-quit') }
elseif ($Mode -eq 'Build') { $arguments += @('-executeMethod', 'PirateGame.Tests.T05.Editor.CombatAuthoring.Build', '-quit') }
elseif ($Mode -eq 'Refine') { $arguments += @('-executeMethod', 'PirateGame.Tests.T05.Editor.CombatAuthoring.Refine', '-quit') }
else {
    $assembly = if ($Mode -eq 'Regression') { 'PirateGame.T04.Tests' } elseif ($Mode -eq 'Rules') { 'PirateGame.T03.Tests' } else { 'PirateGame.T05.Tests' }
    $platform = if ($Mode -eq 'Rules') { 'EditMode' } else { 'PlayMode' }
    $arguments += @('-runTests', '-testPlatform', $platform, '-assemblyNames', $assembly, '-testResults', ('"' + $output + '/' + $name + '.xml"'))
}
$process = Start-Process 'D:/u6-t01/Editor/Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $arguments
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Mode exit $($process.ExitCode)" }
if ($Mode -notin @('Author','Refine','Build')) {
    [xml]$result = Get-Content "$output/$name.xml"
    if ($result.'test-run'.result -ne 'Passed') { throw "Tests: $($result.'test-run'.result)" }
    Write-Output "Passed $($result.'test-run'.passed) tests"
}
