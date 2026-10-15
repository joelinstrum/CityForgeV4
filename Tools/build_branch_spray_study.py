"""Single rounded tree: real leaf-bearing twig sprays inside the fixed crown envelope."""
import bpy,math,random,json,struct
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];A=R/'Assets/CityForge/Art/Trees/DenseForestTrees/BranchSprayStudy';A.mkdir(parents=True,exist_ok=True)
D=R/'SourceAssets/Trees/DenseForestTrees/BranchSprayStudy';D.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceAssets/Trees/DenseForestTrees/CloseLOD0/A_Rounded_Summer.blend'))
wood=bpy.data.objects['Tapered trunk root flare and scaffold branches'];leaf=bpy.data.objects['Irregular branch organized foliage masses'];original=leaf.data.copy();mats=list(original.materials)
recipe=json.loads((R/'SourceAssets/Trees/CityForgeBroadleaf/Refinement01/recipe_geometry.json').read_text());masses=[(Vector(m['center']),Vector(m['radii'])) for m in recipe['foliageMasses']]
rng=random.Random(730421);vv=[];ff=[];uvs=[];slots=[];guides=[];clusterGuides=[];wv=[];wf=[]
low=Vector(tuple(min(v.co[i] for v in original.vertices) for i in range(3)));high=Vector(tuple(max(v.co[i] for v in original.vertices) for i in range(3)))
def metric(p,m):return sum(((p[i]-m[0][i])/m[1][i])**2 for i in range(3))
def guide(p,m,cluster,cn):
 c,rad=m;d=p-c;n=Vector(tuple(d[i]/rad[i]**2 for i in range(3))).normalized();radius=math.sqrt(metric(p,m));t=max(0,min(1,(radius-.45)/.5));return (*p,*n,t*t*(3-2*t))
def tube(start,end,r):
 delta=end-start
 if delta.length<.02:return
 axis=delta.normalized();u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u);base=len(wv)
 for k in range(4):
  t=k/3;center=start+delta*t+Vector((0,0,.045))*math.sin(t*math.pi);radius=r*(1-t)+.002
  for j in range(5):a=j*math.tau/5;wv.append(center+(u*math.cos(a)+v*math.sin(a))*radius)
 for k in range(3):
  for j in range(5):i=base+k*5+j;wf.append((i,base+k*5+(j+1)%5,base+(k+1)*5+(j+1)%5,i+5))
# Pick anchors from authored foliage positions; preserve the existing local crown envelopes.
centers=[sum((original.vertices[i].co for i in p.vertices),Vector())/4 for p in original.polygons]
anchors=[]
for m in masses:
 candidates=[p for p in centers if .48<metric(p,m)<.95]
 count=max(12,round(m[1].x*m[1].y*m[1].z*9))
 for j in range(count):
  p=rng.choice(candidates) if candidates else m[0];anchors.append((p,m))
