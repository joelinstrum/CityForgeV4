"""Copy supplied local character files into ignored authoring/runtime folders. Never overwrite.
Usage: CITYFORGE_PROJECT='/path/to/project' python3 Tools/Npc/prepare_npc_study.py '/path/to/indian stuff'
Original archives, extracted artwork and prepared Unity assets must remain private.
"""
import hashlib
import os
from pathlib import Path
import shutil
import sys
import zipfile


def copy_once(source, destination):
    if destination.exists():
        if hashlib.sha256(source.read_bytes()).digest() != hashlib.sha256(destination.read_bytes()).digest():
            raise RuntimeError(f"Refusing to overwrite different existing file: {destination}")
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)


def main():
    project = Path(os.environ['CITYFORGE_PROJECT']).resolve()
    archive = Path(sys.argv[1]) / 'native fringe dress 3d model.zip'
    source = project / 'SourceAssets/NpcStudy/native fringe dress 3d model'
    with zipfile.ZipFile(archive) as package:
        for entry in package.infolist():
            target = (source / entry.filename).resolve()
            if not target.is_relative_to(source.resolve()):
                raise RuntimeError('Unsafe archive entry')
            if entry.is_dir():
                continue
            data = package.read(entry)
            if target.exists():
                if target.read_bytes() != data:
                    raise RuntimeError(f'Refusing to overwrite {target}')
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(data)
    models = list(source.glob('*.fbx'))
    if len(models) != 1:
        raise RuntimeError('Expected exactly one supplied FBX')
    runtime = project / 'Assets/CityForge/Resources/NpcStudy'
    copy_once(models[0], runtime / 'Resident.fbx')
    for texture in source.rglob('*'):
        if texture.is_file() and texture != models[0]:
            copy_once(texture, runtime / texture.relative_to(source))
    print('Private source prepared. In Unity Edit mode: CityForge > NPC > Prepare local resident')


if __name__ == '__main__':
    main()
