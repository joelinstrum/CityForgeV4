"""Original procedural broadleaf cluster atlas; no vendor foliage images."""
from PIL import Image,ImageDraw
import math,random
from pathlib import Path
r=Path(__file__).resolve().parents[1];out=r/'Assets/CityForge/Art/Trees/CityForgeBroadleaf';out.mkdir(parents=True,exist_ok=True)
n=1024;im=Image.new('RGBA',(n,n));d=ImageDraw.Draw(im);rng=random.Random(730421)
d.line([(480,900),(520,130)],fill=(71,89,41,255),width=8)
for i,(cx,cy,angle) in enumerate([(400,770,-.7),(610,740,.7),(350,590,-.85),(680,540,.8),(390,400,-.6),(620,320,.55),(505,165,0),(240,470,-1.1),(780,410,1.1),(310,230,-.75),(730,170,.7)]):
 length=rng.uniform(190,240);width=length*.43;axis=(math.sin(angle),-math.cos(angle));side=(math.cos(angle),math.sin(angle));base=(cx-axis[0]*length*.48,cy-axis[1]*length*.48)
 color=(int(rng.uniform(102,125)),int(rng.uniform(140,165)),int(rng.uniform(76,99)),255)
 for stripe in range(16):
  # Smooth pointed, broad leaf shape with subtle bands of albedo detail.
  points=[]
  for sign in (1,-1):
   indices=range(stripe,stripe+2) if sign==1 else range(stripe+1,stripe-1,-1)
   for k in indices:
    t=k/16;w=width*math.sin(math.pi*t)**.8;points.append((base[0]+axis[0]*length*t+side[0]*w*sign,base[1]+axis[1]*length*t+side[1]*w*sign))
  f=.9+.1*math.sin(stripe*.7);d.polygon(points,fill=tuple(int(c*f) for c in color[:3])+(255,))
 d.line([base,(base[0]+axis[0]*length*.95,base[1]+axis[1]*length*.95)],fill=(109,145,80,255),width=3)
 for t in (.25,.42,.6,.75):
  for sign in (-1,1):
   a=(base[0]+axis[0]*length*t,base[1]+axis[1]*length*t);b=(base[0]+axis[0]*length*(t+.12)+side[0]*width*math.sin(math.pi*t)*sign*.85,base[1]+axis[1]*length*(t+.12)+side[1]*width*math.sin(math.pi*t)*sign*.85);d.line([a,b],fill=(100,136,72,255),width=2)
im.save(out/'Forge_Leaf.png')
# Original bark texture with irregular longitudinal ridges.
bark=Image.new('RGB',(512,512));pixels=bark.load();rng=random.Random(42);offset=[rng.uniform(-12,12) for _ in range(512)]
for y in range(512):
 for x in range(512):
  v=65+18*math.sin(x*.20+math.sin(y*.045)*.8)+8*math.sin(x*.57+y*.008)+offset[x]+rng.uniform(-3,3);pixels[x,y]=(int(v*1.04),int(v*.85),int(v*.65))
bark.save(out/'Forge_Bark.png')
