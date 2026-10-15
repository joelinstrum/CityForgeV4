"""Shading metadata only. Reads frozen blend/mass recipe; never saves or exports geometry."""
import bpy,json,struct,math
from pathlib import Path
from mathutils import Vector
r=Path(__file__).resolve().parents[1];folder=r/'SourceAssets/Trees/CityForgeBroadleaf/Refinement01';bpy.ops.wm.open_mainfile(filepath=str(folder/'Broadleaf_Refinement01.blend'))
masses=json.loads((folder/'recipe_geometry.json').read_text())['foliageMasses'];masses=[(Vector(m['center']),Vector(m['radii'])) for m in masses]
obj=bpy.data.objects['Irregular branch organized foliage masses'];records=[]
for vertex in obj.data.vertices:
 p=vertex.co;best=1e10;chosen=None
 for c,rad in masses:
  d=p-c;q=Vector((d.x/rad.x,d.y/rad.y,d.z/rad.z));dist=q.length_squared
  if dist<best:best=dist;chosen=(d,rad)
 d,rad=chosen;n=Vector((d.x/(rad.x*rad.x),d.y/(rad.y*rad.y),d.z/(rad.z*rad.z))).normalized()
 t=max(0,min(1,(math.sqrt(best)-.45)/.50));exposure=t*t*(3-2*t)
 records.append((*p,*n,exposure))
a=r/'Assets/CityForge/Art/Trees/FrozenBroadleafEvaluation/Frozen_CrownLightingGuides.bytes'
with a.open('wb') as f:
 f.write(struct.pack('<i',len(records)))
 for record in records:f.write(struct.pack('<7f',*record))
print('Wrote',len(records),'position/lighting-normal/exposure records; frozen source unchanged')
