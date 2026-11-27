from pathlib import Path
import json,shutil
from PIL import Image,ImageChops,ImageFilter
p=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-victorian-bowfront-brownstone-001/b72038fc22c944eeaa22f8c44e0b94d6/Native-Unity-Review');d=json.loads((p/'Materials-Source.json').read_text())
for m in d['materials']:
 rough=m['roughness'];metal=m['metallicMap'];m['metallicGloss']=''
 if rough or metal:
  src=Image.open(p/(rough or metal)).convert('RGB');size=src.size
  r=src.getchannel('G')if m['packedMetalRough'] else Image.open(p/rough).convert('L')if rough else Image.new('L',size,round(m['roughnessValue']*255))
  met=src.getchannel('B')if m['packedMetalRough']else Image.open(p/metal).convert('L').resize(size)if metal else Image.new('L',size,round(m['metallicValue']*255))
  name=m['name']+'-MetallicGloss.png';Image.merge('RGBA',(met,Image.new('L',size,0),Image.new('L',size,0),ImageChops.invert(r))).save(p/name);m['metallicGloss']=name
 if m.get('bowPaneMask'):
  im=Image.open(p/m['albedo']).convert('RGB');r,g,b=im.split();spread=ImageChops.subtract(ImageChops.lighter(r,ImageChops.lighter(g,b)),ImageChops.darker(r,ImageChops.darker(g,b)))
  neutral=spread.point(lambda x:255 if x<38 else 0);bright=im.convert('L').point(lambda x:255 if x>100 else 0);mask=ImageChops.multiply(neutral,bright).filter(ImageFilter.GaussianBlur(.4))
  emission=Image.merge('RGB',(mask,mask.point(lambda x:round(x*.55)),mask.point(lambda x:round(x*.22))));name=m['name']+'-Pane-Emission.png';emission.save(p/name);m['emission']=name;m['emissionValue']=[1,1,1,1]
 # Preserve both frame and fabric materials; emissive pane map has no opaque yellow card geometry.
(p/'Materials.json').write_text(json.dumps(d,indent=2))
project=Path('/Users/joelinstrum/dev/CityForge - V4/Assets/CityForge/Development/ArchitecturalRenderingLab/SubjectAssets/V006');project.mkdir(parents=True,exist_ok=True)
files={'Model.fbx','Distant.fbx','Materials.json'}
for m in d['materials']:
 for k in ['albedo','normal','metallicGloss','emission']:
  if m.get(k):files.add(m[k])
for name in files:shutil.copy2(p/name,project/name)
print('Staged',len(files),'private review files; triangles',d['nearTriangles'],d['mediumTriangles'])
