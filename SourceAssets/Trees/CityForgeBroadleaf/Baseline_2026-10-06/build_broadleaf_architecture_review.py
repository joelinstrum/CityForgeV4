"""Architecture-first review tree. Standalone Blender authoring/render; does not replace Unity assets."""
import bpy,math,random,json
from mathutils import Vector
from pathlib import Path
r=Path(__file__).resolve().parents[1];dest=r/'SourceAssets/Trees/CityForgeBroadleaf/ArchitectureReview';dest.mkdir(parents=True,exist_ok=True)
out=Path('/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs');rng=random.Random(730421)
bpy.ops.wm.read_factory_settings(use_empty=True)
bark=bpy.data.materials.new('Irregular gray brown bark');bark.use_nodes=True;bs=bark.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.115,.091,.067,1);bs.inputs['Roughness'].default_value=.95
noise=bark.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=7;noise.inputs['Detail'].default_value=3
bump=bark.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.35;bump.inputs['Distance'].default_value=.065;bark.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);bark.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
# Tube radii taper along crooked branch paths; angular corrugation produces actual bark relief.
verts=[];faces=[]
def tube(points,radii,sides=10,root=False):
 base=len(verts)
 for j,(p,rad) in enumerate(zip(points,radii)):
  tangent=(points[min(j+1,len(points)-1)]-points[max(0,j-1)]).normalized();u=tangent.cross(Vector((0,1,0))).normalized();v=tangent.cross(u).normalized()
  for k in range(sides):
   a=k*math.tau/sides;ridge=1+.075*math.sin(k*3.1+j*.55)+.035*math.sin(k*5.7-j*.8)
   flare=1
   if root and j<3:flare+=max(0,1-p.z/1.3)*(.12+.16*max(0,math.cos(a*5+.3)))
   verts.append(p+(u*math.cos(a)+v*math.sin(a))*rad*ridge*flare)
 for j in range(len(points)-1):
  for k in range(sides):faces.append((base+j*sides+k,base+j*sides+(k+1)%sides,base+(j+1)*sides+(k+1)%sides,base+(j+1)*sides+k))
 faces.append(tuple(base+k for k in reversed(range(sides))));faces.append(tuple(base+(len(points)-1)*sides+k for k in range(sides)))
trunk=[Vector(p) for p in [(0,0,0),(.02,.03,.35),(.04,.03,1.1),(.12,-.04,2.3),(.05,.04,3.8),(.24,.12,5.2),(.30,.16,6.6),(.48,.10,8.0),(.35,.25,9.6),(.53,.28,11.1),(.60,.35,12.5),(.7,.38,14.2)]]
tube(trunk,[.48,.44,.38,.34,.30,.265,.225,.18,.135,.095,.055,.015],14,True)
# Modest buttress roots sink into the soil, with unequal directions and lengths.
for i,a in enumerate([.1,1.3,2.6,3.8,5.1]):
 d=Vector((math.cos(a),math.sin(a),0));tube([d*.18+Vector((0,0,.35)),d*.48+Vector((0,0,.12)),d*rng.uniform(.70,1.05)+Vector((0,0,-.035))],[.19,.13,.015],8)
masses=[];branches=[]
# Scaffold limbs rise at unequal levels and angles; every crown mass follows a terminal branch.
for i in range(9):
 a=i*2.399+.28;start=trunk[4+i%4].copy();start.z+=rng.uniform(-.2,.3)
 direction=Vector((math.cos(a),math.sin(a),0));reach=rng.uniform(3.5,5.0);rise=rng.uniform(3.1,4.8)
 end=start+direction*reach+Vector((0,0,rise));mid=start+direction*reach*.46+Vector((0,0,rise*.39));mid+=Vector((rng.uniform(-.25,.25),rng.uniform(-.25,.25),0))
 path=[start,start+direction*.6+Vector((0,0,.55)),mid,end-direction*.6-Vector((0,0,.5)),end];rad=.15+.025*(i%3);tube(path,[rad,rad*.87,rad*.64,rad*.30,.03],10);branches.append([list(p) for p in path])
 masses.append((mid.lerp(end,.55)+Vector((0,0,.55)),Vector((1.9,1.75,2.0))))
 for j in range(3):
  aa=a+(j-1)*rng.uniform(.45,.7);dd=Vector((math.cos(aa),math.sin(aa),0));origin=mid if j!=1 else path[3];tip=end+dd*rng.uniform(.7,1.7)+Vector((0,0,rng.uniform(-.7,1.2)))
  tube([origin,origin.lerp(tip,.4)+Vector((0,0,.3)),tip],[rad*.48,rad*.25,.012],8)
  size=Vector((rng.uniform(1.4,2.2),rng.uniform(1.3,2.0),rng.uniform(1.25,1.9)));masses.append((tip,size))
  for k in range(2):
   tt=tip+Vector((rng.uniform(-.7,.7),rng.uniform(-.7,.7),rng.uniform(.2,.8)));tube([origin.lerp(tip,.7),tip,tt],[.045,.026,.005],6)
