"""
Bell's station jingle as shipped: jingles.json, the WAV files, their notes pitch-detected in D major pentatonic,
loudness, peak, silent edges and a deterministic render. Run: python -m unittest discover -s tools/music/tests -t
tools/music
"""
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

import build_music
import numpy as np
import synth
from lofi import analysis, jingle
from lofi.theory import D_MAJOR, note_name

CALL_NOTES = ["D5", "E5", "A5"]
ANSWER_NOTES = ["B5", "A5", "F#5", "D5"]
TAIL_S = 0.3
MAX_TAIL_DB = -20.0
MAX_DETUNE_CENTS = 25.0


class JingleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.manifest = json.loads(build_music.JINGLE_MANIFEST.read_text(encoding="utf-8"))
        cls.entries = {entry["id"]: entry for entry in cls.manifest["jingles"]}
        cls.audio = {spec.id: synth.io.read_audio(build_music.REPO / cls.entries[spec.id]["file"])
                     for spec in jingle.JINGLES}

    def test_manifest_lists_the_short_and_the_full_jingle(self):
        self.assertEqual(self.manifest["version"], 1)
        self.assertEqual(self.manifest["sampleRate"], 44100)
        self.assertEqual(list(self.entries), ["bell_jingle_short", "bell_jingle_full"])
        self.assertEqual(self.entries["bell_jingle_short"]["notes"], CALL_NOTES)
        self.assertEqual(self.entries["bell_jingle_full"]["notes"], CALL_NOTES + ANSWER_NOTES)
        for spec in jingle.JINGLES:
            entry = self.entries[spec.id]
            self.assertEqual(entry["file"], f"Assets/_Project/Audio/Music/Jingles/{spec.id}.wav")
            self.assertEqual(entry["onsets"], [round(onset, 4) for onset, _, _, _ in spec.melody])
            path = build_music.REPO / entry["file"]
            self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), entry["sha256"], spec.id)

    def test_files_are_short_mono_44k1_with_silent_edges_and_a_soft_tail(self):
        for jingle_id, (x, rate) in self.audio.items():
            self.assertEqual(rate, 44100)
            self.assertEqual(x.ndim, 1, jingle_id)
            self.assertTrue(1.5 <= x.shape[0] / rate <= 3.0, jingle_id)
            self.assertLessEqual(max(abs(x[0]), abs(x[-1])), build_music.MAX_JINGLE_EDGE, jingle_id)
            tail = x[-int(TAIL_S * rate):]
            tail_db = 20.0 * np.log10(np.sqrt(np.mean(tail ** 2)) / np.max(np.abs(x)))
            self.assertLess(tail_db, MAX_TAIL_DB, f"{jingle_id} must fade out softly")

    def test_notes_are_heard_in_d_major_pentatonic(self):
        for spec in jingle.JINGLES:
            x, rate = self.audio[spec.id]
            for onset, name, _, _ in spec.melody:
                midi = analysis.onset_note(x, rate, onset, *jingle.MELODY_BAND_HZ)
                nearest = round(midi)
                self.assertEqual(note_name(nearest), name, f"{spec.id} at {onset:.2f} s")
                self.assertIn(nearest % 12, D_MAJOR.pentatonic)
                self.assertLess(abs(100.0 * (midi - nearest)), MAX_DETUNE_CENTS)
            stereo = np.stack([x, x], axis=1)
            core = analysis.to_core_rate(stereo, rate)
            self.assertLessEqual(analysis.chroma_out_of_key(core, D_MAJOR.pentatonic),
                                 build_music.MAX_JINGLE_OUT_OF_PENTATONIC, spec.id)

    def test_loudness_and_true_peak_are_within_spec(self):
        for spec in jingle.JINGLES:
            x, rate = self.audio[spec.id]
            core = analysis.to_core_rate(x, rate)
            loudest = synth.analysis.loudness_momentary_max(core, jingle.MOMENTARY_WINDOW_S)
            self.assertLessEqual(abs(loudest - jingle.LOUDNESS_LUFS), build_music.LOUDNESS_TOLERANCE_LU, spec.id)
            self.assertLessEqual(synth.analysis.true_peak_db(x), build_music.MAX_TRUE_PEAK_DBTP, spec.id)

    def test_render_reproduces_the_committed_file_byte_for_byte(self):
        spec = jingle.jingle_by_id("bell_jingle_short")
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / f"{spec.id}.wav"
            master = jingle.render_master(spec, lambda *keys: synth.rng(spec.seed, *keys))
            synth.io.write_wav(path, jingle.to_delivery_rate(master), bits=16, sample_rate=jingle.DELIVERY_RATE)
            self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), self.entries[spec.id]["sha256"])


if __name__ == "__main__":
    unittest.main()
