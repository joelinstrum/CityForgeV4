import bpy,math
from mathutils import Vector
from pathlib import Path
r=Path(__file__).resolve().parents[1];p=r/'SourceAssets/Trees/CityForgeBroadleaf/CityForge_Broadleaf.blend';bpy.ops.wm.open_mainfile(filepath=str(p))
for o in bpy.data.objects:
 o.hide_render=True;o.hide_set(True)
o=bpy.data.objects['ForgeBroadleafA_LOD0'];o.hide_render=False;o.hide_set(False)
for name,file in [('CityForgeLeaf','Forge_Leaf.png'),('CityForgeBark','Forge_Bark.png')]:
 m=bpy.data.materials[name];m.use_nodes=True;n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(r/'Assets/CityForge/Art/Trees/CityForgeBroadleaf'/file));m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha']);m.node_tree.links.new(bs.outputs[0],out.inputs[0]);bs.inputs['Roughness'].default_value=.9
 m.surface_render_method='DITHERED'
bpy.ops.object.camera_add(location=(25,-35,24));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=22;bpy.context.scene.camera=cam
bpy.ops.object.light_add(type='SUN');bpy.context.object.rotation_euler=(math.radians(32),0,math.radians(-35));bpy.context.object.data.energy=2
world=bpy.context.scene.world or bpy.data.worlds.new('World');bpy.context.scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.32,.40,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=24;s.render.resolution_x=1000;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.filepath='/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/Forge_Broadleaf_Blender.png';s.view_settings.view_transform='AgX'

for image in bpy.data.images:
 if Path(bpy.path.abspath(image.filepath)).exists():image.pack()
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_location=Vector((0,0,8));area.spaces.active.region_3d.view_distance=25;area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
bpy.ops.wm.save_as_mainfile(filepath=str(p));bpy.ops.render.render(write_still=True)
