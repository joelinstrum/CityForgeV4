import bpy,bmesh,json,math
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
from mathutils import Vector,Matrix
ws=_WORK;root=(_PROJECT/'SourceAssets/OptimizedBroadleaf');report=json.loads((root/'manifest.json').read_text())
for name,d in report.items():
 if name not in ['CanyonLiveOakA','CanyonLiveOakB','SouthernLiveOak']:continue
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.wm.open_mainfile(filepath=d['source'],load_ui=False)
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
 if name.endswith('A'):objects=[o for o in objects if not o.name.endswith('.001')]
 if name.endswith('B'):objects=[o for o in objects if o.name.endswith('.001')]
 corners=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box];lo=Vector([min(c[i] for c in corners) for i in range(3)]);hi=Vector([max(c[i] for c in corners) for i in range(3)]);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
 branches=[]
 for o in objects:
  if not o.name.startswith('Branch'):continue
  mesh=o.data.copy();mesh.transform(Matrix.Translation(-center)@o.matrix_world)
  bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();branches.append((o.name,mesh))
 for lod in range(3):
  path=root/name/(name+'_LOD'+str(lod)+'.fbx')
  for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.ops.import_scene.fbx(filepath=str(path));model=next(o for o in bpy.context.scene.objects if o.type=='MESH');bm=bmesh.new();bm.from_mesh(model.data)
  remove=[f for f in bm.faces if 'Branch' in model.data.materials[f.material_index].name]
  bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(model.data);bm.free()
  made=[]
  for title,mesh in branches:
   o=bpy.data.objects.new(title,mesh.copy());bpy.context.scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o
   bm=bmesh.new();bm.from_mesh(o.data);seen=set();remove=[];pieces=[]
   budget=[7000,4000,2000][lod]/len(branches)
   for v in bm.verts:
    if v in seen:continue
    todo=[v];seen.add(v);verts=[]
    while todo:
     a=todo.pop();verts.append(a)
     for e in a.link_edges:
      b=e.other_vert(a)
      if b not in seen:seen.add(b);todo.append(b)
    low=Vector([min(v.co[i] for v in verts) for i in range(3)]);high=Vector([max(v.co[i] for v in verts) for i in range(3)])
    if (high-low).length<[.8,1.2,2.0][lod]:remove.extend(verts)
    else:
     faces=set(f for v in verts for f in v.link_faces);area=sum(f.calc_area() for f in faces)
     pieces.append((area,verts))
   pieces.sort(key=lambda p:p[0],reverse=True)
   for _,verts in pieces[max(1,int(budget/30)):]:remove.extend(verts)
   bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(o.data);bm.free()
   before=sum(len(p.vertices)-2 for p in o.data.polygons);budget=[7000,4000,2000][lod]/len(branches)
   if before>budget:
    mod=o.modifiers.new('Triangulated branch reduction','DECIMATE');mod.ratio=budget/before;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
   made.append(o)
  bpy.ops.object.select_all(action='DESELECT');model.select_set(True)
  for o in made:o.select_set(True)
  bpy.context.view_layer.objects.active=model;bpy.ops.object.join()
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
  print('BRANCH',name,lod,sum(len(p.vertices)-2 for p in model.data.polygons),flush=True)
