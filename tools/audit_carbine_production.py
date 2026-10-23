"""Audit, texture and render the final editable master headlessly."""
import bpy,bmesh,hashlib,json,math,sys
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(sys.argv[sys.argv.index('--')+1]);out=root/'ArtSource/Original/PrecisionCarbine-Production-v1';asset=root/'RailArena/Assets/RailArena/Art/PrecisionCarbine'
source=root/'ArtSource/Original/PrecisionCarbine-References-v3';cal=json.loads((source/'Calibration.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(source/'Carbine-Reference-Study.blend'))
original={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in bpy.context.scene.objects if o.type=='MESH'}
bpy.ops.wm.open_mainfile(filepath=str(out/'Precision-Carbine.blend'));scene=bpy.context.scene
materials=[]
for emissive in (False,True):
    mat=bpy.data.materials.new('Carbine Energy' if emissive else 'Carbine Surface');mat.use_nodes=True
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
    for name,socket in [('BaseColor','Base Color'),('Normal','Normal'),('MetallicSmoothness','Metallic')]+([('Emission','Emission Color')] if emissive else []):
        image=bpy.data.images.load(str(asset/(name+'.png')),check_existing=True);image.pack()
        if name in ('Normal','MetallicSmoothness'):image.colorspace_settings.name='Non-Color'
        tex=nodes.new('ShaderNodeTexImage');tex.image=image
        if name=='Normal':
            normal=nodes.new('ShaderNodeNormalMap');links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bsdf.inputs[socket])
        elif name=='MetallicSmoothness':
            separate=nodes.new('ShaderNodeSeparateColor');links.new(tex.outputs['Color'],separate.inputs['Color']);links.new(separate.outputs['Red'],bsdf.inputs[socket])
            invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],bsdf.inputs['Roughness'])
        else:links.new(tex.outputs['Color'],bsdf.inputs[socket])
    if emissive:bsdf.inputs['Emission Strength'].default_value=.65
    materials.append(mat)
meshes=[o for o in scene.objects if o.type=='MESH'];triangles=0
for obj in meshes:
    assert original[obj.name]==[tuple(obj.matrix_world@v.co) for v in obj.data.vertices],obj.name
    bm=bmesh.new();bm.from_mesh(obj.data);assert all(e.is_manifold for e in bm.edges),obj.name;bm.free()
    obj.data.calc_loop_triangles();assert all(t.area>1e-12 for t in obj.data.loop_triangles)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert all(0<=c<=1 for uv in obj.data.uv_layers.active.data for c in uv.uv)
    triangles+=len(obj.data.loop_triangles);obj.data.materials.clear();obj.data.materials.append(materials[int(obj['paletteIndex'])==6])
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Precision-Carbine.blend'))
scene.render.engine='CYCLES';scene.cycles.samples=4;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=4;scene.render.film_transparent=True;scene.render.image_settings.color_mode='RGBA'
camera=scene.camera;camera.data.type='ORTHO'
(out/'Clean').mkdir(exist_ok=True)
for view in cal['assets'][0]['views']:
    center=Vector(cal['assets'][0]['center']);camera.location=center+Vector(view['normal'])*4
    camera.rotation_euler=Matrix((view['right'],view['up'],view['normal'])).transposed().to_quaternion().to_euler();camera.data.ortho_scale=1.25
    scene.render.resolution_x=scene.render.resolution_y=2048;scene.render.resolution_percentage=100
    scene.render.filepath=str(out/'Clean'/Path(view['file']).name);bpy.ops.render.render(write_still=True)
camera.data.ortho_scale=1.3;camera.location=(1,-1.8,.8);camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.filepath=str(out/'Carbine-Studio.png');bpy.ops.render.render(write_still=True)
(out/'Geometry-Validation.json').write_text(json.dumps(dict(passed=True,approvedGeometryUnchanged=True,manifold=True,nondegenerate=True,finite=True,atlasUvInBounds=True,triangles=triangles,editableParts=len(meshes),masterSha256=hashlib.sha256((out/'Precision-Carbine.blend').read_bytes()).hexdigest()),indent=2)+'\n')
print('PRODUCTION_MASTER_AUDITED_AND_RENDERED')
