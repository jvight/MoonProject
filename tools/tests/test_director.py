"""
tools/director.py helpers behind `land --batch`: the builder regex and the worktree lock shared with `verify`.
Run: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import json
import os
import re
import tempfile
import unittest
from pathlib import Path

import director


class BatchBuilderFilterTests(unittest.TestCase):
    def args(self, boxes, filter_pattern=None, no_scene=False):
        return argparse.Namespace(boxes=boxes, filter=filter_pattern, no_scene=no_scene)

    def test_box_domains_and_the_scene(self):
        pattern = re.compile(director.batch_builder_filter(self.args(["art", "rover"])), re.I)
        for path in ("Art/Rover Kit", "Rover/Wheels", "Main Scene"):
            self.assertTrue(pattern.search(path), path)
        self.assertFalse(pattern.search("World/Sky Materials"))

    def test_filter_override_no_scene_and_nothing_at_all(self):
        self.assertEqual(director.batch_builder_filter(self.args(["art"], "^World/", True)), "(?:^World/)")
        self.assertEqual(director.batch_builder_filter(self.args(["story"], None, True)), "")
        self.assertEqual(director.batch_builder_filter(self.args(["story"])), "(?:^Main Scene$)")


ASSET = """%YAML 1.1
--- !u!114 &11400000
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: GUID, type: 3}
  m_Name: BatteryTuning
  m_EditorClassIdentifier: 
  _cells: 8
  _rangeMeters: 400
