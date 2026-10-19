import bpy,json
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
root=(_PROJECT/'SourceAssets/OptimizedBroadleaf')
for folder in sorted(root.iterdir()):
 if not folder.is_dir() or not (folder/(folder.name+'_LOD0.fbx')).exists():continue
 for lod in range(3):
  path=folder/(folder.name+'_LOD'+str(lod)+'.fbx');bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path));bpy.ops.object.select_all(action='DESELECT')
  objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
  for o in objects:o.select_set(True)
  bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();joined=bpy.context.object;joined.name=folder.name+'_LOD'+str(lod)
  import bmesh
  bm=bmesh.new();bm.from_mesh(joined.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(joined.data);bm.free()
  used=sorted(set(p.material_index for p in joined.data.polygons));materials=[joined.data.materials[i] for i in used];indices=[used.index(p.material_index) for p in joined.data.polygons]
  joined.data.materials.clear()
  for m in materials:joined.data.materials.append(m)
  for p,i in zip(joined.data.polygons,indices):p.material_index=i
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
  print('COMBINED',folder.name,lod,len(joined.data.materials),'materials',flush=True)
