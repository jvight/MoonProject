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


if __name__ == "__main__":
    unittest.main()
