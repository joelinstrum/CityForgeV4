"""Prepare private camp input files. Requires CITYFORGE_PROJECT and download directory argument.
Never overwrites different existing files. Run Blender preparation/bake, then the Unity builder.
"""
import hashlib
import os
from pathlib import Path
import shutil
import sys
import zipfile


def copy(source, destination):
    if destination.exists():
        if hashlib.sha256(source.read_bytes()).digest() != hashlib.sha256(destination.read_bytes()).digest():
            raise RuntimeError(f'Refusing to overwrite {destination}')
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)


def extract(archive, root):
    with zipfile.ZipFile(archive) as package:
        for member in package.infolist():
            path = (root / member.filename).resolve()
            if not path.is_relative_to(root.resolve()):
                raise RuntimeError('Unsafe archive path')
            if member.is_dir():
                continue
            data = package.read(member)
            if path.exists():
                if path.read_bytes() != data:
                    raise RuntimeError(f'Refusing to overwrite {path}')
            else:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)


def main():
    project = Path(os.environ['CITYFORGE_PROJECT']).resolve()
    downloads = Path(sys.argv[1])
    source = project / 'SourceAssets/NativeCamps'
    runtime = project / 'Assets/CityForge/Resources/NativeCamps'
    copy(downloads / 'totem', source / 'Totem.fbx')
    copy(downloads / 'teepees-2/Teepees.fbx', source / 'Teepees.fbx')
    extract(downloads / 'teepees-2/Textures.zip', source / 'Textures')
    for role, folder in [('Chief', 'chief'), ('Warrior', 'native warrior 3d model')]:
        character = project / 'SourceAssets/NpcStudy' / folder
        extract(downloads / (folder + '.zip'), character)
        for path in character.rglob('*'):
            if path.is_file():
                name = Path('Model.fbx') if path.suffix == '.fbx' else path.relative_to(character)
                copy(path, runtime / role / name)
    if len(sys.argv) > 2:
        copy(Path(sys.argv[2]), runtime / 'Courtyard-Dirt.png')
    textures = source / 'Textures/Textures'
    for color in ['Plain', 'Red', 'Blue', 'Brown']:
        copy(textures / ('Teepee ' + color + ' 2K/Teepees_Teepee_BaseColor.png'), runtime / ('Teepee-' + color + '.png'))
    for original, name in [('Teepees_Teepee_Normal.png', 'Teepee-Normal.png'), ('Teepees_Teepee_Roughness.png', 'Teepee-Roughness.png')]:
        copy(textures / 'Teepee Plain 2K' / original, runtime / name)
    print('Private camp sources verified/prepared; existing files preserved.')


if __name__ == '__main__':
    main()
