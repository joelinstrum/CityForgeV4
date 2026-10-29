#!/usr/bin/env python3
"""Verify/replay the local approved forest without publishing or auto-integrating its art."""
import argparse
import hashlib
import json
import sys
from pathlib import Path
import shutil
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]
CHECKPOINT = ROOT / "SourceAssets/ApprovedForest/RefinedC-2026-10-28"
MANIFEST_SHA256 = "3f7b7e4a7bbe3a674679a6923c216db05cc53e2e4f8a02bab50fcc197d8b5bd5"
STATE = ROOT / "Library/ApprovedForestReplay"
ART = ROOT / "Assets/CityForge/LocalTree06Study"
PLAYER = ROOT / "Builds/ApprovedForestReplay/ApprovedForest.app"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify():
    manifest = CHECKPOINT / "Manifest.json"
    if not manifest.is_file() or digest(manifest) != MANIFEST_SHA256:
        raise RuntimeError("Approved local checkpoint missing or changed; no fallback forest is allowed.")
    data = json.loads(manifest.read_text())
    for name, expected in data["files"].items():
        path = CHECKPOINT / name
        if not path.resolve().is_relative_to(CHECKPOINT.resolve()) or not path.is_file() or digest(path) != expected:
            raise RuntimeError(f"Checkpoint mismatch: {name}")
    for name, expected in data.get("publicSources", {}).items():
        if digest(ROOT / name) != expected:
            raise RuntimeError(f"Approved source changed: {name}; review before changing the checkpoint.")
    return data


def ignored(path):
    return subprocess.run(["git", "check-ignore", "--quiet", "--", str(path.relative_to(ROOT))],
                          cwd=ROOT).returncode == 0


def restore(output):
    verify()
    receipt = STATE / "Owned-Files.json"
    if receipt.exists():
        raise RuntimeError("A replay is already restored. Clean it before restoring another output directory.")
    # Keep build/capture writes out of Assets, SourceAssets and public documentation.
    output = output.resolve()
    if output.is_relative_to(ROOT) and not output.is_relative_to(ROOT / "Library"):
        raise RuntimeError("Use Library or an output directory outside the project.")
    if output.exists() and (any(output.glob("C-*-Settings.json")) or (output / "Player-Error.txt").exists()):
        raise RuntimeError("Use a fresh output directory; stale captures must not qualify a replay.")
    output.mkdir(parents=True, exist_ok=True)
    STATE.mkdir(parents=True, exist_ok=True)
    replacements = {"@MESH_DATA@": (CHECKPOINT / "Meshes").as_posix(),
                    "@OUTPUT@": output.as_posix(), "@BUILD_APP@": PLAYER.as_posix()}
    owned = {"files": {}, "directories": []}
    # Persist each created file immediately so an interrupted restore remains cleanable.
    def put(path, content):
        if not ignored(path):
            raise RuntimeError(f"Replay destination is not Git-ignored: {path}")
        if path.exists():
            if path.read_bytes() != content:
                raise RuntimeError(f"Refusing to overwrite existing local work: {path}")
            return
        parent = path.parent
        while parent != ART and not parent.exists():
            name = str(parent.relative_to(ROOT))
            if name not in owned["directories"]:
                owned["directories"].append(name)
            parent = parent.parent
        owned["files"][str(path.relative_to(ROOT))] = hashlib.sha256(content).hexdigest()
        receipt.write_text(json.dumps(owned, indent=2) + "\n")
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)
    try:
        source = CHECKPOINT / "RuntimeAssets"
        for path in source.rglob("*"):
            if not path.is_file() or path.name in {"LargeForestTrial.cs", "LargeForestTrial.cs.meta"}:
                continue
            relative = path.relative_to(source)
            # Archived Tree06Large is the old Resources subtree.
            target = ART / "Resources" / relative if relative.parts[0] in {"Tree06Large", "Tree06Large.meta"} else ART / relative
            put(target, path.read_bytes())
        text = (CHECKPOINT / "Replay/ApprovedForestTrial.cs.template").read_text()
        for token, value in replacements.items():
            text = text.replace(token, value.replace("\\", "\\\\").replace('"', '\\"'))
        put(ART / "LargeForestTrial.cs", text.encode())
        put(ART / "LargeForestTrial.cs.meta", (source / "LargeForestTrial.cs.meta").read_bytes())
        text = (CHECKPOINT / "Replay/Build.cs.template").read_text()
        for token, value in replacements.items():
            text = text.replace(token, value.replace("\\", "\\\\").replace('"', '\\"'))
        (STATE / "Build.cs").write_text(text)
        (STATE / "Output.txt").write_text(str(output))
    except Exception:
        # Never remove files that predated this restore.
        clean()
        raise
    return output


