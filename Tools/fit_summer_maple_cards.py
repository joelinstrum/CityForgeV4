"""Run after prepare_summer_forest.py. Fits one UV quad per original maple leaf without thinning LOD0."""
import bpy,bmesh,numpy as np,json,random
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[1];out=root/'Assets/CityForge/Art/Trees/SummerForest'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'SourceAssets/Trees/SummerForest/Original/summer_forest_FBX_mm.fbx'))
source=bpy.data.objects['tree_maple'];bm=bmesh.new();bm.from_mesh(source.data)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=3],context='FACES');uv=bm.loops.layers.uv.active
seen=set();cards=[]
for v in bm.verts:
 if v in seen:continue
 pending=[v];seen.add(v);island=[]
 while pending:
  q=pending.pop();island.append(q)
  for e in q.link_edges:
   n=e.other_vert(q)
   if n not in seen:seen.add(n);pending.append(n)
 valid=[q for q in island if q.link_loops]
 if not valid:continue
 a=np.array([[*q.link_loops[0][uv].uv,1] for q in valid]);c=np.array([list(q.co) for q in valid]);fit=np.linalg.lstsq(a,c,rcond=None)[0]
 corners=np.array([[0,0,1],[1,0,1],[1,1,1],[0,1,1]])@fit
 avg=sum((q.normal for q in valid),Vector())
 flip=Vector(np.cross(corners[1]-corners[0],corners[2]-corners[0])).dot(avg)<0
 cards.append((corners,flip))
bm.free();report=[]
for level,keep in enumerate([1,.65,.4]):
 # Reuse already reduced trunk/branch mesh from the full-density export.
 bpy.ops.object.select_all(action='DESELECT')
 before=set(bpy.data.objects)
 bpy.ops.import_scene.fbx(filepath=str(out/f'SummerMaple_LOD{level}.fbx'))
 tree=next(o for o in bpy.data.objects if o not in before and o.type=='MESH')
 bm=bmesh.new();bm.from_mesh(tree.data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=0],context='FACES');bm.to_mesh(tree.data);bm.free()
 verts=[];faces=[];uvs=[];rng=random.Random(730421);count=0
 origin=Vector(((min((source.matrix_world@Vector(c)).x for c in source.bound_box)+max((source.matrix_world@Vector(c)).x for c in source.bound_box))/2,(min((source.matrix_world@Vector(c)).y for c in source.bound_box)+max((source.matrix_world@Vector(c)).y for c in source.bound_box))/2,min((source.matrix_world@Vector(c)).z for c in source.bound_box)))
 for corners,flip in cards:
  if rng.random()>keep:continue
  base=len(verts);verts.extend([source.matrix_world@Vector(c)-origin for c in corners]);faces.append(tuple(base+i for i in ([3,2,1,0] if flip else [0,1,2,3])));count+=1
 mesh=bpy.data.meshes.new('Fitted maple leaves');mesh.from_pydata(verts,[],faces);mesh.update();layer=mesh.uv_layers.new(name='UVChannel_1')
 for p in mesh.polygons:
  for loop in p.loop_indices:layer.data[loop].uv=[(0,0),(1,0),(1,1),(0,1)][mesh.loops[loop].vertex_index%4]
 leaf=bpy.data.objects.new('Maple fitted cards',mesh);bpy.context.collection.objects.link(leaf);mesh.materials.append(bpy.data.materials.get('SummerLeaf') or bpy.data.materials.new('SummerLeaf'))
 bpy.ops.object.select_all(action='DESELECT');tree.select_set(True);leaf.select_set(True);bpy.context.view_layer.objects.active=tree;bpy.ops.object.join();tree.name=f'SummerMaple_LOD{level}';tree.data.calc_loop_triangles()
 row=dict(name=tree.name,triangles=len(tree.data.loop_triangles),leafCards=count,sourceLeafCards=len(cards),dimensions=list(tree.dimensions));report.append(row);print('FITTED',json.dumps(row),flush=True)
 bpy.ops.export_scene.fbx(filepath=str(out/(tree.name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
 bpy.data.objects.remove(tree,do_unlink=True)
(out/'maple_fitted_counts.json').write_text(json.dumps(report,indent=2))

counts_path=out/"triangle_counts.json"
if counts_path.exists():
 existing=json.loads(counts_path.read_text());lookup={row["name"]:row for row in report}
 counts_path.write_text(json.dumps([lookup.get(row["name"],row) for row in existing],indent=2)+"\n")
