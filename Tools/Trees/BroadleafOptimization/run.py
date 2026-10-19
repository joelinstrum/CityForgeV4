"""Offline broadleaf geometry build. Does not edit source trees or gameplay assets."""
import argparse,os,subprocess
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--blender',default='/Applications/Blender.app/Contents/MacOS/Blender');p.add_argument('--project',type=Path,default=Path(__file__).resolve().parents[3]);p.add_argument('--source',type=Path);p.add_argument('--work',type=Path);p.add_argument('--preview',action='store_true');a=p.parse_args()
env=dict(os.environ);env['CITYFORGE_PROJECT']=str(a.project.resolve())
if a.source:env['CITYFORGE_V3']=str(a.source.resolve())
if a.work:env['CITYFORGE_OPTIMIZATION_WORK']=str(a.work.resolve())
for stage in ['count','optimize','angel-optimize','coverage','trunks','branches','ground','combine','validate']+(['preview'] if a.preview else []):
 subprocess.run([a.blender,'--factory-startup','-b','-t','2','--python-exit-code','1','--python',str(Path(__file__).with_name(stage+'.py'))],env=env,check=True)
