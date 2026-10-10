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
    def args(self, box, **kw):
        values = dict(box=box, filter=None, assembly=None, platform="editmode")
        values.update(kw)
        return argparse.Namespace(**values)

    def test_default_assembly_filter_and_missing_default(self):
        root = Path("D:/wt/world")
        cmd, label = box_check.test_command(root, self.args("world"))
        self.assertEqual(cmd[-2:], ["--assembly", "MoonProject.World.Tests"])
        self.assertEqual(label, "editmode MoonProject.World.Tests")
        cmd, _ = box_check.test_command(root, self.args("world", filter="LiftTests"))
        self.assertEqual(cmd[-2:], ["--filter", "LiftTests"])
        self.assertNotIn("--assembly", cmd)
        self.assertIsNone(box_check.test_command(root, self.args("music"))[0])


class BoardTests(unittest.TestCase):
    def test_block_verdict_and_last_block(self):
        first = box_check.format_block("20261011-100000", "world", "box/world", "abc1234",
                                       [("compile_check", True, "Logs/c.log")], ["restored 3"])
        second = box_check.format_block("20261011-110000", "world", "box/world", "def5678",
                                        [("compile_check", True, ""), ("builders", False, "Logs/b.log")], [])
        self.assertTrue(first.startswith("## 20261011-100000 box_check world: PASS (box/world @ abc1234)"))
        self.assertIn("- PASS compile_check (log Logs/c.log)", first)
        self.assertIn("- FAIL builders (log Logs/b.log)", second)
        self.assertEqual(box_check.last_block("\n" + first + "\n" + second), second.strip())
        self.assertEqual(box_check.last_block(first), first.strip())
        self.assertEqual(box_check.last_block("no blocks yet\n"), "")

    def test_no_steps_is_a_failure(self):
        self.assertIn(": FAIL (", box_check.format_block("s", "ui", "box/ui", "1", [], []))


if __name__ == "__main__":
    unittest.main()
