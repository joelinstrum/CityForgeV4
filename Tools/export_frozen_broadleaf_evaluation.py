"""Export frozen Refinement01 geometry; only add export UVs and bake existing bark normals."""
import bpy,json,hashlib,shutil
from pathlib import Path
r=Path(__file__).resolve().parents[1];a=r/'Assets/CityForge/Art/Trees/FrozenBroadleafEvaluation';a.mkdir(exist_ok=True,parents=True)
s=r/'SourceAssets/Trees/CityForgeBroadleaf/Refinement01/Broadleaf_Refinement01.blend'
bpy.ops.wm.open_mainfile(filepath=str(s));wood=bpy.data.objects['Tapered trunk root flare and scaffold branches'];leaf=bpy.data.objects['Irregular branch organized foliage masses']
bpy.ops.object.select_all(action='DESELECT');wood.select_set(True);bpy.context.view_layer.objects.active=wood
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.151917,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
img=bpy.data.images.new('Frozen bark normal',1024,1024,alpha=False);img.colorspace_settings.name='Non-Color';mat=wood.data.materials[0];node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=img;mat.node_tree.nodes.active=node
scene=bpy.context.scene;scene.cycles.samples=1;scene.render.bake.margin=8;bpy.ops.object.bake(type='NORMAL');img.filepath_raw=str(a/'Frozen_Bark_Normal.png');img.file_format='PNG';img.save();mat.node_tree.nodes.remove(node)
shutil.copy2(r/'Assets/CityForge/Art/Trees/CityForgeBroadleaf/Forge_Leaf.png',a/'Frozen_Leaf.png')
leaf.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(a/'FrozenBroadleaf.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
report={'sourceSha256':hashlib.sha256(s.read_bytes()).hexdigest(),'source':str(s),'objects':[]}
for obj in [wood,leaf]:
 obj.data.calc_loop_triangles();report['objects'].append({'name':obj.name,'vertices':len(obj.data.vertices),'triangles':len(obj.data.loop_triangles),'materials':[m.name for m in obj.data.materials]})
(a/'export_manifest.json').write_text(json.dumps(report,indent=2));print(report)
