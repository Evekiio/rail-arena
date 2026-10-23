"""Generate the review register and local gallery without touching runtime assets."""
import hashlib
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / 'ArtSource/Original/Design-v1'
PACK.mkdir(parents=True, exist_ok=True)
ASSETS = []


def add(identifier, family, purpose, dimensions, views, materials, states, collision,
        references, status='awaiting-creator-review', notes=''):
    existing = [p for p in references if (ROOT / p).is_file()]
    if not existing:
        status = 'blocked-missing-reference'
    ASSETS.append(dict(id=identifier, family=family, purpose=purpose,
                       dimensions=dimensions, requiredViews=views, materials=materials,
                       states=states, collisionRole=collision, references=existing,
                       intendedReferences=references, approvalStatus=status,
                       approvalEvidence=None, notes=notes))


def ref(name):
    return 'ArtSource/Original/Design-v1/Concepts/' + name + '.png'


six = ['front', 'back', 'left', 'right', 'top', 'bottom', 'three-quarter']
palette = ['ivory ceramic', 'graphite', 'teal', 'cyan emissive', 'copper']
for character in ('Player','Bot01','Bot02','Bot03'):
    folder='ArtSource/Original/StylizedContenders/References-v1/Final/'
    add('CHAR-'+character,'characters',character+' cosmetic body',
        {'heightMetres':1.8,'note':'Target; same gameplay capsule for every costume'},
        six+['head studies','palms','soles','costume detail'],
        palette+['skin','hair','cloth'],['idle','locomotion','hit','death','fade'],
        'Equal shared gameplay capsule/hurtbox specification; costume has no extra collision',
        [folder+character+'-'+suffix+'.png' for suffix in ('Turnaround','Head','Details','Axial')],
        notes='Original concept approved; expanded hidden surfaces and final geometry need review. Preserve fixed anatomical asymmetry from MODELING-GUIDE.md.')
    add('ARMS-'+character,'first-person',character+' glove/cuff appearance',
        {'forearmHandLengthMetres':.45,'note':'Fit shared rig and calibrated contact targets'},
        ['palm','back','inner cuff','outer cuff','grip contact'],palette+['skin','padded glove'],
        ['idle','movement','fire','zoom'],'presentation only; shared rig',
        [ref('FirstPerson-Arms')])

add('WEAPON-RIFLE','weapon','Approved enclosed precision carbine',
    {'lengthMetres':1.0,'widthMetres':.1,'heightMetres':.33,'note':'Approved envelope; integrated production asset'},
    six+['grip contact','muzzle marker','moving parts'],palette,
    ['idle','fire','36-tick recovery','zoom'],'presentation only',
    ['ArtSource/Original/PrecisionCarbine-References-v3/'+name for name in
     ('README.md','index.html','RailRifle-Six-View.png','Assembly-Dimensions.csv')],
    status='design-approved-integrated-human-acceptance-pending',
    notes='Creator approved October 23, 2026. Production asset, atlas, LODs and fitted shared weapon installed. See ArtSource/Original/PrecisionCarbine-Production-v1/README.md; human acceptance and performance qualification remain open.')

add('MAP-FOUNDRY','arena','One shared review blockout',
    {'footprintMetres':[56,40],'deckHeightsMetres':[0,2,4]},
    ['top','lower plan','upper plan','north','south','east','west','section','player-height'],
    ['semantic greybox colors'],['static'], 'Master gameplay geometry proposal; no runtime integration',
    ['ArtSource/Original/Foundry-Arena-v2/'+n for n in
     ('Master-Plan.svg','Lower-Deck.svg','Wing-Deck.svg','Upper-Deck.svg',
      'Section-EW-N8.svg','Section-NS-X0.svg','Map.json','Environment-Kit.json')],
    notes='Foundry v2 is current: duel-first, opaque shot-blocking gates, revised spawns/landings. V1 Blender master is historical. Actual-controller and creator layout approval remain open.')

