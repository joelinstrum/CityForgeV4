"""Copy prepared summer elm meshes and pack vendor PBR channels for URP.
Run from any directory; requires Pillow. Then run CityForge/Import Siberian Elms in Unity.
"""
from pathlib import Path
from PIL import Image, ImageOps
import shutil
root=Path(__file__).resolve().parents[1]
source=root/'SourceAssets/Trees/SiberianElm'
output=root/'Assets/CityForge/Art/Trees/SiberianElm'
output.mkdir(parents=True,exist_ok=True)
for variant in ['A','Large','Young']:
    for lod in range(3):
        name=f'SiberianElm{variant}_LOD{lod}.fbx'
        shutil.copy2(source/'Optimized'/name,output/name)
for kind in ['Bark','Leaf']:
    prefix=source/'Original'/f'T_Siberian_Elm_{kind}'
    color=Image.open(str(prefix)+('_BC.jpg' if kind=='Bark' else '_D.jpg')).convert('RGBA')
    if kind=='Leaf': color.putalpha(Image.open(str(prefix)+'_OP.jpg').convert('L'))
    color.save(output/f'{kind}_Base.png')
    shutil.copy2(str(prefix)+'_N.jpg',output/f'{kind}_Normal.jpg')
    ao,roughness,metal_or_spec=Image.open(str(prefix)+('_ARM.jpg' if kind=='Bark' else '_ARS.jpg')).convert('RGB').split()
    metallic=metal_or_spec if kind=='Bark' else Image.new('L',color.size,0)
    Image.merge('RGBA',(metallic,ao,Image.new('L',color.size,0),ImageOps.invert(roughness))).save(output/f'{kind}_Mask.png')
