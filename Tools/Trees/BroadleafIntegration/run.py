"""Prepare Unity intake from staged optimized broadleaf models; requires Pillow."""
import argparse,os,subprocess,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--blender',default='/Applications/Blender.app/Contents/MacOS/Blender');p.add_argument('--project',type=Path,default=Path(__file__).resolve().parents[3]);p.add_argument('--source',type=Path);p.add_argument('--work',type=Path);a=p.parse_args();env=dict(os.environ);env['CITYFORGE_PROJECT']=str(a.project.resolve())
if a.source:env['CITYFORGE_V3']=str(a.source.resolve())
if a.work:env['CITYFORGE_OPTIMIZATION_WORK']=str(a.work.resolve())
for stage in ['seasonal_pilots','inventory']:
 subprocess.run([a.blender,'--factory-startup','-b','-t','2','--python-exit-code','1','--python',str(Path(__file__).with_name(stage+'.py'))],env=env,check=True)
subprocess.run([sys.executable,str(Path(__file__).with_name('prepare.py'))],env=env,check=True)
print('Intake prepared. In Unity run CityForge/Import/Optimized Forest Broadleaf Trees, or write build to Library/BroadleafImport.request after compilation.')
