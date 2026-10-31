"""Generate cheaper distant LODs by welding subpixel geometry on private near mesh."""
import bpy,bmesh,json,os
from pathlib import Path
output=Path(os.environ['CITYFORGE_PROJECT'])/'SourceAssets/TropicalPalm/Runtime'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(output/'Palm_LOD0.fbx'))
source=next(o for o in bpy.context.scene.objects if o.type=='MESH')
report=[{'lod':0,'triangles':len(source.data.polygons),'vertices':len(source.data.vertices)}]
for lod,distance in [(1,.07),(2,.16),(3,.32)]:
 o=source.copy();o.data=source.data.copy();bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=[v for v in bm.verts if v.co.z>2],dist=distance)
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_area()<.00001],context='FACES');bm.to_mesh(o.data);bm.free()
 group=o.vertex_groups.new(name='Crown reduction');group.add([v.index for v in o.data.vertices if v.co.z>2],1,'REPLACE')
 d=o.modifiers.new('Distant reduction','DECIMATE');d.vertex_group=group.name;d.vertex_group_factor=1;d.ratio=.2;d.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=d.name)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(output/('Palm_LOD'+str(lod)+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
 report.append({'lod':lod,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'vertices':len(o.data.vertices)})
 bpy.data.objects.remove(o,do_unlink=True)
(output/'Report.json').write_text(json.dumps(report,indent=2));print(report,flush=True)