architecture=[('FLOOR',[4,4,.4]),('WALL',[4,.4,4]),('LOW-COVER',[2,.4,1.2]),
              ('PYLON',[2,2,4]),('RAMP',[16,4,4]),('BRIDGE-BEAM',[4,.4,.8]),
              ('EDGE-CAP',[4,.4,.8]),('LANDING',[4,4,.6])]
for name,size in architecture:
    add('ARCH-'+name,'architecture',name.lower().replace('-',' ')+' module',
        {'boundingBoxMetres':size,'note':'Ramp uses 16m run / 4m rise; reconcile snap grid before detail lock'},
        six+['snap joints'],palette,['static'],'walkable/cover only where master specifies; trim cosmetic',
        [ref('Architecture-Resolved'),ref('Hidden-Construction-v2')],
        notes='Foundry v2 needs 16m assembly: 6.5m rise, 3m flat turnout, 6.5m rise. Existing illustrated 7/2/7 ramp is historical and must be revised before approval.')

props=[('CRATE',[2,1,1.2],'hard cover'),('CABINET',[1.4,.8,1.7],'hard cover'),
       ('VENT',[1,1,2.4],'cosmetic unless placed as explicit master cover'),
       ('CONDUIT',[1.2,.25,1.6],'cosmetic'),('FLOODLIGHT',[1,.2,.4],'cosmetic')]
for name,size,collision in props:
    add('PROP-'+name,'props',name.lower(),{'boundingBoxMetres':size},six,palette,['static'],collision,
        [ref('Props-Materials'),ref('Hidden-Construction-v2')],notes='SKIN material swatch is mislabelled teal paint; use approved character skin references.')
for name,size,purpose in [('HEALTH-PEDESTAL',[.8,.8,.25],'Low nonblocking health mount'),
                          ('HEALTH-OBJECT',[.35,.35,.5],'Amber plus-symbol collectible'),
                          ('PAD',[2,2,.35],'Recessed launch pad'),
                          ('PORTAL',[2.4,.8,2.8],'Shared portal geometry, two instances A/B')]:
    add('TRAV-'+name,'traversal',purpose,{'boundingBoxMetres':size,
        'clearApertureMetres':[1.4,2.2] if name=='PORTAL' else None},six+['parts breakdown'],
        palette+['amber health'],
        ['available','collected','returning'] if name.startswith('HEALTH') else
        (['idle','contact','launch'] if name=='PAD' else ['idle','departure','arrival','blocked']),
        'Separate trigger/clearance from cosmetic mesh; pad top flush',
        [ref('Traversal-Health-States')],notes='Two gate identifiers share one mesh. No extra gameplay portal pairs.')

for name,purpose in [('ISLAND','Distant floating landform'),('CITADEL','Distant industrial silhouette'),
                     ('SKY','Sky and cloud atmosphere'),('LIGHTING','Shared sunlight and exposure reference')]:
    add('WORLD-'+name,'world',purpose,{'scale':'background; not traversable','note':'Sky cubemap requires shared-scene rendering, not cropped concept panels'},
        ['hero','north','east','south','west','zenith','nadir'],palette+['clouds','rock'],['static daylight'],
        'none', ['ArtSource/Original/ArenaReferences-v1/08-World-Sky.png'])

for name,states in [('DISCHARGE',['start','peak','halo fade']),('RECOVERY',['fire','cooldown','ready']),
                    ('IMPACT',['spark','debris','small mark']),('HIT-BLOOD',['small burst','droplets','fade']),
                    ('HIT-NO-BLOOD',['spark','marker','fade']),('ELIMINATION',['fall','fade','gone']),
                    ('REGEN',['delay','active','cap']),('PICKUP',['available','collect','return']),
                    ('PAD',['idle','contact','impulse']),('PORTAL',['idle','depart','arrive','blocked']),
                    ('RESPAWN',['absent','fade-in','ready']),('AMBIENT',['vent','dissipate'])]:
    noncombat=name in ('REGEN','PICKUP','PAD','PORTAL','RESPAWN','AMBIENT')
    add('VFX-'+name,'effects',name.lower().replace('-',' '),
        {'scale':'Readability at actual player height; no fullscreen obstruction'},
        ['timed storyboard','layer breakdown','blood-off or reduced-flash variant'],
        ['transparent particles','emissive core','decal where appropriate'],states,'none',
        [ref('Support-Effects-v2')] if noncombat else [],
        notes='Combat/blood artwork remains unavailable after prior filtering; no missing sheet may pass approval.' if not noncombat else
        'Style sheet does not establish final timing; align events to authority and visibility settings.')

