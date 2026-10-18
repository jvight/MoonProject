"""
Unit tests for Task ICON-SET (tools/ui/icons/build_icons.py and Assets/_Project/UI/Icons/*.svg).
Run: python -m unittest discover -s tools/tests -t tools
"""
import json
import re
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from ui.icons import build_icons


class TestSvgPathParserPort(unittest.TestCase):
    """Verifies that the Python port of SvgPathParser.cs enforces the exact C# grammar."""

    def test_valid_move_cubic_arc_and_close_parse(self):
        d = "M10 12C10 12 24 12 24 12A6 6 0 0 1 30 18C30 18 30 30 30 30Z"
        cmds, vals = build_icons.parse_svg_path(d)
        self.assertEqual(
            cmds,
            [
                build_icons.MOVE_TO,
                build_icons.CUBIC_TO,
                build_icons.ARC_TO,
                build_icons.CUBIC_TO,
                build_icons.CLOSE,
            ],
        )
        self.assertEqual(len(vals), 2 + 6 + 6 + 6)

    def test_unsupported_commands_raise_value_error(self):
        for bad in (
            "M10 10 L20 20",
            "M10 10 H20",
            "M10 10 V20",
            "M10 10 Q15 15 20 20",
            "m10 10 c10 10 20 20 30 30",
            "M10 10 20 20",  # implicit L after M is rejected by SvgPathParser.cs
        ):
            with self.subTest(bad=bad):
                with self.assertRaises(ValueError):
                    build_icons.parse_svg_path(bad)

    def test_elliptical_rotated_or_degenerate_arcs_raise_value_error(self):
        for bad_arc in (
            "M10 10 A8 5 0 0 1 20 20",   # rx != ry
            "M10 10 A8 8 15 0 1 20 20",  # rotation != 0
            "M10 10 A8 8 0 1 1 10 10",   # degenerate from == to
        ):
            with self.subTest(bad_arc=bad_arc):
                with self.assertRaises(ValueError):
                    build_icons.parse_svg_path(bad_arc)


class TestIconIdMapping(unittest.TestCase):
    """Verifies that every icon filename maps to real IDs in en.json and RoverSocket.cs."""

    @classmethod
    def setUpClass(cls):
        cls.repo_root = build_icons.REPO_ROOT
        en_path = cls.repo_root / "Assets" / "_Project" / "Data" / "Localization" / "en.json"
        en_data = json.loads(en_path.read_text(encoding="utf-8"))
        cls.en_keys = {entry["key"] for entry in en_data.get("entries", [])}

        socket_cs = (cls.repo_root / "Assets" / "_Project" / "Scripts" / "Core" / "RoverSocket.cs").read_text(
            encoding="utf-8"
        )
        enum_body = re.search(r"enum\s+RoverSocket\s*\{([^}]+)\}", socket_cs, re.DOTALL)
        cls.rover_sockets = [
            m.group(1).lower()
            for m in re.finditer(r"^\s*([A-Z][A-Za-z0-9_]*)\s*=", enum_body.group(1), re.MULTILINE)
        ]

    def test_kit_icons_match_every_rover_socket(self):
        self.assertEqual(len(build_icons.KIT_SPECS), 12)
        kit_ids = [spec.icon_id for spec in build_icons.KIT_SPECS]
        self.assertEqual(kit_ids, self.rover_sockets)

    def test_every_icon_localization_key_exists_in_en_json(self):
        self.assertEqual(len(build_icons.ITEM_SPECS), 13)
        self.assertEqual(len(build_icons.STRUCT_SPECS), 5)
        self.assertEqual(len(build_icons.ALL_SPECS), 30)
        for spec in build_icons.ITEM_SPECS + build_icons.STRUCT_SPECS:
            with self.subTest(icon=spec.filename):
                self.assertIn(
                    spec.localization_key,
                    self.en_keys,
                    f"{spec.filename} references missing en.json key '{spec.localization_key}'",
                )

    def test_expected_svg_files_exist_on_disk(self):
        icon_dir = build_icons.DEFAULT_ICON_DIR
        self.assertTrue(icon_dir.is_dir(), f"Missing icon directory: {icon_dir}")
        actual_files = sorted(p.relative_to(icon_dir).as_posix() for p in icon_dir.rglob("*.svg"))
        expected_files = sorted(spec.filename for spec in build_icons.ALL_SPECS)
        self.assertEqual(actual_files, expected_files)


class TestIconSvgRulesAndBounds(unittest.TestCase):
    """Verifies viewBox, stroke widths, round caps/joins, currentColor, at most 1 flat fill, and bounds."""

    def test_every_committed_svg_parses_and_stays_inside_64x64_viewbox(self):
        ns = {"svg": build_icons.SVG_NAMESPACE}
        for spec in build_icons.ALL_SPECS:
            svg_path = build_icons.DEFAULT_ICON_DIR / spec.filename
            with self.subTest(icon=spec.filename):
                svg_text = svg_path.read_text(encoding="utf-8")
                build_icons.validate_svg_text(svg_text, spec.filename)

                root = ET.fromstring(svg_text)
                self.assertEqual(root.attrib.get("viewBox"), "0 0 64 64")
                self.assertEqual(root.attrib.get("width"), "64")
                self.assertEqual(root.attrib.get("height"), "64")

                paths = root.findall(".//svg:path", ns)
                self.assertGreaterEqual(len(paths), 1)
                self.assertLessEqual(len(paths), 3)

                fills = [p for p in paths if p.attrib.get("fill") == "currentColor"]
                self.assertLessEqual(len(fills), 1, f"{spec.filename} has more than one flat fill")

                for path_el in paths:
                    d = path_el.attrib["d"]
                    cmds, _vals = build_icons.parse_svg_path(d)
                    self.assertGreater(len(cmds), 0)
                    min_x, min_y, max_x, max_y = build_icons.path_bounds(d)
                    self.assertGreaterEqual(min_x, 1.5, f"{spec.filename} min_x={min_x:.2f} too close to 0")
                    self.assertGreaterEqual(min_y, 1.5, f"{spec.filename} min_y={min_y:.2f} too close to 0")
                    self.assertLessEqual(max_x, 62.5, f"{spec.filename} max_x={max_x:.2f} too close to 64")
                    self.assertLessEqual(max_y, 62.5, f"{spec.filename} max_y={max_y:.2f} too close to 64")


class TestIconByteStability(unittest.TestCase):
    """Verifies that rerunning build_icons produces byte-identical SVG files."""

    def test_rerun_is_byte_identical_to_committed_svgs(self):
        with tempfile.TemporaryDirectory() as tmp1, tempfile.TemporaryDirectory() as tmp2:
            dir1 = Path(tmp1)
            dir2 = Path(tmp2)
            build_icons.build_all_icons(dir1)
            build_icons.build_all_icons(dir2)

            for spec in build_icons.ALL_SPECS:
                with self.subTest(icon=spec.filename):
                    b1 = (dir1 / spec.filename).read_bytes()
                    b2 = (dir2 / spec.filename).read_bytes()
                    committed = (build_icons.DEFAULT_ICON_DIR / spec.filename).read_bytes()
                    self.assertEqual(b1, b2, f"{spec.filename} is not byte-stable across runs")
                    self.assertEqual(b1, committed, f"{spec.filename} on disk differs from generator output")


if __name__ == "__main__":
    unittest.main()
