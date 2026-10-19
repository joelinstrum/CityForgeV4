import bpy,bmesh,json,math,time
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
from mathutils import Vector,Matrix
workspace=_WORK
root=(_PROJECT/'SourceAssets/OptimizedBroadleaf')
inventory=json.loads((workspace/'work/broadleaf-intake/counts.json').read_text());report={}
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
def bounds(objects):
 v=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
 return [min(c[i] for c in v) for i in range(3)],[max(c[i] for c in v) for i in range(3)]
def simplify(o,budget):
 bpy.context.view_layer.objects.active=o
 # Preserve disconnected cards: first remove coplanar subdivision, then collapse.
 bm=bmesh.new();bm.from_mesh(o.data)
 bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
 bmesh.ops.dissolve_limit(bm,angle_limit=.035,verts=list(bm.verts),edges=list(bm.edges),delimit={'UV'})
 bm.to_mesh(o.data);bm.free()
 count=tris(o)
 if count>budget:
  m=o.modifiers.new('Geometry reduction','DECIMATE');m.ratio=max(.0001,budget/count);bpy.ops.object.modifier_apply(modifier=m.name)
 # Decimation cannot remove disconnected cards below their minimum topology.
 # Thin only the smallest disconnected pieces when that floor exceeds budget.
 if tris(o)>budget*1.08:
  bm=bmesh.new();bm.from_mesh(o.data);bm.verts.ensure_lookup_table();seen=set();pieces=[]
  for v in bm.verts:
   if v in seen:continue
   todo=[v];seen.add(v);verts=[];faces=set()
   while todo:
    a=todo.pop();verts.append(a);faces.update(a.link_faces)
    for e in a.link_edges:
     b=e.other_vert(a)
     if b not in seen:seen.add(b);todo.append(b)
   if faces:
    size=sum(max(0,len(f.verts)-2) for f in faces)
    center=sum((a.co for a in verts),Vector())/len(verts)
    # Stable spatial hash distributes removals across the canopy.
    h=math.sin(center.x*12.9898+center.y*78.233+center.z*37.719)*43758.5453;h-=math.floor(h)
    span=max((a.co-center).length for a in verts)
    pieces.append((span*(.65+.7*h),size,verts))
  current=tris(o);remove=[]
  for _,n,verts in sorted(pieces,key=lambda p:p[0]):
   if current<=budget:break
   # Never discard the connected trunk/major branch mesh.
   if n>max(100,budget*.2):continue
   remove.extend(verts);current-=n
  bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(o.data);bm.free()
 return tris(o)
for label,info in inventory.items():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if info['source'].endswith('.blend'):bpy.ops.wm.open_mainfile(filepath=info['source'],load_ui=False)
 else:bpy.ops.import_scene.fbx(filepath=info['source'])
 allobjs=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
 groups=[('',allobjs)]
 if label in ['Silver Maple','Canyon Live Oak']:
  groups=[('A',[o for o in allobjs if not o.name.endswith('.001')]),('B',[o for o in allobjs if o.name.endswith('.001')])]
 for variant,objects in groups:
  name=label.replace(' ','')+variant;out=root/name;out.mkdir(parents=True,exist_ok=True)
  for marker in out.glob('*.coverage.json'):marker.unlink()
  low,high=bounds(objects);height=high[2]-low[2];center=Vector(((low[0]+high[0])/2,(low[1]+high[1])/2,low[2]))
  originals=[]
  for o in objects:
   mesh=o.data.copy();mesh.transform(Matrix.Translation(-center)@o.matrix_world)
   originals.append((o,mesh))
  source=sum(sum(len(p.vertices)-2 for p in m.polygons) for _,m in originals)
  levels=[]
  for lod,target in enumerate([60000,20000,8000]):
   for o in allobjs:o.hide_render=True;o.hide_set(True)
   sizes=[sum(len(p.vertices)-2 for p in m.polygons) for _,m in originals]
   foliage=[any(tag in o.name.lower() for tag in ['leaf','moss']) for o,_ in originals]
   leaf_total=sum(n for n,f in zip(sizes,foliage) if f);wood_total=sum(n for n,f in zip(sizes,foliage) if not f)
   leaf_share=[.85,.9,.92][lod] if leaf_total and wood_total else (1 if leaf_total else 0)
   for (o,mesh),size,is_leaf in zip(originals,sizes,foliage):
    o.data=mesh.copy();o.parent=None;o.matrix_world=Matrix.Identity(4);o.hide_render=False;o.hide_set(False)
    simplify(o,max(8,round(target*(leaf_share if is_leaf else 1-leaf_share)*size/(leaf_total if is_leaf else wood_total))))
   bpy.ops.object.select_all(action='DESELECT')
   for o in objects:o.select_set(True)
   bpy.ops.export_scene.fbx(filepath=str(out/(name+'_LOD'+str(lod)+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
   actual=sum(tris(o) for o in objects);lo,hi=bounds(objects)
   levels.append({'triangles':actual,'height_m':hi[2]-lo[2],'target':target})
   print('OPTIMIZED',name,lod,actual,flush=True)
  report[name]={'source':info['source'],'source_triangles':source,'height_m':height,'lods':levels}
  (workspace/'work/broadleaf-intake/optimized.json').write_text(json.dumps(report,indent=2))
(root/'manifest.json').write_text(json.dumps(report,indent=2))
