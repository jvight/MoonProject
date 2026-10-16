"""
tools/art/review: the R1 palette read from the game's recipes, the GLB material rewrite, and round merging in the
review manifest. Run: python -m unittest discover -s tools/tests -t tools
"""
import struct
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "art" / "review"))

import r1_palette  # noqa: E402
import stage_round  # noqa: E402

REPO = Path(__file__).resolve().parents[2]


def tiny_glb(path, names):
    gltf = {"asset": {"version": "2.0"}, "materials": [{"name": n, "emissiveFactor": [1, 1, 1]} for n in names]}
    r1_palette.write_glb(path, gltf, struct.pack("<II", 4, 0x004E4942) + b"\0\0\0\0")


class PaletteTests(unittest.TestCase):
    def test_every_r1_slot_is_read_from_the_recipes(self):
        palette = r1_palette.load_palette(REPO)
        self.assertEqual(len(palette), 14)
        self.assertEqual(palette["R1_BareSteel"].metallic, 1.0)
        self.assertEqual(palette["R1_CreamEnamel"].metallic, 0.0)
        self.assertTrue(any(palette["R1_LampAmber"].emissive))
        self.assertFalse(any(palette["R1_Rubber"].emissive))
        for look in palette.values():
            self.assertTrue(all(0.0 <= c <= 1.0 for c in look.base))
            self.assertTrue(0.0 <= look.roughness <= 1.0)

    def test_materials_are_rewritten_by_slot_and_duplicates_folded(self):
        palette = r1_palette.load_palette(REPO)
        with tempfile.TemporaryDirectory() as tmp:
            src, dst = Path(tmp, "in.glb"), Path(tmp, "out.glb")
            tiny_glb(src, ["R1_Rubber", "R1_CreamEnamel.001"])
            used = r1_palette.apply_palette(src, dst, palette)
            gltf, rest = r1_palette.read_glb(dst)
        self.assertEqual(used, ["R1_CreamEnamel", "R1_Rubber"])
        self.assertEqual([m["name"] for m in gltf["materials"]], ["R1_Rubber", "R1_CreamEnamel"])
        self.assertNotIn("emissiveFactor", gltf["materials"][0])
        self.assertEqual(gltf["materials"][1]["pbrMetallicRoughness"]["metallicFactor"], 0.0)
        self.assertTrue(rest.endswith(b"\0\0\0\0"))

    def test_a_material_outside_the_slots_stops_the_round(self):
        with tempfile.TemporaryDirectory() as tmp:
            src = Path(tmp, "in.glb")
            tiny_glb(src, ["R1_Rubber", "Material"])
            with self.assertRaises(ValueError):
                r1_palette.apply_palette(src, Path(tmp, "out.glb"), r1_palette.load_palette(REPO))


class ManifestTests(unittest.TestCase):
    def entry(self, round_, concepts=None):
        return {"round": round_, "glb": f"r{round_}.txt", "concepts": concepts or [], "captures": [], "changed": "",
                "staged": "2026-10-15", "stats": {}}

    def test_rounds_merge_in_order_replace_and_inherit_concepts(self):
        manifest = {"models": []}
        stage_round.merge_round(manifest, "rover", "07", self.entry(2, [{"src": "c2.jpg", "label": "c"}]))
        stage_round.merge_round(manifest, "rover", None, self.entry(1, [{"src": "c1.jpg", "label": "c"}]))
        stage_round.merge_round(manifest, "rover", None, self.entry(3))
        stage_round.merge_round(manifest, "rover", None, self.entry(2, [{"src": "c2b.jpg", "label": "c"}]))
        rounds = manifest["models"][0]["rounds"]
        self.assertEqual([r["round"] for r in rounds], [1, 2, 3])
        self.assertEqual(rounds[1]["concepts"][0]["src"], "c2b.jpg")
        self.assertEqual(rounds[2]["concepts"][0]["src"], "c2.jpg")

    def test_a_new_model_needs_a_name(self):
        with self.assertRaises(SystemExit):
            stage_round.merge_round({"models": []}, "pad", None, self.entry(1))


if __name__ == "__main__":
    unittest.main()
