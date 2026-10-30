"""Prepare a scoped published README while preserving earlier local README edits."""
from pathlib import Path
import subprocess

root=Path(__file__).resolve().parents[2]
refs='ArtSource/Original/StylizedContenders/References-v1/Final'
banner='![Rail Arena logo and four contender concepts in a stylized arena](docs/branding/rail-arena-banner.png)\n\n*Concept brand art. The gameplay capture below shows a separate development milestone.*\n\n'
navigation='[Play and build](docs/BUILD-AND-PLAY.md) · [Character references](docs/CHARACTER-REFERENCES.md) · [Brand kit](docs/branding/README.md) · [Validation](docs/VALIDATION.md)\n\n'
characters='''## Character references

The approved visual direction is high-detail, low-poly sci-fi armor with distinct faces, hair and readable color palettes. The expanded pack provides **four sheets per character**: eight body directions, larger head studies, equipment details, and guided top/bottom concepts.

![Approved player and three bot character concepts](REFS/Approved-Concept.png)

| Character | Body · 8 views | Head · 6 studies | Equipment details | Top / bottom |
| --- | --- | --- | --- | --- |
'''.replace('REFS',refs)
for name,label in [('Player','Player · ivory / cyan'),('Bot01','Bot 01 · terracotta / amber'),('Bot02','Bot 02 · slate / ivory'),('Bot03','Bot 03 · plum / copper')]:
    characters+='| '+label+' | '+' | '.join(f'[{text}]({refs}/{name}-{kind}.png)' for kind,text in [('Turnaround','Turnaround'),('Head','Head'),('Details','Details'),('Axial','Top / bottom')])+' |\n'
characters+='\n[Browse all sixteen sheets and the modeling notes](docs/CHARACTER-REFERENCES.md). These are concept/modeling references; the new hidden-surface details remain review candidates. Exact geometric consistency and final model quality must be checked on the shared 3D masters. Runtime art can differ while production continues.\n\n'

def branded(text):
    text=text.replace('\r\n','\n')
    if 'docs/branding/rail-arena-banner.png' in text:
        raise ValueError('Branding has already been applied; avoid duplicate README sections.')
    if not text.startswith('# Rail Arena\n') or text.count('## Project\n')!=1:
        raise ValueError('Unexpected README structure.')
    text=banner+text
    text=text.replace('# Rail Arena\n\n','# Rail Arena\n\n'+navigation,1)
    return text.replace('## Project\n',characters+'## Project\n',1)

published=subprocess.check_output(['git','show','HEAD:README.md'],cwd=root).decode('utf-8')
local=(root/'README.md').read_text(encoding='utf-8')
(root/'Validation/Branding/README.publish.md').write_text(branded(published),encoding='utf-8')
(root/'README.md').write_text(branded(local),encoding='utf-8')

gallery=['# Rail Arena character reference gallery','',
         'Sixteen selected sheets expand the approved player and three bot concepts. These are high-detail low-poly modeling references, distinct from gameplay screenshots and final-model acceptance.','',
         '| Character | Turnaround | Head | Details | Top / bottom |','| --- | --- | --- | --- | --- |']
for name,label in [('Player','Player'),('Bot01','Bot 01'),('Bot02','Bot 02'),('Bot03','Bot 03')]:
    gallery.append('| '+label+' | '+' | '.join(f'[{kind}](../{refs}/{name}-{kind}.png)' for kind in ['Turnaround','Head','Details','Axial'])+' |')
gallery+=['','[Modeling guide and fixed left/right features](../'+refs+'/MODELING-GUIDE.md) · [Original approved concept](../'+refs+'/Approved-Concept.png) · [Prompts](../'+refs+'/PromptSet.json) · [Top/bottom prompts](../'+refs+'/Axial-Prompts.json)','',
          'The body sheets cover front, both front three-quarter views, both profiles, both rear three-quarter views and back. Additional sheets cover crown/under-jaw, glove palms and backs, boot inner/outer/top/sole, torso and belt construction, plus camera-guided whole-body poles.','',
          'The extended backs, palms, tread and underside transitions are proposed continuations of the approved concept. The drawings are not calibrated renders of one finished mesh. The top/bottom concepts use headless orthographic guides for camera/occlusion; a single final 3D master still needs to establish exact geometric agreement.','']
for name,label in [('Player','Player · ivory / cyan'),('Bot01','Bot 01 · terracotta / amber'),('Bot02','Bot 02 · slate / ivory'),('Bot03','Bot 03 · plum / copper')]:
    gallery+=['## '+label,'']
    for kind,title in [('Turnaround','Body turnaround · eight directions'),('Head','Large head studies'),('Details','Equipment and construction details'),('Axial','Guided top / bottom concepts')]:
        path='../'+refs+'/'+name+'-'+kind+'.png'
        gallery+=['<details>','<summary>'+title+'</summary>','',f'[![{label}: {title}]({path})]({path})','','</details>','']
(root/'docs/CHARACTER-REFERENCES.md').write_text('\n'.join(gallery),encoding='utf-8')
print('Prepared working README, scoped publication draft and GitHub character reference gallery.')
