import bpy,json,math
from pathlib import Path
from mathutils import Vector
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-victorian-bowfront-brownstone-001/b72038fc22c944eeaa22f8c44e0b94d6/Revision');P.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath='/Users/joelinstrum/Documents/CityForgeAssets/Buildings/Era - Art Deco/Victorian Bow-front Brownstone/v005-Olive-Cap/Bowfront-Composition.blend')
geo=bpy.data.collections['01 — Townhouse composition']
params=dict(sideStileWidth=.055,sillFrameOverlap=.008,horizontalFrameWidth=.065,additionalRecess=.05,stoneEmbedDepth=.08,sillApronOverlap=.012,sideLintelHeight=.19,chimneyHeight=[1.5,1.3],weatheringOpacity=.035)
import sys
if '--parameters' in sys.argv:params.update(json.loads(Path(sys.argv[sys.argv.index('--parameters')+1]).read_text()))
(P/'AssemblyParameters.json').write_text(json.dumps(params,indent=2))
# Window parts retain their original local-to-world basis, including side rotations.
prefixes=sorted(set(o.name.split(' / ')[0] for o in geo.objects if ' / Frame top' in o.name))
print('PREFIXES',prefixes)
changes=[]
for pre in prefixes:
 parts={o.name[len(pre)+3:]:o for o in list(geo.objects) if o.name.startswith(pre+' / ')}
 if not all(x in parts for x in ['Frame top','Frame bottom','Frame left','Frame right']):continue
 basis=parts['Frame top'].matrix_world.to_3x3();u=(basis@Vector((1,0,0))).normalized();up=Vector((0,0,1));out=(basis@Vector((0,-1,0))).normalized()
 def extent(obj,axis):
  a=[(obj.matrix_world@v.co).dot(axis) for v in obj.data.vertices];return min(a),max(a)
 left=extent(parts['Frame left'],u);right=extent(parts['Frame right'],u);bottom=extent(parts['Frame bottom'],up);top=extent(parts['Frame top'],up)
 il=left[0]+params['sideStileWidth'];ir=right[1]-params['sideStileWidth'];ib=bottom[0]+params['horizontalFrameWidth'];it=top[1]-params['horizontalFrameWidth']
 def remap(obj,axis,old,new):
  inv=obj.matrix_world.inverted()
  for v in obj.data.vertices:
   p=obj.matrix_world@v.co;t=(p.dot(axis)-old[0])/max(1e-8,old[1]-old[0]);p+=axis*(new[0]+t*(new[1]-new[0])-p.dot(axis));v.co=inv@p
 for key,axis,new in [('Frame left',u,(left[0],il)),('Frame right',u,(ir,right[1])),('Frame top',up,(it,top[1])),('Frame bottom',up,(bottom[0],ib))]:remap(parts[key],axis,extent(parts[key],axis),new)
 for key in ['Meeting rail','Upper glazing','Lower glazing']:
  if key in parts:remap(parts[key],u,extent(parts[key],u),(il,ir))
 if 'Upper glazing' in parts:
  ob=parts['Upper glazing'];a=extent(ob,up);remap(ob,up,a,(a[0],it))
 if 'Lower glazing' in parts:
  ob=parts['Lower glazing'];a=extent(ob,up);remap(ob,up,a,(ib,a[1]))
 for key,o in parts.items():
  if not any(s in key for s in ['Sill','Carved','Apron']):o.location-=out*params['additionalRecess']
 # Extend the hidden back of stone into the masonry rather than leaving a facade card.
 for key,o in parts.items():
  if any(s in key for s in ['Sill','Carved','Apron']):
   lo,hi=extent(o,out);remap(o,out,(lo,hi),(min(lo,parts['Frame top'].location.dot(out)-params['stoneEmbedDepth']),hi))
 if 'Sill' in parts:
  ob=parts['Sill'];a=extent(ob,up);remap(ob,up,a,(a[0],max(a[1],bottom[0]+params['sillFrameOverlap'])))
 if 'Sill undercut' in parts and 'Carved apron' in parts:
  ob=parts['Sill undercut'];a=extent(ob,up);ap=extent(parts['Carved apron'],up);remap(ob,up,a,(min(a[0],ap[1]-params['sillApronOverlap']),a[1]))
 if 'side window' in pre:
  # Keep a restrained plain stone lintel; remove front-only carved apron pieces.
  for key in ['Carved apron','Apron raised center']:
   if key in parts:bpy.data.objects.remove(parts[key],do_unlink=True)
  if 'Carved lintel' in parts:
   ob=parts['Carved lintel'];a=extent(ob,up);remap(ob,up,a,(top[1]-.025,top[1]-.025+params['sideLintelHeight']));ob.name=pre+' / Plain bearing lintel'
   # Reuse the wall stone, not the carved front atlas.
   wall=next((o for o in geo.objects if o.name.startswith('Right brownstone wall')),None)
   if wall:
    ob.data.materials.clear();ob.data.materials.append(wall.data.materials[0])
    for poly in ob.data.polygons:
     for li in poly.loop_indices:
      p=ob.matrix_world@ob.data.vertices[ob.data.loops[li].vertex_index].co;ob.data.uv_layers.active.data[li].uv=(p.dot(u)/2.7,p.z/2.1)
 changes.append(pre)
