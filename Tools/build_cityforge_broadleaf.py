"""Easy Tree branching + CityForge authored lobe foliage. Run Blender --background --python this_file."""
import bpy, bmesh, math, random,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]; source=root/'SourceAssets/Trees/CityForgeBroadleaf'; out=root/'Assets/CityForge/Art/Trees/CityForgeBroadleaf';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
with bpy.data.libraries.load(str(source/'EasyTree/assets.blend'),link=False) as (a,b):b.node_groups=['Simple Tree Generator']
g=bpy.data.node_groups['Simple Tree Generator']
bark=bpy.data.materials.new('CityForgeBark');bark.diffuse_color=(.14,.095,.06,1)
leaf=bpy.data.materials.new('CityForgeLeaf');leaf.diffuse_color=(.12,.26,.07,1)
report=[]
for variant in range(3):
 rng=random.Random(730421+variant);name='ForgeBroadleaf'+chr(65+variant)
 bpy.ops.mesh.primitive_cube_add();generator=bpy.context.object;generator.name=name+'_Editable_EasyTree_Branches';mod=generator.modifiers.new('Easy Tree branching','NODES');mod.node_group=g
 for key,value in {'Socket_40':3,'Socket_26':2,'Socket_27':5,'Socket_28':7,'Socket_29':.65,'Socket_30':.18,'Socket_31':.28,'Socket_32':2.4,'Socket_16':730421+variant,'Socket_19':False,'Socket_8':bark,'Socket_34':False}.items():mod[key]=value
 generator.update_tag();bpy.context.view_layer.update()
 deps=bpy.context.evaluated_depsgraph_get();mesh=bpy.data.meshes.new_from_object(generator.evaluated_get(deps));trunk=bpy.data.objects.new(name+'_Bark',mesh);bpy.context.collection.objects.link(trunk)
 coords=[v.co for v in mesh.vertices];lo=min(v.z for v in coords);hi=max(v.z for v in coords);scale=14/(hi-lo)
 for v in mesh.vertices:v.co=Vector((v.co.x*scale,v.co.y*scale,(v.co.z-lo)*scale))
 for v in mesh.vertices:v.co.x*=1.22;v.co.y*=1.22
 mesh.materials.clear();mesh.materials.append(bark)
 for p in mesh.polygons:p.material_index=0;p.use_smooth=True
 # Eight overlapping asymmetric lobes form an umbrella crown, with a lower skirt.
 lobes=[]
 for i in range(7):
  a=i*2*math.pi/7+rng.uniform(-.16,.16);radius=rng.uniform(2.5,3.5)
  lobes.append((Vector((math.cos(a)*radius,math.sin(a)*radius,rng.uniform(9.2,10.6))),Vector((rng.uniform(2.4,3.2),rng.uniform(2.4,3.2),rng.uniform(2.8,3.5)))))
 lobes.append((Vector((.2,-.25,11.9)),Vector((3.4,3.2,3.0))))
 lobes=[(Vector((c.x*1.22,c.y*1.22,c.z)),Vector((d.x*1.22,d.y*1.22,d.z))) for c,d in lobes]
 # Prune exposed fine twigs outside the authored crown envelope.
 bm=bmesh.new();bm.from_mesh(mesh)
 def outside(co):
  return co.z>6 and all(sum(((co[k]-c[k])/(d[k]*1.03))**2 for k in range(3))>1 for c,d in lobes)
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if all(outside(v.co) for v in f.verts)],context='FACES');bm.to_mesh(mesh);bm.free()
 cards=[]
 for center,radii in lobes:
  for j in range(950):
   z=rng.uniform(-1,1);a=rng.uniform(0,math.tau);direction=Vector((math.sqrt(1-z*z)*math.cos(a),math.sqrt(1-z*z)*math.sin(a),z));rad=rng.uniform(.55,1)**(1/3)
   pos=center+Vector((direction.x*radii.x,direction.y*radii.y,direction.z*radii.z))*rad
   normal=(direction*.7+Vector((rng.uniform(-1,1),rng.uniform(-1,1),rng.uniform(-1,1)))*.5).normalized()
   u=normal.cross(Vector((0,0,1)))
   if u.length<.01:u=Vector((1,0,0))
   u.normalize();v=normal.cross(u).normalized();angle=rng.uniform(0,math.tau);u,v=u*math.cos(angle)+v*math.sin(angle),v*math.cos(angle)-u*math.sin(angle)
   cards.append((pos,u,v,rng.uniform(.65,.95),rng.random()))
 for level,keep in enumerate((1,.65,.4)):
  vertices=[];faces=[]
  for pos,u,v,size,key in cards:
   if key>keep:continue
   # Larger lower-tier clusters compensate modest thinning while retaining lobe coverage.
   size*=1 if level==0 else 1.12 if level==1 else 1.25
   start=len(vertices);vertices.extend([pos+u*x*size+v*y*size for x,y in ((-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5))]);faces.append(tuple(start+i for i in range(4)))
  lm=bpy.data.meshes.new(name+'_Leaf');lm.from_pydata(vertices,[],faces);lm.update();lm.materials.append(leaf);uv=lm.uv_layers.new()
  for p in lm.polygons:
   for li in p.loop_indices:uv.data[li].uv=((0,0),(1,0),(1,1),(0,1))[lm.loops[li].vertex_index%4]
  foliage=bpy.data.objects.new(name+'_Leaf',lm);bpy.context.collection.objects.link(foliage)
  copy=trunk.copy();copy.data=trunk.data.copy();bpy.context.collection.objects.link(copy)
  bpy.context.view_layer.objects.active=copy
  dec=copy.modifiers.new('Bark budget','DECIMATE');dec.ratio=(.55,.35,.2)[level];bpy.ops.object.modifier_apply(modifier=dec.name)
  # Bark UVs from cylindrical projection; leaf clusters have a separate atlas material.
  uvb=copy.data.uv_layers.new(name='BarkUV')
  for p in copy.data.polygons:
   for li in p.loop_indices:
    co=copy.data.vertices[copy.data.loops[li].vertex_index].co;uvb.data[li].uv=(math.atan2(co.y,co.x)/math.tau,co.z/3)
  bpy.ops.object.select_all(action='DESELECT');copy.select_set(True);foliage.select_set(True);bpy.context.view_layer.objects.active=copy;bpy.ops.object.join();copy.name=f'{name}_LOD{level}'
  copy.data.calc_loop_triangles();report.append(dict(name=copy.name,triangles=len(copy.data.loop_triangles),leafCards=len(faces),dimensions=list(copy.dimensions)))
  bpy.ops.export_scene.fbx(filepath=str(out/(copy.name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
  if level==0:
   copy.hide_render=True;copy.hide_set(True)
  else:bpy.data.objects.remove(copy,do_unlink=True)
 trunk.hide_render=True;trunk.hide_set(True);generator.hide_render=True;generator.hide_set(True)
# Keep reproducible Easy Tree modifiers and custom meshes in the authoring file.
(source/'mesh_counts.json').write_text(json.dumps(report,indent=2));(out/'mesh_counts.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(source/'CityForge_Broadleaf.blend'));print('COUNTS',json.dumps(report))
