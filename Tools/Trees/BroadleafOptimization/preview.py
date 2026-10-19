import bpy,json,sys,math
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-intake').mkdir(parents=True,exist_ok=True)
(_WORK/'outputs').mkdir(parents=True,exist_ok=True)
from mathutils import Vector,Matrix
ws=_WORK;root=(_PROJECT/'SourceAssets/OptimizedBroadleaf')
report=json.loads((ws/'work/broadleaf-intake/optimized.json').read_text());raw=ws/'work/broadleaf-intake/renders';raw.mkdir(exist_ok=True)
for name,d in report.items():
 for level in ['Source','LOD0','LOD1','LOD2']:
  bpy.ops.wm.read_factory_settings(use_empty=True)
  if level=='Source':
   path=d['source']
   if path.endswith('.blend'):bpy.ops.wm.open_mainfile(filepath=path,load_ui=False)
   else:bpy.ops.import_scene.fbx(filepath=path)
   objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
   if name in ['SilverMapleA','CanyonLiveOakA']:objects=[o for o in objects if not (o.name.endswith('.001') or o.name.startswith('Bark2'))]
   if name in ['SilverMapleB','CanyonLiveOakB']:objects=[o for o in objects if o.name.endswith('.001') or o.name.startswith('Bark2')]
  else:
   bpy.ops.import_scene.fbx(filepath=str(root/name/(name+'_'+level+'.fbx')))
   objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)]
  for o in objects:
   matrix=o.matrix_world.copy();o.parent=None;o.matrix_world=matrix
  keep=set(objects)
  for o in list(bpy.context.scene.objects):
   if o not in keep:bpy.data.objects.remove(o,do_unlink=True)
  # Normalize each exported coordinate convention to vertical Z (FBX importer does this).
  corners=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
  lo=Vector([min(v[i] for v in corners) for i in range(3)]);hi=Vector([max(v[i] for v in corners) for i in range(3)])
  center=(lo+hi)*.5
  if level=='Source':framing=max(d['height_m'],(hi-lo).x)*1.25
  mat=bpy.data.materials.new('Geometry QA - opaque, no texture');mat.diffuse_color=(.12,.3,.12,1)
  for o in objects:
   o.hide_render=False;o.hide_set(False);o.data.materials.clear();o.data.materials.append(mat)
   for p in o.data.polygons:p.material_index=0
  scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
  scene.render.resolution_x=320;scene.render.resolution_y=360;scene.render.resolution_percentage=100;scene.render.film_transparent=True
  camdata=bpy.data.cameras.new('QA camera');cam=bpy.data.objects.new('QA camera',camdata);scene.collection.objects.link(cam)
  cam.location=center+Vector((0,-d['height_m']*2,d['height_m']*.35));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=framing;scene.camera=cam
  scene.render.filepath=str(raw/(name+'_'+level+'.png'));bpy.ops.render.render(write_still=True)
  print('RENDERED',name,level,flush=True)
