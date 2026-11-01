"""Bake supplied sculpt vertex colors to a private URP-ready texture after model preparation."""
import bpy,os
from pathlib import Path
root=Path(os.environ['CITYFORGE_PROJECT'])/'Assets/CityForge/Resources/NativeCamps'
output=root/'Totem-Color.png'
if output.exists():raise RuntimeError('Refusing to overwrite '+str(output))
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(root/'Totem.fbx'))
mesh=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=mesh;mesh.select_set(True)
attribute=mesh.data.color_attributes[0]
print('Supplied colors:',attribute.name,'samples:',[list(attribute.data[i].color) for i in range(0,len(attribute.data),max(1,len(attribute.data)//10))])
image=bpy.data.images.new('Totem supplied color',width=2048,height=2048,alpha=False)
for material in mesh.data.materials:
 material.use_nodes=True;nodes=material.node_tree.nodes;nodes.clear()
 color=nodes.new('ShaderNodeVertexColor');color.layer_name=attribute.name
 emission=nodes.new('ShaderNodeEmission');material.node_tree.links.new(color.outputs['Color'],emission.inputs['Color'])
 surface=nodes.new('ShaderNodeOutputMaterial');material.node_tree.links.new(emission.outputs[0],surface.inputs['Surface'])
 target=nodes.new('ShaderNodeTexImage');target.image=image;nodes.active=target
bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=1;bpy.context.scene.render.bake.margin=8
bpy.ops.object.bake(type='EMIT')
image.filepath_raw=str(output);image.file_format='PNG';image.save();print('Baked',output)
