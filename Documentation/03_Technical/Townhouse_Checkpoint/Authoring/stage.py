from pathlib import Path
from PIL import Image,ImageOps
import json,hashlib,shutil
P=Path('/Users/joelinstrum/Documents/CityForgeAssets/Work/cf-building-art-deco-brick-townhouse-2-001/92dabbefe84443e2b59fb84934e9c45d/Recessed-Candidate');E=P/'Native-Export';T=E/'Textures';N=Path('/Users/joelinstrum/Documents/Codex/2026-11-27/files-pasted-by-the-user-cityforge/work/facade-weathering/UnityQA/Assets/CityForge/Development/RecessedWindowsReview/CandidateAssets');N.mkdir(parents=True,exist_ok=True);old=Path('/Users/joelinstrum/dev/CityForge - V4/Assets/CityForge/Resources/Lots/RebuiltBuildings/BrickTownhouse02');r=json.loads((E/'Recipe.json').read_text())
for m in r['materials']:
 for role in ['albedo','normal','emission']:
  if role not in m:continue
  f=m[role];src=T/f if (T/f).exists() else old/f;assert src.exists(),f
  if not (old/f).exists() or (old/f).read_bytes()!=src.read_bytes():shutil.copy2(src,N/f)
 if 'roughnessMap' in m or 'metallicMap' in m:
  files=[Image.open(T/m[k]).convert('L') for k in ['roughnessMap','metallicMap'] if k in m];size=max((im.size for im in files),key=lambda z:z[0]*z[1]);rough=Image.open(T/m['roughnessMap']).convert('L').resize(size) if 'roughnessMap' in m else Image.new('L',size,round(m['roughness']*255));metal=Image.open(T/m['metallicMap']).convert('L').resize(size) if 'metallicMap' in m else Image.new('L',size,round(m['metallic']*255));zero=Image.new('L',size,0);im=Image.merge('RGBA',(metal,zero,zero,ImageOps.invert(rough)));name='PackedPBR-'+hashlib.sha256(im.tobytes()).hexdigest()[:14]+'.png';m['metallicGloss']=name
  if not (old/name).exists():im.save(N/name)
 if 'FarWindow' in m['sourceName']:
  src=(N/m['albedo']) if (N/m['albedo']).exists() else old/m['albedo'];im=Image.open(src).convert('RGB');mask=Image.new('L',im.size);mask.putdata([round(max(0,min(1,(66-max(rgb))/14))*255) for rgb in im.getdata()]);name=Path(m['albedo']).stem+'-FrameMask.png';mask.save(N/name);m['frameMask']=name
for tier in ['Near','Medium','Far']:shutil.copy2(E/(tier+'.fbx'),N/(tier+'.fbx'))
(N/'Recipe.json').write_text(json.dumps(r,indent=2));print('Candidate assets staged; original maps reused read-only. Counts',[(t['tier'],t['triangles']) for t in r['tiers']])
