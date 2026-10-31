"""Find a verified historical Windows package containing the exact art master.

This describes a family in an identified committed build, never arbitrary later
runtime edits or artistic/native-input acceptance.
"""
import json
import re
from pathlib import Path
import subprocess


def shipping_followup(root: Path, source_record: Path, master_sha256: str):
    reports = list((root / 'Validation').glob('art-package-verification-*.json'))
    reports.sort(key=lambda path: int(path.stem.rsplit('-', 1)[1]), reverse=True)
    for report in reports:
        review = int(report.stem.rsplit('-', 1)[1])
        integrated_path = root / 'Validation' / f'art-integrated-final-{review:02d}.json'
        if not integrated_path.is_file():
            continue
        package = json.loads(report.read_text(encoding='utf-8-sig'))
        integrated = json.loads(integrated_path.read_text(encoding='utf-8-sig'))
        if re.fullmatch(r'[0-9a-f]{40}', integrated.get('sourceCommit', '')) is None:
            continue
        if not integrated.get('validationExecutedOnShippingArtifact') or package.get('verifiedFiles', 0) <= 0 or package.get('verifiedFiles', 0) != package.get('extractedFilesVerified', -1):
            continue
        path = source_record.relative_to(root).as_posix()
        result = subprocess.run(['git', 'show', integrated['sourceCommit'] + ':' + path], cwd=root, capture_output=True)
        if result.returncode != 0:
            continue
        committed = json.loads(result.stdout.decode('utf-8-sig'))
        if committed.get('masterSha256') != master_sha256:
            continue
        return {'review': review, 'sourceCommit': integrated['sourceCommit'], 'fingerprint': integrated['fingerprint'],
                'evidence': integrated_path.relative_to(root).as_posix(),
                'packageVerification': report.relative_to(root).as_posix(), 'packageSha256': package['sha256'],
                'scope': 'This exact art master is included in the identified verified historical Windows source. Later runtime edits, native input/audio, performance and artistic acceptance are separate.'}
    return None
