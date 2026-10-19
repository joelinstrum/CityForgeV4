from pathlib import Path
exec(open(str(Path(__file__).with_name('optimize.py'))).read().split('for label,info in inventory.items():')[0])
report=json.loads((workspace/'work/broadleaf-intake/optimized.json').read_text())
for name,d in report.items():
 if name=='AngelOak':continue
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if d['source'].endswith('.blend'):bpy.ops.wm.open_mainfile(filepath=d['source'],load_ui=False)
 else:bpy.ops.import_scene.fbx(filepath=d['source'])
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
 if name in ['SilverMapleA','CanyonLiveOakA']:objects=[o for o in objects if not o.name.endswith('.001')]
 if name in ['SilverMapleB','CanyonLiveOakB']:objects=[o for o in objects if o.name.endswith('.001')]
 lo,hi=bounds(objects);center=Vector(((lo[0]+hi[0])/2,(lo[1]+hi[1])/2,lo[2]))
 trunks={}
 for o in objects:
  if o.name.lower().startswith('bark') and tris(o)>1000:
   mesh=o.data.copy();mesh.transform(Matrix.Translation(-center)@o.matrix_world)
   trunks[o.name]=mesh
 # Mesh datablocks need to survive model cleanup between imports.
 for lod,budget in [(1,700),(2,450)]:
  path=root/name/(name+'_LOD'+str(lod)+'.fbx')
  for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.ops.import_scene.fbx(filepath=str(path))
  objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
  for o in objects:
   if o.name in trunks and tris(o)<budget:
    o.data=trunks[o.name].copy();o.parent=None;o.matrix_world=Matrix.Identity(4);simplify(o,budget)
  bpy.ops.object.select_all(action='DESELECT')
  for o in objects:o.select_set(True)
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
  print('TRUNK',name,lod,sum(tris(o) for o in objects),flush=True)
