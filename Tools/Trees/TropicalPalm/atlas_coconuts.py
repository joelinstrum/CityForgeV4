"""Pack private source texture columns without modifying downloaded images."""
import os
from pathlib import Path
from PIL import Image
root=Path(os.environ['CITYFORGE_PROJECT'])/'SourceAssets/TropicalPalm/CoconutPack'
source=root/'Source/Texture';out=root/'Runtime';out.mkdir(parents=True,exist_ok=True)
diffuse=['trunk_10_dif.jpg','stalk_06_dif.jpg','stalk_04_dif.jpg','sheath_03_dif.jpg','leaf_21_dif.jpg','leaf_20_dif.jpg','stalk_07_dif.jpg','leaf_05_dif.jpg']
normal=[None,None,'stalk_01_nor.jpg','sheath_02_nor.jpg','leaf_21_nor.jpg','leaf_20_nor.jpg',None,'leaf_05_nor.jpg']
for name,files,color in [('Atlas.png',diffuse,(255,255,255)),('Normal.png',normal,(128,128,255))]:
 atlas=Image.new('RGB',(2048,2048),color)
 for slot,file in enumerate(files):
  im=Image.open(source/file).convert('RGB') if file else Image.new('RGB',(248,2048),color)
  im=im.resize((248,2048),Image.Resampling.LANCZOS)
  atlas.paste(im,(slot*256+4,0));atlas.paste(im.crop((0,0,1,2048)).resize((4,2048)),(slot*256,0));atlas.paste(im.crop((247,0,248,2048)).resize((4,2048)),(slot*256+252,0))
 atlas.save(out/name)
