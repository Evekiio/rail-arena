"""Small metre-scale hard-surface helpers for the isolated carbine reference study."""
import bpy
import bmesh
import math

class Geometry:
    def __init__(self, surface, energy):
        self.surface=surface;self.energy=energy;self.parts=[]

    @staticmethod
    def activate(obj):
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
        bpy.context.view_layer.objects.active=obj

    def finish(self,obj,name,color=0,bevel=.001):
        self.activate(obj);obj.name=name
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        if bevel:
            mod=obj.modifiers.new('Reference facets','BEVEL');mod.width=bevel;mod.segments=1
            bpy.ops.object.modifier_apply(modifier=mod.name)
        bm=bmesh.new();bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
        bmesh.ops.triangulate(bm,faces=list(bm.faces))
        bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
        obj.data.materials.clear();obj.data.materials.append(self.energy if color==6 else self.surface)
        for uv in list(obj.data.uv_layers):obj.data.uv_layers.remove(uv)
        uv=obj.data.uv_layers.new(name='PaletteUV')
        for loop in uv.data:loop.uv=((color+.5)/8,.5)
        obj['paletteIndex']=color;self.parts.append(obj);return obj

    def box(self,name,loc,size,color=0,bevel=.001):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc);obj=bpy.context.object;obj.dimensions=size
        return self.finish(obj,name,color,bevel)

    def raw_profile(self,name,points,width,side=0):
        n=len(points);vertices=[(x,side+s*width/2,z) for s in (-1,1) for x,z in points]
        faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
        obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj);return obj

    def profile(self,name,points,width,color=0,side=0,bevel=.001):
        return self.finish(self.raw_profile(name,points,width,side),name,color,bevel)

    @staticmethod
    def cut(obj,cutter):
        Geometry.activate(obj);mod=obj.modifiers.new('Reference opening','BOOLEAN')
        mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
        bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)

    def cylinder(self,name,loc,radius,length,color=0,axis='X',sides=12):
        bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=radius,depth=length,location=loc)
        obj=bpy.context.object
        if axis=='X':obj.rotation_euler[1]=math.pi/2
        elif axis=='Y':obj.rotation_euler[0]=math.pi/2
        return self.finish(obj,name,color,.0007)

    def ring(self,name,loc,radius,inner,length,color=0,axis='X',sides=12):
        bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=radius,depth=length,location=loc)
        obj=bpy.context.object
        if axis=='X':obj.rotation_euler[1]=math.pi/2
        elif axis=='Y':obj.rotation_euler[0]=math.pi/2
        self.activate(obj);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=inner,depth=length+.02,location=loc)
        cutter=bpy.context.object
        if axis=='X':cutter.rotation_euler[1]=math.pi/2
        elif axis=='Y':cutter.rotation_euler[0]=math.pi/2
        self.cut(obj,cutter);return self.finish(obj,name,color,.0007)
