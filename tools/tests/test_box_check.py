"""
tools/box_check.py helpers: builder filters, test defaults and the board blocks director.py status prints.
Run: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import re
import unittest
from pathlib import Path

import box_check


class BuilderFilterTests(unittest.TestCase):
    def test_box_domains_and_the_main_scene_build(self):
        pattern = re.compile(box_check.builder_filter(["world", "art"]), re.I)
        for path in ("World/Terrain", "Art/Rover Kit", "Main Scene"):
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

    def test_fast_skips_playmode_and_art_has_no_playmode(self):
        self.assertEqual([l for l, _ in box_check.test_commands(self.ROOT, self.args("world", fast=True))],
                         ["editmode MoonProject.World.Tests"])
        self.assertEqual(len(box_check.test_commands(self.ROOT, self.args("art"))), 1)

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
