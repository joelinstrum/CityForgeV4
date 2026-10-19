import bpy,json
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
base=(_SOURCE/'Authoring/Flora')
sources={
'Silver Maple': 'IncomingVarious/silver-maple/v01_source/raw/Blender-Silver Maple Set/Silver Maple Set.blend',
'Canyon Live Oak': 'OakTrees/canyon-live-oak/v01_source/raw/Blender-Canyon Live Oak/Canyon live oak.blend',
'Southern Live Oak': 'OakTrees/quercus-virginiana/v01_source/raw/Blender-Quercus virginiana/Quercus virginiana.blend',
'Angel Oak': 'OakTrees/angel-oak-authoritative/v01_source/angel-oak-vray-import-v01.blend',
'OakPilot Summer': 'SpeedTree/OakPilot/v02_exports/summer/CF_Flora_OakPilot_Summer_v02.fbx',
'OakPilot Variant B Summer': 'SpeedTree/OakPilot/v09_variants/variant-b/summer/CF_Flora_OakPilot_VariantB_Summer_v01.fbx'}
result={}
for name,rel in sources.items():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 path=str(base/rel)
 if path.endswith('.blend'):bpy.ops.wm.open_mainfile(filepath=path,load_ui=False)
 else:bpy.ops.import_scene.fbx(filepath=path)
 rows=[]
 for o in bpy.context.scene.objects:
  if o.type!='MESH':continue
  rows.append({'name':o.name,'triangles':sum(max(0,len(p.vertices)-2) for p in o.data.polygons),'dimensions':list(o.dimensions),'hidden':o.hide_render,'modifiers':[m.type for m in o.modifiers]})
 result[name]={'source':path,'objects':rows,'scene_triangles':sum(r['triangles'] for r in rows)}
 print('TREE_COUNT',name,result[name]['scene_triangles'],flush=True)
 (_WORK/'work/broadleaf-intake/counts.json').write_text(json.dumps(result,indent=2))
