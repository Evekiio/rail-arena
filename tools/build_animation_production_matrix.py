"""Join current authored evidence to the unchanged Gate 1 action inventory."""
import csv
import json
from pathlib import Path
from art_shipping_evidence import shipping_followup

root=Path(__file__).resolve().parent.parent
art=root/'ArtSource/Original/ArtPass-v1'
with (art/'Gate1/animation-matrix.csv').open(newline='',encoding='utf-8-sig') as stream:rows=list(csv.DictReader(stream))
columns=list(rows[0])+['productionState','sourceMaster','measuredReferences','playback','validation','remainingEvidence']
installed={
    ('character','idle'):('Animation/Standing','Contender-Standing-v1.blend','Standing-Playback.mp4','StandingReady'),
    ('character','walk forward'):('Animation/Forward','Contender-Advance-v1.blend','Advance-Playback.mp4','AdvanceWalk'),
    ('character','walk backward'):('Animation','Contender-Retreat-v1.blend','Retreat-Playback.mp4','RetreatWalk'),
    ('character','strafe left'):('Animation/Strafe','Contender-Strafe-v1.blend','Strafe-Playback.mp4','StrafeLeftWalk'),
    ('character','strafe right'):('Animation/Strafe','Contender-Strafe-v1.blend','Strafe-Playback.mp4','StrafeRightWalk'),
    ('character','sprint'):('Animation/Forward','Contender-Advance-v1.blend','Advance-Playback.mp4','AdvanceRun'),
    ('character','jump start'):('Animation/Airborne','Contender-Airborne-v1.blend','Controller-Jumps.mp4','Takeoff'),
    ('character','fall loop'):('Animation/Airborne','Contender-Airborne-v1.blend','Controller-Jumps.mp4','AirborneHold'),
    ('character','landing'):('Animation/Airborne','Contender-Airborne-v1.blend','Controller-Jumps.mp4','LandingRecover'),
    ('character','hit reaction'):('Animation/Reactions','Contender-Reactions-v1.blend','Reaction-Playback.mp4','HitResponse'),
    ('character','death'):('Animation/Reactions','Contender-Reactions-v1.blend','Reaction-Playback.mp4','DeathFall'),
}
for row in rows:
    row.update(productionState='remaining-production',sourceMaster='',measuredReferences='',playback='',validation='',remainingEvidence='Original Gate 1 requirements remain open unless evidence below explicitly covers them. Sanders native/artistic acceptance pending.')
    spec=installed.get((row['group'],row['action']))
    if spec:
        directory,master,video,clip=spec
        files=[f'{directory}/{master}',f'{directory}/References/{clip}/measured-poses.png',f'{directory}/{video}',f'{directory}/review-evidence.json']
        for file in files:assert (art/file).is_file(),file
        row.update(productionState='integrated-review',sourceMaster=files[0],measuredReferences=files[1],playback=files[2],validation=files[3],currentEvidence='Original authored IK/bone motion installed in optional Sky Gardens art profile; shared rig and simulation retained.',remainingEvidence='All movement/stance/traversal combinations, rear motion views, native sound/input and final artistic acceptance remain open.')
    if row['group']=='character' and row['action']=='idle':
        row.update(currentEvidence='Original four-second standing-ready loop integrated at zero velocity in the Sky Gardens graph; 964 candidate and 964 installed samples across four identities preserve planted feet, wrist contacts, actor roots and standing capsule. Shipping build pending; Review22 remains current.',remainingEvidence='Idle/movement/stance/combat interruption blends, moving-world contact and native artistic acceptance remain open.')
    if row['group']=='first-person' and row['action']=='magazine reload':
        row.update(productionState='integrated-review',sourceMaster='Gate2/RifleActions/Carbine-Reload-v1.blend',playback='Gate2/RifleActions/index.html',validation='Gate2/RifleActions/ReloadAction.json',currentEvidence='Original two-second reload, magazine/wrist contacts and interruption/reset review integrated; see action package.',remainingEvidence='Complete first-person action library and native contact/audio/artistic acceptance remain open.')
    if row['group']=='first-person' and row['action']=='ready/equip':
        row.update(productionState='integrated-review',sourceMaster='Animation/FirstPersonReady/Carbine-FirstPerson-Ready-v1.blend',measuredReferences='Animation/FirstPersonReady/Ready-Measured-Sheet.png',playback='Animation/FirstPersonReady/Ready-Before-After.mp4',validation='Animation/FirstPersonReady/review-evidence.json',currentEvidence='Original 0.3-second sampled Blender rifle raise integrated in the optional art profile; 720 player-eye hip/ADS/ADS-entry samples preserve wrist contacts, physical sight alignment, actor state, ammunition and shot timing. Reset and profile removal restore rest/legacy behavior. Shipping milestone pending.',remainingEvidence='Assembled Blender arm/weapon pose references, full first-person action library, magazine/fire interruptions in active play, palm/finger surface and native input/audio/artistic acceptance remain open.')
    if row['group']=='character' and row['action'] in ('crouch idle','crouch movement','crouch enter/exit'):
        row.update(productionState='authored-candidate',sourceMaster='Animation/Crouch/Contender-Crouch-v1.blend',measuredReferences='Animation/Crouch/index.html',validation='Animation/Crouch/baked-contact-review.json',currentEvidence='Seven original source actions, all-bound-mesh Blender floor/contact checks and valid humanoid imports. Not referenced by the live controller.',remainingEvidence='Native all-avatar retarget/grip/stance/transition/hitbox and moving-world foot contacts; prevent double lowering with existing procedural crouch. Normal-speed installed playback and Sanders acceptance remain pending.')
        if row['action']!='crouch enter/exit':
            row.update(productionState='integrated-review',playback='Animation/Crouch/Crouch-Playback.mp4',validation='Animation/Crouch/review-evidence.json',currentEvidence='Five grounded motions installed in the art-controller source with native foot IK, four-character playback, sixteen-direction contacts and immediate stance/lifecycle checks.',remainingEvidence='Moving-world contacts, complete transition/interruption coverage and Sanders native/artistic acceptance remain open.')
    if row['group']=='character' and row['action'] in ('slide entry','slide loop','slide exit'):
        row.update(productionState='integrated-review',sourceMaster='Animation/Slide/Contender-Slide-v1.blend',measuredReferences='Animation/Slide/index.html',playback='Animation/Slide/Slide-Before-After.mp4',validation='Animation/Slide/review-evidence.json',currentEvidence='Original source-frame-clocked entry, feet-first hold and compact recovery; all-character moving trajectory comparison and configurable-duration/lifecycle checks pass.',remainingEvidence='First-person slide actions, broader terrain/combat interruption and native input/audio/artistic acceptance remain open.')
    followups={('character','idle'):'Animation/Standing/standing-production.json',('first-person','ready/equip'):'Animation/FirstPersonReady/ReadyAction.json'}
    reference=followups.get((row['group'],row['action']))
    if reference:
        record=art/reference;spec=json.loads(record.read_text(encoding='utf-8'))
        shipping=shipping_followup(root,record,spec['masterSha256'])
        if shipping:
            row['currentEvidence']=row['currentEvidence'].replace('Shipping build pending; Review22 remains current.','Shipping evidence recorded separately.').replace('Shipping milestone pending.','Shipping evidence recorded separately.')
            row['currentEvidence']+=f" This exact master is included in verified Windows Review{shipping['review']} at {shipping['sourceCommit']}; current runtime and final acceptance are separate."
with (art/'animation-production-matrix.csv').open('w',newline='',encoding='utf-8') as stream:
    writer=csv.DictWriter(stream,columns);writer.writeheader();writer.writerows(rows)
print('ANIMATION_MATRIX_UPDATED',len(rows),'baseline actions;',sum(row['productionState']=='integrated-review' for row in rows),'with evidence; final approval remains pending')
