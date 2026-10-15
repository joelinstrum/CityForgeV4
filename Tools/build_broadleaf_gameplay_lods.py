"""Derived game meshes only; accepted source remains untouched."""
import bpy,json,random,struct,math
from pathlib import Path
from mathutils import Vector
r=Path(__file__).resolve().parents[1];out=r/'Assets/CityForge/Art/Trees/FrozenBroadleafEvaluation/GameplayLOD';out.mkdir(exist_ok=True)
masses=json.loads((r/'SourceAssets/Trees/CityForgeBroadleaf/Refinement01/recipe_geometry.json').read_text())['foliageMasses'];masses=[(Vector(m['center']),Vector(m['radii'])) for m in masses];report=[]
for tier,wood_ratio,leaf_keep,spray_scale in [(0,.22,1,1),(1,.09,.65,1.20),(2,.03,.25,1.80)]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(out.parent/'FrozenBroadleaf.fbx'))
 wood=next(o for o in bpy.context.scene.objects if o.type=='MESH' and 'trunk' in o.name.lower());leaf=next(o for o in bpy.context.scene.objects if o.type=='MESH' and 'foliage' in o.name.lower())
 bpy.context.view_layer.objects.active=wood;mod=wood.modifiers.new('Gameplay wood reduction','DECIMATE');mod.ratio=wood_ratio;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 if tier:
  rng=random.Random(730421);old=leaf.data;verts=[];faces=[];uvs=[];slots=[]
  for face in old.polygons:
   if rng.random()>leaf_keep:continue
   pp=[old.vertices[i].co.copy() for i in face.vertices];center=sum(pp,Vector())/len(pp);base=len(verts);verts.extend(center+(v-center)*spray_scale for v in pp);faces.append(tuple(range(base,base+len(pp))));uvs.extend(old.uv_layers.active.data[i].uv.copy() for i in face.loop_indices);slots.append(face.material_index)
  mesh=bpy.data.meshes.new('Gameplay foliage LOD'+str(tier));mesh.from_pydata(verts,[],faces);mesh.update();[mesh.materials.append(m) for m in old.materials];uv=mesh.uv_layers.new(name='UVMap')
  for i,v in enumerate(uvs):uv.data[i].uv=v
  for i,mi in enumerate(slots):mesh.polygons[i].material_index=mi
  leaf.data=mesh
 records=[]
 for vertex in leaf.data.vertices:
  p=vertex.co;best=1e10
  for c,rad in masses:
   d=p-c;dist=sum((d[i]/rad[i])**2 for i in range(3))
   if dist<best:best=dist;chosen=(d,rad)
  d,rad=chosen;n=Vector(tuple(d[i]/rad[i]**2 for i in range(3))).normalized();t=max(0,min(1,(math.sqrt(best)-.45)/.50));records.append((*p,*n,t*t*(3-2*t)))
 with (out/f'LOD{tier}_Guides.bytes').open('wb') as f:
  f.write(struct.pack('<i',len(records)))
  for v in records:f.write(struct.pack('<7f',*v))
 bpy.ops.object.select_all(action='DESELECT');wood.select_set(True);leaf.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/f'Broadleaf_LOD{tier}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
 entry={'tier':tier,'woodRatio':wood_ratio,'leafKeep':leaf_keep,'sprayScale':spray_scale,'meshes':[]}
 for o in [wood,leaf]:o.data.calc_loop_triangles();entry['meshes'].append({'name':o.name,'triangles':len(o.data.loop_triangles)})
 report.append(entry);print(entry,flush=True)
(out/'recipe.json').write_text(json.dumps(report,indent=2))
