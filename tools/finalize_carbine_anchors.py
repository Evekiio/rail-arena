"""Record final fitted wrist frames in the editable master, without changing geometry."""
import bpy,hashlib,json,sys
from pathlib import Path
from mathutils import Matrix,Quaternion,Vector
root=Path(sys.argv[sys.argv.index('--')+1]);out=root/'ArtSource/Original/PrecisionCarbine-Production-v1'
profile=json.loads((root/'RailArena/Assets/RailArena/Characters/Hand grip.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(out/'Precision-Carbine.blend'))
convert=Matrix(((0,-1,0),(0,0,1),(1,0,0)))
anchors={}
for name,key in [('RightGrip','right'),('LeftSupport','left')]:
    p=profile[key+'Position'];q=profile[key+'Rotation'];point=Vector((p['x'],p['y'],p['z']))
    rotation=Quaternion((q['w'],q['x'],q['y'],q['z'])).to_matrix()
    obj=bpy.data.objects[name];obj.location=convert.inverted()@point
    obj.rotation_mode='QUATERNION';obj.rotation_quaternion=(convert.inverted()@rotation@convert).to_quaternion()
    anchors[name]=dict(unityPosition=p,unityRotation=q,blenderPosition=list(obj.location))
for old,new in [('AimLine','Aim'),('StockContact','Shoulder')]:
    obj=bpy.data.objects.get(old) or bpy.data.objects.get(new);obj.name=new
    anchors[new]=dict(blenderPosition=list(obj.location),unityPosition=list(convert@obj.location))
anchors['Muzzle']=dict(blenderPosition=list(bpy.data.objects['Muzzle'].location),unityPosition=list(convert@bpy.data.objects['Muzzle'].location))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Precision-Carbine.blend'))
(out/'Final-Anchors.json').write_text(json.dumps(anchors,indent=2)+'\n')
report=json.loads((out/'Geometry-Validation.json').read_text());report['masterSha256']=hashlib.sha256((out/'Precision-Carbine.blend').read_bytes()).hexdigest();report['finalFittedAnchors']=True
(out/'Geometry-Validation.json').write_text(json.dumps(report,indent=2)+'\n');print('FINAL_FITTED_ANCHORS_SAVED')
