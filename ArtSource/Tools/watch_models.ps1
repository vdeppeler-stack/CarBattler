# Watches the ComfyUI output folder and converts each new vehicle GLB to a game FBX with headless Blender.
# Stops when every listed id has an FBX or after the timeout.
param(
    [string[]]$Ids = @('sandviper','iron_jaw','nocturne','vortex_f1','bastion_mk4','interceptor_v8','chrome_igniter','phase_gt','rustfang','bonechaser','mammoth_4x4','leviathan_apc','jester_van','dread_jester'),
    [int]$TimeoutMinutes = 360
)
$blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
$src = "C:\Users\vdepp\AppData\Local\Comfy-Desktop\ComfyUI-Installs\ComfyUI\ComfyUI\output\3d\IronSyndicate"
$out = Join-Path $PSScriptRoot "..\..\Assets\_Project\Art\Vehicles" | Resolve-Path
$script = Join-Path $PSScriptRoot "process_vehicle_cli.py"
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
while ((Get-Date) -lt $deadline) {
    $pending = 0
    foreach ($id in $Ids) {
        if (Test-Path (Join-Path $out "$id.fbx")) { continue }
        $pending++
        $glb = Get-ChildItem $src -Filter "${id}_*.glb" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
        if ($glb -eq $null) { continue }
        # Wait until the file is no longer being written.
        $size = $glb.Length; Start-Sleep -Seconds 5; $glb.Refresh(); if ($glb.Length -ne $size) { continue }
        & $blender -b --factory-startup -P $script -- $id $glb.FullName $out 2>&1 | Select-String "^OK |Error" | ForEach-Object { "$(Get-Date -Format HH:mm:ss) $_" }
    }
    if ($pending -eq 0) { "$(Get-Date -Format HH:mm:ss) all models converted"; break }
    Start-Sleep -Seconds 30
}
