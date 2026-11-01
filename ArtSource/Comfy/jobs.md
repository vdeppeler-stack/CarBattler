# ComfyUI-Jobs Fahrzeugmodelle (TRELLIS.2, Bild → 3D)

Eingabe: Hauptbild je Fahrzeug aus `ArtSource/Handbook/crops/` (aus den Handbuch-Bildern ausgeschnitten).
Ausgabe: ComfyUI `output/3d/IronSyndicate/<id>*.glb`.
Workflow: `trellis2_image_to_model.json` (volle Qualität, Goliath), `trellis2_fast.json` (Serie: 1024 Upsample, 8 Schritte, 60k Faces).
Jester Van und Dread Jester werden vorher mit FLUX.2 Klein zum Kirmes-Schwertransporter umgestaltet (`klein_*.json`), danach 3D.

| Fahrzeug | prompt_id |
|---|---|
| goliath_wheel | a5842400-0dfb-4391-9b30-5e6301d03334 |
| jester_van (Flux-Umgestaltung) | f2a0f41f-14fb-49cc-8136-0be46db87c99 |
| dread_jester (Flux-Umgestaltung) | 44c6dbb4-06a5-42a3-af3e-b42f74f48b28 |
| sandviper | d1167bf0-dbd3-42d1-ade0-41c07547634c |
| iron_jaw | 6839acc1-9cb4-4378-9b13-d4e91f5bf63d |
| nocturne | 29cc2039-0d10-41d7-bc9c-cb82c27c5941 |
| vortex_f1 | 9a9bfc11-e734-4a19-b133-acb75e9ddde9 |
| bastion_mk4 | 8b664eb8-2dd0-49b8-a57e-c0bfe880e60e |
| interceptor_v8 | e48b16bc-4896-4035-bc6a-da5d7992b54c |
| chrome_igniter | 7c3ad40e-b018-4d8b-b404-98fdf0d40e15 |
| phase_gt | 11420733-fafd-480e-af27-99b6bb1e2fd9 |
| rustfang | 447f3e2a-73c5-443a-879d-b65644682882 |
| bonechaser | 3c883b5b-151e-45af-b6f1-132aa710d154 |
| mammoth_4x4 | 075e70bc-8c66-4314-aa90-45f3482dfbcd |
| leviathan_apc | c41704c1-6f89-47dc-bbd9-9a1e3edf9b14 |
