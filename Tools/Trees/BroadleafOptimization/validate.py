import bpy,json,math
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
from mathutils import Vector
ws=_WORK;root=(_PROJECT/'SourceAssets/OptimizedBroadleaf');report=json.loads((root/'manifest.json').read_text());fail=[]
for name,d in report.items():
 levels=[]
 for lod in range(3):
  bpy.ops.wm.read_factory_settings(use_empty=True);path=root/name/(name+'_LOD'+str(lod)+'.fbx');bpy.ops.import_scene.fbx(filepath=str(path))
  objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)];count=0;uv=True;materials=True;finite=True
  for o in objects:
   count+=sum(len(p.vertices)-2 for p in o.data.polygons)
   uv&=len(o.data.uv_layers)>0
   materials&=len(o.data.materials)>0 and all(m is not None for m in o.data.materials)
   finite&=all(math.isfinite(c) for v in o.data.vertices for c in v.co)
   finite&=all(math.isfinite(c) for layer in o.data.uv_layers for datum in layer.data for c in datum.uv)
  corners=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box];lo=[min(v[i] for v in corners) for i in range(3)];hi=[max(v[i] for v in corners) for i in range(3)]
  height=hi[2]-lo[2]
  # FBX meshes are exported with Y up, Blender importer restores Z up.
  valid=bool(objects) and uv and materials and finite and .8*d['height_m']<=height<=1.2*d['height_m'] and abs(lo[2])<max(.5,d['height_m']*.05)
  if not valid:fail.append(name+' LOD'+str(lod))
  levels.append({'triangles':count,'height_m':height,'mesh_objects':len(objects),'uv_layers_present':uv,'material_slots_present':materials,'finite_vertices_and_uvs':finite,'file_bytes':path.stat().st_size,'valid':valid})
 counts=[l['triangles'] for l in levels]
 if not(counts[0]>counts[1]>counts[2]>0):fail.append(name+' LOD ordering')
 if counts[0]>70000 or counts[1]>30000 or counts[2]>15000:fail.append(name+' budget')
 d['lods']=levels;d['near_reduction_percent']=100*(1-counts[0]/d['source_triangles']);d['far_reduction_percent']=100*(1-counts[2]/d['source_triangles'])
 print('VALIDATED',name,counts,'valid',all(x['valid'] for x in levels),flush=True)
report_out={'checks':'FBX round-trip; decreasing geometry LODs; budgets; finite vertex/UV data; UV and material slots; height and ground contact. Geometry only, not Unity material or GPU performance acceptance.','failures':fail,'trees':report}
(ws/'outputs/Broadleaf-Optimization-Checks.json').write_text(json.dumps(report_out,indent=2));(root/'manifest.json').write_text(json.dumps(report,indent=2));(ws/'work/broadleaf-intake/optimized.json').write_text(json.dumps(report,indent=2))
if fail:raise RuntimeError(str(fail))
