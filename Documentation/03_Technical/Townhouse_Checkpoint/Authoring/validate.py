import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-art-deco-brick-townhouse-2-001/92dabbefe84443e2b59fb84934e9c45d/Recessed-Candidate');S=Path('/Users/joelinstrum/Documents/CityForgeAssets/Buildings/Era - Art Deco/Art Deco Brick Townhouse 2/v004-Authenticity-Review');rows=[]
def sig(o):return hashlib.sha256(repr((tuple(tuple(v) for v in o.matrix_world),tuple(tuple(v.co) for v in o.data.vertices),tuple(tuple(p.vertices) for p in o.data.polygons),tuple(tuple(l.uv) for uv in o.data.uv_layers for l in uv.data))).encode()).hexdigest()
for tier in ['Near','Medium','Far']:
 bpy.ops.wm.open_mainfile(filepath=str(S/(tier+'.blend')));col=bpy.data.collections['Townhouse-'+tier];protected={o.name:sig(o) for o in col.all_objects if o.type=='MESH' and not o.name.startswith(('Side1.Window','Side-1.Window','Front.Window','Rear.Window','Walls.'))};rootTransforms={o.name:tuple(tuple(v) for v in o.matrix_world) for o in col.all_objects if o.type=='EMPTY'}
 bpy.ops.wm.open_mainfile(filepath=str(P/(tier+'.blend')));bpy.context.view_layer.update();col=bpy.data.collections['Townhouse-'+tier]
 assert all(sig(bpy.data.objects[n])==s for n,s in protected.items()),tier+' unrelated mesh changed'
 assert all(tuple(tuple(v) for v in bpy.data.objects[n].matrix_world)==s for n,s in rootTransforms.items()),tier+' component moved'
 records=[]
 for root in col.all_objects:
  if root.type!='EMPTY' or not root.name.startswith(('Side1.Window','Side-1.Window','Front.Window','Rear.Window')):continue
  meshes=[o for o in root.children_recursive if o.type=='MESH' and (o.name.endswith('.Sash') if tier!='Far' else '.Pane' in o.name)]
  depths=[(root.matrix_world.inverted()@o.matrix_world@v.co).y for o in meshes for v in o.data.vertices]
  assert abs(min(depths)-.1016)<1e-5,(tier,root.name,min(depths))
  if tier!='Far':assert abs(max(depths)-.1466)<1e-5
  records.append({'window':root.name,'frontRecessMetres':min(depths),'backDepthMetres':max(depths)})
 assert len(records)==32
 assert all(all(float('-inf')<c<float('inf') for c in v.co) for o in col.all_objects if o.type=='MESH' for v in o.data.vertices)
 missing=[im.name for im in bpy.data.images if im.users and im.source=='FILE' and not im.packed_file and not Path(bpy.path.abspath(im.filepath)).exists()];assert not missing
 rows.append({'tier':tier,'windows':records,'protectedUnrelatedMeshes':len(protected),'componentTransformsUnchanged':len(rootTransforms),'missingImages':missing})
(P/'Final-Geometry-Validation.json').write_text(json.dumps(rows,indent=2));print('PASS exact 101.6mm recess on all 32 flat openings in all 3 LODs; unrelated geometry and roots preserved')
