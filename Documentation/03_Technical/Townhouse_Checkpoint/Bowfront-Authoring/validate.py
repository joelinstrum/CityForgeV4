import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-victorian-bowfront-brownstone-001/b72038fc22c944eeaa22f8c44e0b94d6/Revision')
def digest(o):
 return hashlib.sha256(str(([tuple(round(a,6) for a in v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],[list(r) for r in o.matrix_world])).encode()).hexdigest()
def protected():return {o.name:digest(o) for o in bpy.data.collections['01 — Townhouse composition'].objects if o.type=='MESH' and (any(x in o.name.lower() for x in ['cornice','roof','parapet','bay'])or o.name.startswith('Continuous brownstone crown'))}
bpy.ops.wm.open_mainfile(filepath='/Users/joelinstrum/Documents/CityForgeAssets/Buildings/Era - Art Deco/Victorian Bow-front Brownstone/v005-Olive-Cap/Bowfront-Composition.blend');before=protected()
bpy.ops.wm.open_mainfile(filepath=str(P/'Bowfront-Composition.blend'));after=protected();assert before==after,[(k,after.get(k)) for k in before if before[k]!=after.get(k)]
obs=[o for o in bpy.data.collections['01 — Townhouse composition'].objects if o.type=='MESH'];missing=[i.filepath for i in bpy.data.images if i.source=='FILE' and not i.packed_file and not Path(bpy.path.abspath(i.filepath)).exists()];assert not missing
points=[o.matrix_world@Vector(v) for o in obs for v in o.bound_box];bounds=[[min(p[k] for p in points),max(p[k] for p in points)] for k in range(3)]
assert bounds[0][0]>=-4.01 and bounds[0][1]<=4.01 and bounds[1][0]>=-10.01 and bounds[1][1]<=10.01
coping=[o for o in obs if o.name.startswith('Chimney ') and '/ stone coping' in o.name]
assert len(coping)==2 and all('User — Warm reddish brownstone blocks'==o.data.materials[0].name for o in coping)
report=dict(nonEmissiveCopingVerified=True,protectedGeometryUnchanged=True,protectedObjects=list(before),missingImages=missing,bounds=bounds,meshObjects=len(obs),nativeValidation='Preparation-Checks.txt',limitations=['Near/Medium exceed current city-scale budgets','Far tier not added by this architectural refinement','No gameplay save/load or runtime door interaction certification'])
(P/'Refinement-Validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
