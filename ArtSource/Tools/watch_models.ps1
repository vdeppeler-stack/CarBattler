# Watches the ComfyUI output folder and converts each new vehicle GLB to a game FBX + textures with
# headless Blender (process_vehicle.py). A model is (re)converted when its GLB is newer than its FBX.
# Stops when every listed id is converted or after the timeout.
param(
    [string[]]$Ids = @('goliath_wheel','leviathan_apc','dread_jester','sandviper','iron_jaw','nocturne','vortex_f1','bastion_mk4','interceptor_v8','chrome_igniter','phase_gt','rustfang','bonechaser','mammoth_4x4','jester_van','hellfire_carousel'),
    [string]$Source = "C:\Users\vdepp\AppData\Local\Comfy-Desktop\ComfyUI-Installs\ComfyUI\ComfyUI\output\3d\IronSyndicate\v2",
    [int]$TimeoutMinutes = 360
)
$blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
$out = Join-Path $PSScriptRoot "..\..\Assets\_Project\Art\Vehicles" | Resolve-Path
$script = Join-Path $PSScriptRoot "process_vehicle_cli.py"
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
while ((Get-Date) -lt $deadline) {
    $pending = 0
    foreach ($id in $Ids) {
        $glb = Get-ChildItem $Source -Filter "${id}_*.glb" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
        $fbx = Join-Path $out "$id.fbx"
        if ($glb -ne $null -and (Test-Path $fbx) -and (Get-Item $fbx).LastWriteTime -gt $glb.LastWriteTime) { continue }
        $pending++
        if ($glb -eq $null) { continue }
        $size = $glb.Length; Start-Sleep -Seconds 5; $glb.Refresh(); if ($glb.Length -ne $size) { continue }
        & $blender -b --factory-startup -P $script -- $id $glb.FullName $out 2>&1 | Select-String "^OK |^WARN|Error" | ForEach-Object { "$(Get-Date -Format HH:mm:ss) $_" }
    }
    if ($pending -eq 0) { "$(Get-Date -Format HH:mm:ss) all models converted"; break }
    Start-Sleep -Seconds 30
}
