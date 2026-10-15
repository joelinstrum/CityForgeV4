"""Run Blender -b --python Tools/prepare_summer_forest.py. Keeps the original FBX untouched."""
import bpy,bmesh,random,json,math
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[1]
out=root/'Assets/CityForge/Art/Trees/SummerForest';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'SourceAssets/Trees/SummerForest/Original/summer_forest_FBX_mm.fbx'))
report=[]
for srcname,name in [('tree_maple','SummerMaple'),('tree_linder','SummerLinden'),('tree_elm','SummerElm')]:
 source=bpy.data.objects[srcname]
 for level in range(3):
  pieces=[]
  for leaf in [False,True]:
   obj=source.copy();obj.data=source.data.copy();bpy.context.collection.objects.link(obj)
   bm=bmesh.new();bm.from_mesh(obj.data)
   bmesh.ops.delete(bm,geom=[f for f in bm.faces if (f.material_index==3)!=leaf],context='FACES')
   if leaf:
    seen=set();rng=random.Random(730421);remove=[]
    # Nested deterministic whole-leaf thinning; retain intact leaf UVs.
    keep=[1.0,.65,.4][level]
    for v in bm.verts:
     if v in seen:continue
     pending=[v];seen.add(v);island=[]
     while pending:
      q=pending.pop();island.append(q)
      for e in q.link_edges:
       n=e.other_vert(q)
       if n not in seen:seen.add(n);pending.append(n)
     if rng.random()>keep:remove.extend(island)
     else:
      center=sum((q.co for q in island),Vector())/len(island)
      for q in island:q.co=center+(q.co-center)*1.0
    bmesh.ops.delete(bm,geom=remove,context='VERTS')
   if not leaf:
    seen=set();remove=[]
    for v in bm.verts:
     if v in seen:continue
     pending=[v];seen.add(v);island=[];faces=set()
     while pending:
      q=pending.pop();island.append(q);faces.update(q.link_faces)
      for e in q.link_edges:
       n=e.other_vert(q)
       if n not in seen:seen.add(n);pending.append(n)
     # Discard disconnected fine twigs hidden within the dense crown.
     if len(faces)<120:remove.extend(island)
    bmesh.ops.delete(bm,geom=remove,context='VERTS')
   bm.to_mesh(obj.data);bm.free()
   bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
   if leaf and srcname=='tree_maple':
    mod=obj.modifiers.new('Simplify curved leaf interiors','DECIMATE');mod.ratio=.45
    bpy.ops.object.modifier_apply(modifier=mod.name)
   elif not leaf:
    mod=obj.modifiers.new('Reduce branches','DECIMATE');mod.ratio=[.25,.16,.10][level]
    bpy.ops.object.modifier_apply(modifier=mod.name)
   for p in obj.data.polygons:p.material_index=0
   obj.data.materials.clear();obj.data.materials.append(bpy.data.materials.get('SummerLeaf' if leaf else 'SummerBark') or bpy.data.materials.new('SummerLeaf' if leaf else 'SummerBark'))
   pieces.append(obj)
  bpy.ops.object.select_all(action='DESELECT')
  for obj in pieces:obj.select_set(True)
  bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();tree=bpy.context.object
  bounds=[tree.matrix_world@Vector(c) for c in tree.bound_box]
  origin=Vector(((min(p.x for p in bounds)+max(p.x for p in bounds))/2,(min(p.y for p in bounds)+max(p.y for p in bounds))/2,min(p.z for p in bounds)))
  for v in tree.data.vertices:v.co=tree.matrix_world@v.co-origin
  tree.matrix_world=Matrix.Identity(4);tree.name=name+f'_LOD{level}';tree.data.calc_loop_triangles()
  row=dict(name=tree.name,triangles=len(tree.data.loop_triangles),vertices=len(tree.data.vertices),dimensions=[v/1000 for v in tree.dimensions]);report.append(row);print('EXPORT',json.dumps(row),flush=True)
  bpy.ops.export_scene.fbx(filepath=str(out/(tree.name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
  bpy.data.objects.remove(tree,do_unlink=True)
(out/'triangle_counts.json').write_text(json.dumps(report,indent=2))
