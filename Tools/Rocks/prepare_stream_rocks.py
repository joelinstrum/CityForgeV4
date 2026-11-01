"""Run with Blender --background --python; preserves purchased originals and UVs.
CITYFORGE_PROJECT points to the project. Outputs are licensed private derivatives.
The source is one connected open scan: do not pretend its stones are separate meshes.
"""
import bpy, os, json
from pathlib import Path
root=Path(os.environ['CITYFORGE_PROJECT'])
out=root/'Assets/CityForge/Resources/StreamRocks/RiverRockPatch.fbx'
if out.exists(): raise RuntimeError('Preserve existing rock preparation before rebuilding')
bpy.ops.wm.open_mainfile(filepath=str(root/'SourceAssets/RiverRocks/riverRocks.blend'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for obj in objects: obj.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
print(json.dumps([{'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'dimensions':list(o.dimensions)} for o in objects]))
