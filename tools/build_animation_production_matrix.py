"""Join current authored evidence to the unchanged Gate 1 action inventory."""
import csv
from pathlib import Path

root=Path(__file__).resolve().parent.parent
art=root/'ArtSource/Original/ArtPass-v1'
with (art/'Gate1/animation-matrix.csv').open(newline='') as stream:rows=list(csv.DictReader(stream))
columns=list(rows[0])+['productionState','sourceMaster','measuredReferences','playback','validation','remainingEvidence']
installed={
    ('character','walk forward'):('Animation/Forward','Contender-Advance-v1.blend','Advance-Playback.mp4','AdvanceWalk'),
    ('character','walk backward'):('Animation','Contender-Retreat-v1.blend','Retreat-Playback.mp4','RetreatWalk'),
    ('character','sprint'):('Animation/Forward','Contender-Advance-v1.blend','Advance-Playback.mp4','AdvanceRun'),
    ('character','jump start'):('Animation/Airborne','Contender-Airborne-v1.blend','Controller-Jumps.mp4','Takeoff'),
    ('character','fall loop'):('Animation/Airborne','Contender-Airborne-v1.blend','Controller-Jumps.mp4','AirborneHold'),
    ('character','landing'):('Animation/Airborne','Contender-Airborne-v1.blend','Controller-Jumps.mp4','LandingRecover'),
}
for row in rows:
    row.update(productionState='remaining-production',sourceMaster='',measuredReferences='',playback='',validation='',remainingEvidence='Original Gate 1 requirements remain open unless evidence below explicitly covers them. Sanders native/artistic acceptance pending.')
    spec=installed.get((row['group'],row['action']))
    if spec:
        directory,master,video,clip=spec
        files=[f'{directory}/{master}',f'{directory}/References/{clip}/measured-poses.png',f'{directory}/{video}',f'{directory}/review-evidence.json']
        for file in files:assert (art/file).is_file(),file
        row.update(productionState='integrated-review',sourceMaster=files[0],measuredReferences=files[1],playback=files[2],validation=files[3],currentEvidence='Original authored IK/bone motion installed in optional Sky Gardens art profile; shared rig and simulation retained.',remainingEvidence='All movement/stance/traversal combinations, rear motion views, native sound/input and final artistic acceptance remain open.')
    if row['group']=='first-person' and row['action']=='magazine reload':
        row.update(productionState='integrated-review',sourceMaster='Gate2/RifleActions/Carbine-Reload-v1.blend',playback='Gate2/RifleActions/index.html',validation='Gate2/RifleActions/ReloadAction.json',currentEvidence='Original two-second reload, magazine/wrist contacts and interruption/reset review integrated; see action package.',remainingEvidence='Complete first-person action library and native contact/audio/artistic acceptance remain open.')
with (art/'animation-production-matrix.csv').open('w',newline='',encoding='utf-8') as stream:
    writer=csv.DictWriter(stream,columns);writer.writeheader();writer.writerows(rows)
print('ANIMATION_MATRIX_UPDATED',len(rows),'baseline actions;',sum(row['productionState']=='integrated-review' for row in rows),'with evidence; final approval remains pending')
