param([Parameter(Mandatory=$true)][string]$CaptureDirectory)
$ErrorActionPreference = 'Stop'
$results = foreach ($file in Get-ChildItem $CaptureDirectory -Recurse -Filter '*.mp4') {
    $probe = (& ffprobe -v error -select_streams v:0 -show_entries stream=width,height,nb_frames,r_frame_rate,duration -of json $file.FullName) | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Cannot inspect $($file.FullName)" }
    $stream = $probe.streams[0]
    $statistics = Join-Path $file.DirectoryName 'video-signal.log'
    $decode = Start-Process -FilePath (Get-Command ffmpeg).Source -WindowStyle Hidden -Wait -PassThru -RedirectStandardError $statistics -ArgumentList @('-hide_banner','-loglevel','info','-i',('"'+$file.FullName+'"'),'-vf','signalstats,metadata=print','-an','-f','null','NUL')
    if ($decode.ExitCode -ne 0) { throw "Cannot decode $($file.FullName)" }
    $luma = @(Select-String -Path $statistics -Pattern 'lavfi.signalstats.YAVG=([0-9.]+)' | ForEach-Object { [double]::Parse($_.Matches[0].Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture) })
    $difference = @(Select-String -Path $statistics -Pattern 'lavfi.signalstats.YDIF=([0-9.]+)' | ForEach-Object { [double]::Parse($_.Matches[0].Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture) })
    $valid = $stream.width -eq 1920 -and $stream.height -eq 1080 -and $stream.nb_frames -eq '1800' -and $stream.r_frame_rate -eq '30/1' -and [double]$stream.duration -eq 60
    $nonblank = @($luma | Where-Object { $_ -le 16.1 }).Count -eq 0
    $moving = @($difference | Where-Object { $_ -gt 0.01 }).Count
    [pscustomobject]@{path=$file.FullName;stream=$stream;formatValid=$valid;decodedFrames=$luma.Count;noBlackFrames=$nonblank;changedFrames=$moving;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}
    if (!$valid -or $luma.Count -ne 1800 -or !$nonblank -or $moving -lt 1700) { throw "Footage integrity check failed: $($file.FullName)" }
}
if (@($results).Count -ne 3) { throw 'Expected three condition videos' }
$results | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $CaptureDirectory 'video-validation.json')
$results | Select-Object path,formatValid,decodedFrames,noBlackFrames,changedFrames | Format-Table -AutoSize
