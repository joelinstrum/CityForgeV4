"""Blender background preparation of the purchased eight-palm FBX; private derivatives only."""
import bpy,json,os
from pathlib import Path
from mathutils import Vector
root=Path(os.environ['CITYFORGE_PROJECT']);source=root/'SourceAssets/TropicalPalm/CoconutPack/Source';output=source.parent/'Runtime'
output.mkdir(parents=True,exist_ok=True)
if (output/'Palm0_LOD0.fbx').exists():raise RuntimeError('Coconut output exists; preserve before rebuilding')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source/'Coconut Tree.fbx'))
material=bpy.data.materials.new('Coconut atlas');report=[]
for variant,tree in enumerate(sorted([o for o in bpy.context.scene.objects if o.type=='MESH'],key=lambda o:o.name)):
 matrix=tree.matrix_world.copy()
 for v in tree.data.vertices:v.co=matrix@v.co
 minimum=min(v.co.z for v in tree.data.vertices);base=[v.co for v in tree.data.vertices if v.co.z<minimum+.15];origin=sum(base,Vector())/len(base);origin.z=minimum
 for v in tree.data.vertices:v.co-=origin
 tree.matrix_world.identity()
 # Atlas columns repeat vertically, retaining the source's trunk tiling (V up to 3).
 for face in tree.data.polygons:
  slot=face.material_index
  for loop in face.loop_indices:
   uv=tree.data.uv_layers.active.data[loop].uv;uv.x=(slot*256+4+uv.x*248)/2048
  face.material_index=0
 tree.data.materials.clear();tree.data.materials.append(material)
 for lod,target in enumerate([30000,12000,4000,1200]):
  obj=tree.copy();obj.data=tree.data.copy();bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj
  group=obj.vertex_groups.new(name='Crown reduction');group.add([v.index for v in obj.data.vertices if v.co.z>2],1,'REPLACE')
  count=sum(len(p.vertices)-2 for p in obj.data.polygons)
  if count>target:
   d=obj.modifiers.new('Runtime LOD','DECIMATE');d.ratio=target/count;d.vertex_group=group.name;d.vertex_group_factor=1;d.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=d.name)
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
  bpy.ops.export_scene.fbx(filepath=str(output/f'Palm{variant}_LOD{lod}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
  item={'variant':variant,'source':tree.name,'lod':lod,'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'height':tree.dimensions.z}
  report.append(item);print(item,flush=True);bpy.data.objects.remove(obj,do_unlink=True)
(output/'Report.json').write_text(json.dumps(report,indent=2))
