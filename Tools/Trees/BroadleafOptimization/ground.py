import bpy,bmesh,json
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
from mathutils import Vector,Matrix
ws=_WORK;root=(_PROJECT/'SourceAssets/OptimizedBroadleaf');report=json.loads((root/'manifest.json').read_text())
def bbox(objects):
 cs=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box];return Vector([min(c[i] for c in cs) for i in range(3)]),Vector([max(c[i] for c in cs) for i in range(3)])
for name,d in report.items():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if d['source'].endswith('.blend'):bpy.ops.wm.open_mainfile(filepath=d['source'],load_ui=False)
 else:bpy.ops.import_scene.fbx(filepath=d['source'])
 allobjs=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)];objects=allobjs;second=None;oldobjects=objects
 if name.startswith(('SilverMaple','CanyonLiveOak')):
  is_b=name.endswith('B');oldobjects=[o for o in allobjs if o.name.endswith('.001')==is_b]
  objects=[o for o in allobjs if (o.name.endswith('.001') or o.name.startswith('Bark2'))==is_b]
  lo,hi=bbox(oldobjects);oldcenter=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
  if is_b:
   second=next(o for o in allobjs if o.name.startswith('Bark2')).data.copy()
   src=next(o for o in allobjs if o.name.startswith('Bark2'));second.transform(Matrix.Translation(-oldcenter)@src.matrix_world)
 lo,hi=bbox(objects);height=hi.z-lo.z;d['height_m']=height;d['source_triangles']=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)
 for lod in range(3):
  path=root/name/(name+'_LOD'+str(lod)+'.fbx')
  for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.ops.import_scene.fbx(filepath=str(path));model=next(o for o in bpy.context.scene.objects if o.type=='MESH')
  if name in ['SilverMapleA','CanyonLiveOakA']:
   bm=bmesh.new();bm.from_mesh(model.data);remove=[f for f in bm.faces if model.data.materials[f.material_index].name.startswith('Bark2')];bmesh.ops.delete(bm,geom=remove,context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(model.data);bm.free()
  if second:
   bark=bpy.data.objects.new('Restored second-variant base',second.copy());bpy.context.scene.collection.objects.link(bark);bpy.context.view_layer.objects.active=bark
   mod=bark.modifiers.new('Base detail','DECIMATE');mod.ratio=min(1,[1100,700,450][lod]/max(1,sum(len(p.vertices)-2 for p in bark.data.polygons)));mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
   bpy.ops.object.select_all(action='DESELECT');model.select_set(True);bark.select_set(True);bpy.context.view_layer.objects.active=model;bpy.ops.object.join()
  lo,hi=bbox([model]);factor=height/(hi.z-lo.z)
  inverse=model.matrix_world.inverted()
  for v in model.data.vertices:
   point=model.matrix_world@v.co;point.z-=lo.z;point*=factor;v.co=inverse@point
  bpy.ops.object.select_all(action='DESELECT');model.select_set(True)
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
  print('GROUNDED',name,lod,height,flush=True)
(root/'manifest.json').write_text(json.dumps(report,indent=2));(ws/'work/broadleaf-intake/optimized.json').write_text(json.dumps(report,indent=2))
