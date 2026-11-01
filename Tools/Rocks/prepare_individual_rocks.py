"""Prepare purchased individual stones; originals and all derivatives stay private."""
import bpy,os,json
from pathlib import Path
from mathutils import Vector,Matrix
from math import pi
root=Path(os.environ['CITYFORGE_PROJECT']);output=root/'Assets/CityForge/Resources/IndividualStreamRocks/IndividualStones.fbx'
if output.exists():raise RuntimeError('Preserve existing individual rock output before rebuilding')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'SourceAssets/IndividualRiverRocks/River_Rocks01.fbx'))
obj=bpy.data.objects['Rocks_low'];bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
stones=sorted([o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Rocks_low')],key=lambda o:o.name)
if len(stones)!=10:raise RuntimeError('Expected ten separate stone components')
report=[]
for i,o in enumerate(stones):
 transform=o.matrix_world.copy()
 for v in o.data.vertices:v.co=transform@v.co
 o.matrix_world.identity()
 lo=Vector([min(v.co[k] for v in o.data.vertices) for k in range(3)]);hi=Vector([max(v.co[k] for v in o.data.vertices) for k in range(3)])
 smallest=min(range(3),key=lambda k:(hi-lo)[k]);rotation=Matrix.Rotation(pi/2,4,'Y' if smallest==0 else 'X') if smallest!=2 else Matrix.Identity(4)
 for v in o.data.vertices:v.co=rotation@v.co
 lo=Vector([min(v.co[k] for v in o.data.vertices) for k in range(3)]);hi=Vector([max(v.co[k] for v in o.data.vertices) for k in range(3)])
 center=(lo+hi)/2;center.z=lo.z;scale=1/max((hi-lo).x,(hi-lo).y)
 for v in o.data.vertices:v.co=(v.co-center)*scale
 o.name=f'Stone{i:02d}';report.append({'name':o.name,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons)})
bpy.ops.object.select_all(action='DESELECT')
for o in stones:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
print(json.dumps(report))