screens=['MAIN','CHARACTER','PRACTICE','DUEL-HOST','DUEL-JOIN','DUEL-READY','HUD',
         'PAUSE','SETTINGS','SCOREBOARD','RESULTS','ERRORS','QUICKSTART']
for name in screens:
    add('UI-'+name,'interface',name.lower().replace('-',' '),
        {'referencePixels':[1920,1080],'hudScaleRange':[.8,1.4]},
        ['full screen','keyboard focus','disabled','empty','error where applicable'],
        ['graphite panels','ivory text','cyan focus','non-color status labels'],
        ['normal','focused','disabled','empty','error'],'none',
        [ref('UI-HUD-Settings-v2' if name in ('HUD','PAUSE','RESULTS','ERRORS','QUICKSTART') else
             ('UI-Settings-Scoreboard' if name in ('SETTINGS','SCOREBOARD') else 'UI-Navigation'))],
        notes='Generated mockups require exact branding/portrait and value corrections against DESIGN.md; artwork is not runtime UI.')

for name in ('IDLE','WALK','RUN','STRAFE','JUMP','LAND','HOLD','FIRE','RECOVERY',
             'HIT','DEATH','RESPAWN','FP-IDLE','FP-MOVE','FP-FIRE','FP-ZOOM'):
    add('ANIM-'+name,'animation',name.lower(),{'rig':'shared humanoid; first-person contact rig'},
        ['key poses','side','front','contact or timing notes'],['shared character materials'],
        ['enter','key poses','exit'],'animation never changes authoritative damage/movement',
        [ref('Animation-Poses')],notes='Overview pose sheet is not full coverage. Rifle hold/fire/recovery and first-person clips need approved rifle geometry and additional contact poses.')

audio=['RIFLE-SHOT','RIFLE-READY','IMPACT-METAL','HIT-CONFIRM','ELIMINATION',
       'FOOTSTEP','JUMP','LAND','PAD','PORTAL-DEPART','PORTAL-ARRIVE','PORTAL-BLOCKED',
       'HEALTH','REGEN','RESPAWN','COUNTDOWN','MATCH-END','UI-FOCUS','UI-CONFIRM',
       'UI-ERROR','AMBIENCE','MENU-MUSIC']
for name in audio:
    add('AUDIO-'+name,'audio',name.lower(),
        {'durationSeconds':'loop' if name in ('AMBIENCE','MENU-MUSIC') else 'event-specific short cue'},
        ['event/mix brief; no model sheet required'],['audio bus: music' if name=='MENU-MUSIC' else 'audio bus: effects'],
        ['trigger','tail','loop boundary' if name in ('AMBIENCE','MENU-MUSIC') else 'end'],
        'none',['docs/DESIGN.md'],status='brief-approved-audio-production-pending',
        notes='No new audio produced by concept package. Separate sound-on approval required.')

register={'version':1,'design':'docs/DESIGN.md','approvedDesignDate':'2026-10-23',
          'runtimeDesignMigration':'pending; never infer from design approval',
          'approvalGate':'Explicit creator approval per family; missing required views fail gate',
          'assets':ASSETS}
