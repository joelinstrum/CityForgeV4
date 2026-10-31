"""Run with Blender --background --python; derives private palm LODs from local V3 source."""
import bpy, json, os
from pathlib import Path
from mathutils import Vector
root=Path(os.environ['CITYFORGE_PROJECT'])
source=Path(os.environ.get('CITYFORGE_V3',str(root.parent/'CityForge - V3')))/'Authoring/Flora/IncomingMeshy/date-palm/v01_source/raw'
output=root/'SourceAssets/TropicalPalm/Runtime'
output.mkdir(parents=True,exist_ok=True)
if (output/'Palm_LOD0.fbx').exists(): raise RuntimeError('Palm output exists; preserve it before rebuilding')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(next(source.rglob('*.fbx'))))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
print('INPUT',[(o.name,len(o.data.polygons),tuple(o.dimensions)) for o in meshes],flush=True)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.object.join()
tree=bpy.context.object
scale=11/tree.dimensions.z;tree.scale*=scale
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
corners=[tree.matrix_world@Vector(c) for c in tree.bound_box]
tree.location-=Vector(((min(c.x for c in corners)+max(c.x for c in corners))/2,(min(c.y for c in corners)+max(c.y for c in corners))/2,min(c.z for c in corners)))
bpy.ops.object.transform_apply(location=True,rotation=False,scale=False)
source_mesh=tree.data
report=[]
for index,target in enumerate([24000]):
 o=bpy.data.objects.new('Palm_LOD'+str(index),source_mesh.copy());bpy.context.collection.objects.link(o)
 bpy.context.view_layer.objects.active=o
 d=o.modifiers.new('Runtime reduction','DECIMATE');d.ratio=min(1,target/len(source_mesh.polygons));d.use_collapse_triangulate=True
 bpy.ops.object.modifier_apply(modifier=d.name)
 report.append({'lod':index,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'vertices':len(o.data.vertices)})
 print('LOD',report[-1],flush=True)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(output/('Palm_LOD'+str(index)+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
 bpy.data.objects.remove(o,do_unlink=True)
(output/'Report.json').write_text(json.dumps(report,indent=2))
print('DONE',flush=True)
