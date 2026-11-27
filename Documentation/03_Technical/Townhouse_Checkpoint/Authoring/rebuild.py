import bpy,json,hashlib,math
import numpy as np
from pathlib import Path
from mathutils import Vector
SRC=Path('/Users/joelinstrum/Documents/CityForgeAssets/Buildings/Era - Art Deco/Art Deco Brick Townhouse 2/v004-Authenticity-Review')
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-art-deco-brick-townhouse-2-001/92dabbefe84443e2b59fb84934e9c45d/Recessed-Candidate');P.mkdir(exist_ok=True);(P/'Textures').mkdir(exist_ok=True)
rows=[]
def signature(o):return hashlib.sha256(repr((tuple(tuple(v) for v in o.matrix_world),tuple(tuple(v.co) for v in o.data.vertices),tuple(tuple(p.vertices) for p in o.data.polygons),tuple(tuple(l.uv) for uv in o.data.uv_layers for l in uv.data))).encode()).hexdigest()
def cubedata(name,boxes,material):
 vs=[];fs=[]
 for cx,cy,cz,dx,dy,dz in boxes:
  n=len(vs);vs.extend([(cx+a*dx/2,cy+b*dy/2,cz+c*dz/2) for a,b,c in [(-1,-1,-1),(-1,1,-1),(1,1,-1),(1,-1,-1),(-1,-1,1),(-1,1,1),(1,1,1),(1,-1,1)]])
  fs.extend([tuple(n+i for i in face) for face in [(0,1,2,3),(4,7,6,5),(0,4,5,1),(3,2,6,7),(0,3,7,4),(1,5,6,2)]])
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.materials.append(material);uv=me.uv_layers.new(name='UV0')
 for face in me.polygons:
  for li in face.loop_indices:
   v=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v.x*.5,v.z*.5)
 return me
