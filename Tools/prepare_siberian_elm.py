"""Run Blender -b Original/Siberian Elm.blend --python this script.
Preserves source files. Exports LOD0, LOD1 and LOD2 plus a packed comparison .blend.
"""
import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector,Matrix
out=Path(__file__).resolve().parents[1]/'SourceAssets/Trees/SiberianElm/Optimized';out.mkdir(parents=True,exist_ok=True)
sources=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Siberian')]
# Work in an isolated scene, leaving source datablocks available for copying.
scene=bpy.data.scenes.new('Siberian Elm LOD Comparison');bpy.context.window.scene=scene
report=[]
names={'Siberian Elm Tree ':'SiberianElmA','Siberian Elm Tree Young':'SiberianElmYoung','Siberian Elm Tree Large':'SiberianElmLarge','Siberian Elm Tree Aut':'SiberianElmA_Autumn','Siberian Elm Tree M Aut':'SiberianElmYoung_Autumn','Siberian Elm Tree Lrg Aut':'SiberianElmLarge_Autumn'}
for source in sources:
 for level in range(3):
  pieces=[]
  for slot in range(2):
   obj=source.copy();obj.data=source.data.copy();scene.collection.objects.link(obj)
   obj.parent=None;obj.matrix_world=source.matrix_world.copy()
   bm=bmesh.new();bm.from_mesh(obj.data)
   bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=slot],context='FACES')
   bm.to_mesh(obj.data);bm.free()
   bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
   if level and slot==0:
    mod=obj.modifiers.new('Reduce bark geometry','DECIMATE');mod.ratio=.45 if level==1 else .18
    bpy.ops.object.modifier_apply(modifier=mod.name)
   if level and slot==1:
    mod=obj.modifiers.new('Dissolve planar foliage interiors','DECIMATE');mod.decimate_type='DISSOLVE';mod.angle_limit=math.radians(3 if level==1 else 8);mod.delimit={'UV','MATERIAL'}
    bpy.ops.object.modifier_apply(modifier=mod.name)
    # Thin whole connected leaf cards, never collapse them into broken UV fragments.
    # Use the same random ranks so LOD2 cards are a subset of LOD1.
    bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();seen=set();rng=random.Random(730421)
    remove=[]
    for vertex in bm.verts:
     if vertex in seen:continue
     stack=[vertex];island=[];seen.add(vertex)
     while stack:
      v=stack.pop();island.append(v)
      for e in v.link_edges:
       other=e.other_vert(v)
       if other not in seen:seen.add(other);stack.append(other)
     if rng.random()>(.92 if level==1 else .62):remove.extend(island)
    bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(obj.data);bm.free()
   pieces.append(obj)
  bpy.ops.object.select_all(action='DESELECT')
  for obj in pieces:obj.select_set(True)
  bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();tree=bpy.context.object
  bounds=[tree.matrix_world@Vector(c) for c in tree.bound_box]
  origin=Vector(((min(p.x for p in bounds)+max(p.x for p in bounds))/2,(min(p.y for p in bounds)+max(p.y for p in bounds))/2,min(p.z for p in bounds)))
  for v in tree.data.vertices:v.co=tree.matrix_world@v.co-origin
  tree.matrix_world=Matrix.Identity(4);tree.name=names[source.name]+f'_LOD{level}';tree.data.calc_loop_triangles()
  row={'name':tree.name,'triangles':len(tree.data.loop_triangles),'vertices':len(tree.data.vertices),'materials':[m.name for m in tree.data.materials],'dimensions':list(tree.dimensions)};report.append(row);print('ELM_EXPORT',json.dumps(row),flush=True)
  bpy.ops.export_scene.fbx(filepath=str(out/(tree.name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
  if 'Autumn' in tree.name:tree.hide_render=True;tree.hide_set(True)
  else:
   tree.hide_render=False;tree.hide_set(False)
   variant=['SiberianElmLarge','SiberianElmA','SiberianElmYoung'].index(names[source.name])
   tree.location=Vector((level*24,variant*40,0))
# Neutral consistent presentation for a side-by-side review.
world=bpy.data.worlds.new('Elm Review Sky');scene.world=world;world.use_nodes=True
background=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');background.inputs['Color'].default_value=(.55,.65,.8,1);background.inputs['Strength'].default_value=.5
light_data=bpy.data.lights.new('Elm Review Sun','SUN');light_data.energy=2;light_data.angle=math.radians(6)
light=bpy.data.objects.new('Elm Review Sun',light_data);scene.collection.objects.link(light);light.rotation_euler=(math.radians(30),math.radians(-20),math.radians(-25))
cam_data=bpy.data.cameras.new('Elm Review Camera');cam=bpy.data.objects.new('Elm Review Camera',cam_data);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(24,-100,140);target=Vector((24,40,7));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam_data.type='ORTHO';cam_data.ortho_scale=112
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1800;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(out/'Elm_LOD_Comparison.png');scene.render.film_transparent=False
# Pack imported image data so review does not depend on the vendor's Windows paths.
bpy.ops.file.pack_all()
(out/'triangle_counts.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'SiberianElm_LOD_Comparison.blend'))
bpy.ops.render.render(write_still=True)
