import bpy,json
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-integration').mkdir(parents=True,exist_ok=True)
ws=_WORK;root=(_PROJECT/'SourceAssets/OptimizedBroadleaf');report=json.loads((root/'manifest.json').read_text());items=[]
for name,d in report.items():
 for season in (['Summer','Fall','Winter'] if name.startswith('OakPilot') else ['Summer']):
  paths=[root/name/(name+f'_LOD{i}.fbx') if season=='Summer' else root/name/season/f'LOD{i}.fbx' for i in range(3)]
  names=set()
  for path in paths:
   bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path))
   for o in bpy.context.scene.objects:
    if o.type=='MESH':names.update(o.data.materials[i].name for i in set(p.material_index for p in o.data.polygons))
  items.append({'species':name,'season':season,'paths':list(map(str,paths)),'source':d['source'],'materials':sorted(names)})
(ws/'work/broadleaf-integration/inventory.json').write_text(json.dumps(items,indent=2))
