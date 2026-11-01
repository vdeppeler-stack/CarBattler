# Blender: TRELLIS GLB -> game-ready FBX for Unity.
# Usage inside Blender:  exec(open(path).read()); process("goliath_wheel", glb, out_dir)
# Steps: clean scene, import GLB, join meshes, decimate to a triangle budget, downscale textures,
# save textures as PNG next to the FBX, export FBX (Y up, -Z forward baked). Prints bounds for orientation.
import bpy, os, math

def _clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for item in list(block):
            if item.users == 0:
                block.remove(item)

def process(vid, glb, out_dir, tris=12000, tex=1024):
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
    # Detach from glTF empties, keep world transform.
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    for o in list(bpy.context.scene.objects):
        if o != obj:
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    faces = len(obj.data.polygons)
    if faces > tris:
        mod = obj.modifiers.new("dec", 'DECIMATE')
        mod.ratio = tris / faces
        bpy.ops.object.modifier_apply(modifier=mod.name)

    tex_dir = os.path.join(out_dir, "Textures")
    os.makedirs(tex_dir, exist_ok=True)
    n = 0
    for slot in obj.material_slots:
        mat = slot.material
        if mat is None or not mat.use_nodes:
            continue
        mat.name = f"{vid}_mat{n}"
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image is not None:
                img = node.image
                if img.size[0] > tex:
                    img.scale(tex, tex)
                path = os.path.join(tex_dir, f"{vid}_{n}_{node.label or node.name}.png".replace(" ", "_"))
                img.filepath_raw = path
                img.file_format = 'PNG'
                img.save()
                n += 1

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
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, path_mode='RELATIVE', embed_textures=False,
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             apply_scale_options='FBX_SCALE_UNITS', mesh_smooth_type='FACE')
    print(f"OK {vid} faces {faces}->{len(obj.data.polygons)} size x={maxx-minx:.3f} y={maxy-miny:.3f} z={maxz-minz:.3f} textures={n}")
