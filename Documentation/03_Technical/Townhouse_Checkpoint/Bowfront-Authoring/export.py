import bpy,json,re,hashlib,shutil
from pathlib import Path
from mathutils import Vector
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-victorian-bowfront-brownstone-001/b72038fc22c944eeaa22f8c44e0b94d6')/'Native-Unity-Review';P.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath='/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-victorian-bowfront-brownstone-001/b72038fc22c944eeaa22f8c44e0b94d6/Revision/Bowfront-Composition.blend')
sc=bpy.context.scene;geo=bpy.data.collections['01 — Townhouse composition'];obs=[o for o in geo.objects if o.type=='MESH'];used=sorted(set(m for o in obs for m in o.data.materials if m),key=lambda m:m.name);records=[]
def image(socket):
 if not socket.is_linked:return None
 node=socket.links[0].from_node
 if node.type=='TEX_IMAGE':return node.image
 if node.type in ['SEPARATE_COLOR','NORMAL_MAP']:
  return image(node.inputs['Color'])
 return None
images={}
def extract(im,role):
 if not im:return ''
 key=(im.name,role)
 if key in images:return images[key]
 if im.packed_file:
  data=im.packed_file.data;ext='jpg'if data[:2]==b'\xff\xd8'else'png';name='Tex-'+hashlib.sha256(data).hexdigest()[:12]+'-'+role+'.'+ext;(P/name).write_bytes(data)
 else:
  src=Path(bpy.path.abspath(im.filepath));assert src.exists(),str(src);name='Tex-'+hashlib.sha256(src.read_bytes()).hexdigest()[:12]+'-'+role+src.suffix;shutil.copy2(src,P/name)
 images[key]=name;return name
for idx,m in enumerate(used):
 original=m.name;bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');a=image(bs.inputs['Base Color']);normal=image(bs.inputs['Normal']);rough=image(bs.inputs['Roughness']);met=image(bs.inputs['Metallic']);em=image(bs.inputs['Emission Color']);normalstrength=1
 if bs.inputs['Normal'].is_linked:
  node=bs.inputs['Normal'].links[0].from_node
  if node.type=='NORMAL_MAP':normalstrength=node.inputs['Strength'].default_value
 def value(key):
  x=bs.inputs[key].default_value;return list(x)if hasattr(x,'__len__')else float(x)
 gain=float(m.get('nightEmissionStrength',0))
 if 'LampGlow' in original:gain=1.5
 if 'Window_Interior' in original:gain=.12
 if 'Window_Curtain' in original:gain=.08
 if 'unlit room' in original:gain=0
 name='CF_Detail_Bowfront_'+str(idx).zfill(2);m.name=name
 row=dict(name=name,sourceName=original,albedo=extract(a,'Albedo'),normal=extract(normal,'Normal'),roughness=extract(rough,'Roughness'),metallicMap=extract(met,'MetallicSource'),emission=extract(em,'Emission'),color=[1,1,1,1]if a else value('Base Color'),roughnessValue=value('Roughness'),metallicValue=value('Metallic'),normalStrength=normalstrength,emissionValue=value('Emission Color'),emissionGain=gain,transparent=value('Transmission Weight')>.7,alpha=.04 if value('Transmission Weight')>.7 else 1,repeat=True,packedMetalRough=bool(rough and met and rough==met))
 if 'tripo_mat_278b' in original:row['bowPaneMask']=True;row['emissionGain']=.75;row['emissionValue']=[1,.58,.25,1]
 records.append(row)
# Combine static objects while preserving material slots, UVs and the door as a separate object.
parts={}
for o in obs:
 label='Door'if 'Entrance / Door-Leaf'in o.name else 'Glass'if all(m and ('ClearGlass'in str(next((r['sourceName']for r in records if r['name']==m.name),'')))for m in o.data.materials)else 'Body'
 parts.setdefault(label,[]).append(o)
for key,objects in parts.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name='Bowfront-'+key
near=[o for o in geo.objects if o.type=='MESH']
def export(path,objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=True,mesh_smooth_type='FACE',use_custom_props=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
export(P/'Model.fbx',near);neartri=0
for o in near:o.data.calc_loop_triangles();neartri+=len(o.data.loop_triangles)
medium=[]
for src in near:
 o=src.copy();o.data=src.data.copy();geo.objects.link(o);o.name='Reduced-'+src.name
 if len(o.data.polygons)>100 and 'Glass'not in o.name:
  mod=o.modifiers.new('Review reduced mesh','DECIMATE');mod.ratio=.30;mod.use_collapse_triangulate=True;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 medium.append(o)
export(P/'Distant.fbx',medium);mt=0
for o in medium:o.data.calc_loop_triangles();mt+=len(o.data.loop_triangles)
(P/'Materials-Source.json').write_text(json.dumps(dict(materials=records,nearTriangles=neartri,mediumTriangles=mt),indent=2))
print('EXPORTED',neartri,mt)
