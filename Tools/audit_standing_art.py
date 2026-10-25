#!/usr/bin/env python3
"""Read-only audit of the standing portrait PNGs and their Unity metadata."""
import hashlib
import re
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/_Project/Resources/Art'


def main():
    errors = []
    guids = set()
    hashes = {}
    targets = sorted((ART / 'Standing').glob('*.png'))
    for target in targets:
        with Image.open(target) as image:
            if image.size != (1024, 1536) or image.mode != 'RGBA':
                errors.append(f'{target.name}: expected 1024x1536 RGBA, got {image.size} {image.mode}')
            elif image.getchannel('A').getextrema()[0] != 0 or image.getchannel('A').getextrema()[1] == 0:
                errors.append(f'{target.name}: missing transparent edge or visible content')
        meta = Path(str(target) + '.meta')
        match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(), re.M) if meta.exists() else None
        if not match or match[1] in guids:
            errors.append(f'{target.name}: missing or duplicate Unity GUID')
        else:
            guids.add(match[1])
        digest = hashlib.sha256(target.read_bytes()).hexdigest()
        if digest in hashes:
            errors.append(f'{target.name}: identical to {hashes[digest]}')
        hashes[digest] = target.name
    print(f'Standing: {len(targets)}; errors: {len(errors)}')
    for error in errors:
        print('ERROR:', error)
    return bool(errors)


if __name__ == '__main__':
    raise SystemExit(main())
