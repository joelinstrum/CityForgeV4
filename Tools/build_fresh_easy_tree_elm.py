"""Fresh Easy Tree American elm, one conventional real-time asset; no existing tree inputs."""
import bpy,sys,math,json,random,shutil
from pathlib import Path
from mathutils import Vector
import numpy as np
R=Path(__file__).resolve().parents[1];S=R/'SourceAssets/Trees/AmericanElmFresh';A=R/'Assets/CityForge/Art/Trees/AmericanElmFresh';G=S/'Generator/easy_tree/EasyTree';T=G/'assets/textures'
sys.path.insert(0,str(G.parent));import EasyTree
EasyTree.register();bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.scene.tree_preset='LARGE';bpy.context.scene.season='SUMMER';bpy.ops.object.add_tree_tool();generator=bpy.context.object;generator.name='American Elm - Easy Tree authoring'
m=generator.modifiers[0]
params={'Socket_25':1,'Socket_40':2,'Socket_26':2,'Socket_27':4,'Socket_28':8,'Socket_29':.60,'Socket_30':.22,'Socket_31':.35,'Socket_32':2.6,'Socket_16':3187,'Socket_19':True,'Socket_11':1,'Socket_17':.5,'Socket_12':4,'Socket_13':6.,'Socket_14':.85,'Socket_15':1.15,'Socket_39':True,'Socket_23':False,'Socket_34':False}
for k,v in params.items():m[k]=v
# Native leaf/twig generator; retain twig arrangement, use simple curved cutout cards.
bpy.data.node_groups['Tree to Mesh and Leaves'].nodes['Make Twig & Leaf'].inputs['X Divisions'].default_value=2
bpy.data.node_groups['Tree to Mesh and Leaves'].nodes['Make Twig & Leaf'].inputs['Y Divisions'].default_value=2
bpy.data.node_groups['Tree to Mesh'].nodes['Subdivision Surface.001'].inputs['Level'].default_value=0
bpy.data.node_groups['Tree to Mesh'].nodes['Curve Circle'].inputs['Resolution'].default_value=10
bpy.data.node_groups['Make Twig & Leaf'].nodes['Curve Circle'].inputs['Resolution'].default_value=3
leaf_group=bpy.data.node_groups['Make Leaf'];width=leaf_group.nodes['Math.001'].inputs[0]
for link in list(width.links):leaf_group.links.remove(link)
width.default_value=1.0
ng=m.node_group;out=next(n for n in ng.nodes if n.bl_idname=='NodeGroupOutput');link=out.inputs['Geometry'].links[0];real=ng.nodes.new('GeometryNodeRealizeInstances');ng.links.new(link.from_socket,real.inputs['Geometry']);ng.links.new(real.outputs['Geometry'],out.inputs['Geometry'])
generator.update_tag();bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=generator.evaluated_get(dg);mesh=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg);mesh.name='New Easy Tree elm mesh';tree=bpy.data.objects.new('American Elm export',mesh);bpy.context.collection.objects.link(tree)
# Scale a real mature tree to 14m, uniformly (no hand-designed crown masses).
base=min(v.co.z for v in mesh.vertices);height=max(v.co.z for v in mesh.vertices)-base;scale=14/height
for v in mesh.vertices:v.co=Vector((v.co.x*scale,v.co.y*scale,(v.co.z-base)*scale))
# Separate wood/leaves for conventional materials and UV atlas tile indexing.
bpy.ops.object.select_all(action='DESELECT');tree.select_set(True);bpy.context.view_layer.objects.active=tree;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='MATERIAL');bpy.ops.object.mode_set(mode='OBJECT')
objects=[o for o in bpy.context.selected_objects if o.type=='MESH'];wood=next(o for o in objects if o.data.materials[0].name=='Bark');leaves=next(o for o in objects if 'Leaves' in o.data.materials[0].name);wood.name='Elm Trunk and Branches';leaves.name='Elm Summer Foliage'
# Export the main Easy Tree skeleton separately from tiny twig tubes.
m['Socket_19']=False;generator.update_tag();bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();body=bpy.data.meshes.new_from_object(generator.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg);wood.data=body
for v in body.vertices:v.co=Vector((v.co.x*scale,v.co.y*scale,(v.co.z-base)*scale))
m['Socket_19']=True;generator.update_tag();bpy.context.view_layer.update()
# Native leaf outline needs its middle row; weld collapsed tips and remove degenerate faces.
bpy.ops.object.select_all(action='DESELECT');leaves.select_set(True);bpy.context.view_layer.objects.active=leaves;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.000001);bpy.ops.mesh.dissolve_degenerate(threshold=.000001);bpy.ops.object.mode_set(mode='OBJECT')
# Bake Easy Tree's six-image leaf-atlas selection into UV0. One tile per connected native leaf.
adj=[[] for _ in leaves.data.vertices]
for e in leaves.data.edges:a,b=e.vertices;adj[a].append(b);adj[b].append(a)
labels={};rng=random.Random(3187);components=0
for v in leaves.data.vertices:
 if v.index in labels:continue
 tile=rng.randrange(6);stack=[v.index];labels[v.index]=tile;components+=1
 while stack:
  for q in adj[stack.pop()]:
   if q not in labels:labels[q]=tile;stack.append(q)