# Fixed total budget: about the same quad count, arranged on branch-supported fans.
per=5;leafRects=[(.403,.733,.583,.951),(.295,.157,.473,.309),(.523,.194,.713,.383)]
woodPoints=[v.co.copy() for v in wood.data.vertices]
for ci,(anchor,m) in enumerate(anchors):
 c,rad=m;outward=(anchor-c).normalized();up=(outward*.35+Vector((0,0,.85))).normalized();u=up.cross(Vector((0,1,0))).normalized();v=up.cross(u)
 # Fan axis is a shoot; short lateral twigs carry leaves in alternating pairs.
 axis=(outward*.45+u*rng.uniform(-.3,.3)+Vector((0,0,.18))).normalized();start=anchor-axis*.32;end=anchor+axis*.32
 nearest=min(woodPoints,key=lambda p:(p-start).length_squared)
 if ci%16==0:tube(nearest,start,.013)
 if ci%8==0:tube(start,end,.008)
 for j in range(per):
  t=(j+.5)/per;side=1 if j%2 else -1;base=start.lerp(end,t);spread=.10+.22*math.sin(t*math.pi);tip=base+u*side*spread+v*rng.uniform(-.12,.12)+Vector((0,0,rng.uniform(-.08,.10)))
  if ci%16==0 and j%6==0:tube(base,tip,.0035)
  normal=(up*.84+outward*.10+Vector((rng.uniform(-.16,.16),rng.uniform(-.16,.16),rng.uniform(-.06,.10)))).normalized()
  longAxis=(tip-base).normalized();longAxis=(longAxis-normal*longAxis.dot(normal)).normalized();short=normal.cross(longAxis).normalized()
  length=rng.uniform(.72,.94);width=length*rng.uniform(.68,.88);center=tip+longAxis*length*.3
  points=[center+short*x*width+longAxis*y*length+normal*(.003 if x*y>0 else -.003) for x,y in ((-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5))]
  # Shallow envelope clipping keeps the authored crown rather than inflating it.
  for p in points:
   rel=p-c;rr=math.sqrt(metric(p,m));limit=1.02
   if rr>limit:p[:]=c+rel*(limit/rr)
   for k in range(3):p[k]=min(high[k],max(low[k],p[k]))
  index=len(vv);vv.extend(points);ff.append(tuple(range(index,index+4)));rect=(0,0,1,1);uvs.extend([(rect[0],rect[1]),(rect[2],rect[1]),(rect[2],rect[3]),(rect[0],rect[3])]);depth=math.sqrt(metric(center,m));slots.append(0 if depth>.84 else 1 if depth>.65 else 2)
  for p in points:guides.append(guide(p,m,anchor,up));clusterGuides.append((*p,*up))
# Dark inner cards maintain volume while the new organized sprays define the surface.
for p in original.polygons:
 center=sum((original.vertices[i].co for i in p.vertices),Vector())/4;m=min(masses,key=lambda m:metric(center,m))
 if metric(center,m)>.45 or rng.random()>.45:continue
 index=len(vv);points=[original.vertices[i].co.copy() for i in p.vertices];vv.extend(points);ff.append(tuple(range(index,index+4)));uvs.extend(original.uv_layers.active.data[i].uv.copy() for i in p.loop_indices);slots.append(2)
 for pt in points:guides.append(guide(pt,m,center,Vector((0,0,1))));clusterGuides.append((*pt,0,0,1))
mesh=bpy.data.meshes.new('Layered branch-supported leaf sprays');mesh.from_pydata(vv,[],ff);mesh.update();[mesh.materials.append(m) for m in mats];uv=mesh.uv_layers.new()
for i,q in enumerate(uvs):uv.data[i].uv=q
for p in mesh.polygons:p.material_index=slots[p.index]
leaf.data=mesh
me=bpy.data.meshes.new('Leaf-bearing terminal shoots');me.from_pydata(wv,[],wf);me.update();me.materials.append(wood.data.materials[0]);shoot=bpy.data.objects.new('Leaf bearing branch shoots',me);bpy.context.collection.objects.link(shoot)
uv=me.uv_layers.new()
for p in me.polygons:
 p.use_smooth=True
 for li in p.loop_indices:q=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=((q.x+q.y)*.65,q.z/3)
# Keep major wood and previously-added branch fans, not scene ground/cameras.
objects=[wood,leaf,shoot]+[o for o in bpy.data.objects if o.name.startswith('Close secondary branch fans')]
bpy.ops.wm.save_as_mainfile(filepath=str(D/'A_Rounded_BranchSprays.blend'))
for label,gs,width in [('CrownGuides',guides,7),('SprayGuides',clusterGuides,6)]:
 with (A/(label+'.bytes')).open('wb') as f:
  f.write(struct.pack('<i',len(gs)))
  for g in gs:f.write(struct.pack('<'+str(width)+'f',*g))
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(A/'LOD0.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False)
counts={}
for ob in objects:ob.data.calc_loop_triangles();counts[ob.name]=len(ob.data.loop_triangles)
(A/'recipe.json').write_text(json.dumps(dict(seed=730421,clusters=len(anchors),leavesPerShoot=per,quads=len(ff),sprayLengthsMeters=[.72,.94],sprayWidthsRatio=[.68,.88],individualLeafLengthApproxMeters=[.14,.22],originalBounds=[list(low),list(high)],meshTriangles=counts,existingMajorMasses=recipe['foliageMasses'],source=str(D/'A_Rounded_BranchSprays.blend')),indent=2));print(counts,flush=True)
