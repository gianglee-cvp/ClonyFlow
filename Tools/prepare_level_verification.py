import json
import pathlib
import shutil

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROJECT = ROOT / 'Temp/LevelVerification/Project'
PROJECT.mkdir(parents=True, exist_ok=True)
assets = PROJECT / 'Assets'
if assets.exists():
    for path in assets.rglob('*'):
        relative = path.relative_to(assets)
        if relative.parts[0] == 'Verification' or relative.name == 'Verification.meta':
            continue
        if path.is_file() and not (ROOT / 'Assets' / relative).exists():
            if path.resolve().is_relative_to(assets.resolve()):
                path.unlink()
for directory in ('Assets', 'ProjectSettings'):
    shutil.copytree(ROOT / directory, PROJECT / directory, dirs_exist_ok=True)
editor = PROJECT / 'Assets/Verification/Editor'
editor.mkdir(parents=True, exist_ok=True)
for source in (ROOT / 'Tools/Unity').glob('*.cs'):
    shutil.copy2(source, editor / source.name)

manifest = json.loads((ROOT / 'Packages/manifest.json').read_text(encoding='utf-8-sig'))
lock = json.loads((ROOT / 'Packages/packages-lock.json').read_text(encoding='utf-8-sig'))['dependencies']
cache = ROOT / 'Library/PackageCache'
for name, dependency in lock.items():
    if name.startswith('com.unity.modules.'):
        continue
    for path in cache.glob(name + '@*'):
        package = json.loads((path / 'package.json').read_text(encoding='utf-8-sig'))
        if package['version'] == dependency['version']:
            manifest['dependencies'][name] = 'file:' + path.as_posix()
            break
packages = PROJECT / 'Packages'
packages.mkdir(exist_ok=True)
(packages / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(PROJECT)
