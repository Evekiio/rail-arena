import bpy, os, sys
from mathutils import Vector
source, folder, *options = sys.argv[sys.argv.index('--') + 1:]
hand_name = options[0] if options else 'RightHand'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source)
armature = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
hand = armature.matrix_world @ armature.data.bones[hand_name].matrix_local
for obj in list(bpy.data.objects):
    if obj.type != 'MESH': continue
    groups = {g.index for g in obj.vertex_groups if g.name.startswith(hand_name)}
    if not groups: obj.hide_render = True; continue
    selected = {v.index for v in obj.data.vertices if sum(w.weight for w in v.groups if w.group in groups) > .6}
    faces = [list(p.vertices) for p in obj.data.polygons if all(i in selected for i in p.vertices)]
    indices = sorted({i for face in faces for i in face})
    mapping = {v: i for i, v in enumerate(indices)}
    mesh = bpy.data.meshes.new('Unposed right hand geometry')
    mesh.from_pydata([hand.inverted() @ obj.matrix_world @ obj.data.vertices[i].co for i in indices], [],
        [[mapping[i] for i in face] for face in faces])
    view = bpy.data.objects.new('Isolated right hand', mesh); bpy.context.collection.objects.link(view)
    view.color = (.68, .45, .2, 1); obj.hide_render = True
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'; scene.display.shading.color_type = 'OBJECT'
scene.display.shading.show_cavity = True; scene.display.shading.cavity_type = 'BOTH'
scene.display.shading.show_shadows = True
scene.world = bpy.data.worlds.new('Hand inspection background')
scene.display.shading.background_type = 'WORLD'; scene.world.color = (.025, .035, .05)
scene.render.resolution_x = 900; scene.render.resolution_y = 1000; scene.render.resolution_percentage = 100
bpy.ops.object.camera_add(); camera = bpy.context.object; camera.data.type = 'ORTHO'; camera.data.ortho_scale = .28
scene.camera = camera; center = Vector((.015, .103, -.02))
os.makedirs(folder, exist_ok=True)
for name, direction in [('Palm', (0, 0, 1)), ('Side', (1, 0, 0)), ('ThreeQuarter', (.8, .1, 1))]:
    camera.location = center + Vector(direction).normalized()
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = os.path.join(folder, 'Source-' + name + '.png')
    bpy.ops.render.render(write_still=True)
