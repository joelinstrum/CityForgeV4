"""Prepare private supplied bluegill: metre-long, +Z facing, Y up, with a derived looping rig swim."""
import bpy,math,os
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
root=Path(os.environ['CITYFORGE_PROJECT'])
out=Path(os.environ.get('CITYFORGE_FISH_OUTPUT',str(root/'Assets/CityForge/Resources/StreamFish/Bluegill.fbx')))
if out.exists(): raise RuntimeError('Preserve existing prepared bluegill')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'SourceAssets/Bluegill/BLUEGILL.fbx'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.data.objects if o.type=='MESH')
old={o:o.matrix_world.copy() for o in [rig,mesh]}
up=Vector((0,0,1));forward=Vector((-1,.19,0)).normalized();right=up.cross(forward).normalized()
rot=Matrix((right,up,forward));points=[old[mesh]@Vector(c) for c in mesh.bound_box]
center=sum(points,Vector())/8;length=max(forward.dot(p) for p in points)-min(forward.dot(p) for p in points)
m=rot.to_4x4()@Matrix.Translation(-center);m=Matrix.Scale(1/length,4)@m
axis=(old[rig].inverted().to_3x3()@up).normalized()
for o in [rig,mesh]:o.matrix_world=m@old[o]
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
axis=Vector((0,1,0))
scene=bpy.context.scene;scene.render.fps=30;scene.frame_start=1;scene.frame_end=41
for pb in rig.pose.bones:
 if pb.name.startswith('SPINE_') or pb.name.startswith('TAIL_'):
  index=int(pb.name.split('_')[1]);amp=(max(0,index-1)*2.5 if pb.name.startswith('SPINE') else 7)
  local_axis=(pb.bone.matrix_local.inverted().to_3x3()@axis).normalized();pb.rotation_mode='QUATERNION'
  for frame in range(1,42):
   phase=(frame-1)/40*2*math.pi-(index*.5 if pb.name.startswith('SPINE') else 2.5)
   pb.rotation_quaternion=Quaternion(local_axis,math.radians(amp)*math.sin(phase));pb.keyframe_insert('rotation_quaternion',frame=frame)
if rig.animation_data and rig.animation_data.action:rig.animation_data.action.name='Bluegill Swim'
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
print('PREPARED_BLUEGILL',len(mesh.data.polygons),'polygons',length,'source length; output1m; swim40frames')