for asset in ASSETS:
    family=asset['family']
    if family=='audio':
        missing=['sound production','sound-on approval']
    elif family=='weapon':
        missing=['human acceptance of installed visual/finger contact/animation','external duel acceptance and measured performance qualification']
    elif family=='arena':
        missing=['creator layout review','actual-controller traversal and visibility tests']
    elif family=='characters':
        missing=['hidden-surface approval','resolve cross-view geometry inconsistencies']
    elif family=='architecture':
        missing=['cross-sheet back/underside consistency review','master-aligned joint verification']
    elif family=='props':
        missing=['cross-sheet back/underside consistency review']
    elif family=='first-person':
        missing=['approved-rifle-specific support and trigger contacts']
    elif family=='traversal':
        missing=['consistent hidden/underside views','clearance and mounting verification']
    elif family=='world':
        missing=['shared-world geometry and seamless directional lighting verification']
    elif family=='animation':
        missing=['complete clip-specific key poses','contact/rig timing verification']
    elif family=='interface':
        missing=['final brand and portrait fidelity','screen-specific empty/error/focus review']
    elif family=='effects':
        missing=['layer breakdown and exact timing','blood-off/reduced-flash variants']
    else:
        missing=['new complete multi-view reference']
    asset['remainingRequirements']=missing
    asset['coverageStatus']='partial; see remainingRequirements'
    asset['detailedModelingAllowed']=family=='weapon'
    if family=='weapon':
        asset['approvalEvidence']='Explicit creator approval and production authorization in chat, October 23, 2026'
        asset['references'].append('ArtSource/Original/PrecisionCarbine-Production-v1/README.md')
        asset['coverageStatus']='Integrated production asset; acceptance gates remain'
(PACK/'AssetRegister.json').write_text(json.dumps(register,indent=2)+'\n',encoding='utf-8')
rows=['# Rail Arena asset review register','','Authority: [design](../../../docs/DESIGN.md). '
      'A sheet link means a reference exists, not that every required view is complete or approved. '
      'See AssetRegister.json for dimensions, states, required views and blockers.','',
      '| ID | Family | Status | Reference coverage |','| --- | --- | --- | --- |']
for asset in ASSETS:
    refs=' Ã‚Â· '.join(f'[{Path(p).stem}](../../../{p})' for p in asset['references']) or 'Missing / blocked'
    rows.append(f"| {asset['id']} | {asset['family']} | {asset['approvalStatus']} | {refs} |")
(PACK/'ASSET-REGISTER.md').write_text('\n'.join(rows)+'\n',encoding='utf-8')
images=sorted((PACK/'Concepts').glob('*.png'))+sorted((PACK/'Greybox').glob('*.png'))
images=[p for p in images if not p.with_stem(p.stem+'-v2').exists()]
gallery=['<!doctype html><meta charset="utf-8"><title>Rail Arena design review</title>',
         '<style>body{background:#121a22;color:#efe9d9;font:17px system-ui;max-width:1400px;margin:40px auto;padding:20px}a{color:#48dae8}img{width:100%;height:auto}section{margin:40px 0}h1{font-size:44px}</style>',
         '<h1>Rail Arena / Design v1</h1><p>Creator-review candidates. Design approved; final art, geometry and runtime acceptance remain separate.</p>',
         '<p><a href="../../../docs/DESIGN.md">Design lock</a> Ã‚Â· <a href="ASSET-REGISTER.md">Asset register</a></p>']
for p in images:
    relative=p.relative_to(PACK).as_posix()
    gallery.append(f'<section><h2>{html.escape(p.stem)}</h2><a href="{relative}"><img loading="lazy" src="{relative}" alt="{html.escape(p.stem)}"></a></section>')
gallery.append('<p>No game scenes or runtime code modified by this package generator.</p>')
(PACK/'index.html').write_text('\n'.join(gallery),encoding='utf-8')
print(f'Register: {len(ASSETS)} items; gallery: {len(images)} images')

if __name__=='__main__':
    ids=[a['id'] for a in ASSETS]
    assert len(ids)==len(set(ids))
    assert all(all((ROOT/p).is_file() for p in a['references']) for a in ASSETS)
