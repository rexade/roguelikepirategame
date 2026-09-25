$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$results = foreach ($fps in @(30, 60, 120)) {
    $folder = Join-Path $PSScriptRoot "footage-$fps"
    $video = Join-Path $folder "ship-$fps.mp4"
    & ffmpeg -v error -i $video -f null NUL
    if ($LASTEXITCODE -ne 0) { throw "Video decode failed: $video" }
    $probe = (& ffprobe -v error -select_streams v:0 -show_entries stream=width,height,nb_frames,r_frame_rate -of json $video | ConvertFrom-Json).streams[0]
    $rows = @(Import-Csv "$folder/frames.csv").Count
    if ([int]$probe.nb_frames -ne $rows -or $probe.r_frame_rate -ne "$fps/1") { throw "Video cadence/length mismatch: $video" }
    $colors = [System.Collections.Generic.HashSet[int]]::new()
    $bitmap = [System.Drawing.Bitmap]::new((Join-Path $folder 'frame-0000.jpg'))
    try {
        for ($y = 0; $y -lt $bitmap.Height; $y += 20) {
            for ($x = 0; $x -lt $bitmap.Width; $x += 20) { [void]$colors.Add($bitmap.GetPixel($x,$y).ToArgb()) }
        }
    } finally { $bitmap.Dispose() }
    if ($colors.Count -lt 100) { throw "Blank/flat capture: $folder" }
    $first = (Get-FileHash "$folder/frame-0000.jpg").Hash
    $last = (Get-FileHash (Join-Path $folder ('frame-{0:D4}.jpg' -f ($rows - 1)))).Hash
    if ($first -eq $last) { throw "Unchanging capture: $folder" }
    [ordered]@{ fps = $fps; frames = $rows; width = $probe.width; height = $probe.height;
        decoded = $true; distinctSampledColors = $colors.Count; firstLastDiffer = $true;
        sha256 = (Get-FileHash $video -Algorithm SHA256).Hash }
}
$results | ConvertTo-Json -Depth 4 | Set-Content "$PSScriptRoot/video-validation.json"
Get-Content "$PSScriptRoot/video-validation.json"
