"""Pack supplied opacity/detail channels for the summer forest Unity test; requires Pillow."""
from pathlib import Path
from PIL import Image,ImageOps
import shutil
r=Path(__file__).resolve().parents[1];o=r/'Assets/CityForge/Art/Trees/SummerForest';s=r/'SourceAssets/Trees/SummerForest/Original/maps'
for name,opacity,detail in [('Maple','Maple_leaf_opacity.jpg','Maple_leaf_mask.jpg'),('Linden','MT_PM_V32_Fagus_sylvatica_Leaf_04_opacity.jpg','MT_PM_V32_Fagus_sylvatica_Leaf_06_translucency.jpg'),('Elm','MT_PM_V32_Fagus_sylvatica_Leaf_04_opacity.jpg','MT_PM_V32_Fagus_sylvatica_Leaf_06_translucency.jpg')]:
 alpha=Image.open(s/opacity).convert('L')
 gray=Image.open(s/detail).convert('L').resize(alpha.size)
 # Approximate procedural Corona diffuse with a restrained green leaf + existing vein detail.
 base=ImageOps.colorize(gray,(100,120,69),(164,179,119)).convert('RGBA');base.putalpha(alpha);base.save(o/(name+'_Leaf.png'))
for name,src in [('Maple_Bark','K782db793-2294-4a60-9e7e-be43ab682968_Color.jpg'),('Broadleaf_Bark','MT_PM_V32_Celtis_australis_Trunk_02_albedo.jpg'),('Maple_Bark_Normal','MT_PM_V32_Aesculus_hippocastanum_Trunk_normal.jpg'),('Broadleaf_Bark_Normal','MT_PM_V32_Celtis_australis_Trunk_02_normal.jpg')]:shutil.copy2(s/src,o/(name+'.jpg'))