def clean():
    receipt = STATE / "Owned-Files.json"
    if not receipt.exists():
        return
    ownership = json.loads(receipt.read_text())
    files = ownership.get("files", ownership)
    directories = ownership.get("directories", [])
    for name, expected in files.items():
        path = ROOT / name
        if not path.resolve().is_relative_to(ART.resolve()) or not ignored(path):
            raise RuntimeError(f"Invalid cleanup destination: {name}")
        if path.exists() and digest(path) != expected:
            raise RuntimeError(f"Replay file changed; preserving it for review: {name}")
    for name in files:
        path = ROOT / name
        path.unlink(missing_ok=True)
    # Remove only now-empty generated directories; preserve original study assets.
    for name in sorted(directories, key=lambda name: len(Path(name).parts), reverse=True):
        directory = ROOT / name
        if not directory.resolve().is_relative_to(ART.resolve()):
            raise RuntimeError(f"Invalid owned directory: {name}")
        if directory.is_dir() and not any(directory.iterdir()):
            directory.rmdir()
    receipt.unlink()


def cli(unity, *arguments):
    result = subprocess.run([unity, "--json", "command", "--project-path", str(ROOT), *arguments],
                            cwd=ROOT, text=True, capture_output=True, timeout=1000)
    data = json.loads(result.stdout)
    inner = data.get("data", {}).get("result", {})
    if result.returncode or not data.get("success") or inner.get("success") is False:
        raise RuntimeError(result.stdout + result.stderr)
    return inner.get("result")


def capture(unity, output):
    if sys.platform != "darwin":
        raise RuntimeError("The checked replay targets macOS; other platforms need separate qualification.")
    verify()
    state = cli(unity, "script", "eval", "--code",
                'var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene(); return new { scene=s.name, dirty=s.isDirty, playing=UnityEditor.EditorApplication.isPlaying };')
    if state != {"scene": "MainMenu", "dirty": False, "playing": False}:
        raise RuntimeError("Replay requires a saved MainMenu in Edit mode; preserve current work first.")
    output = restore(output)
    try:
        cli(unity, "script", "eval", "--code", 'UnityEditor.AssetDatabase.Refresh(); return "Imported replay";')
        time.sleep(12)  # Allow the local Editor domain reload before querying Pipeline again.
        state = cli(unity, "script", "eval", "--code",
                    'return new { compiling=UnityEditor.EditorApplication.isCompiling, failed=UnityEditor.EditorUtility.scriptCompilationFailed };')
        if state != {"compiling": False, "failed": False}:
            raise RuntimeError(f"Editor is not ready: {state}")
        report = cli(unity, "script", "run", "--file", str(STATE / "Build.cs"),
                     "--entry", "LargeForestBuild.Main", "--timeout", "900")
        print(report, flush=True)
        # Launch once through Launch Services. Direct executable + open -a can create duplicate players.
        subprocess.run(["open", "-W", "-n", "--env", "CITYFORGE_LARGE_FOREST=1", "--env",
                        "CITYFORGE_COVERAGE=1", "--env", "CITYFORGE_600=1",
                        str(PLAYER), "--args", "-logFile", str(output / "Player.log")], check=True)
        error = output / "Player-Error.txt"
        if error.exists() or len(list(output.glob("C-*-Settings.json"))) != 16:
            raise RuntimeError("Replay did not complete all 16 captures; inspect the output and player log.")
    finally:
        clean()
        cli(unity, "script", "eval", "--code", 'UnityEditor.AssetDatabase.Refresh(); return "Replay removed";')
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["verify", "restore", "capture", "clean"])
    parser.add_argument("--output", type=Path, default=ROOT / "Library/ApprovedForestReview")
    default_unity = Path.home() / ".unity/bin/unity"
    parser.add_argument("--unity", default=str(default_unity) if default_unity.exists() else "unity")
    args = parser.parse_args()
    if args.action == "verify":
        print(json.dumps({"id": verify()["id"], "status": "verified"}))
    elif args.action == "restore":
        print(restore(args.output))
    elif args.action == "capture":
        print(capture(args.unity, args.output))
    else:
        clean()
        print("Removed only unchanged files owned by this replay. Refresh Unity if running.")


if __name__ == "__main__":
    main()
