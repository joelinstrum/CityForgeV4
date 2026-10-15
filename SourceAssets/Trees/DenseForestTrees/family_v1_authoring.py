"""Controlled family from accepted crown; no source edits. Blender 4/5 CLI."""
import bpy, math, json, random, struct, hashlib, os
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1]
D=R/'SourceAssets/Trees/DenseForestTrees'; A=R/'Assets/CityForge/Art/Trees/DenseForestTrees'
O=Path('/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/DenseForestTrees');O.mkdir(parents=True,exist_ok=True)
SOURCE=R/'SourceAssets/Trees/CityForgeBroadleaf/Refinement01/BarkBrightness01/Broadleaf_BarkBrightness01.blend'
M=json.loads((SOURCE.parent.parent/'recipe_geometry.json').read_text())['foliageMasses']
PARAMS=[dict(name='A_Rounded',xy=(1,1),lift=0,top=0,bulge=0),dict(name='B_Tall',xy=(.94,.97),lift=.65,top=.55,bulge=.025),dict(name='C_Full',xy=(1.07,1.03),lift=-.35,top=-.25,bulge=.035),dict(name='D_HighCrown',xy=(.97,1.01),lift=1.8,top=.2,bulge=.025)]
PALETTE=[(.44,.34,.13,1),(.33,.20,.105,1),(.29,.32,.18,1)]
report={'category':'Dense Forest Trees','seed':730421,'source':str(SOURCE),'sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'easyTreeParametersChanged':False,'variants':[]}
if os.environ.get('DENSE_WINTER_ONLY') and (A/'family_recipe.json').exists():
 report['variants']=[v for v in json.loads((A/'family_recipe.json').read_text())['variants'] if v['season']!='Winter']
def warp(p,k):
 q=p.copy();t=max(0,min(1,(p.z-2)/5));s=t*t*(3-2*t);theta=math.atan2(p.y,p.x)
 f=1+k['bulge']*math.sin(theta*3+.8)*s
 q.x*=k['xy'][0]*f;q.y*=k['xy'][1]*f;q.z+=k['lift']*s+k['top']*max(0,min(1,(p.z-10)/6))
 return q
for vi,k in enumerate(PARAMS):
 for season in ['Summer','Fall','Winter']:
  if os.environ.get('DENSE_WINTER_ONLY') and season!='Winter':continue
  bpy.ops.wm.open_mainfile(filepath=str(SOURCE));wood=bpy.data.objects['Tapered trunk root flare and scaffold branches'];leaf=bpy.data.objects['Irregular branch organized foliage masses']
  for ob in (wood,leaf):
   for v in ob.data.vertices:v.co=warp(v.co,k)
   ob.data.update()
  masses=[(warp(Vector(m['center']),k),Vector(m['radii'])*Vector((k['xy'][0],k['xy'][1],1))) for m in M]
  if season=='Fall':
   # Stable patch-scale colors: muted ochre, orange brown, retained olive green.
   original=list(leaf.data.materials);leaf.data.materials.clear()
   for i,col in enumerate(PALETTE):
    mat=original[0].copy();tex=next(n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE');gray=tex.image.copy();pixels=list(gray.pixels[:]);
    for j in range(0,len(pixels),4):
     lum=pixels[j]*.2126+pixels[j+1]*.7152+pixels[j+2]*.0722;pixels[j]=pixels[j+1]=pixels[j+2]=lum
    gray.pixels[:]=pixels;gray.pack();gray.filepath_raw=str(A/'Fall_Leaf.png');gray.file_format='PNG';gray.save();tex.image=gray;mat.name=['Fall muted ochre','Fall orange brown','Fall olive green'][i]
    mul=next(n for n in mat.node_tree.nodes if n.type=='MIX_RGB');mul.inputs[2].default_value=col;leaf.data.materials.append(mat)
   for p in leaf.data.polygons:
    center=p.center; a=math.sin(center.x*.63+center.z*.43+vi)+.65*math.cos(center.y*.72-center.z*.35)
    p.material_index=0 if a>.15 else 1 if a<-.55 else 2
  snow=None
  if season=='Winter':
   bpy.data.objects.remove(leaf,do_unlink=True);leaf=None
   # Connected terminal twig fans reveal the fine architecture hidden by summer cards.
   vv=[v.co.copy() for v in wood.data.vertices];ff=[tuple(p.vertices) for p in wood.data.polygons];rr=random.Random(730421)
   def twig(start,end,radius):
    direction=(end-start).normalized();u=direction.cross(Vector((0,1,0))).normalized();v=direction.cross(u);base=len(vv)
    for point,rad in [(start,radius),(end,radius*.2)]:
     for j in range(5):vv.append(point+(u*math.cos(j*math.tau/5)+v*math.sin(j*math.tau/5))*rad)
    for j in range(5):ff.append((base+j,base+(j+1)%5,base+5+(j+1)%5,base+5+j))
   existing=[v.co.copy() for v in wood.data.vertices]
   for center,rad in masses[:41]:
    origin=min(existing,key=lambda p:(p-center).length_squared)
    for j in range(4):
     angle=rr.uniform(0,math.tau);direction=Vector((math.cos(angle)*.65,math.sin(angle)*.65,rr.uniform(.35,.85))).normalized();end=origin+direction*rr.uniform(.75,1.55);twig(origin,end,.024)
     for sign in [-1,1]:
      tip=end+Vector((direction.x*.3+sign*.23,direction.y*.3-sign*.17,rr.uniform(.35,.75)));twig(origin.lerp(end,.72),tip,.012)
   me=bpy.data.meshes.new('Winter connected branch architecture');me.from_pydata(vv,[],ff);me.update();me.materials.append(wood.data.materials[0]);wood.data=me
   for p in me.polygons:p.use_smooth=True
   verts=[];faces=[]
   # Cap upward-facing woody surfaces; no snow painted on vertical trunk.
   for p in wood.data.polygons:
    if p.center.z<3.5 or p.normal.z<.5:continue
    center=p.center; thickness=.045+.035*(.5+.5*math.sin(center.x*2+center.y*1.7))
    base=len(verts);verts.extend(wood.data.vertices[i].co+Vector((0,0,thickness)) for i in p.vertices);faces.append(tuple(range(base,base+len(p.vertices))))
   me=bpy.data.meshes.new('Settled branch snow');me.from_pydata(verts,[],faces);me.update();snow=bpy.data.objects.new('Snow on upper branch surfaces',me);bpy.context.collection.objects.link(snow)
   mat=bpy.data.materials.new('Winter muted snow');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.72,.77,.80,1);bs.inputs['Roughness'].default_value=.98;me.materials.append(mat)
  tree=[wood]+([leaf] if leaf else [])+([snow] if snow else [])
  # Wood UV is export only; source unchanged.
  bpy.ops.object.select_all(action='DESELECT');wood.select_set(True);bpy.context.view_layer.objects.active=wood;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
  name=k['name']+'_'+season;folder=A/name;folder.mkdir(parents=True,exist_ok=True)
  bpy.ops.wm.save_as_mainfile(filepath=str(D/(name+'.blend')))
  # Actual camera elevation; neutral isolated review, not a Unity screenshot.
  sc=bpy.context.scene;cam=sc.camera;target=Vector((0,0,8.5));elev=math.radians(35.264);az=math.radians(-45);cam.location=target+Vector((math.cos(az)*math.cos(elev),math.sin(az)*math.cos(elev),math.sin(elev)))*45;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=25
  sc.render.engine='CYCLES';sc.cycles.samples=12;sc.render.resolution_x=640;sc.render.resolution_y=640;sc.render.resolution_percentage=100;sc.render.filepath=str(O/(name+'.png'));bpy.ops.render.render(write_still=True)
  entry={'name':name,'controls':k,'season':season,'fallPaletteLinear':PALETTE if season=='Fall' else None,'lods':[]}
  originals={ob:ob.data.copy() for ob in tree}
  for tier,ratio,keep,scale in [(0,.22,1,1),(1,.09,.65,1.2),(2,.03,.25,1.8)]:
   for ob in tree:ob.data=originals[ob].copy()
   for ob in [wood]+([snow] if snow else []):
    bpy.context.view_layer.objects.active=ob;mod=ob.modifiers.new('Derived reduction','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
   if leaf and tier:
    old=leaf.data;vv=[];ff=[];uvs=[];slots=[];rng=random.Random(730421)
    for p in old.polygons:
     if rng.random()>keep:continue
     points=[old.vertices[i].co.copy() for i in p.vertices];c=sum(points,Vector())/len(points);base=len(vv);vv.extend(c+(v-c)*scale for v in points);ff.append(tuple(range(base,base+len(points))));slots.append(p.material_index);uvs.extend(old.uv_layers.active.data[i].uv.copy() for i in p.loop_indices)
    me=bpy.data.meshes.new('Foliage');me.from_pydata(vv,[],ff);me.update();[me.materials.append(m) for m in old.materials];uv=me.uv_layers.new()
    for i,v in enumerate(uvs):uv.data[i].uv=v
    for i,slot in enumerate(slots):me.polygons[i].material_index=slot
    leaf.data=me
   if leaf:
    with (folder/f'LOD{tier}_Guides.bytes').open('wb') as f:
     f.write(struct.pack('<i',len(leaf.data.vertices)))
     for v in leaf.data.vertices:
      c,rad=min(masses,key=lambda m:sum(((v.co[i]-m[0][i])/m[1][i])**2 for i in range(3)));d=v.co-c;n=Vector(tuple(d[i]/rad[i]**2 for i in range(3))).normalized();rr=math.sqrt(sum((d[i]/rad[i])**2 for i in range(3)));t=max(0,min(1,(rr-.45)/.5));f.write(struct.pack('<7f',*v.co,*n,t*t*(3-2*t)))
   bpy.ops.object.select_all(action='DESELECT')
   for ob in tree:ob.select_set(True)
   bpy.ops.export_scene.fbx(filepath=str(folder/f'LOD{tier}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False)
   meshes=[]
   for ob in tree:ob.data.calc_loop_triangles();meshes.append({'name':ob.name,'triangles':len(ob.data.loop_triangles)})
   entry['lods'].append({'tier':tier,'woodRatio':ratio,'leafKeep':keep,'sprayScale':scale,'meshes':meshes})
  report['variants'].append(entry);(A/'family_recipe.json').write_text(json.dumps(report,indent=2));print('FAMILY COMPLETED '+name,flush=True)
(O/'family_recipe.json').write_text(json.dumps(report,indent=2))
