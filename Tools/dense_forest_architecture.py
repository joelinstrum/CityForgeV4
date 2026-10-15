"""Distinct branch-organized crown architectures for Dense Forest Trees v2."""
import bpy, math, random
from mathutils import Vector

def build_architecture(variant, wood, leaf):
 rng=random.Random(730421+variant*1013)
 cfg={1:dict(label='Tall oval / high foliage',scaffolds=7,secondary=3,height=16.0,reach=(2.3,3.25),crown_z=(10.5,13.8),radii=(1.55,1.75,2.1),cards=22500),2:dict(label='Broad spreading / low fork',scaffolds=5,secondary=4,height=12.0,reach=(4.3,5.55),crown_z=(7.0,9.4),radii=(2.35,2.15,1.8),cards=25000),3:dict(label='Uneven multi-leader / open edge',scaffolds=8,secondary=3,height=15.4,reach=(3.0,4.5),crown_z=(8.0,13.3),radii=(1.8,1.9,1.85),cards=23500)}[variant]
 vv=[];ff=[];masses=[];paths=[];terminals=[]
 def tube(points,radii,sides=10):
  base=len(vv)
  for j,(p,rad) in enumerate(zip(points,radii)):
   tangent=(points[min(j+1,len(points)-1)]-points[max(j-1,0)]).normalized();u=tangent.cross(Vector((0,1,0))).normalized();v=tangent.cross(u)
   for k in range(sides):
    a=math.tau*k/sides;ridge=1+.07*math.sin(k*3.1+j*.7)+.035*math.sin(k*5.7-j*.4);vv.append(p+(u*math.cos(a)+v*math.sin(a))*rad*ridge)
  for j in range(len(points)-1):
   for k in range(sides):ff.append((base+j*sides+k,base+j*sides+(k+1)%sides,base+(j+1)*sides+(k+1)%sides,base+(j+1)*sides+k))
  ff.append(tuple(base+k for k in reversed(range(sides))));ff.append(tuple(base+(len(points)-1)*sides+k for k in range(sides)))
 h=cfg['height'];lean=Vector((.5,-.22,0)) if variant==1 else Vector((-.3,.45,0)) if variant==2 else Vector((1.0,.55,0))
 zs=[0,.35,1.1,2.3,3.8,5.2,6.7,8.2,10,11.7,h]
 trunk=[Vector((lean.x*(z/h)**1.3+.07*math.sin(z*1.1),lean.y*(z/h)**1.2+.05*math.sin(z*.8),z)) for z in zs]
 # Root flare and crooked taper, rather than cylindrical leaders.
 tube(trunk,[.63,.5,.39,.34,.30,.26,.22,.18,.13,.08,.015],14)
 for i in range(cfg['scaffolds']):
  angle=i*math.tau/cfg['scaffolds']+rng.uniform(-.28,.28)+variant*.42
  direction=Vector((math.cos(angle),math.sin(angle),0));reach=rng.uniform(*cfg['reach']);endz=rng.uniform(*cfg['crown_z'])
  if variant==2:originz=3.5+i*.42
  elif variant==1:originz=6.1+i*.43
  else:originz=4.4+(i%4)*.85
  origin=Vector((lean.x*(originz/h)**1.3,lean.y*(originz/h)**1.2,originz))
  if variant==3:
   reach*=1.25 if i in (0,1,6) else .8;endz+=1.1 if i in (2,3) else -1.0 if i in (5,6) else 0
  end=origin+direction*reach;end.z=endz
  middle=origin.lerp(end,.48)+Vector((rng.uniform(-.3,.3),rng.uniform(-.3,.3),.25))
  path=[origin,origin+direction*.55+Vector((0,0,.65)),middle,end-direction*.35-Vector((0,0,.4)),end]
  radius=.19 if variant==2 else .16+(i%3)*.015;tube(path,[radius,radius*.90,radius*.63,radius*.32,.018]);paths.append([list(p) for p in path])
  major=Vector(cfg['radii'])*rng.uniform(.92,1.10);masses.append((end+Vector((0,0,.35)),major))
  count=cfg['secondary']+(1 if variant==3 and i%3==0 else -1 if variant==3 and i%3==1 else 0)
  for j in range(count):
   a=angle+(j-(count-1)/2)*(.60 if variant==2 else .48)+rng.uniform(-.12,.12);d=Vector((math.cos(a),math.sin(a),0))
   tip=end+d*rng.uniform(.65,1.55)+Vector((0,0,rng.uniform(-.55,1.15)))
   start=middle if j%2==0 else path[3];tube([start,start.lerp(tip,.48)+Vector((0,0,.25)),tip],[radius*.5,radius*.23,.009],8);terminals.append(tip)
   rad=major*Vector((rng.uniform(.68,.86),rng.uniform(.68,.86),rng.uniform(.7,.9)));masses.append((tip+Vector((0,0,.15)),rad))
   for q in range(3):
    twig=tip+Vector((rng.uniform(-.55,.55),rng.uniform(-.55,.55),rng.uniform(.25,.75)));tube([start.lerp(tip,.75),tip,twig],[.036,.018,.004],6)
 # Dense connected center; several upper lobes, not the original single recognizable top puff.
 if variant==1:upper=[((.1,0,11.2),(2.0,1.9,2.5)),((.4,.1,14.0),(1.8,1.75,2.2)),((-.6,.5,15.6),(1.55,1.55,1.8))]
 elif variant==2:upper=[((0,0,8.6),(2.7,2.5,2.4)),((-.9,.5,10.8),(2.1,2.0,1.9)),((1.4,-.7,10.2),(2.15,1.9,1.9))]
 else:upper=[((.3,0,10.2),(2.0,2.2,2.5)),((1.0,.4,13.1),(2.0,1.75,2.1)),((1.7,.2,15.0),(1.6,1.5,1.65)),((-.8,1.4,12.4),(1.6,1.7,1.8))]
 masses.extend((Vector(c),Vector(rad)) for c,rad in upper)
 mesh=bpy.data.meshes.new('Distinct scaffold architecture');mesh.from_pydata(vv,[],ff);mesh.update();mesh.materials.append(wood.data.materials[0]);wood.data=mesh
 bpy.ops.object.select_all(action='DESELECT');wood.select_set(True);bpy.context.view_layer.objects.active=wood
 rem=wood.modifiers.new('Organic branch junctions','REMESH');rem.mode='VOXEL';rem.voxel_size=.055;rem.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=rem.name)
 sm=wood.modifiers.new('Soften unions','SMOOTH');sm.factor=.5;sm.iterations=2;bpy.ops.object.modifier_apply(modifier=sm.name)
 materials=list(leaf.data.materials);vv=[];ff=[];slots=[]
 # Budget follows lobe volumes: dense small cards throughout, not sparse branches.
 weights=[rad.x*rad.y*rad.z for c,rad in masses];total=sum(weights)
 for index,(center,radii) in enumerate(masses):
  count=round(cfg['cards']*weights[index]/total)
  for j in range(count):
   z=rng.uniform(-1,1);a=rng.uniform(0,math.tau);n=Vector((math.sqrt(1-z*z)*math.cos(a),math.sqrt(1-z*z)*math.sin(a),z));radius=rng.uniform(.18,1)**(1/3)
   irregular=1+.12*math.sin(a*3+index*.9)*math.cos(z*4)+.07*math.sin(a*7+z*5+index)
   pos=center+Vector((n.x*radii.x,n.y*radii.y,n.z*radii.z))*radius*irregular
   normal=(n*.65+Vector((rng.uniform(-.65,.65),rng.uniform(-.65,.65),rng.uniform(-.65,.65)))).normalized();u=normal.cross(Vector((0,0,1)))
   if u.length<.01:u=Vector((1,0,0))
   u.normalize();v=normal.cross(u);ang=rng.uniform(0,math.tau);u,v=u*math.cos(ang)+v*math.sin(ang),v*math.cos(ang)-u*math.sin(ang)
   size=rng.uniform(.46,.70);base=len(vv);vv.extend(pos+u*x*size+v*y*size for x,y in ((-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5)));ff.append(tuple(range(base,base+4)));slots.append(0 if radius>.87 else 1 if radius>.65 else 2)
 mesh=bpy.data.meshes.new('Distinct branch organized dense foliage');mesh.from_pydata(vv,[],ff);mesh.update();[mesh.materials.append(m) for m in materials];uv=mesh.uv_layers.new()
 for p in mesh.polygons:
  p.material_index=slots[p.index]
  for li in p.loop_indices:uv.data[li].uv=((0,0),(1,0),(1,1),(0,1))[mesh.loops[li].vertex_index%4]
 leaf.data=mesh
 cfg.update(seed=730421+variant*1013,scaffoldPaths=paths,foliageMasses=[dict(center=list(c),radii=list(rad)) for c,rad in masses],actualLeafCards=len(ff),secondaryBranches=len(terminals))
 return masses,cfg
