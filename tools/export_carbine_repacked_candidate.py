"""Export an isolated rifle FBX candidate using the audited AO atlas UVs."""
import bpy
import bmesh
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
atlas = json.loads((source.parent / "SurfaceAtlas.json").read_text(encoding="utf-8"))
output = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedModels"
if output.exists():
    raise RuntimeError("Candidate output already exists")
output.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
scene = bpy.context.scene
parts = sorted((o for o in scene.objects if o.type == "MESH"), key=lambda o: o.name)
entries = {entry["name"]: entry for entry in atlas["tiles"]}
if set(entries) != {obj.name for obj in parts}:
    raise RuntimeError("Source parts and atlas manifest differ")
gutter = atlas["gutterPixels"] / atlas["size"]
scale = 1 / 9 - 2 * gutter
for obj in parts:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.data.uv_layers.active = obj.data.uv_layers["SurfaceAtlas"]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(35 if obj.name == "C3 muzzle collar" else 66), island_margin=0.01)
    bpy.ops.object.mode_set(mode="OBJECT")
    entry = entries[obj.name]
    for loop in obj.data.uv_layers["SurfaceAtlas"].data:
        loop.uv = (entry["column"] / 9 + gutter + loop.uv.x * scale,
                   entry["row"] / 9 + gutter + loop.uv.y * scale)
counts = []
for level, budget in enumerate((12000, 6000, 3000)):
    clones = []
    for obj in parts:
        clone = obj.copy()
        clone.data = obj.data.copy()
        scene.collection.objects.link(clone)
        clones.append(clone)
    original = sum(len(o.data.polygons) for o in clones)
    ratio = min(1, (budget - 250) / original) if level else 1
    for obj in clones:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        if ratio < 1:
            modifier = obj.modifiers.new("LOD reduction", "DECIMATE")
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.triangulate(mesh, faces=list(mesh.faces))
        bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))
        mesh.to_mesh(obj.data)
        mesh.free()
        obj.data.calc_loop_triangles()
        if any(t.area <= 1e-12 for t in obj.data.loop_triangles):
            raise RuntimeError("Degenerate triangle on " + obj.name)
    groups = {}
    for obj in clones:
        kind = "trigger" if obj.name.startswith("C3 trigger.") or obj.name == "C3 trigger" else "shell"
        groups.setdefault((obj["assemblyId"], int(obj["paletteIndex"]) == 6, kind), []).append(obj)
    exports = []
    for (assembly, energy, kind), objects in groups.items():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join()
        obj = bpy.context.object
        obj.name = f"{assembly.replace('-', '_')}_{kind}_{'Energy' if energy else 'Surface'}"
        scene.cursor.location = (-.04, 0, -.055) if kind == "trigger" else (0, 0, 0)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        obj.matrix_world = Matrix.Rotation(-math.pi / 2, 4, "Z") @ obj.matrix_world
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        exports.append(obj)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in exports:
        obj.select_set(True)
        obj.data.calc_loop_triangles()
    triangles = sum(len(o.data.loop_triangles) for o in exports)
    if triangles > budget:
        raise RuntimeError("LOD triangle budget exceeded")
    counts.append(triangles)
    bpy.ops.export_scene.fbx(filepath=str(output / f"Carbine_LOD{level}.fbx"), use_selection=True, object_types={"MESH"}, axis_forward="-Z", axis_up="Y", apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
    for obj in exports:
        bpy.data.objects.remove(obj, do_unlink=True)
(output / "export.json").write_text(json.dumps({"source": str(source), "sourceBlendSaved": False, "parts": len(parts), "lodTriangles": counts}, indent=2) + "\n", encoding="utf-8")
print("CANDIDATE_MODELS_EXPORTED " + json.dumps(counts))