uv=leaves.data.uv_layers.active
for loop in leaves.data.loops:
 t=labels[loop.vertex_index];u,v=uv.data[loop.index].uv;uv.data[loop.index].uv=((u+t%3)/3,(v+t//3)/2)
# Small root flare and bark relief on the new generator wood; native curve/taper retained.
for ob in objects:
 for v in ob.data.vertices:
  v.co.x+=.075*math.sin(v.co.z*.55)
for v in wood.data.vertices:
 p=v.co;rad=math.hypot(p.x,p.y)
 a=math.atan2(p.y,p.x)
 if p.z<4 and rad<1.3:
  irregular=1+.045*math.sin(a*5+p.z*.4)+.025*math.sin(a*3-p.z*.7);p.x*=irregular;p.y*=irregular
 if p.z<.8 and rad<1.3:
  a=math.atan2(p.y,p.x);flare=1+.4*(1-p.z/.8)**1.7*(.75+.25*math.sin(a*3+.8));p.x*=flare;p.y*=flare
wood.data.update()
# Single game mesh, not an LOD: remove excessive twig tube tessellation, retain curve silhouette.
bpy.ops.object.select_all(action='DESELECT');wood.select_set(True);bpy.context.view_layer.objects.active=wood;dec=wood.modifiers.new('Game mesh wood tessellation','DECIMATE');dec.ratio=.40;dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
for ob in objects:
 for p in ob.data.polygons:p.use_smooth=True
 colors=ob.data.color_attributes.new(name="Color",type="FLOAT_COLOR",domain="CORNER")
 for loop in ob.data.loops:
  p=ob.data.vertices[loop.vertex_index].co
  ao=1.0 if ob==wood else .45+.55*min(1,math.sqrt((p.x/6)**2+(p.y/6)**2+((p.z-9)/5)**2))
  colors.data[loop.index].color=(ao,ao,ao,1)
# Standard Unity texture packing: source photographic albedo + source opacity -> RGBA.
def pixels(path,noncolor=False):
 im=bpy.data.images.load(str(path),check_existing=False)
 if noncolor:im.colorspace_settings.name='Non-Color'
 a=np.empty(len(im.pixels),np.float32);im.pixels.foreach_get(a);return im,a.reshape((-1,4))
def write_image(name,width,height,data,noncolor=False):
 im=bpy.data.images.new(name,width=width,height=height,alpha=True)
 if noncolor:im.colorspace_settings.name='Non-Color'
 im.pixels.foreach_set(data.astype(np.float32).ravel());im.filepath_raw=str(A/name);im.file_format='PNG';im.save();return im
ci,c=pixels(T/'LeafSet014_1K-JPG_Color.jpg');oi,o=pixels(T/'LeafSet014_1K-JPG_Opacity.jpg',True);c[:,3]=o[:,0];leafimage=write_image('ElmLeaf_AlbedoOpacity.png',*ci.size,c)
for prefix,outname in [('Bark001_2K-JPG','ElmBark'),('LeafSet014_1K-JPG','ElmLeaf')]:
 shutil.copyfile(T/(prefix+'_NormalGL.jpg'),A/(outname+'_Normal.jpg'))
 ri,rr=pixels(T/(prefix+'_Roughness.jpg'),True);packed=np.zeros_like(rr);packed[:,3]=1-rr[:,0];write_image(outname+'_MetalSmooth.png',*ri.size,packed,True)
shutil.copyfile(T/'Bark001_2K-JPG_Color.jpg',A/'ElmBark_Albedo.jpg')
# Persist native editable generator and a separate exported game mesh in one fresh source file.
generator.hide_render=True;generator.hide_set(True)
for ob in objects:
 mat=bpy.data.materials.new(ob.name+' fresh export material');mat.use_nodes=True;nt=mat.node_tree;bsdf=nt.nodes.get('Principled BSDF');tex=nt.nodes.new('ShaderNodeTexImage');tex.image=leafimage if ob==leaves else bpy.data.images.load(str(A/'ElmBark_Albedo.jpg'));nt.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.88
 if ob==leaves:nt.links.new(tex.outputs['Alpha'],bsdf.inputs['Alpha'])
 ob.data.materials.clear();ob.data.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=str(S/'AmericanElm_EasyTree.blend'));bpy.ops.object.select_all(action='DESELECT')
for ob in objects:ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(A/'AmericanElm.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False)
counts={}
for ob in objects:ob.data.calc_loop_triangles();counts[ob.name]=len(ob.data.loop_triangles)
recipe={'generator':'Easy Tree 1.0.1 official Blender Extensions','seed':3187,'preset':'LARGE (overridden)','season':'SUMMER','sockets':{x.name:params.get(x.identifier,m.get(x.identifier)) for x in ng.interface.items_tree if x.item_type=='SOCKET' and x.in_out=='INPUT' and x.identifier in params},'nativeLeafCards':components,'leafGrid':[2,2],'woodProfileSides':10,'twigProfileSides':3,'woodDecimate':.40,'rootFlare':{'height':.8,'maxFactor':1.4},'heightMeters':14,'uniformScale':scale,'triangles':counts,'bounds':[[min(v.co[i] for ob in objects for v in ob.data.vertices),max(v.co[i] for ob in objects for v in ob.data.vertices)] for i in range(3)],'noExistingTreeInputs':True,'noLODs':True}
(S/'recipe.json').write_text(json.dumps(recipe,indent=2));print(json.dumps(recipe),flush=True)
