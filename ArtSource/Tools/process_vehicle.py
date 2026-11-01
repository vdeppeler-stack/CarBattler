# Blender: TRELLIS GLB -> game FBX + Unity textures.
# Usage inside Blender:  exec(open(path).read()); process("goliath_wheel", glb, out_dir)
# The GLB must already be game-sized (TRELLIS decimates to ~15k faces BEFORE unwrapping and baking).
# Decimating here would distort the UVs of the baked atlas, so this script only cleans up and exports:
# join meshes, origin to bottom centre, export FBX (Y up, -Z forward baked) and write textures for URP Lit:
#   <id>_BaseColor.png, <id>_Normal.png, <id>_MS.png (R metallic, A smoothness = 1 - roughness), <id>_AO.png
import bpy, os
import numpy as np

METAL_SCALE = 0.4
SATURATION = 1.3
BRIGHTNESS = 1.12

def _clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for item in list(block):
            if item.users == 0:
                block.remove(item)

def _pixels(img):
    a = np.empty(img.size[0] * img.size[1] * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(img.size[1], img.size[0], 4)

def _save(name, arr, path, srgb):
    h, w = arr.shape[:2]
    out = bpy.data.images.new(name, w, h, alpha=True, float_buffer=False)
    if not srgb:
        out.colorspace_settings.name = 'Non-Color'
    out.pixels.foreach_set(arr.astype(np.float32).ravel())
    out.filepath_raw = path
    out.file_format = 'PNG'
    out.save()
    bpy.data.images.remove(out)

def _find_images(obj):
    found = {}
    for slot in obj.material_slots:
        mat = slot.material
        if mat is None or not mat.use_nodes:
            continue
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image is not None:
                # glTF importer labels: "BASE COLOR", "METALLIC ROUGHNESS", "NORMAL MAP", "OCCLUSION"
                # (occlusion often shares the ORM image: R occlusion, G roughness, B metallic).
                key = (node.label or node.name).upper().replace(" ", "_")
                for tag in ("BASE_COLOR", "METALLIC_ROUGHNESS", "NORMAL", "OCCLUSION"):
                    if tag in key and tag not in found:
                        found[tag] = node.image
    return found

def process(vid, glb, out_dir, max_faces=30000):
    _clear()
    bpy.ops.import_scene.gltf(filepath=glb)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = vid
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    for o in list(bpy.context.scene.objects):
        if o != obj:
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    faces = len(obj.data.polygons)
    if faces > max_faces:
        print(f"WARN {vid}: {faces} faces - decimate in TRELLIS (DecimateMesh) instead of here")

    tex_dir = os.path.join(out_dir, "Textures")
    os.makedirs(tex_dir, exist_ok=True)
    imgs = _find_images(obj)
    if "BASE_COLOR" in imgs:
        # Light grade: TRELLIS washes out the painted liveries; lift saturation and brightness a little.
        bc = _pixels(imgs["BASE_COLOR"])
        rgb = bc[..., :3]
        lum = (rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32))[..., None]
        bc[..., :3] = np.clip((lum + (rgb - lum) * SATURATION) * BRIGHTNESS, 0.0, 1.0)
        _save(vid + "_bc", bc, os.path.join(tex_dir, vid + "_BaseColor.png"), True)
    if "NORMAL" in imgs:
        _save(vid + "_n", _pixels(imgs["NORMAL"]), os.path.join(tex_dir, vid + "_Normal.png"), False)
    if "METALLIC_ROUGHNESS" in imgs:
        mr = _pixels(imgs["METALLIC_ROUGHNESS"])
        ms = np.zeros_like(mr)
        # Metallic capped: the arenas have no sky to reflect, full metal renders almost black.
        metal = mr[..., 2] * METAL_SCALE      # glTF blue
        ms[..., 0] = metal
        ms[..., 1] = metal
        ms[..., 2] = metal
        ms[..., 3] = 1.0 - mr[..., 1]      # smoothness = 1 - roughness (glTF green)
        _save(vid + "_ms", ms, os.path.join(tex_dir, vid + "_MS.png"), False)
    if "OCCLUSION" in imgs:
        ao = _pixels(imgs["OCCLUSION"])
        ao[..., 1] = ao[..., 0]
        ao[..., 2] = ao[..., 0]
        ao[..., 3] = 1.0
        _save(vid + "_ao", ao, os.path.join(tex_dir, vid + "_AO.png"), False)

    # Origin: bottom centre.
    bb = [obj.matrix_world @ v.co for v in obj.data.vertices]
    minx, maxx = min(p.x for p in bb), max(p.x for p in bb)
    miny, maxy = min(p.y for p in bb), max(p.y for p in bb)
    minz, maxz = min(p.z for p in bb), max(p.z for p in bb)
    cx, cy = (minx + maxx) / 2, (miny + maxy) / 2
    for v in obj.data.vertices:
        v.co.x -= cx
        v.co.y -= cy
        v.co.z -= minz
    obj.location = (0, 0, 0)

    fbx = os.path.join(out_dir, vid + ".fbx")
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, path_mode='STRIP', embed_textures=False,
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             apply_scale_options='FBX_SCALE_UNITS', mesh_smooth_type='FACE')
    print(f"OK {vid} faces {faces} size x={maxx-minx:.3f} y={maxy-miny:.3f} z={maxz-minz:.3f} textures={sorted(imgs)}")
