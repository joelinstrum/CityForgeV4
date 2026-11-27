import bpy,json,hashlib,math,re
from pathlib import Path
from mathutils import Vector
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-art-deco-brick-townhouse-2-001/92dabbefe84443e2b59fb84934e9c45d/Recessed-Candidate');O=P/'Native-Export';O.mkdir(exist_ok=True);(O/'Textures').mkdir(exist_ok=True)
sha=lambda d:hashlib.sha256(d).hexdigest();allspec={};tiers=[];imagecache={};materialcache={};priorImages={}
prior=Path('/Users/joelinstrum/Documents/Codex/2026-11-25/ar/work/brick-townhouse-2-front-shades-native/Export/Textures')
for f in prior.iterdir():
 if f.is_file():priorImages[(sha(f.read_bytes()),re.sub(r'^(?:[0-9a-f]{8,64}-)+','',f.name))]=f.name
def image(im):
 cachekey=bpy.path.abspath(im.filepath)
 if cachekey in imagecache:return imagecache[cachekey]
 if im.packed_file:data=bytes(im.packed_file.data)
 else:data=Path(bpy.path.abspath(im.filepath)).read_bytes()
 name=priorImages.get((sha(data),re.sub(r'^(?:[0-9a-f]{8,64}-)+','',Path(im.filepath).name)),sha(data)[:12]+'-'+Path(im.filepath).name)
 dst=O/'Textures'/name
 if not dst.exists():dst.write_bytes(data)
 imagecache[cachekey]=name
 return name

def linked(p,name):
 sock=p.inputs[name]
 if not sock.is_linked:return None
 n=sock.links[0].from_node
 if n.type=='TEX_IMAGE':return n.image
 if n.type=='NORMAL_MAP' and n.inputs['Color'].is_linked:
  n=n.inputs['Color'].links[0].from_node
  if n.type=='TEX_IMAGE':return n.image
 raise RuntimeError('Unsupported source material graph '+p.name+' '+name+' '+n.name)
def matrecipe(m,excludeRoof=False,shade=False):
 cachekey=(m.name,excludeRoof,shade)
 if cachekey in materialcache:return materialcache[cachekey]
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');r={'sourceName':m.name,'color':list(p.inputs['Base Color'].default_value),'metallic':float(p.inputs['Metallic'].default_value),'roughness':float(p.inputs['Roughness'].default_value),'alpha':float(p.inputs['Alpha'].default_value),'emissionColor':list(p.inputs['Emission Color'].default_value),'strength':float(m.get('nightStrength',m.get('nightEmission',0))),'normalStrength':1.,'twoSided':'glass' in m.name.lower(),'paintableRegion':str(m.get('paintableRegion',''))}
 if m.get('forceUnlit'):r['strength']=0
 if r['paintableRegion'].lower() in ['walls.brick','brick']:r['paintableRegion']='brick'
 if r['paintableRegion'].lower() in ['walls.stone','stone']:r['paintableRegion']='stone'
 if excludeRoof:r['paintableRegion']=''
 for inp,k in [('Base Color','albedo'),('Normal','normal'),('Roughness','roughnessMap'),('Metallic','metallicMap'),('Emission Color','emission')]:
  im=linked(p,inp)
  if im:r[k]=image(im)
 if p.inputs['Normal'].is_linked:
  n=p.inputs['Normal'].links[0].from_node
  if n.type=='NORMAL_MAP':r['normalStrength']=float(n.inputs['Strength'].default_value)
 if shade:r['sourceName']+='__BlindShade';r['normal']='Native-BlindPleats-Normal.png';r['normalStrength']=.35;r['nativeRole']='Blind shade; subpixel source pleats represented by source-scale portable normal map'
 key={k:v for k,v in r.items() if k!='sourceName'};digest=sha(json.dumps(key,sort_keys=True).encode())[:14];name='BT2_'+digest
 if name not in allspec:r['name']=name;allspec[name]=r
 materialcache[cachekey]=name
 return name

