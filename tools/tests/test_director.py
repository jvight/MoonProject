"""
tools/director.py helpers behind `land --batch`: the builder regex and the worktree lock shared with `verify`.
Run: python -m unittest discover -s tools/tests -t tools
"""
import argparse
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


if __name__ == "__main__":
    unittest.main()
