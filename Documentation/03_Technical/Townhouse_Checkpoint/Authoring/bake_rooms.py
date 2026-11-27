import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-art-deco-brick-townhouse-2-001/92dabbefe84443e2b59fb84934e9c45d/Recessed-Candidate');bpy.ops.wm.open_mainfile(filepath=str(P/'Near.blend'));s=bpy.context.scene;col=bpy.data.collections['Townhouse-Near'];styles=['OneThird','ThreeQuarter','Down'];s.render.engine='CYCLES';s.cycles.samples=8;s.render.resolution_x=256;s.render.resolution_y=632;s.render.resolution_percentage=100;s.render.film_transparent=True;s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0;s.view_settings.gamma=1
for o in s.objects:o.hide_render=True
cam=bpy.data.objects.new('Interior Card Bake',bpy.data.cameras.new('Interior Card Bake'));s.collection.objects.link(cam);s.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.2;cam.location=(0,-5,1.32);cam.rotation_euler=(Vector((0,0,1.32))-cam.location).to_track_quat('-Z','Y').to_euler()
# Unlit base-color transfer, not a lit beauty render: lighting remains runtime-controlled.
for m in bpy.data.materials:
 if not m.use_nodes:continue
 bs=m.node_tree.nodes.get('Principled BSDF');out=next((n for n in m.node_tree.nodes if n.type=='OUTPUT_MATERIAL'),None)
 if not bs or not out:continue
 if 'ClearGlass' in m.name:n=m.node_tree.nodes.new('ShaderNodeBsdfTransparent');m.node_tree.links.new(n.outputs[0],out.inputs['Surface']);continue
 n=m.node_tree.nodes.new('ShaderNodeEmission');n.name='CARD';n.inputs['Strength'].default_value=1
 if bs.inputs['Base Color'].is_linked:m.node_tree.links.new(bs.inputs['Base Color'].links[0].from_socket,n.inputs['Color'])
 else:n.inputs['Color'].default_value=bs.inputs['Base Color'].default_value
 m.node_tree.links.new(n.outputs[0],out.inputs['Surface'])
for style in styles:
 root=next(o for o in col.objects if o.type=='EMPTY' and any(c.name.endswith('.Interior') and '/Single.Near.'+style in c.name for c in o.children_recursive));matrix=root.matrix_world.copy();root.matrix_world=Matrix.Identity(4)
 objs=list(root.children_recursive)
 for o in objs:
  role=o.get('partRole','');o.hide_render=o.type!='MESH' or any(k in o.name for k in ['Stone','Brick','Keystone','ShadePleats'])
 for phase in ['Albedo','Emission']:
  for m in bpy.data.materials:
   if not m.use_nodes:continue
   n=m.node_tree.nodes.get('CARD');bs=m.node_tree.nodes.get('Principled BSDF')
   if not n or not bs:continue
   while n.inputs['Color'].links:m.node_tree.links.remove(n.inputs['Color'].links[0])
   if phase=='Emission':
    # Linen/room materials retain the existing selective night-light pattern in the native prefab.
    emit=('Window-Room' in m.name or 'Window-Linen' in m.name or 'LampLinen' in m.name)
    n.inputs['Color'].default_value=(.42,.23,.10,1) if emit else (0,0,0,1)
   elif bs.inputs['Base Color'].is_linked:m.node_tree.links.new(bs.inputs['Base Color'].links[0].from_socket,n.inputs['Color'])
   else:n.inputs['Color'].default_value=bs.inputs['Base Color'].default_value
  s.render.filepath=str(P/'Textures'/f'Recessed-Window-{style}-{phase}.png');bpy.ops.render.render(write_still=True)
 for o in objs:o.hide_render=True
 root.matrix_world=matrix
# Install revised card maps only in private Far master; preserve per-window lit/unlit materials.
bpy.ops.wm.open_mainfile(filepath=str(P/'Far.blend'))
for m in bpy.data.materials:
 if not m.use_nodes or 'FarWindow' not in m.name:continue
 bs=m.node_tree.nodes.get('Principled BSDF');style=next(v for v in styles if v in m.name)
 for role,socket in [('Albedo','Base Color'),('Emission','Emission Color')]:
  path=P/'Textures'/f'Recessed-Window-{style}-{role}.png';im=bpy.data.images.load(str(path),check_existing=True);im.pack()
  if bs.inputs[socket].is_linked:bs.inputs[socket].links[0].from_node.image=im
  else:nd=m.node_tree.nodes.new('ShaderNodeTexImage');nd.image=im;m.node_tree.links.new(nd.outputs['Color'],bs.inputs[socket])
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Far.blend'));print('FAR ROOM CARDS UPDATED',flush=True)
