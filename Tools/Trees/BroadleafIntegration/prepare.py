import json,re,shutil
from pathlib import Path

import os
_PROJECT=Path(os.environ.get('CITYFORGE_PROJECT',str(Path(__file__).resolve().parents[3])))
_SOURCE=Path(os.environ.get('CITYFORGE_V3',str(_PROJECT.parent/'CityForge - V3')))
_WORK=Path(os.environ.get('CITYFORGE_OPTIMIZATION_WORK',str(_PROJECT/'SourceAssets/OptimizedBroadleaf/Build')))
(_WORK/'work/broadleaf-integration').mkdir(parents=True,exist_ok=True)
from PIL import Image
ws=_WORK;project=_PROJECT;art=project/'Assets/CityForge/Art/Trees/ForestBroadleaf';art.mkdir(parents=True,exist_ok=True)
items=json.loads((ws/'work/broadleaf-integration/inventory.json').read_text());recipe={'models':[],'materials':[]};made=set();errors=[]
heights=[18,16,14,13,16,18,18,18];species=list(dict.fromkeys(i['species'] for i in items))
def relative(p):return str(p.relative_to(project))
for item in items:
 name=item['species'];season=item['season'];directory=art/name/season;directory.mkdir(parents=True,exist_ok=True);model={'species':name,'season':season,'height':heights[species.index(name)],'files':[],'bindings':[]}
 for lod,path in enumerate(item['paths']):
  dest=directory/f'LOD{lod}.fbx';shutil.copy2(path,dest);model['files'].append(relative(dest))
 source=Path(item['source']);textures=source.parent/'textures'
 if name.startswith('OakPilot'):textures=source.parent.parent/({'Summer':'summer','Fall':'autumn','Winter':'winter'}[season])
 for raw in sorted(set(re.sub(r'\.\d+$','',m) for m in item['materials'])):
  stem=raw.removesuffix('_Mat');normal=None;opacity=None
  if name=='AngelOak':
   folder=(_SOURCE/'Assets/CityForgeV3/Resources/CityForgeV3/Flora/TreeRepairsV01/AngelTextures');n=1 if '#18' in raw else 2 if '#16' in raw else 0
   color=folder/f'AngelMaterial{n}-color.jpeg';opacity=folder/f'AngelMaterial{n}-opacity.jpeg'
  else:
   color=textures/(stem+'.png');normal=textures/(stem+'_Normal.png');opacity=textures/(stem+'_Opacity.png')
  if not color.exists():errors.append(str(color));continue
  safe=re.sub(r'[^a-zA-Z0-9_-]','_',stem);mat=directory/(safe+'.mat');base=directory/(safe+'_Albedo.png');bump=directory/(safe+'_Normal.png')
  image=Image.open(color).convert('RGBA')
  if opacity.exists():
   mask=Image.open(opacity).convert('L');mask=mask.resize(image.size,Image.Resampling.LANCZOS);image.putalpha(mask)
  cutout=image.getchannel('A').getextrema()[0]<250
  if name.startswith('SilverMaple') and stem.startswith('GreenLeaf'):
   hsv=image.convert('RGB').convert('HSV');h,s,v=hsv.split();h=h.point(lambda x:min(85,max(58,64+x*.16)));s=s.point(lambda x:min(220,int(x*.8)));v=v.point(lambda x:int(x*.8));green=Image.merge('HSV',(h,s,v)).convert('RGBA');green.putalpha(image.getchannel('A'));image=green
  image.save(base)
  normalpath=''
  if normal and normal.exists():shutil.copy2(normal,bump);normalpath=relative(bump)
  recipe['materials'].append({'path':relative(mat),'albedo':relative(base),'normal':normalpath,'cutout':cutout});model['bindings'].append({'source':raw,'material':relative(mat)})
 recipe['models'].append(model)
if errors:raise RuntimeError('Missing source textures:\n'+'\n'.join(errors))
(art/'Import.json').write_text(json.dumps(recipe,indent=2));print('PREPARED',len(recipe['models']),'source/season sets and',len(recipe['materials']),'texture mappings')
