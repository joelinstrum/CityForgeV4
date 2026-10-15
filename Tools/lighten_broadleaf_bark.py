import bpy,json,hashlib
from pathlib import Path
p=Path('/Users/joelinstrum/dev/CityForge - V4');src=p/'SourceAssets/Trees/CityForgeBroadleaf/Refinement01/Broadleaf_Refinement01.blend';dst=src.parent/'BarkBrightness01';dst.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(src))
changes=[]
for o in bpy.data.objects:
 if o.type=='MESH':
  for m in o.data.materials:
   if m and ('bark' in m.name.lower() or 'wood' in m.name.lower()):
    for n in m.node_tree.nodes:
     if n.type=='BSDF_PRINCIPLED':
      old=list(n.inputs['Base Color'].default_value);n.inputs['Base Color'].default_value=(.26,.235,.205,1);changes.append({'object':o.name,'material':m.name,'before':old,'after':[.26,.235,.205,1]})
assert changes,'No bark material found'
bpy.ops.wm.save_as_mainfile(filepath=str(dst/'Broadleaf_BarkBrightness01.blend'))
(dst/'material_changes.json').write_text(json.dumps(changes,indent=2));print(changes)
