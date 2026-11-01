# ComfyUI-Jobs Fahrzeugmodelle (TRELLIS.2, Bild → 3D)

Eingabe: Hauptbild je Fahrzeug aus `ArtSource/Handbook/crops/` (erzeugt mit `ArtSource/Tools/crop_handbook.ps1`).
Ausgabe: ComfyUI `output/3d/IronSyndicate/<id>_00001.glb` → Blender `ArtSource/Tools/process_vehicle.py`
(12k Dreiecke, 1K-Texturen) → `Assets/_Project/Art/Vehicles/<id>.fbx` → Menü
`Iron Syndicate/Build/Vehicle Bodies (generated models)`.

Workflows: `trellis2_image_to_model.json` (volle Qualität, Goliath, ~30 min), `trellis2_fast.json`
(Serie: 1024 Upsample, 8 Schritte, 60k Faces, 2K-Bake).

Bekanntes Problem: Ein Wechsel TRELLIS → FLUX in derselben ComfyUI-Sitzung führte zu
`Windows fatal exception: access violation` in `caching.py ram_release`. Deshalb TRELLIS-Serie und
FLUX-Umgestaltungen (Jester Van, Dread Jester; `klein_*.json`) in getrennten Sitzungen ausführen.

| Fahrzeug | prompt_id (2. Sitzung) |
|---|---|
| goliath_wheel | fertig (1. Sitzung) |
| sandviper | 2462d343-e0a8-425b-bc1a-09e45ed93504 |
| iron_jaw | 023a171b-b0ce-4034-adb2-3af77f88f585 |
| nocturne | deff2aa6-a5e8-4a4a-b50a-aa1db22f5650 |
| vortex_f1 | 98c44553-e9cc-4c18-b043-3962ffcb488d |
| bastion_mk4 | 389ed6af-efee-4953-bdfc-4e7f68d51b90 |
| interceptor_v8 | 949fd3ad-3ad6-445b-8b92-87baad881dfd |
| chrome_igniter | 4013512c-f0ab-4233-97c0-48ad6556eddf |
| phase_gt | 5a038592-3dcd-4272-8c4f-d4f93538079b |
| rustfang | 594c5aeb-4cc1-40cf-a496-736bd0f63710 |
| bonechaser | 81dbacf6-4dac-4d46-9bc2-e767751d895b |
| mammoth_4x4 | ebe1d06e-9cbd-42dd-9909-3ba9349f20f2 |
| leviathan_apc | 01cea0c8-bd66-4c2c-93a0-f14e673b9225 |
| jester_van, dread_jester | nach FLUX-Umgestaltung (eigene Sitzung) |
