# Re-runs process_vehicle.py on the newest GLB of every vehicle (after changing the conversion settings).
param([string]$Source = "C:\Users\vdepp\AppData\Local\Comfy-Desktop\ComfyUI-Installs\ComfyUI\ComfyUI\output\3d\IronSyndicate\v2")
$blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
$out = Join-Path $PSScriptRoot "..\..\Assets\_Project\Art\Vehicles" | Resolve-Path
$script = Join-Path $PSScriptRoot "process_vehicle_cli.py"
Get-ChildItem $Source -Filter "*_0000*.glb" | Group-Object { $_.Name -replace '_\d+\.glb$', '' } | ForEach-Object {
    $glb = $_.Group | Sort-Object LastWriteTime | Select-Object -Last 1
    & $blender -b --factory-startup -P $script -- $_.Name $glb.FullName $out 2>&1 | Select-String "^OK |^WARN|Error" | ForEach-Object { "$_" }
}
