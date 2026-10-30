"""Create editable 2K flat-surface atlas maps for the faceted carbine."""
import json
from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[1];out=root/'ArtSource/Original/PrecisionCarbine-Production-v1';asset=root/'RailArena/Assets/RailArena/Art/PrecisionCarbine'
report=json.loads((out/'Production.json').read_text());palette=Image.open(root/'RailArena/Assets/RailArena/Art/CarbineReferenceReview/FoundryPalette.png').convert('RGB')
colors=[palette.getpixel((int((i+.5)*palette.width/8),palette.height//2)) for i in range(8)]
size=2048;grid=report['atlasGrid']
maps={'BaseColor':Image.new('RGBA',(size,size),(20,25,30,255)),'Normal':Image.new('RGB',(size,size),(128,128,255)),'MetallicSmoothness':Image.new('RGBA',(size,size),(0,0,0,90)),'Emission':Image.new('RGB',(size,size))}
metal=[20,170,35,205,0,0,30,0];smooth=[135,155,100,165,65,90,180,90]
for tile in report['tiles']:
    x=tile['column'];y=grid-1-tile['row'];index=tile['palette']
    rect=(round(x*size/grid),round(y*size/grid),round((x+1)*size/grid),round((y+1)*size/grid))
    ImageDraw.Draw(maps['BaseColor']).rectangle(rect,fill=colors[index]+(255,))
    ImageDraw.Draw(maps['MetallicSmoothness']).rectangle(rect,fill=(metal[index],0,0,smooth[index]))
    if index==6:ImageDraw.Draw(maps['Emission']).rectangle(rect,fill=(8,175,200))
for name,image in maps.items():image.save(asset/(name+'.png'))
(out/'SurfaceAtlas.json').write_text(json.dumps(dict(size=size,tiles=report['tiles'],colors=colors,metallic=metal,smoothness=smooth,normal='Geometry-authored bevels and facets; flat tangent-space texture',gutterPixels=8),indent=2)+'\n')
print('Four 2048px surface maps generated')