"""
OLD_GUID = "a" * 32
NEW_GUID = "b" * 32


class OrphanedAssetTests(unittest.TestCase):
    def make(self, root, rel, text):
        path = root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def tree(self, root):
        self.make(root, "Assets/_Project/Scripts/Gameplay/OldTuning.cs.meta", f"fileFormatVersion: 2\nguid: {OLD_GUID}\n")
        self.make(root, "Assets/_Project/Data/Tuning/Old.asset", ASSET.replace("GUID", OLD_GUID))
        self.make(root, "Assets/_Project/Data/Tuning/New.asset", ASSET.replace("GUID", NEW_GUID))
        self.make(root, "Assets/Elsewhere/Foreign.asset", ASSET.replace("GUID", NEW_GUID))
        self.make(root, "Assets/_Project/Data/Tuning/Plain.txt", "x")

    def test_script_guid_and_values(self):
        text = ASSET.replace("GUID", NEW_GUID)
        self.assertEqual(director.script_guid(text), NEW_GUID)
        self.assertEqual(director.script_guid("no script here"), "")
        self.assertEqual(director.asset_values(text), ["  _cells: 8", "  _rangeMeters: 400"])

    def test_values_are_capped(self):
        text = "m_EditorClassIdentifier: \n" + "\n".join(f"  _f{i}: {i}" for i in range(200))
        values = director.asset_values(text)
        self.assertEqual(len(values), director.ASSET_VALUE_LINES + 1)
        self.assertIn("120 more", values[-1])

    def test_only_added_assets_whose_script_has_no_meta_are_orphans(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.tree(root)
            added = ["Assets/_Project/Data/Tuning/Old.asset", "Assets/_Project/Data/Tuning/New.asset",
                     "Assets/Elsewhere/Foreign.asset", "Assets/_Project/Data/Tuning/Plain.txt"]
            orphans = director.orphaned_assets(root, added)
            self.assertEqual([rel for rel, _ in orphans], ["Assets/_Project/Data/Tuning/New.asset"])
            self.assertEqual(orphans[0][1], ["  _cells: 8", "  _rangeMeters: 400"])
            self.assertEqual(director.orphaned_assets(root, added[:1]), [])
            self.assertEqual(director.orphaned_assets(root, []), [])

    def test_warning_lines_name_the_asset_and_list_its_values(self):
        lines = director.orphan_warnings([("Assets/_Project/Data/Tuning/New.asset", ["  _cells: 8"])])
        self.assertTrue(lines[0].startswith("WARNING: Assets/_Project/Data/Tuning/New.asset"))
        self.assertEqual(lines[1], "      _cells: 8")
        self.assertEqual(director.orphan_warnings([]), [])


class WorktreeLockTests(unittest.TestCase):
    def test_a_live_holder_refuses_and_a_stale_one_is_replaced(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp)
            with director.WorktreeLock(path, "verify"):
                self.assertTrue((path / director.WORKTREE_LOCK).read_text().startswith(f"{os.getpid()} verify"))
                with self.assertRaises(SystemExit):
                    director.WorktreeLock(path, "land --batch").__enter__()
            self.assertFalse((path / director.WORKTREE_LOCK).exists())
            (path / director.WORKTREE_LOCK).write_text("999999 verify", encoding="utf-8")
            with director.WorktreeLock(path, "land --batch"):
                self.assertIn("land --batch", (path / director.WORKTREE_LOCK).read_text())


class PlayModeShardTests(unittest.TestCase):
    TIMES = {"A": 408.0, "B": 256.0, "C": 170.0, "D": 14.0, "E": 1.0}

    def test_longest_first_onto_the_lightest_shard(self):
        self.assertEqual(director.shard(list(self.TIMES), self.TIMES, 2), [["A", "D", "E"], ["B", "C"]])
        self.assertEqual(director.shard(list(self.TIMES), self.TIMES, 3), [["A"], ["B"], ["C", "D", "E"]])

    def test_one_shard_more_shards_than_assemblies_and_unknown_times(self):
        self.assertEqual(director.shard(["A", "B"], {}, 1), [["A", "B"]])
        self.assertEqual(len(director.shard(["A", "B"], {}, 3)), 2)
        self.assertEqual(director.shard(["A", "B", "New"], {"A": 10.0, "B": 2.0}, 2), [["A", "B"], ["New"]])

    def test_assembly_seconds_from_results_xml(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "results.xml"
            path.write_text('<test-run><test-suite type="Assembly" name="MoonProject.UI.PlayModeTests.dll" '
                            'duration="170.5"/><test-suite type="TestFixture" name="X" duration="3"/></test-run>',
                            encoding="utf-8")
            self.assertEqual(director.assembly_seconds(path), {"MoonProject.UI.PlayModeTests": 170.5})
            self.assertEqual(director.assembly_seconds(Path(tmp) / "missing.xml"), {})

    def test_shard_worktrees_leave_director2_to_a_parallel_playthrough(self):
        def pool(parallel, shards, skip=False):
            args = argparse.Namespace(parallel=parallel, shards=shards, skip_playthrough=skip)
            return [path for path, _ in director.shard_worktrees(args)]

        self.assertEqual(pool(False, 1), [director.VERIFY_WORKTREE])
        self.assertEqual(pool(False, 2), [director.VERIFY_WORKTREE, director.VERIFY_WORKTREE_2])
        self.assertNotIn(director.VERIFY_WORKTREE_2, pool(True, 3))
        self.assertIn(director.VERIFY_WORKTREE_2, pool(True, 2, skip=True))



class ExplicitSessionTests(unittest.TestCase):
    def tree(self, tmp):
        root = Path(tmp)
        folder = root / "Assets/_Project/Scripts/Gameplay/Tests/PlayMode"
        folder.mkdir(parents=True)
        (folder / "G.asmdef").write_text(json.dumps({"name": "MoonProject.Gameplay.PlayModeTests",
                                                     "defineConstraints": ["UNITY_INCLUDE_TESTS"],
                                                     "references": ["UnityEngine.TestRunner"]}), encoding="utf-8")
        (folder / "Lift.cs").write_text(
            "namespace MoonProject.Gameplay.PlayModeTests\n{\n    /// <summary>x</summary>\n"
            "    [Explicit(\"slow\")]\n    [Category(\"LiftSession\")]\n    public sealed class LiftSession { }\n"
            "    public sealed class Quick { }\n}\n", encoding="utf-8")
        (folder / "Golden.cs").write_text(
            "namespace MoonProject.Gameplay.PlayModeTests\n{\n    [Explicit]\n    [Category(\"Playthrough\")]\n"
            "    public sealed class GoldenPath { }\n}\n", encoding="utf-8")
        return root

    def test_explicit_fixtures_are_found_with_their_category_and_the_playthrough_is_left_out(self):
        with tempfile.TemporaryDirectory() as tmp:
            sessions = director.explicit_sessions(self.tree(tmp))
        self.assertEqual(sessions, [("LiftSession", "MoonProject.Gameplay.PlayModeTests.LiftSession",
                                     "MoonProject.Gameplay.PlayModeTests")])

    def test_steps_shard_categories_by_measured_time_and_size_their_timeout(self):
        sessions = [("A", "N.A", "X"), ("B", "N.B", "X"), ("C", "N.C", "X")]
        steps = director.explicit_steps(sessions, {"N.A": 3000.0, "N.B": 100.0}, 2, ["py", "tests", "--platform"])
        # The unmeasured C weighs the median measured time (3000 s), so it gets a shard of its own.
        self.assertEqual([label for label, _ in steps], ["explicit A B", "explicit C"])
        first, second = steps[0][1], steps[1][1]
        self.assertEqual(first[first.index("--timeout") + 1], str(int(3100 * 1.5) + 300))
        self.assertEqual(first[-4:], ["--category", "A", "--category", "B"])
        # 300 s margin + 600 s unmeasured allowance is under the floor, the Playthrough's 2700 s.
        self.assertEqual(second[second.index("--timeout") + 1], "2700")
        self.assertEqual(director.explicit_steps([], {}, 2, ["py"]), [])

    def test_a_fixture_without_a_category_stops_the_plan(self):
        with self.assertRaises(SystemExit):
            director.explicit_steps([(None, "N.A", "X")], {}, 1, ["py"])


if __name__ == "__main__":
    unittest.main()
