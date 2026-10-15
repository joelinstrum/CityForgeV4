"""Run Blender -b SOURCE.blend --python Tools/export_tree_library.py -- PACK.
PACK is birch, plane, green, or fir. Keep the original leaf/material assignments.
"""
import bpy, os, sys, json, shutil
import numpy as np
from mathutils import Vector, Matrix
pack=sys.argv[sys.argv.index('--')+1]
out=os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Assets/CityForge/Art/Trees')
original=list(bpy.data.objects)
if pack=='birch': groups=[('HimalayanBirch'+v,[o for o in original if o.type=='MESH' and len(o.data.vertices)>0 and o.name.endswith('.001')==bool(s)]) for v,s in [('A',''),('B','.001')]]
elif pack=='plane':groups=[('LondonPlane'+v,[bpy.data.objects['London_Plane_Summer '+n]]) for v,n in [('A','01'),('B','02')]]
elif pack=='green':groups=[('LondonPlane'+v,[bpy.data.objects['Green_Plane_'+part+s] for part in ['Branches','Caps','Leaves']]) for v,s in [('C','001'),('D','002')]]
else:groups=[('FraserFirA',[bpy.data.objects['FraserFir_Small']])]
textures={}
def material_info(mat):
 name=pack+'_'+mat.name.replace('.','_')
 if name in textures:return textures[name]
 nodes=[n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
 base=next(n.image for n in nodes if any(l.to_socket.name=='Base Color' for o in n.outputs for l in o.links))
 normal=next((n.image for n in nodes if 'normal' in n.image.filepath.lower()),None)
 opacity=next((n.image for n in nodes if 'opacity' in n.image.filepath.lower()),None)
 cutout=any(n.type=='BSDF_TRANSPARENT' for n in mat.node_tree.nodes)
 def resolve(img):
  basename=os.path.basename(img.filepath)
  return next((os.path.join(root,basename) for root,ds,fs in os.walk(os.path.dirname(bpy.data.filepath)) if basename in fs),None)
 if opacity:
  base=bpy.data.images.load(resolve(base),check_existing=False)
  mask=bpy.data.images.load(resolve(opacity),check_existing=False)
  rgba=np.array(base.pixels[:],dtype=np.float32).reshape(-1,4)
  rgba[:,3]=np.array(mask.pixels[:],dtype=np.float32).reshape(-1,4)[:,0]
  image=bpy.data.images.new(name,width=base.size[0],height=base.size[1],alpha=True)
  image.pixels.foreach_set(rgba.ravel());image.filepath_raw=out+'/'+name+'.png';image.file_format='PNG';image.save()
 else:
  source=resolve(base)
  if source:shutil.copyfile(source,out+'/'+name+'.png')
  else:
   base.filepath_raw=out+'/'+name+'.png';base.file_format='PNG';base.save()
 if normal:
  source=resolve(normal)
  if source:shutil.copyfile(source,out+'/'+name+'_Normal.png')
  else:
   normal.filepath_raw=out+'/'+name+'_Normal.png';normal.file_format='PNG';normal.save()
 info={'name':name,'normal':bool(normal),'cutout':cutout};textures[name]=info;return info
for name,objects in groups:
 infos=[]
 for lod in [0,1]:
  copies=[]
  for ob in objects:
   cp=ob.copy();cp.data=ob.data.copy();bpy.context.collection.objects.link(cp);cp.parent=None;cp.matrix_world=ob.matrix_world.copy()
   mats=list(cp.data.materials)
   for i,mat in enumerate(mats):
    info=material_info(mat)
    replacement=bpy.data.materials.get(info['name']) or bpy.data.materials.new(info['name'])
    cp.data.materials[i]=replacement
   bpy.context.view_layer.objects.active=cp
   ratio=1
   if pack=='birch':ratio=(1.0 if lod==0 else .7) if not ob.name.startswith('Branch') else (.10 if lod==0 else .03)
   if pack=='fir':ratio=.10 if lod==0 else .025
   if pack in ['plane','green'] and lod==1:ratio=.35
   if ratio<1:
    mod=cp.modifiers.new('Preserve canopy, simplify surfaces','DECIMATE');mod.ratio=ratio
    bpy.ops.object.modifier_apply(modifier=mod.name)
   copies.append(cp)
  bpy.ops.object.select_all(action='DESELECT')
  for cp in copies:cp.select_set(True)
  bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();tree=bpy.context.object
  bounds=[tree.matrix_world@Vector(c) for c in tree.bound_box];origin=Vector(((min(p.x for p in bounds)+max(p.x for p in bounds))/2,(min(p.y for p in bounds)+max(p.y for p in bounds))/2,min(p.z for p in bounds)))
  for vertex in tree.data.vertices:vertex.co=tree.matrix_world@vertex.co-origin
  tree.matrix_world=Matrix.Identity(4);tree.name=name+('_LOD1' if lod else '')
  info=[textures[m.name] for m in tree.data.materials]
  bpy.ops.export_scene.fbx(filepath=out+'/'+tree.name+'.fbx',use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
  with open(out+'/'+tree.name+'.json','w') as f:json.dump({'name':name,'height':18 if pack in ['plane','green'] else 15,'materials':info},f)
  print('TREE_EXPORT',tree.name,len(tree.data.vertices),len(tree.data.polygons))
  bpy.data.objects.remove(tree,do_unlink=True)
