from pathlib import Path
from PIL import Image, ImageDraw
import json, math
root=Path(__file__).resolve().parents[1]; out=root/'RailArena/Assets/RailArena/Art/PrecisionCarbine'
atlas=json.loads((root/'ArtSource/Original/PrecisionCarbine-Production-v1/SurfaceAtlas.json').read_text()); grid=9; size=2048
mask=Image.new('RGBA',(size,size),(255,255,255,0)); draw=ImageDraw.Draw(mask)
for tile in atlas['tiles']:
 x=tile['column']; y=grid-1-tile['row']; alpha=120 if tile['palette']==3 else 0
 draw.rectangle((round(x*size/grid),round(y*size/grid),round((x+1)*size/grid)-1,round((y+1)*size/grid)-1),fill=(255,255,255,alpha))
mask.save(out/'MetalDetailMask.png')
n=256; normal=Image.new('RGB',(n,n)); pixels=normal.load()
def height(x,y):
 a=(x%n)*math.tau/n; b=(y%n)*math.tau/n
 return .02*math.sin(a*19+b*3)+.015*math.cos(a*37-b*11)+.01*math.sin(a*7+b*23)
for y in range(n):
 for x in range(n):
  dx=(height(x+1,y)-height(x-1,y))*2; dy=(height(x,y+1)-height(x,y-1))*2; length=math.sqrt(dx*dx+dy*dy+1)
  pixels[x,y]=(round((-.5*dx/length+.5)*255),round((-.5*dy/length+.5)*255),round((.5/length+.5)*255))
normal.save(out/'MetalMicroNormal.png')
print('Metal mask tiles:',sum(t['palette']==3 for t in atlas['tiles']))
