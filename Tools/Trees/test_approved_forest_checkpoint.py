"""Filesystem safety checks; no Unity or licensed checkpoint is required."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


class CheckpointSafety(unittest.TestCase):
    def setUp(self):
        spec = importlib.util.spec_from_file_location("checkpoint_under_test",
            Path(__file__).with_name("approved_forest_checkpoint.py"))
        self.tool = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.tool)
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name) / "project"
        root.mkdir()
        self.tool.ROOT = root
        self.tool.CHECKPOINT = root / "SourceAssets/ApprovedForest/test"
        self.tool.ART = root / "Assets/CityForge/LocalTree06Study"
        self.tool.STATE = root / "Library/ApprovedForestReplay"
        self.tool.PLAYER = root / "Builds/ApprovedForestReplay/ApprovedForest.app"
        self.tool.ignored = lambda path: path.is_relative_to(self.tool.ART)
        self.tool.ART.mkdir(parents=True)
        (self.tool.ART / "original.txt").write_text("keep")
        (self.tool.ART / "original-empty-directory").mkdir()
        checkpoint = self.tool.CHECKPOINT
        contents = {"Meshes/test.json": "mesh", "RuntimeAssets/Tree06Large/leaf.txt": "private texture",
                    "RuntimeAssets/LargeForestTrial.cs.meta": "runtime-guid",
                    "Replay/ApprovedForestTrial.cs.template": 'data="@MESH_DATA@"; out="@OUTPUT@";',
                    "Replay/Build.cs.template": '@BUILD_APP@'}
        files = {}
        for name, content in contents.items():
            path = checkpoint / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content)
            files[name] = hashlib.sha256(path.read_bytes()).hexdigest()
        manifest = checkpoint / "Manifest.json"
        manifest.write_text(json.dumps({"id": "test", "files": files}))
        self.tool.MANIFEST_SHA256 = self.tool.digest(manifest)
        self.output = root / "Library/Review"

    def test_restore_and_clean_preserve_preexisting_files_and_directories(self):
        self.tool.restore(self.output)
        runtime = self.tool.ART / "LargeForestTrial.cs"
        self.assertIn(str(self.tool.CHECKPOINT / "Meshes"), runtime.read_text())
        self.tool.clean()
        self.assertFalse(runtime.exists())
        self.assertFalse((self.tool.ART / "Resources").exists())
        self.assertEqual((self.tool.ART / "original.txt").read_text(), "keep")
        self.assertTrue((self.tool.ART / "original-empty-directory").is_dir())

    def test_corrupted_checkpoint_is_rejected_before_restore(self):
        (self.tool.CHECKPOINT / "Meshes/test.json").write_text("changed")
        with self.assertRaisesRegex(RuntimeError, "Checkpoint mismatch"):
            self.tool.restore(self.output)
        self.assertFalse((self.tool.ART / "LargeForestTrial.cs").exists())

    def test_modified_replay_is_preserved_for_review(self):
        self.tool.restore(self.output)
        runtime = self.tool.ART / "LargeForestTrial.cs"
        runtime.write_text("user revision")
        with self.assertRaisesRegex(RuntimeError, "preserving it for review"):
            self.tool.clean()
        self.assertEqual(runtime.read_text(), "user revision")
        self.assertTrue((self.tool.STATE / "Owned-Files.json").exists())

    def test_collision_cleans_partial_restore_without_overwriting_local_work(self):
        collision = self.tool.ART / "LargeForestTrial.cs"
        collision.write_text("user work")
        with self.assertRaisesRegex(RuntimeError, "Refusing to overwrite"):
            self.tool.restore(self.output)
        self.assertEqual(collision.read_text(), "user work")
        self.assertFalse((self.tool.ART / "Resources/Tree06Large/leaf.txt").exists())

    def test_stale_captures_do_not_qualify_a_new_replay(self):
        self.output.mkdir(parents=True)
        (self.output / "C-Refined-Near-Settings.json").write_text("old")
        with self.assertRaisesRegex(RuntimeError, "fresh output"):
            self.tool.restore(self.output)

    def test_nonignored_destinations_are_rejected(self):
        self.tool.ignored = lambda path: False
        with self.assertRaisesRegex(RuntimeError, "not Git-ignored"):
            self.tool.restore(self.output)
        self.assertFalse((self.tool.ART / "Resources").exists())


if __name__ == "__main__":
    unittest.main()
