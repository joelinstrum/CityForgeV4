"""LOD0-only wood treatment. Foliage geometry is copied exactly from saved family sources."""
import bpy, numpy as np, math, json, hashlib, struct
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1]; A=R/'Assets/CityForge/Art/Trees/DenseForestTrees'; D=R/'SourceAssets/Trees/DenseForestTrees'
recipe=json.loads((A/'family_recipe.json').read_text());report=[]
# Seamless mature hardwood fissures: layered wandering longitudinal ridges and broken plates.
n=1024;y,x=np.mgrid[0:n,0:n]/n
phase=x*math.tau*38+0.8*np.sin(y*math.tau*3+x*math.tau*2)+.3*np.sin(y*math.tau*11)
ridge=(.5+.5*np.cos(phase))**.4
cracks=(.5+.5*np.cos(y*math.tau*29+np.sin(x*math.tau*7)*2))**18
height=.65*ridge+.18*np.sin(x*math.tau*87+y*math.tau*5)+.07*np.sin(x*math.tau*211+y*math.tau*41)-.16*cracks
shade=np.clip(.48+.36*height,.24,.87)
def save(name,rgb,alpha=None):
 im=bpy.data.images.new(name,width=n,height=n,alpha=True);arr=np.ones((n,n,4),np.float32);arr[:,:,:3]=rgb;arr[:,:,3]=1 if alpha is None else alpha;im.pixels.foreach_set(arr.ravel());im.filepath_raw=str(A/'CloseLOD0'/name);im.file_format='PNG';im.save();bpy.data.images.remove(im)
save('Hardwood_Albedo.png',np.stack([shade*.72,shade*.66,shade*.56],axis=-1))
dx=(np.roll(height,-1,1)-np.roll(height,1,1))*7;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*7
normal=np.stack([-dx,-dy,np.ones_like(dx)],axis=-1);normal/=np.linalg.norm(normal,axis=-1)[:,:,None];save('Hardwood_Normal.png',normal*.5+.5)
save('Hardwood_Roughness.png',np.repeat(np.clip(.82+.12*(1-ridge)+.04*cracks,0,1)[:,:,None],3,axis=2))
rough=np.clip(.82+.12*(1-ridge)+.04*cracks,0,1)
save('Hardwood_MetalSmooth.png',np.zeros((n,n,3)),1-rough)
for entry in recipe['variants']:
 name=entry['name'];bpy.ops.wm.open_mainfile(filepath=str(D/(name+'.blend')))
 wood=bpy.data.objects['Tapered trunk root flare and scaffold branches']; leaf=bpy.data.objects.get('Irregular branch organized foliage masses')
 leafhash=hashlib.sha256(b''.join(struct.pack('<3f',*v.co) for v in leaf.data.vertices)).hexdigest() if leaf else None
 # Full authored wood retains junctions and secondary branches removed in the original .22 decimation.
 for v in wood.data.vertices:
  p=v.co;theta=math.atan2(p.y,p.x);r=math.hypot(p.x,p.y)
  if r<1.15 and p.z<6:
   fac=1+.065*math.sin(theta*5+p.z*.65)+.035*math.sin(theta*9-p.z*1.2)
   if p.z<.75:fac+=.36*(1-p.z/.75)**2*(.5+.5*math.cos(theta*5+.6))
   p.x*=fac;p.y*=fac
  p+=v.normal*(.009*math.sin(p.z*13+theta*11)+.005*math.sin(p.z*29+theta*23))
 wood.data.update()
 # Cylindrical bark mapping on trunk; dominant-axis mapping on woody limbs.
 uv=wood.data.uv_layers.active or wood.data.uv_layers.new()
 for poly in wood.data.polygons:
  for li in poly.loop_indices:
   p=wood.data.vertices[wood.data.loops[li].vertex_index].co
   uv.data[li].uv=(math.atan2(p.y,p.x)/math.tau*3,p.z/3) if math.hypot(p.x,p.y)<1.2 else ((p.x+p.y)*.65,p.z/3)
 bpy.ops.object.select_all(action='DESELECT');wood.select_set(True);bpy.context.view_layer.objects.active=wood
 mod=wood.modifiers.new('Close wood junction preserving reduction','DECIMATE');mod.ratio=.65;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 for p in wood.data.polygons:p.use_smooth=True
 tree=[wood]+([leaf] if leaf else [])+[o for o in bpy.data.objects if o.type=='MESH' and 'snow' in o.name.lower()]
 folder=A/'CloseLOD0'/name;folder.mkdir(parents=True,exist_ok=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(D/'CloseLOD0'/(name+'.blend')))
 bpy.ops.object.select_all(action='DESELECT')
 for ob in tree:ob.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(folder/'LOD0.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False)
 actual=hashlib.sha256(b''.join(struct.pack('<3f',*v.co) for v in leaf.data.vertices)).hexdigest() if leaf else None
 assert actual==leafhash
 wood.data.calc_loop_triangles();report.append(dict(name=name,foliageVertexHash=leafhash,foliageUnchanged=True,woodTriangles=len(wood.data.loop_triangles),woodRatio=.65))
 print('CLOSE LOD0 '+name,flush=True)
(A/'CloseLOD0'/'recipe.json').write_text(json.dumps(report,indent=2))
