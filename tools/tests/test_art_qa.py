"""
tools/art/qa: concept_compare's keying, calibration, alignment and IoU on synthetic sheets, and model_lint's
verdicts on synthetic facts. The Blender halves run in the real gates.
Run: python -m unittest discover -s tools/tests -t tools
"""
import sys
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "art" / "qa"))

import concept_compare  # noqa: E402
import model_lint  # noqa: E402

BACKGROUND = (52, 60, 71)
SLOTS = ["R1_CreamEnamel", "R1_Rubber"]


def sheet(height, width):
    image = np.zeros((height, width, 3), np.uint8)
    image[:] = BACKGROUND
    return image


class KeyingTests(unittest.TestCase):
    def test_body_with_a_hole_is_filled_shadow_and_specks_are_dropped(self):
        image = sheet(60, 80)
        image[10:40, 20:60] = (230, 220, 200)
        image[20:25, 30:35] = BACKGROUND            # a hole inside the body
        image[40:46, 15:65] = (26, 30, 35)          # the painted ground shadow: background hue, darker
        image[2:4, 2:4] = (230, 220, 200)           # a speck of label text
        mask = concept_compare.keyed_mask(image, BACKGROUND, 12)
        self.assertEqual(int(mask.sum()), 30 * 40)
        self.assertTrue(mask[22, 32])
        self.assertFalse(mask[43, 40])

    def test_dark_parts_of_another_hue_stay_foreground(self):
        image = sheet(40, 40)
        image[10:30, 10:30] = (40, 40, 45)          # tyre rubber: darker but not the background's hue
        self.assertEqual(int(concept_compare.keyed_mask(image, BACKGROUND, 12).sum()), 400)
        self.assertEqual(int(concept_compare.keyed_mask(image, BACKGROUND, 12, None).sum()), 400)

    def test_border_colour_is_the_median_of_the_edges(self):
        image = sheet(20, 20)
        image[0, 0:3] = (255, 0, 0)
        self.assertEqual(concept_compare.border_colour(image), list(BACKGROUND))


class CompareTests(unittest.TestCase):
    def test_scale_alignment_and_iou(self):
        concept = np.zeros((100, 100), bool)
        concept[20:80, 30:70] = True                # 60 px tall on the sheet = 1.2 m: 0.02 m per pixel
        model = np.zeros((300, 300), bool)
        model[50:170, 100:180] = True               # 1.2 x 1.6 m at 0.01 m per pixel
        results = concept_compare.compare({"front": model}, {"front": concept},
                                          {"view": "front", "axis": "height", "metres": 1.2}, 0.01)
        front = results["front"]
        self.assertAlmostEqual(front["iou"], 1.0, places=3)
        self.assertEqual(front["model_size_m"], [0.8, 1.2])
        self.assertEqual(front["concept_size_m"], [0.8, 1.2])
        self.assertEqual(front["size_ratio"], [1.0, 1.0])

    def test_a_longer_model_scores_lower_and_reports_its_ratio(self):
        concept = np.zeros((50, 120), bool)
        concept[10:30, 10:110] = True
        model = np.zeros((50, 160), bool)
        model[10:30, 10:150] = True
        result = concept_compare.compare({"side": model}, {"side": concept},
                                         {"view": "side", "axis": "height", "metres": 0.2}, 0.01)["side"]
        self.assertAlmostEqual(result["iou"], 100 / 140, places=2)
        self.assertEqual(result["size_ratio"][0], 1.4)

    def test_elevations_share_the_ground_line(self):
        a = np.zeros((40, 20), bool)
        a[20:40, 5:15] = True
        b = np.zeros((10, 10), bool)
        b[:, :] = True
        model, concept = concept_compare.align(concept_compare.crop_to_content(a), b, "front")
        self.assertEqual(model.shape[0] - 1, int(np.nonzero(model)[0].max()))
        self.assertEqual(model.shape[0] - 1, int(np.nonzero(concept)[0].max()))


def facts(**mesh):
    node = {"name": "Body", "type": "MESH", "parent": None, "pivot": [0.0, 0.0, 0.5], "rotation": [0.0, 0.0, 0.0],
            "scale": [1.0, 1.0, 1.0], "local_scale": [1.0, 1.0, 1.0], "triangles": 100, "slots": ["R1_Rubber"],
            "empty_slots": 0, "color_attributes": ["Col"], "non_manifold_edges": 0, "boundary_edges": 0,
            "inconsistent_edges": 0, "closed_shells": 1, "inverted_shells": 0}
    node.update(mesh)
    return {"source": "x.fbx", "nodes": [node]}


def verdicts(report):
    return {c["check"]: c["pass"] for c in report["checks"]}


class LintTests(unittest.TestCase):
    def test_a_clean_model_passes_its_contract(self):
        contract = {"model": "m", "nodes": {"Body": {"pivot": [0, 0, 0.5]}}, "max_triangles": 100,
                    "max_meshes": 1, "max_draw_calls": 1}
        report = model_lint.evaluate(facts(), contract, SLOTS)
        self.assertTrue(report["pass"], report)
        self.assertIn("PASS", model_lint.summary(report).splitlines()[0])

    def test_each_rule_catches_its_defect(self):
        bad = facts(slots=["R1_Rubber", "Material"], color_attributes=[], scale=[2.0, 2.0, 2.0],
                    rotation=[90.0, 0.0, 0.0], non_manifold_edges=1, inconsistent_edges=4, inverted_shells=1,
                    triangles=500, pivot=[0.0, 0.0, 0.7])
        contract = {"model": "m", "nodes": {"Body": {"pivot": [0, 0, 0.5]}, "Wheel": {}}, "max_triangles": 100}
        result = verdicts(model_lint.evaluate(bad, contract, SLOTS))
        self.assertEqual(result, {"required_nodes": False, "pivots": False, "slots": False, "vertex_colors": False,
                                  "triangles": False, "transforms": False, "topology": False})

    def test_allowed_rotation_and_open_borders(self):
        rotated = facts(name="Antenna", rotation=[0.0, 15.0, 0.0], boundary_edges=6)
        self.assertTrue(model_lint.evaluate(rotated, {"allow_rotation": ["Ant*"]}, SLOTS)["pass"])
        closed = model_lint.evaluate(rotated, {"allow_rotation": ["Ant*"], "allow_boundary_edges": False}, SLOTS)
        self.assertFalse(verdicts(closed)["topology"])

    def test_bounds_check_passes_within_tolerance_and_fails_outside(self):
        bounded = facts(bounds={"min": [-0.74, -0.90, 0.0], "max": [0.74, 0.90, 1.59], "size": [1.48, 1.80, 1.59]})
        ok_contract = {
            "model": "m",
            "bounds": {
                "size": [1.48, 1.80, 1.59],
                "tolerance": 0.05,
                "min_size": [1.43, 1.75, 1.54],
                "max_size": [1.53, 1.85, 1.64],
            },
        }
        self.assertTrue(verdicts(model_lint.evaluate(bounded, ok_contract, SLOTS))["bounds"])
        bad_contract = {
            "model": "m",
            "bounds": {
                "size": [1.20, 1.80, 1.59],
                "tolerance": 0.05,
                "min_size": [1.15, 1.75, 1.54],
                "max_size": [1.25, 1.85, 1.64],
            },
        }
        self.assertFalse(verdicts(model_lint.evaluate(bounded, bad_contract, SLOTS))["bounds"])


if __name__ == "__main__":
    unittest.main()