for tier in ['Near','Medium','Far']:
 bpy.ops.wm.open_mainfile(filepath=str(SRC/(tier+'.blend')));col=bpy.data.collections['Townhouse-'+tier];bpy.context.view_layer.update()
 protected={o.name:signature(o) for o in col.all_objects if o.type=='MESH' and any(t in o.name for t in ['Stone','Keystone','Cornice','Entry','Stair','Roof'])}
 roots=[o for o in col.all_objects if o.type=='EMPTY' and (o.name.startswith(('Side1.Window','Side-1.Window','Front.Window','Rear.Window')))]
 assert len(roots)==32,(tier,len(roots))
 openings=[]
 for root in roots:
  if tier!='Far':frame=next(o for o in root.children_recursive if o.name.endswith('.Sash'))
  else:frame=next(o for o in root.children_recursive if '.Pane' in o.name)
  pts=[root.matrix_world.inverted()@o.matrix_world@v.co for o in ([frame] if tier!='Far' else [c for c in root.children_recursive if '.Pane' in c.name]) for v in o.data.vertices];x0,x1=min(v.x for v in pts),max(v.x for v in pts);z0,z1=min(v.z for v in pts),max(v.z for v in pts);w=x1-x0;h=z1-z0
  openings.append(dict(name=root.name,width=w,height=h,recess=.1016,frameWidth=.028,meetingRail=.022))
  if tier=='Far':
   # Punch actual openings into the formerly solid side/rear distant walls.
   wall=bpy.data.objects['Walls.Side1.Brick' if root.name.startswith('Side1.') else 'Walls.Side-1.Brick' if root.name.startswith('Side-1.') else 'Walls.Rear.Brick' if root.name.startswith('Rear.') else 'Walls.Front.Brick']
   cutter=bpy.data.objects.new('Window aperture cutter',cubedata('Aperture',[( (x0+x1)/2,.115,(z0+z1)/2,w,.65,h)],frame.data.materials[0]));col.objects.link(cutter);cutter.matrix_world=root.matrix_world
   mod=wall.modifiers.new('Genuine recessed window aperture','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter;bpy.context.view_layer.objects.active=wall;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
   # The card will be rebaked from the thinner recessed sash. Its physical plane is recessed too.
   for card in [c for c in root.children_recursive if c.type=='MESH' and '.Pane' in c.name]:
    card.data=card.data.copy()
    for v in card.data.vertices:v.co.y=.1016
   frame['recessMetres']=.1016
   continue
  material=frame.data.materials[0]
  # Full 28 mm sections at front AND back, rather than the prior front-edge-only taper.
  boxes=[(x0+.014,.1241,(z0+z1)/2,.028,.045,h),(x1-.014,.1241,(z0+z1)/2,.028,.045,h),((x0+x1)/2,.1241,z0+.014,w-.056,.045,.028),((x0+x1)/2,.1241,z1-.014,w-.056,.045,.028),((x0+x1)/2,.1241,(z0+z1)/2,w-.056,.045,.022)]
  if w>1.4:boxes.append(((x0+x1)/2,.1241,(z0+z1)/2,.028,.045,h-.056))
  frame.data=cubedata(frame.name+' · slender historic sash',boxes,material);frame['recessMetres']=.1016;frame['frameWidthMetres']=.028
  # Reposition glazing/interior together; keep furnishings and the original lit-room selection.
  for o in list(root.children_recursive):
   if o.type!='MESH' or o==frame:continue
   if any(k in o.name for k in ['Stone','BrickReturns','Keystone']):
    if 'BrickReturns' in o.name:o.hide_render=True;o.hide_viewport=True;o['omitNativeExport']=True
    continue
   o.data=o.data.copy()
   inv=o.matrix_world.inverted();world=root.matrix_world
   for v in o.data.vertices:
    local=root.matrix_world.inverted()@o.matrix_world@v.co
    local.y+=.1306
    if 'Glazing' in o.name:pass # Rebuild the glass to fit the new clear opening below.
    elif o.name.endswith('.Interior'):
     local.x*=1.10;local.z=(z0+z1)/2+(local.z-(z0+z1)/2)*1.025
    elif 'OpenCurtain' in o.name:local.x+=.028*(1 if local.x>0 else -1)
    elif 'RoomReveal' in o.name:local.x+=.028*(1 if local.x>0 else -1)
    elif 'RoomCeiling' in o.name or 'InteriorShelf' in o.name:local.x*=1.07
    v.co=inv@(world@local)
   o.data.update()
   if 'Glazing' in o.name:
    panes=[(x0+.028,x1-.028)] if w<1.4 else [(x0+.028,(x0+x1)/2-.014),((x0+x1)/2+.014,x1-.028)]
    oldmat=o.data.materials[0];o.data=cubedata(o.name+' · enlarged clear opening',[((lo+hi)/2,.1306,(z0+z1)/2,hi-lo,.012,h-.056) for lo,hi in panes],oldmat);o.data.transform(inv@world)
  # Extend the sill's existing seat into the aperture without changing the visible stone sill.
  seat=next((o for o in root.children_recursive if o.name.endswith('.RebateSeat')),None)
  if seat:
   # Original seat is unchanged; a recessed bed joins it to the sash's lower edge.
   o=bpy.data.objects.new(root.name+'.RecessedSillBed',cubedata('Recessed sill bed',[((x0+x1)/2,.057,z0-.010,w,.126,.020)],seat.data.materials[0]));col.objects.link(o);o.parent=root
 # Reuse canonical brick on the genuine wall-hole faces, preserving exterior wall UVs exactly.
 revealFaces=0
 for wall in [o for o in col.all_objects if o.type=='MESH' and o.name.startswith('Walls.')]:
  axis=0 if '.Side' in wall.name else 1;sign=-1 if 'Side-1' in wall.name or '.Front' in wall.name else 1;plane=sign*3.6 if axis==0 else (-6.85 if sign<0 else 9.15)
  if not wall.data.uv_layers:wall.data.uv_layers.new(name='UV0')
  exterior=[]
  for face in wall.data.polygons:
   verts=[wall.matrix_world@wall.data.vertices[i].co for i in face.vertices]
   if all(abs(v[axis]-plane)<.004 for v in verts):
    for li in face.loop_indices:
     v=wall.matrix_world@wall.data.vertices[wall.data.loops[li].vertex_index].co;exterior.append(([v[1-axis],v.z,1],list(wall.data.uv_layers.active.data[li].uv)))
  if len(exterior)<3:continue
  A=np.array([a for a,b in exterior]);B=np.array([b for a,b in exterior]);fit=np.linalg.lstsq(A,B,rcond=None)[0];scale=float(np.linalg.norm(fit[0]));uv=wall.data.uv_layers.active
  for face in wall.data.polygons:
   vs=[wall.matrix_world@wall.data.vertices[i].co for i in face.vertices];center=sum(vs,Vector())/len(vs)
   for root in roots:
    p=root.matrix_world.inverted()@center;opening=next(a for a in openings if a['name']==root.name);half=opening['width']/2;z0=.22;z1=2.42
    if -.005<=p.y<=.235 and -half-.008<=p.x<=half+.008 and z0-.008<=p.z<=z1+.008 and (abs(abs(p.x)-half)<.009 or abs(p.z-z0)<.009 or abs(p.z-z1)<.009):
     # Do not touch exterior surface faces; only interior jamb/head/sill returns.
     if all(abs(v[axis]-plane)<.004 for v in vs):continue
     face.material_index=0;revealFaces+=1
     for li in face.loop_indices:
      v=wall.matrix_world@wall.data.vertices[wall.data.loops[li].vertex_index].co;lp=root.matrix_world.inverted()@v;base=np.array([v[1-axis],v.z,1])@fit;base[0]+=lp.y*scale;uv.data[li].uv=base
     break
 for n,h in protected.items():assert signature(bpy.data.objects[n])==h,(tier,n,'protected masonry/detail changed')
 missing=[im.name for im in bpy.data.images if im.source=='FILE' and im.users and not im.packed_file and not Path(bpy.path.abspath(im.filepath)).exists()];assert not missing,missing
 bpy.ops.wm.save_as_mainfile(filepath=str(P/(tier+'.blend')))
 rows.append(dict(tier=tier,openings=openings,revealFaces=revealFaces,protectedStoneCorniceEntryRoofMeshes=len(protected),missingTextures=missing))
(P/'Recess-Validation.json').write_text(json.dumps(rows,indent=2));print('REBUILT',[(r['tier'],len(r['openings']),r['revealFaces']) for r in rows])
