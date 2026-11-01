"""Blender batch preparation. Private FBX inputs/outputs; run with CITYFORGE_PROJECT set.
Preserves original scans and refuses to overwrite prepared outputs.
"""
import bpy,os,math,json
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(os.environ['CITYFORGE_PROJECT'])
source=root/'SourceAssets/NativeCamps';out=root/'Assets/CityForge/Resources/NativeCamps';out.mkdir(parents=True,exist_ok=True)
report=[]
for kind in ['Teepee','Totem']:
 target=out/(kind+'.fbx')
 if target.exists():raise RuntimeError('Refusing to overwrite '+str(target))
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(source/('Teepees.fbx' if kind=='Teepee' else 'Totem.fbx')))
 meshes=[o for o in bpy.data.objects if o.type=='MESH']
 before=sum(len(o.data.vertices) for o in meshes)
 if kind=='Totem':
  for o in meshes:
   bpy.context.view_layer.objects.active=o
   triangles=sum(len(p.vertices)-2 for p in o.data.polygons)
   modifier=o.modifiers.new('Runtime silhouette reduction','DECIMATE');modifier.ratio=min(1,30000/max(1,triangles));bpy.ops.object.modifier_apply(modifier=modifier.name)
  corners=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
  center=Vector(((min(v.x for v in corners)+max(v.x for v in corners))/2,(min(v.y for v in corners)+max(v.y for v in corners))/2,min(v.z for v in corners)))
  scale=5/(max(v.z for v in corners)-center.z);rotation=Matrix.Identity(4)
 else:
  tarp=next(o for o in meshes if 'Tarp' in o.name);corners=[tarp.matrix_world@Vector(v) for v in tarp.bound_box]
  center=Vector(((min(v.x for v in corners)+max(v.x for v in corners))/2,(min(v.y for v in corners)+max(v.y for v in corners))/2,min(v.z for v in corners)))
  scale=3.8/(max(v.z for v in corners)-center.z);rotation=Matrix.Rotation(-math.pi/2,4,'Z')
 for o in meshes:
  transform=Matrix.Scale(scale,4)@rotation@Matrix.Translation(-center)@o.matrix_world
  o.data.transform(transform);o.parent=None;o.matrix_world=Matrix.Identity(4)
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,path_mode='STRIP')
 report.append(dict(model=kind,source_vertices=before,runtime_vertices=sum(len(o.data.vertices) for o in meshes)))
(out/'Preparation.json').write_text(json.dumps(report,indent=2))
print(report)
