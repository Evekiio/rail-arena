"""Compare AO with selected carbine parts isolated in a disposable Blender session."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
coverage_only = "--coverage" in sys.argv
reunwrap = "--reunwrap" in sys.argv
tile = "--tile" in sys.argv
surroundings = "--surroundings" in sys.argv
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
output = root / ("Validation/SkyGardens-Playable-2026-10-29/CarbineAOForeendSurrounded" if surroundings else "Validation/SkyGardens-Playable-2026-10-29/CarbineAOIsolationTileAO" if tile and not coverage_only else "Validation/SkyGardens-Playable-2026-10-29/CarbineAOIsolationTile2" if tile else "Validation/SkyGardens-Playable-2026-10-29/CarbineAOIsolationReunwrap" if reunwrap else "Validation/SkyGardens-Playable-2026-10-29/CarbineAOIsolationCoverage" if coverage_only else "Validation/SkyGardens-Playable-2026-10-29/CarbineAOIsolation")
if output.exists():
    raise RuntimeError("Isolation output already exists")
output.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
scene = bpy.context.scene
parts = [o for o in scene.objects if o.type == "MESH"]
scene.render.engine = "CYCLES"
scene.cycles.samples = 16
scene.cycles.device = "CPU"
scene.world.light_settings.distance = 0.03
scene.render.bake.margin = 8
targets = ["C3 enclosed vented fore-end"] if reunwrap else ["C3 enclosed vented fore-end", "C3 receiver core", "C3 reinforced stock"]
report = []
for index, name in enumerate(targets):
    obj = bpy.data.objects[name]
    for part in parts:
        part.hide_render = not surroundings and part != obj
        part.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.data.uv_layers.active = obj.data.uv_layers["SurfaceAtlas"]
    if reunwrap:
        layer = obj.data.uv_layers.new(name="DisposableCoverageUV")
        obj.data.uv_layers.active = layer
        layer.active_render = True
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(island_margin=0.01)
        bpy.ops.object.mode_set(mode="OBJECT")
        layer = obj.data.uv_layers["DisposableCoverageUV"]
        if tile:
            atlas = json.loads((source.parent / "SurfaceAtlas.json").read_text(encoding="utf-8"))
            entry = next(item for item in atlas["tiles"] if item["name"] == name)
            grid = 9
            gutter = 8 / atlas["size"]
            scale = 1 / grid - 2 * gutter
            for loop in layer.data:
                loop.uv = (entry["column"] / grid + gutter + loop.uv.x * scale,
                           entry["row"] / grid + gutter + loop.uv.y * scale)
    image = bpy.data.images.new("isolated AO " + str(index), width=4096, height=4096, alpha=False)
    image.colorspace_settings.name = "Non-Color"
    for slot in obj.material_slots:
        if slot.material is None:
            raise RuntimeError("Missing material on " + name)
        slot.material.use_nodes = True
        nodes = slot.material.node_tree.nodes
        node = nodes.new("ShaderNodeTexImage")
        node.image = image
        nodes.active = node
        if coverage_only:
            emission = nodes.new("ShaderNodeEmission")
            emission.inputs["Color"].default_value = (1, 1, 1, 1)
            output_node = next(n for n in nodes if n.type == "OUTPUT_MATERIAL" and n.is_active_output)
            slot.material.node_tree.links.new(emission.outputs[0], output_node.inputs["Surface"])
    bpy.ops.object.bake(type="EMIT" if coverage_only else "AO", use_clear=True, margin=8)
    image.filepath_raw = str(output / (str(index) + ".png"))
    image.file_format = "PNG"
    image.save()
    obj.data.calc_loop_triangles()
    uv = obj.data.uv_layers.active
    coords = []
    for tri in obj.data.loop_triangles:
        points = [uv.data[i].uv for i in tri.loops]
        coords.append([sum(p.x for p in points) / 3, sum(p.y for p in points) / 3])
    report.append({"name": name, "image": str(index) + ".png", "centroids": coords})
    bpy.data.images.remove(image)
(output / "centroids.json").write_text(json.dumps(report) + "\n", encoding="utf-8")
print("ISOLATED_AO_DONE " + str(output))
