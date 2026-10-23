"""Original finger articulation for the CC0 contender's previously wrist-only skin."""
import math
import bpy
from mathutils import Vector

# Joint guides are in source wrist-local metres, observed from the unposed source.
GUIDES = {
    'Thumb': [(-.027, .045, -.032), (-.035, .067, -.055), (-.035, .084, -.073), (-.031, .095, -.082)],
    'Index': [(-.022, .101, -.003), (-.024, .139, -.008), (-.026, .169, -.012), (-.026, .191, -.017)],
    'Middle': [(.004, .106, .017), (.004, .147, .010), (.001, .179, .008), (0, .204, .002)],
    'Ring': [(.026, .100, .029), (.039, .135, .024), (.048, .164, .020), (.054, .182, .014)],
    'Pinky': [(.044, .085, .025), (.058, .112, .030), (.068, .137, .028), (.073, .155, .020)],
}
GRIP_DEGREES = {'Thumb': (0, 0, 0), 'Index': (32, 35, 12), 'Middle': (45, 50, 20),
                'Ring': (45, 50, 20), 'Pinky': (50, 55, 20)}


def smoothstep(value):
    value = max(0, min(1, value))
    return value * value * (3 - 2 * value)


def project(point, guide):
    nearest = None
    travelled = 0
    for index, (a, b) in enumerate(zip(guide, guide[1:])):
        delta = b - a
        fraction = max(0, min(1, (point - a).dot(delta) / delta.length_squared))
        distance = (point - a - fraction * delta).length
        candidate = (distance, travelled + fraction * delta.length)
        if nearest is None or candidate[0] < nearest[0]: nearest = candidate
        travelled += delta.length
    return nearest


def articulate(armature, meshes):
    guides = {}
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode='EDIT')
    for side in ('Left', 'Right'):
        hand_name = side + 'Hand'
        hand = armature.data.edit_bones[hand_name]
        hand_matrix = hand.matrix.copy()
        guides[hand_name] = {}
        for finger, values in GUIDES.items():
            points = [Vector((-x if side == 'Left' else x, y, z)) for x, y, z in values]
            guides[hand_name][finger] = points
            parent = hand
            for index in range(3):
                bone = armature.data.edit_bones.new(hand_name + finger + str(index + 1))
                bone.head = hand_matrix @ points[index]; bone.tail = hand_matrix @ points[index + 1]
                bone.parent = parent; bone.use_connect = index > 0
                bone.align_roll(hand_matrix.to_3x3() @ Vector((0, 0, 1)))
                parent = bone
    bpy.ops.object.mode_set(mode='OBJECT')
    counts = {}
    for mesh in meshes:
        for hand_name, fingers in guides.items():
            hand_group = mesh.vertex_groups.get(hand_name)
            if hand_group is None: continue
            hand_matrix = armature.matrix_world @ armature.data.bones[hand_name].matrix_local
            to_hand = hand_matrix.inverted() @ mesh.matrix_world
            groups = {finger: [mesh.vertex_groups.new(name=hand_name + finger + str(i + 1)) for i in range(3)] for finger in fingers}
            counts[hand_name] = counts.get(hand_name, 0)
            for vertex in mesh.data.vertices:
                weight = next((w.weight for w in vertex.groups if w.group == hand_group.index), 0)
                if weight <= .6: continue
                point = to_hand @ vertex.co
                projections = {name: project(point, points) for name, points in fingers.items()}
                nearest = min(distance for distance, arc in projections.values())
                shares = {name: math.exp(-((distance - nearest) / .007) ** 2) for name, (distance, arc) in projections.items()}
                share_sum = sum(shares.values()); palm = 0; transferred = 0
                for finger, (distance, arc) in projections.items():
                    share = shares[finger] / share_sum
                    if share < .001: continue
                    guide = fingers[finger]
                    along = (point - guide[0]).dot((guide[1] - guide[0]).normalized())
                    influence = smoothstep((along + .012) / .028)
                    palm += share * (1 - influence); transferred += share * influence
                    centres = []; length = 0
                    for a, b in zip(guide, guide[1:]):
                        segment = (b - a).length; centres.append(length + segment / 2); length += segment
                    strengths = [math.exp(-((arc - centre) / .018) ** 2) for centre in centres]
                    total = sum(strengths)
                    for group, strength in zip(groups[finger], strengths):
                        group.add([vertex.index], weight * share * influence * strength / total, 'REPLACE')
                hand_group.add([vertex.index], weight * palm, 'REPLACE')
                if transferred > .001: counts[hand_name] += 1
        # Match the runtime four-influence skin budget with normalized strongest weights.
        for vertex in mesh.data.vertices:
            strongest = sorted([(g.group, g.weight) for g in vertex.groups if g.weight > 1e-8], key=lambda p: -p[1])[:4]
            total = sum(weight for group, weight in strongest)
            for group in [entry.group for entry in vertex.groups]: mesh.vertex_groups[group].remove([vertex.index])
            for group, weight in strongest: mesh.vertex_groups[group].add([vertex.index], weight / total, 'REPLACE')
    for hand_name in guides:
        for finger, angles in GRIP_DEGREES.items():
            for index, angle in enumerate(angles):
                bone = armature.pose.bones[hand_name + finger + str(index + 1)]
                bone.rotation_mode = 'XYZ'; bone.rotation_euler.x = -math.radians(angle)
    # Bake the combat pose as the editable bind pose; later actions articulate its joints.
    for mesh in meshes:
        bpy.context.view_layer.objects.active = mesh
        for modifier in list(mesh.modifiers):
            if modifier.type == 'ARMATURE' and modifier.object == armature:
                bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')
    for mesh in meshes:
        modifier = mesh.modifiers.new('Articulated contender', 'ARMATURE'); modifier.object = armature
    skin = []
    for mesh in meshes:
        weights = [[g.weight for g in vertex.groups if g.weight > 1e-8] for vertex in mesh.data.vertices]
        maximum = max(len(values) for values in weights)
        error = max(abs(sum(values) - 1) for values in weights if values)
        if maximum > 4 or error > 1e-5: raise ValueError('Invalid normalized four-bone skin.')
        skin.append({'mesh': mesh.name, 'vertices': len(mesh.data.vertices), 'maximumInfluences': maximum,
            'maximumWeightError': error, 'fingerGroups': {g.name: sum(any(w.group == g.index and w.weight > .001 for w in v.groups)
                for v in mesh.data.vertices) for g in mesh.vertex_groups if any(g.name.startswith(name) and g.name != name for name in guides)}})
    return {'fingerBones': 30, 'weightedVertices': counts, 'skin': skin, 'rightWristLocalGuides': GUIDES, 'bindGripDegrees': GRIP_DEGREES}


if __name__ == '__main__':
    import json, os, sys
    source, folder = sys.argv[sys.argv.index('--') + 1:]
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=source)
    armature = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    armature.animation_data_clear()
    for bone in armature.pose.bones: bone.matrix_basis.identity()
    report = articulate(armature, meshes)
    os.makedirs(folder, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(folder, 'ArticulatedContender.blend'))
    bpy.ops.object.select_all(action='DESELECT'); armature.select_set(True)
    for mesh in meshes: mesh.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(folder, 'ArticulatedContender.fbx'), use_selection=True,
        object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, use_armature_deform_only=True,
        bake_anim=False, axis_forward='-Z', axis_up='Y', path_mode='AUTO')
    with open(os.path.join(folder, 'hand-rig.json'), 'w') as handle:
        json.dump(report, handle, indent=2)
