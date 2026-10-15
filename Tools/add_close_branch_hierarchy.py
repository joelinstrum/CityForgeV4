import bpy,json,math,random
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];A=R/'Assets/CityForge/Art/Trees/DenseForestTrees';D=R/'SourceAssets/Trees/DenseForestTrees/CloseLOD0';recipe=json.loads((A/'family_recipe.json').read_text())
for e in recipe['variants']:
 if e['season']=='Winter':continue
 name=e['name'];bpy.ops.wm.open_mainfile(filepath=str(D/(name+'.blend')));wood=bpy.data.objects['Tapered trunk root flare and scaffold branches']
 for old in list(bpy.data.objects):
  if old.name.startswith(('Close branch supported foliage twigs','Close secondary branch fans')):bpy.data.objects.remove(old,do_unlink=True)
 controls=e['controls'];masses=controls.get('foliageMasses')
 if not masses:masses=json.loads((R/'SourceAssets/Trees/CityForgeBroadleaf/Refinement01/recipe_geometry.json').read_text())['foliageMasses']
 vv=[];ff=[];rng=random.Random(730421);points=[v.co.copy() for v in wood.data.vertices]
 def tube(start,tip,r):
  delta=tip-start;side=delta.cross(Vector((0,0,1))).normalized();axis=delta.normalized();bend=side*rng.uniform(-.18,.18)+Vector((0,0,.18));base=len(vv)
  u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u)
  for k in range(9):
   t=k/8;center=start+delta*t+bend*math.sin(t*math.pi);radius=r*(1-t)**1.15+.003
   for j in range(8):
    a=j*math.tau/8;rr=radius*(1+.10*math.sin(j*2+k*.45));vv.append(center+(u*math.cos(a)+v*math.sin(a))*rr)
  for k in range(8):
   for j in range(8):i=base+k*8+j;ff.append((i,base+k*8+(j+1)%8,base+(k+1)*8+(j+1)%8,i+8))
 for index,m in enumerate(masses):
  if index%2:continue
  center=Vector(m['center']);rad=Vector(m['radii']);target=center-Vector((0,0,rad.z*.3));start=min(points,key=lambda p:(p-target).length_squared)
  if (target-start).length<.25:continue
  tube(start,target,.045)
  for j in range(3):
   theta=j*2.1+index;tip=target+Vector((math.cos(theta)*rad.x*.32,math.sin(theta)*rad.y*.32,rad.z*.28));tube(start.lerp(target,.7),tip,.021)
 mesh=bpy.data.meshes.new('Curved secondary branch fans');mesh.from_pydata(vv,[],ff);mesh.update();mesh.materials.append(wood.data.materials[0]);ob=bpy.data.objects.new('Close secondary branch fans',mesh);bpy.context.collection.objects.link(ob)
 uv=mesh.uv_layers.new()
 for p in mesh.polygons:
  p.use_smooth=True
  for li in p.loop_indices:q=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=((q.x+q.y)*.65,q.z/3)
 bpy.ops.wm.save_as_mainfile(filepath=str(D/(name+'.blend')));bpy.ops.object.select_all(action='DESELECT')
 for obj in [wood,ob,bpy.data.objects['Irregular branch organized foliage masses']]:obj.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(A/'CloseLOD0'/name/'LOD0.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False)
 print('BRANCH FANS '+name,flush=True)