for tier in ['Near','Medium','Far']:
 master=P/(tier+'.blend');initial=sha(master.read_bytes());bpy.ops.wm.open_mainfile(filepath=str(master));col=bpy.data.collections['Townhouse-'+tier];graph=bpy.context.evaluated_depsgraph_get();groups={};groupMaterials={};hinges={};sourcecoords=[];fixtures=[];probes=[];materialcache={};discarded=0;microRemoved=0;hiddenGlassRemoved=0
 for o in col.all_objects:
  if o.type=='LIGHT':
   if tier!='Far':fixtures.append({'name':o.name,'position':list(o.matrix_world.translation),'color':list(o.data.color),'energy':o.data.energy,'range':3})
   continue
  if o.type!='MESH' or o.get('omitNativeExport'):continue
  eo=o.evaluated_get(graph);me=eo.to_mesh();matrices=o.matrix_world;
  role=o.get('partRole','')
  if role=='ShadePleats':
   me.calc_loop_triangles();microRemoved+=len(me.loop_triangles);eo.to_mesh_clear();continue
  if any(word in o.name for word in ['Bay.Floor0','Entry/']) and len(probes)<24 and me.vertices:
   vv=matrices@me.vertices[0].co;probes.append([-vv.x,vv.z,-vv.y])
  nm=matrices.to_3x3().inverted().transposed();sourcecoords.extend([matrices@v.co for v in me.vertices]);normals=me.corner_normals
  hinge=o.parent
  while hinge and 'Door.Hinge' not in hinge.name:hinge=hinge.parent
  moving=hinge is not None and tier!='Far'
  if moving:hinges['Door.Hinge']={'name':'Door.Hinge','position':list(hinge.matrix_world.translation),'outwardDegrees':-75,'axis':[0,0,1]}
  me.calc_loop_triangles()
  for tri in me.loop_triangles:
   face=me.polygons[tri.polygon_index]
   vv=[matrices@me.vertices[me.loops[i].vertex_index].co for i in tri.loops]
   if (vv[1]-vv[0]).cross(vv[2]-vv[0]).length<1e-10:discarded+=1;continue
   m=o.data.materials[face.material_index] if face.material_index<len(o.data.materials) else None
   if m is None:raise RuntimeError('Missing source material '+o.name)
   if 'BT2-Window-ClearGlass' in m.name and (tri.normal.y>=-.1 or tri.area<.05):hiddenGlassRemoved+=1;continue
   key=matrecipe(m,excludeRoof=o.name.startswith('Roof.'),shade=role=='Shade');groupkey=('DoorLeaf__' if moving else 'Body__')+key;groupMaterials[groupkey]=key;v,f,uv,ns=groups.setdefault(groupkey,([],[],[],[]));indices=[]
   # Export source winding/positions unchanged; Unity FBX import performs required reflection Native(-x,z,-y), preserving front-view left/right.
   for li in tri.loops:
    vi=me.loops[li].vertex_index;co=matrices@me.vertices[vi].co;no=nm@normals[li].vector;no.normalize();indices.append(len(v));v.append((co.x,co.y,co.z));uv.append(tuple(me.uv_layers[0].data[li].uv) if me.uv_layers else (0,0));ns.append((no.x,no.y,no.z))
   f.append(tuple(indices))
  eo.to_mesh_clear()
 # Copy native geometry into fresh clean scene; do not alter saved source master.
 bpy.ops.wm.read_factory_settings(use_empty=True);root=bpy.context.scene.collection;records=[];hingeobj=None;pivot=Vector((0,0,0))
 if hinges:
  anchor=hinges['Door.Hinge'];p=anchor['position'];pivot=Vector((p[0],p[1],p[2]));hingeobj=bpy.data.objects.new('Door.Hinge',None);root.objects.link(hingeobj);hingeobj.location=pivot;hingeobj.select_set(True)
 for key,(vs,fs,uvs,normals) in groups.items():
  if key.startswith('DoorLeaf__'):vs=[tuple(Vector(v)-pivot) for v in vs]
  me=bpy.data.meshes.new(key);me.from_pydata(vs,[],fs);me.update();uv=me.uv_layers.new(name='UV0')
  for i,co in enumerate(uvs):uv.data[i].uv=co
  for p in me.polygons:p.use_smooth=True
  me.normals_split_custom_set(normals);m=bpy.data.materials.get(groupMaterials[key]) or bpy.data.materials.new(groupMaterials[key]);me.materials.append(m);o=bpy.data.objects.new(key,me);root.objects.link(o);
  if key.startswith('DoorLeaf__'):o.parent=hingeobj
  o.select_set(True);bpy.context.view_layer.objects.active=o;records.append({'name':key,'triangles':sum(len(f)-2 for f in fs)})
 bpy.ops.export_scene.fbx(filepath=str(O/(tier+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='STRIP',bake_anim=False,use_mesh_modifiers=True,add_leaf_bones=False,use_custom_props=False)
 lo=[min(v[i] for v in sourcecoords) for i in range(3)];hi=[max(v[i] for v in sourcecoords) for i in range(3)];tiers.append({'tier':tier,'triangles':sum(r['triangles'] for r in records),'sourceMin':lo,'sourceMax':hi,'min':[-hi[0],lo[2],-hi[1]],'max':[-lo[0],hi[2],-lo[1]],'materials':sorted(set(groupMaterials.values())),'fixtures':fixtures,'hinges':list(hinges.values()),'zeroAreaTrianglesRemoved':discarded,'subpixelBlindPleatTrianglesReplacedByNormalMap':microRemoved,'hiddenGlassBoxTrianglesRemoved':hiddenGlassRemoved,'probes':[x for p in probes for x in p],'sourceSHA256':initial})
 assert sha(master.read_bytes())==initial
 print('EXPORTED',tier,tiers[-1]['triangles'],len(groups),flush=True)
(O/'Recipe.json').write_text(json.dumps({'coordinateContract':'Native(x,y,z)=Source(-x,z,-y); unchanged source FBX winding/coordinates, Unity importer supplies handedness reflection. Hinge positions/axis stored in source coordinates; native outward angle reverses source sign.','materials':list(allspec.values()),'tiers':tiers},indent=2));print('DONE',flush=True)
