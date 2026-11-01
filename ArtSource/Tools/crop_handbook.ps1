# Extracts the vehicle sheets from the developer handbook and crops the main vehicle panel per vehicle.
# Output: ArtSource/Handbook/image*.jpg and ArtSource/Handbook/crops/<id>.png (inputs for ComfyUI TRELLIS.2).
# The images are not versioned; run this script to recreate them.
param(
    [string]$Docx = "$PSScriptRoot\..\..\..\Iron_Syndicate_Entwicklerhandbuch.docx",
    [string]$Out = "$PSScriptRoot\..\Handbook"
)
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem
New-Item -ItemType Directory -Force $Out, "$Out\crops" | Out-Null
$z = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $Docx))
foreach ($e in $z.Entries) { if ($e.FullName -like 'word/media*') { [IO.Compression.ZipFileExtensions]::ExtractToFile($e, (Join-Path $Out $e.Name), $true) } }
$z.Dispose()

# image index (document order) -> id, crop box x0,y0,x1,y1 on the 1536x1024 sheet
$boxes = @{
    1 = @('goliath_wheel', 0, 0, 1180, 590);   2 = @('sandviper', 80, 200, 1160, 820)
    3 = @('iron_jaw', 0, 0, 1140, 600);        4 = @('nocturne', 0, 40, 1100, 560)
    5 = @('vortex_f1', 0, 100, 1000, 700);     6 = @('bastion_mk4', 0, 100, 1160, 800)
    7 = @('interceptor_v8', 0, 320, 1160, 840); 8 = @('chrome_igniter', 80, 320, 1200, 840)
    9 = @('phase_gt', 0, 80, 1060, 550);       10 = @('rustfang', 0, 80, 1160, 660)
    11 = @('bonechaser', 0, 240, 1120, 840);   12 = @('mammoth_4x4', 160, 120, 1080, 720)
    13 = @('jester_van', 0, 0, 980, 680);      14 = @('leviathan_apc', 0, 80, 1070, 650)
    15 = @('dread_jester', 0, 80, 1080, 680)
}
foreach ($k in $boxes.Keys) {
    $b = $boxes[$k]
    $im = [System.Drawing.Bitmap]::FromFile((Join-Path $Out "image$k.jpg"))
    $r = New-Object System.Drawing.Rectangle $b[1], $b[2], ($b[3] - $b[1]), ($b[4] - $b[2])
    $c = $im.Clone($r, $im.PixelFormat)
    $c.Save((Join-Path "$Out\crops" "$($b[0]).png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $c.Dispose(); $im.Dispose()
}
"Crops written to $Out\crops"
