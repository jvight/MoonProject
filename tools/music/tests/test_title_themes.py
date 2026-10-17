"""
Title theme variants (A, B, C) as shipped: title.json, committed .ogg files, -20 LUFS loudness, seamless loops,
pure D major pentatonic harmony/melody, zero drums, and the Logs/titlemusic/index.html audition page.
Run: python -m unittest discover -s tools/music/tests -t tools/music
"""
import json
import tempfile
import unittest
from pathlib import Path

import build_music
import synth
from lofi import analysis
from lofi.composer import compose
from lofi.spec import TITLE_BPM, TITLE_TARGET_LUFS
from lofi.theory import D_MAJOR, key_named
from lofi.title_themes import DEFAULT_TITLE_THEME_ID, TITLE_DESCRIPTIONS_VI, TITLE_THEMES

PLAYLIST_KEYS = ("id", "file", "title", "bpm", "key", "duration", "loudnessLufs", "truePeakDbtp", "pcmSha256")
TITLE_SECONDS = (120.0, 180.0)
SAMPLE_RATE = 48000


def _manifest(path):
    return json.loads(path.read_text(encoding="utf-8"))


class TitleManifestTests(unittest.TestCase):
    def test_title_json_lists_all_three_variants_with_playlist_schema(self):
        manifest = _manifest(build_music.TITLE.manifest)
        self.assertEqual(manifest["version"], 1)
        self.assertEqual([t["id"] for t in manifest["tracks"]], [s.id for s in TITLE_THEMES])
        self.assertEqual(len(TITLE_THEMES), 3)
        self.assertEqual(DEFAULT_TITLE_THEME_ID, "title_a_turning_moon")
        for entry, spec in zip(manifest["tracks"], TITLE_THEMES):
            self.assertEqual(set(entry), set(PLAYLIST_KEYS), entry["id"])
            self.assertEqual(entry["file"], f"Assets/_Project/Audio/Music/Title/{spec.id}.ogg")
            path = build_music.REPO / entry["file"]
            self.assertTrue(path.exists(), entry["file"])
            self.assertEqual((entry["bpm"], entry["key"]), (spec.bpm, spec.key))
            self.assertEqual(synth.io.audio_digest(path), entry["pcmSha256"], entry["id"])


class ShippedTitleAudioTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.audio = {}
        for entry in _manifest(build_music.TITLE.manifest)["tracks"]:
            cls.audio[entry["id"]] = (entry, *synth.io.read_audio(build_music.REPO / entry["file"]))

    def test_title_themes_hit_minus_twenty_lufs_and_stay_under_peak_ceiling(self):
        for track_id, (entry, x, rate) in self.audio.items():
            self.assertEqual((rate, x.shape[1]), (SAMPLE_RATE, 2), track_id)
            loudness = synth.analysis.loudness_integrated(x)
            peak = synth.analysis.true_peak_db(x)
            self.assertLessEqual(abs(loudness - TITLE_TARGET_LUFS), build_music.LOUDNESS_TOLERANCE_LU, track_id)
            self.assertLessEqual(peak, build_music.MAX_TRUE_PEAK_DBTP, track_id)
            self.assertAlmostEqual(loudness, entry["loudnessLufs"], delta=1e-3)
            self.assertAlmostEqual(peak, entry["truePeakDbtp"], delta=1e-3)

    def test_title_themes_last_two_to_three_minutes_and_loop_seamlessly(self):
        for spec in TITLE_THEMES:
            _, x, rate = self.audio[spec.id]
            duration_s = x.shape[0] / rate
            self.assertTrue(TITLE_SECONDS[0] <= duration_s <= TITLE_SECONDS[1], f"{spec.id}: {duration_s}s")
            seam = analysis.seam(x)
            self.assertLessEqual(seam["ratio"], build_music.MAX_SEAM_RATIO, spec.id)
            self.assertLessEqual(seam["loudness_step_lu"], build_music.MAX_SEAM_STEP_LU, spec.id)

    def test_title_themes_keep_key_and_tempo(self):
        for spec in TITLE_THEMES:
            _, x, _ = self.audio[spec.id]
            self.assertLessEqual(analysis.chroma_out_of_key(x, D_MAJOR.pentatonic),
                                 build_music.MAX_CHROMA_OUT_OF_KEY, spec.id)
            self.assertIn(analysis.detect_scale(x)["scale"], ("D major", "A major"), spec.id)
            bpm, _ = analysis.estimate_tempo(x)
            self.assertLessEqual(abs(bpm - spec.bpm) / spec.bpm, build_music.MAX_TEMPO_ERROR, spec.id)


class TitleDesignTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scores = {spec.id: compose(spec) for spec in TITLE_THEMES}

    def test_every_title_theme_is_pure_d_major_pentatonic_with_zero_drums(self):
        pent = D_MAJOR.pentatonic
        for spec in TITLE_THEMES:
            self.assertTrue(spec.title_theme, spec.id)
            self.assertTrue(all(section.drums == "off" for section in spec.form), spec.id)
            self.assertEqual(spec.ending, "loop", spec.id)
            self.assertEqual(spec.key, "D major", spec.id)
            self.assertEqual(spec.mood.texture, "old_radio", spec.id)
            self.assertTrue(TITLE_BPM[0] <= spec.bpm <= TITLE_BPM[1], spec.id)
            score = self.scores[spec.id]
            self.assertEqual(score.drums, [], spec.id)
            for slot in score.chords:
                for p in slot.keys_voicing + slot.pad_voicing:
                    self.assertIn(p % 12, pent, f"{spec.id} chord {slot.chord.symbol} pitch {p}")
                self.assertTrue((slot.chord.melodic_pcs(D_MAJOR) | {slot.chord.bass}) <= pent,
                                f"{spec.id} chord {slot.chord.symbol} not pentatonic")
            for part_name, events in score.parts.items():
                for note in events:
                    self.assertIn(note.pitch % 12, pent, f"{spec.id} {part_name} pitch {note.pitch}")

    def test_variants_a_b_c_reflect_balanced_melodic_and_minimal_roles(self):
        lead_counts = {
            spec.id: sum(len(notes) for name, notes in self.scores[spec.id].parts.items() if name.startswith("lead."))
            for spec in TITLE_THEMES
        }
        self.assertGreater(lead_counts["title_b_earthlight_cradle"], lead_counts["title_a_turning_moon"])
        self.assertGreater(lead_counts["title_a_turning_moon"], lead_counts["title_c_quiet_horizon"])
        bpms = {spec.id: spec.bpm for spec in TITLE_THEMES}
        self.assertEqual(min(bpms, key=bpms.get), "title_c_quiet_horizon")
        self.assertEqual(max(bpms, key=bpms.get), "title_b_earthlight_cradle")

    def test_audition_page_writes_audio_players_vietnamese_descriptions_and_default_pick(self):
        with tempfile.TemporaryDirectory() as tmp:
            out_dir = Path(tmp)
            original_dir = build_music.TITLE_AUDITION_DIR
            try:
                build_music.TITLE_AUDITION_DIR = out_dir
                page = build_music.write_title_audition_page()
                html = page.read_text(encoding="utf-8")
                self.assertIn(DEFAULT_TITLE_THEME_ID, html)
                for spec in TITLE_THEMES:
                    self.assertIn(f'{spec.id}.ogg', html)
                    self.assertIn(TITLE_DESCRIPTIONS_VI[spec.id], html)
                    self.assertTrue((out_dir / f"{spec.id}.ogg").exists(), spec.id)
            finally:
                build_music.TITLE_AUDITION_DIR = original_dir


if __name__ == "__main__":
    unittest.main()