# Existing brownstone is the supplied finish. Subtle localized runoff is actual geometry,
# shallow coplanar stain patches using a transparent, low-opacity material, no broad dirt layer.
mat=bpy.data.materials.new('CF_Localized_Runoff_Subtle');mat.use_nodes=True;bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.10,.075,.052,1);bs.inputs['Roughness'].default_value=.94
# Small masonry-toned discolored strips are embedded into the simple side lintel/sill material
# through per-face material assignment; avoid a transparent overlay changing the export surface.
stone=next(o for o in geo.objects if o.name.startswith('Right brownstone wall')).data.materials[0]
weather=stone.copy();weather.name='CF_SideStone_Localized_Weathering';wn=next(n for n in weather.node_tree.nodes if n.type=='BSDF_PRINCIPLED');socket=wn.inputs['Base Color']
if socket.is_linked:
 source=socket.links[0].from_socket;mix=weather.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=params['weatheringOpacity'];mix.inputs[2].default_value=(.25,.23,.20,1);weather.node_tree.links.new(source,mix.inputs[1]);weather.node_tree.links.new(mix.outputs[0],socket)
 # Bake multiply directly into a private albedo so it survives FBX material extraction.
 im=source.node.image if source.node.type=='TEX_IMAGE' else None
 if im:
  import numpy as np
  data=np.empty(len(im.pixels),dtype=np.float32);im.pixels.foreach_get(data);data=data.reshape(-1,4);data[:,:3]*=(1-params['weatheringOpacity'])+params['weatheringOpacity']*np.array([.25,.23,.20]);ni=bpy.data.images.new('Side-Stone-Weathering.png',width=im.size[0],height=im.size[1]);ni.pixels.foreach_set(data.ravel());ni.filepath_raw=str(P/'Side-Stone-Weathering.png');ni.file_format='PNG';ni.save();ni.pack();tex=weather.node_tree.nodes.new('ShaderNodeTexImage');tex.image=ni;weather.node_tree.links.new(tex.outputs['Color'],socket)
for o in geo.objects:
 if 'side window' in o.name and ('Sill undercut' in o.name or 'Plain bearing lintel' in o.name) and ('2-2' in o.name or '3-1' in o.name):
  o.data.materials.clear();o.data.materials.append(weather)
# Masonry stacks with stone coping and hollow terracotta flues.
roof=12.38
brick=stone
capmat=stone  # Non-emissive masonry coping; never inherit an entry light mask.
clay=bpy.data.materials.new('CF_Chimney_Terracotta');clay.use_nodes=True;bs=next(n for n in clay.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.20,.075,.038,1);bs.inputs['Roughness'].default_value=.85
metal=bpy.data.materials.new('CF_Chimney_Lead_Flashing');metal.use_nodes=True;bs=next(n for n in metal.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.12,.13,.125,1);bs.inputs['Metallic'].default_value=.35;bs.inputs['Roughness'].default_value=.78
# All objects linked only to the composition collection.
def cube(name,loc,size,material):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for c in list(o.users_collection):c.objects.unlink(o)
 geo.objects.link(o);o.data.materials.append(material)
 bevel=o.modifiers.new('Soft masonry edges','BEVEL');bevel.width=.014;bevel.segments=2;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=bevel.name)
 if material==stone:
  for poly in o.data.polygons:
   for li in poly.loop_indices:
    p=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=((p.x if abs(poly.normal.y)>.5 else p.y)/2.7,p.z/2.1)
 return o
for i,(x,y,h) in enumerate([(-2.45,6.7,1.5),(2.4,2.7,1.3)]):
 cube(f'Chimney {i+1} / lead flashing',(x,y,roof+.035),(.94,1.02,.08),metal)
 cube(f'Chimney {i+1} / masonry stack',(x,y,roof+h/2),(.68,.80,h),brick)
 cube(f'Chimney {i+1} / stone coping',(x,y,roof+h+.055),(.88,1,.11),capmat)
 for j in [-1,1]:
  verts=[];faces=[];n=16
  for z,r in [(roof+h+.11,.115),(roof+h+.38,.115),(roof+h+.38,.082),(roof+h+.13,.082)]:
   for k in range(n):a=2*math.pi*k/n;verts.append((x+j*.18+r*math.cos(a),y+r*math.sin(a),z))
  for ring in range(3):
   for k in range(n):faces.append((ring*n+k,ring*n+(k+1)%n,(ring+1)*n+(k+1)%n,(ring+1)*n+k))
  me=bpy.data.meshes.new('Hollow flue');me.from_pydata(verts,[],faces);me.materials.append(clay);o=bpy.data.objects.new(f'Chimney {i+1} / hollow clay flue {j}',me);geo.objects.link(o)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(P/'Bowfront-Composition.blend'))
(P/'Refinement-Checks.json').write_text(json.dumps(dict(windowCount=len(changes),windows=changes,parameters=params,preserved=['bow assets','olive cornice cap','roof felt','parapets','front masonry','selected lamp pattern'],weathering='3.5% local trim discoloration; source wall albedo preserved'),indent=2))
print('REFINED',len(changes))