# Dense inner crown follows the rising leader; only narrow branch windows remain.
for c,d in [((.3,.15,9.1),(2.35,2.15,2.5)),((-.4,-.3,11.0),(2.4,2.2,2.2))]:masses.append((Vector(c),Vector(d)))
# Uneven upper leader foliage, avoiding a flat umbrella lid.
for c,d in [((.7,.3,13.6),(2.0,1.7,2.1)),((-1.0,.7,12.7),(1.6,1.7,1.8)),((1.5,-1.0,12.4),(1.8,1.6,1.7))]:masses.append((Vector(c),Vector(d)))
mesh=bpy.data.meshes.new('Branch architecture');mesh.from_pydata(verts,[],faces);mesh.update();wood=bpy.data.objects.new('Tapered trunk root flare and scaffold branches',mesh);bpy.context.collection.objects.link(wood);mesh.materials.append(bark)
for p in mesh.polygons:p.use_smooth=True
# Blend branch unions, retaining gentle irregular bark relief.
bpy.context.view_layer.objects.active=wood;wood.select_set(True);rem=wood.modifiers.new('Organic branch junctions','REMESH');rem.mode='VOXEL';rem.voxel_size=.055;rem.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=rem.name)
sm=wood.modifiers.new('Soften junctions','SMOOTH');sm.factor=.5;sm.iterations=2;bpy.ops.object.modifier_apply(modifier=sm.name)
# Dense textured leaf clusters with irregular shells, small occluded seams, and dark interior layers.
texture=bpy.data.images.load(str(r/'Assets/CityForge/Art/Trees/CityForgeBroadleaf/Forge_Leaf.png'));texture.pack();materials=[]
for name,color in [('Summer outer foliage',(.36,.46,.30,1)),('Summer middle foliage',(.28,.37,.245,1)),('Summer interior foliage',(.19,.27,.18,1))]:
 m=bpy.data.materials.new(name);m.use_nodes=True;nodes=m.node_tree.nodes;bs=nodes.get('Principled BSDF');tex=nodes.new('ShaderNodeTexImage');tex.image=texture;mul=nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=color;m.node_tree.links.new(tex.outputs['Color'],mul.inputs[1]);m.node_tree.links.new(mul.outputs[0],bs.inputs['Base Color']);m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha']);bs.inputs['Roughness'].default_value=.93;m.surface_render_method='DITHERED';materials.append(m)
vv=[];ff=[];matids=[]
for index,(center,radii) in enumerate(masses):
 for j in range(460):
  z=rng.uniform(-1,1);a=rng.uniform(0,math.tau);n=Vector((math.sqrt(1-z*z)*math.cos(a),math.sqrt(1-z*z)*math.sin(a),z));rad=rng.uniform(.35,1)**(1/3)
  # Lobes are not clean ellipsoids: correlated bulges and uneven edge tips.
  irregular=1+.12*math.sin(a*3+index*.9)*math.cos(z*4)+.07*math.sin(a*7+z*5+index)
  pos=center+Vector((n.x*radii.x,n.y*radii.y,n.z*radii.z))*rad*irregular
  # A few narrow seams reveal woody interior, without thinning the overall crown.
  if index%7==0 and abs(n.x+.25)<.11 and n.y<-.45 and rad>.75:continue
  normal=(n*.65+Vector((rng.uniform(-.65,.65),rng.uniform(-.65,.65),rng.uniform(-.65,.65)))).normalized();u=normal.cross(Vector((0,0,1)))
  if u.length<.01:u=Vector((1,0,0))
  u.normalize();v=normal.cross(u).normalized();ang=rng.uniform(0,math.tau);u,v=u*math.cos(ang)+v*math.sin(ang),v*math.cos(ang)-u*math.sin(ang)
  size=rng.uniform(.46,.70);base=len(vv);vv.extend([pos+u*x*size+v*y*size for x,y in ((-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5))]);ff.append(tuple(base+k for k in range(4)));matids.append(0 if rad>.87 else 1 if rad>.65 else 2)
lm=bpy.data.meshes.new('Branch organized dense foliage');lm.from_pydata(vv,[],ff);lm.update();obj=bpy.data.objects.new('Irregular branch organized foliage masses',lm);bpy.context.collection.objects.link(obj)
for m in materials:lm.materials.append(m)
uv=lm.uv_layers.new()
for p in lm.polygons:
 p.material_index=matids[p.index]
 for li in p.loop_indices:uv.data[li].uv=((0,0),(1,0),(1,1),(0,1))[lm.loops[li].vertex_index%4]
# Neutral ground only in the review scene, not CityForge terrain.
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='Review ground';ground.location.z=-.07;gm=bpy.data.materials.new('Neutral muted ground');gm.diffuse_color=(.145,.16,.12,1);ground.data.materials.append(gm)
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.rotation_euler=(math.radians(28),math.radians(-15),math.radians(-35));sun.data.energy=2.4;sun.data.angle=.13
s=bpy.context.scene;s.world=bpy.data.worlds.new('Neutral sky');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.38,.43,.5,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.65
bpy.ops.object.camera_add();cam=bpy.context.object;target=Vector((0,0,8));elev=math.radians(50);az=math.radians(-55);cam.location=target+Vector((math.cos(az)*math.cos(elev),math.sin(az)*math.cos(elev),math.sin(elev)))*40;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=24;s.camera=cam
s.render.engine='CYCLES';s.cycles.samples=48;s.render.resolution_x=1400;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.view_settings.view_transform='AgX';s.render.image_settings.file_format='PNG';s.render.filepath=str(out/'Broadleaf_Architecture_Gameplay_50deg.png')
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_location=target;area.spaces.active.region_3d.view_distance=28;area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();area.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.wm.save_as_mainfile(filepath=str(dest/'Broadleaf_Architecture_Review.blend'));bpy.ops.render.render(write_still=True)
(dest/'architecture.json').write_text(json.dumps({'scaffoldBranches':branches,'foliageMasses':[{'center':list(c),'radii':list(d)} for c,d in masses],'leafClusterCards':len(ff),'cameraElevationDegrees':50,'authoringSeed':730421},indent=2))
