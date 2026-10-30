"""Compose unaltered reference/render thumbnails for an at-a-glance review."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageStat

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/Original/Foundry-Models-v1'
REF=ROOT/'ArtSource/Original/Foundry-Arena-v2/References-v1'
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',24)
for path in (OUT/'Renders').glob('*.png'):
    with Image.open(path) as frame:
        frame.load()
        # Thin edge caps occupy little of the frame; an empty world is uniform.
        assert max(ImageStat.Stat(frame.convert('RGB')).stddev)>1, f'Empty review render: {path}'


def comparison(name,reference,render):
    canvas=Image.new('RGB',(1920,800),'#101a20')
    draw=ImageDraw.Draw(canvas)
    for i,(path,label) in enumerate(((reference,'REFERENCE'),(render,'MODELED REVIEW CANDIDATE'))):
        image=Image.open(path).convert('RGB')
        image=ImageOps.contain(image,(940,720),Image.Resampling.LANCZOS)
        canvas.paste(image,(i*960+(960-image.width)//2,60+(720-image.height)//2))
        draw.text((i*960+24,15),label,font=font,fill='#e5e7df')
    canvas.save(OUT/f'{name}.png')


comparison('Arena-Comparison',REF/'Concepts/08-Arena-Art-Overview.png',OUT/'Renders/Arena-SW.png')
comparison('Measured-Comparison',REF/'Measured/Overview-SW.png',OUT/'Renders/Arena-SW.png')
comparison('Pad-Comparison',REF/'Concepts/03-Jump-Pad.png',OUT/'Renders/PAD-Studio.png')
comparison('Portal-Comparison',REF/'Concepts/04-Portal.png',OUT/'Renders/PORTAL-Studio.png')
for name, concept, asset in (
    ('Floor-Comparison', '01-Structural-Kit.png', 'FLOOR-4'),
    ('Ramp-Comparison', '01-Structural-Kit.png', 'RAMP'),
    ('Cover-Comparison', '02-Cover-Kit-v2.png', 'COVER-01'),
    ('Health-Comparison', '05-Health-Pickup.png', 'HEALTH'),
    ('Dressing-Comparison', '06-Dressing-Landmarks.png', 'VENT'),
):
    comparison(name, REF/'Concepts'/concept, OUT/'Renders'/f'{asset}-Studio.png')
print('Created nine side-by-side PNG comparison boards')
