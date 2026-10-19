import bpy,bmesh,json
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-integration').mkdir(parents=True,exist_ok=True)
from mathutils import Vector,Matrix
ws=_WORK;root=(_PROJECT/'SourceAssets/OptimizedBroadleaf');v3=(_SOURCE/'Authoring/Flora/SpeedTree/OakPilot')
# Reuse the geometry simplification routine, without running the authoring build.
import ast,math
code=(_PROJECT/'Tools/Trees/BroadleafOptimization/optimize.py').read_text();module=ast.parse(code);functions=[n for n in module.body if isinstance(n,ast.FunctionDef) and n.name in ['tris','simplify']];exec(compile(ast.Module(body=functions,type_ignores=[]),'<geometry helpers>','exec'),globals())
result={}
for species,prefix in [('OakPilotSummer',v3/'v02_exports'),('OakPilotVariantBSummer',v3/'v09_variants/variant-b')]:
 for season,folder in [('Fall','autumn'),('Winter','winter')]:
  source=next((prefix/folder).glob('*.fbx'));bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(source))
  objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
  cs=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box];lo=Vector([min(c[i] for c in cs) for i in range(3)]);hi=Vector([max(c[i] for c in cs) for i in range(3)]);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
  originals=[]
  for o in objects:
   m=o.data.copy();m.transform(Matrix.Translation(-center)@o.matrix_world);originals.append((o,m))
  total=sum(sum(len(p.vertices)-2 for p in m.polygons) for _,m in originals);counts=[];out=root/species/season;out.mkdir(exist_ok=True)
  for lod,budget in enumerate([60000,20000,8000]):
   for o,m in originals:
    o.data=m.copy();o.parent=None;o.matrix_world=Matrix.Identity(4);n=sum(len(p.vertices)-2 for p in m.polygons);simplify(o,max(8,round(budget*n/total)))
   bpy.ops.object.select_all(action='DESELECT')
   for o in objects:o.select_set(True)
   bpy.ops.export_scene.fbx(filepath=str(out/f'LOD{lod}.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
   counts.append(sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects))
  result[species+season]={'source':str(source),'counts':counts};print('SEASONS',species,season,counts,flush=True)
(ws/'work/broadleaf-integration/pilot-seasons.json').write_text(json.dumps(result,indent=2))
