import bpy,bmesh,json,math
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
from mathutils import Vector
ws=_WORK;root=(_PROJECT/'SourceAssets/OptimizedBroadleaf')
report=json.loads((ws/'work/broadleaf-intake/optimized.json').read_text())
def leaf(o):return any(s in o.name.lower() for s in ['leaf','moss'])
for name,d in report.items():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if d['source'].endswith('.blend'):bpy.ops.wm.open_mainfile(filepath=d['source'],load_ui=False)
 else:bpy.ops.import_scene.fbx(filepath=d['source'])
 if name=='AngelOak':
  bpy.ops.object.select_all(action='DESELECT')
  main=next(o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)>10000)
  main.select_set(True);bpy.context.view_layer.objects.active=main;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='MATERIAL');bpy.ops.object.mode_set(mode='OBJECT')
  for part in bpy.context.selected_objects:
   if part.type=='MESH' and len(part.data.polygons):
    material=part.data.materials[part.data.polygons[0].material_index].name
    part.name='AngelOakLeaf' if '#16' in material else 'AngelOakBark'
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
 if name in ['SilverMapleA','CanyonLiveOakA']:objects=[o for o in objects if not o.name.endswith('.001')]
 if name in ['SilverMapleB','CanyonLiveOakB']:objects=[o for o in objects if o.name.endswith('.001')]
 areas={}
 for o in objects:
  if leaf(o):
   bm=bmesh.new();bm.from_mesh(o.data);bm.transform(o.matrix_world);areas[o.name]=sum(f.calc_area() for f in bm.faces);bm.free()
 for lod in range(3):
  path=root/name/(name+'_LOD'+str(lod)+'.fbx');marker=path.with_suffix('.coverage.json')
  previous=json.loads(marker.read_text()) if marker.exists() else {}
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path))
  objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)];scales={}
  for o in objects:
   if not leaf(o) or o.name not in areas:continue
   bm=bmesh.new();bm.from_mesh(o.data);bm.transform(o.matrix_world)
   current=sum(f.calc_area() for f in bm.faces)
   old=previous.get(o.name,1)
   limit=[1.0,1.15,1.3][lod] if 'moss' in o.name.lower() else [2.0,2.4,3.5][lod]
   desired=min(limit,max(1,old*math.sqrt(areas[o.name]/max(current,.00001))))
   factor=desired/old
   seen=set()
   for v in bm.verts:
    if v in seen:continue
    todo=[v];seen.add(v);verts=[]
    while todo:
     a=todo.pop();verts.append(a)
     for e in a.link_edges:
      b=e.other_vert(a)
      if b not in seen:seen.add(b);todo.append(b)
    center=sum((a.co for a in verts),Vector())/len(verts)
    for a in verts:a.co=center+(a.co-center)*factor
   bm.transform(o.matrix_world.inverted());bm.to_mesh(o.data);bm.free();scales[o.name]=desired
  bpy.ops.object.select_all(action='DESELECT')
  for o in objects:o.select_set(True)
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
  marker.write_text(json.dumps(scales,indent=2));print('COVERAGE',name,lod,flush=True)
