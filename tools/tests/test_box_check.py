"""
tools/box_check.py helpers: builder filters, test defaults and the board blocks director.py status prints.
Run: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import re
import subprocess
import tempfile
import unittest
from pathlib import Path

import box_check


class BuilderFilterTests(unittest.TestCase):
    def test_box_domains_and_the_main_scene_build(self):
        pattern = re.compile(box_check.builder_filter(["world", "ui"]), re.I)
        for path in ("World/Terrain", "UI/Wordmark", "Main Scene"):
            self.assertTrue(pattern.search(path), path)
        for path in ("Gameplay/Lift", "Main Scene Extras", "Rover/Art/X"):
            self.assertFalse(pattern.search(path), path)

    def test_a_box_without_builders_still_builds_the_scene(self):
        self.assertEqual(box_check.builder_filter(["foundation"]), "^(Main Scene$)")


class TestCommandTests(unittest.TestCase):
    ROOT = Path("D:/wt/world")

    def args(self, box, **kw):
        values = dict(box=box, filter=None, assembly=None, platform="editmode", fast=False)
        values.update(kw)
        return argparse.Namespace(**values)

    def test_default_is_editmode_then_the_box_playmode_assembly(self):
        planned = box_check.test_commands(self.ROOT, self.args("world"))
        self.assertEqual([label for label, _ in planned],
                         ["editmode MoonProject.World.Tests", "playmode MoonProject.World.PlayModeTests"])
        self.assertEqual(planned[0][1][-3:], ["editmode", "--assembly", "MoonProject.World.Tests"])
        self.assertEqual(planned[1][1][-5:], ["playmode", "--assembly", "MoonProject.World.PlayModeTests",
                                              "--category", "!Playthrough"])

    def test_fast_skips_playmode(self):
        self.assertEqual([l for l, _ in box_check.test_commands(self.ROOT, self.args("world", fast=True))],
                         ["editmode MoonProject.World.Tests"])
        self.assertEqual(len(box_check.test_commands(self.ROOT, self.args("art", fast=True))), 1)

    def test_filter_or_assembly_is_one_custom_step_and_unknown_boxes_have_none(self):
        planned = box_check.test_commands(self.ROOT, self.args("world", filter="LiftTests"))
        self.assertEqual(len(planned), 1)
        self.assertEqual(planned[0][1][-2:], ["--filter", "LiftTests"])
        planned = box_check.test_commands(self.ROOT, self.args("ui", platform="playmode", assembly=["A", "B"]))
        self.assertEqual(planned[0][1][-7:], ["playmode", "--assembly", "A", "--assembly", "B",
                                              "--category", "!Playthrough"])
        self.assertEqual(box_check.test_commands(self.ROOT, self.args("music")), [])

    def test_no_playmode_step_runs_the_playthrough(self):
        # 2026-10-11: --assembly MoonProject.Gameplay.PlayModeTests selected the [Explicit] Playthrough and hit the
        # 40-minute PlayMode timeout.
        for kw in ({}, dict(platform="playmode", assembly=["MoonProject.Gameplay.PlayModeTests"]),
                   dict(platform="playmode", filter="Lift")):
            for label, cmd in box_check.test_commands(self.ROOT, self.args("gameplay", **kw)):
                if label.startswith("playmode"):
                    self.assertEqual(cmd[-2:], ["--category", "!Playthrough"], label)
                else:
                    self.assertNotIn("--category", cmd, label)


class TrialMergeAbortTests(unittest.TestCase):
    def run_git(self, root, *args):
        subprocess.run(["git", "-c", "safe.directory=*", "-c", "user.name=t", "-c", "user.email=t@t", *args],
                       cwd=root, check=True, capture_output=True)

    def repo(self, tmp):
        root = Path(tmp)
        self.run_git(root, "init", "-q", "-b", "main")
        (root / "a.txt").write_text("a", encoding="utf-8")
        self.run_git(root, "add", "a.txt")
        self.run_git(root, "commit", "-q", "-m", "a")
        self.run_git(root, "checkout", "-q", "-b", "box")
        self.run_git(root, "checkout", "-q", "main")
        (root / "b.txt").write_text("b", encoding="utf-8")
        (root / "a.txt").write_text("a from main", encoding="utf-8")
        self.run_git(root, "add", "a.txt", "b.txt")
        self.run_git(root, "commit", "-q", "-m", "b")
        self.run_git(root, "checkout", "-q", "box")
        return root

    def test_a_merge_left_in_progress_is_ended_whatever_the_flag_says(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = self.repo(tmp)
            self.assertEqual(box_check.trial_merge(root), "")
            self.assertTrue(box_check.merge_in_progress(root))
            self.assertEqual(box_check.abort_trial_merge(root), "")
            self.assertFalse(box_check.merge_in_progress(root))
            self.assertFalse((root / "b.txt").exists(), "main's files are gone again")
            self.assertEqual(box_check.git(root, "status", "--porcelain"), "")

    def test_a_merge_touched_file_rewritten_by_the_builders_does_not_keep_the_merge_alive(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = self.repo(tmp)
            self.assertEqual(box_check.trial_merge(root), "")
            (root / "a.txt").write_text("rewritten by a batch Unity", encoding="utf-8")
            self.assertEqual(box_check.abort_trial_merge(root), "")
            self.assertFalse(box_check.merge_in_progress(root))
            self.assertEqual((root / "a.txt").read_text(encoding="utf-8"), "a")
            self.assertEqual(box_check.git(root, "status", "--porcelain"), "")

    def test_nothing_to_abort_is_fine(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertEqual(box_check.abort_trial_merge(self.repo(tmp)), "")


class BoardTests(unittest.TestCase):
    def test_block_verdict_and_last_block(self):
        first = box_check.format_block("20261011-100000", "world", "box/world @ abc1234 + main @ 1111111, trial merge",
                                       [("compile_check", True, "Logs/c.log", 41.2)], ["restored 3"])
        second = box_check.format_block("20261011-110000", "world", "box/world @ def5678, contains main",
                                        [("compile_check", True, "", 40.0), ("builders", False, "Logs/b.log", 95.6)],
                                        [])
        self.assertTrue(first.startswith("## 20261011-100000 box_check world: PASS (box/world @ abc1234 + main @ "
                                         "1111111, trial merge)"))
        self.assertIn("- PASS compile_check 41 s (log Logs/c.log)", first)
        self.assertIn("- FAIL builders 96 s (log Logs/b.log)", second)
        self.assertEqual(box_check.last_block("\n" + first + "\n" + second), second.strip())
        self.assertEqual(box_check.last_block(first), first.strip())
        self.assertEqual(box_check.last_block("no blocks yet\n"), "")

    def test_no_steps_is_a_failure(self):
        self.assertIn(": FAIL (", box_check.format_block("s", "ui", "box/ui @ 1", [], []))


if __name__ == "__main__":
    unittest.main()
