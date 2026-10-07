"""
Ro's cassette tapes as shipped: tapes.json, the files, loudness and peak, seamless loops, distinct designs, and the
radio's playlist.json left exactly as it was. Run: python -m unittest discover -s tools/music/tests -t tools/music
"""
import hashlib
import json
import unittest

import build_music
import synth
from lofi import analysis
from lofi.composer import compose
from lofi.jingle import QUOTES
from lofi.mixer import TARGET_LUFS
from lofi.tapes import TAPES
from lofi.theory import D_MAJOR, key_named
from lofi.tracks import CATALOGUE, PLAYLIST

# docs/STORY.md "Cassettes": the M3-05 tapes and their titles.
CASSETTES = {"after_dark_1": "Lumen After Dark, Vol. 1", "dust_and_honey": "Dust & Honey", "slow_orbit": "Slow Orbit"}
PLAYLIST_KEYS = ("id", "file", "title", "bpm", "key", "duration", "loudnessLufs", "truePeakDbtp", "pcmSha256")
# playlist.json as committed with the six radio tracks (b395e44), line endings normalised. The radio must never list
# a tape (it plays one only once 07 has collected it), so this file is frozen while tapes ship in tapes.json.
FROZEN_PLAYLIST_SHA256 = "4973c6294fe5852ffe52827b90957dae3a54b008c934bc93e69fa4a62c96e2d4"
TAPE_SECONDS = (120.0, 150.0)
SAMPLE_RATE = 48000
A = 9


def _manifest(path):
    return json.loads(path.read_text(encoding="utf-8"))


class ManifestTests(unittest.TestCase):
    def test_tapes_json_lists_every_tape_once_with_the_playlist_schema_plus_tape(self):
        manifest = _manifest(build_music.TAPE_DECK.manifest)
        self.assertEqual(manifest["version"], 1)
        self.assertEqual([t["id"] for t in manifest["tracks"]], [s.id for s in TAPES])
        self.assertEqual(set(CASSETTES), {s.id for s in TAPES})
        for entry, spec in zip(manifest["tracks"], TAPES):
            self.assertEqual(set(entry), set(PLAYLIST_KEYS) | {"tape"}, entry["id"])
            self.assertEqual(entry["tape"], spec.id)
            self.assertEqual(entry["title"], CASSETTES[spec.id])
            self.assertEqual(entry["file"], f"Assets/_Project/Audio/Music/Tapes/{spec.id}.ogg")
            self.assertTrue((build_music.REPO / entry["file"]).exists(), entry["file"])
            self.assertEqual((entry["bpm"], entry["key"]), (spec.bpm, spec.key))

    def test_playlist_json_is_unchanged_and_holds_no_tape(self):
        text = build_music.RADIO.manifest.read_bytes().replace(b"\r\n", b"\n")
        self.assertEqual(hashlib.sha256(text).hexdigest(), FROZEN_PLAYLIST_SHA256)
        manifest = json.loads(text)
        self.assertEqual([t["id"] for t in manifest["tracks"]], [s.id for s in PLAYLIST])
        for entry in manifest["tracks"]:
            self.assertNotIn("tape", entry)
            self.assertTrue(entry["file"].startswith("Assets/_Project/Audio/Music/Radio/"), entry["file"])

    def test_manifest_digests_match_the_committed_audio(self):
        for collection in build_music.COLLECTIONS:
            for entry in _manifest(collection.manifest)["tracks"]:
                path = build_music.REPO / entry["file"]
                self.assertEqual(synth.io.audio_digest(path), entry["pcmSha256"], entry["id"])


class ShippedAudioTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.audio = {}
        for collection in build_music.COLLECTIONS:
            for entry in _manifest(collection.manifest)["tracks"]:
                cls.audio[entry["id"]] = (entry, *synth.io.read_audio(build_music.REPO / entry["file"]))

    def test_loudness_and_true_peak_are_within_spec_and_match_the_manifests(self):
        for track_id, (entry, x, rate) in self.audio.items():
            self.assertEqual((rate, x.shape[1]), (SAMPLE_RATE, 2), track_id)
            loudness = synth.analysis.loudness_integrated(x)
            peak = synth.analysis.true_peak_db(x)
            self.assertLessEqual(abs(loudness - TARGET_LUFS), build_music.LOUDNESS_TOLERANCE_LU, track_id)
            self.assertLessEqual(peak, build_music.MAX_TRUE_PEAK_DBTP, track_id)
            self.assertAlmostEqual(loudness, entry["loudnessLufs"], delta=1e-3)
            self.assertAlmostEqual(peak, entry["truePeakDbtp"], delta=1e-3)

    def test_tapes_last_two_to_two_and_a_half_minutes(self):
        for spec in TAPES:
            _, x, rate = self.audio[spec.id]
            self.assertTrue(TAPE_SECONDS[0] <= x.shape[0] / rate <= TAPE_SECONDS[1], spec.id)

    def test_tapes_loop_without_a_seam(self):
        for spec in TAPES:
            _, x, _ = self.audio[spec.id]
            seam = analysis.seam(x)
            self.assertLessEqual(seam["ratio"], build_music.MAX_SEAM_RATIO, spec.id)
            self.assertLessEqual(seam["loudness_step_lu"], build_music.MAX_SEAM_STEP_LU, spec.id)

    def test_tapes_keep_the_key_and_their_tempo(self):
        for spec in TAPES:
            _, x, _ = self.audio[spec.id]
            self.assertLessEqual(analysis.chroma_out_of_key(x, key_named(spec.key).pcs),
                                 build_music.MAX_CHROMA_OUT_OF_KEY, spec.id)
            self.assertEqual(analysis.detect_scale(x)["scale"], "D major", spec.id)
            bpm, _ = analysis.estimate_tempo(x)
            self.assertLessEqual(abs(bpm - spec.bpm) / spec.bpm, build_music.MAX_TEMPO_ERROR, spec.id)


class TapeDesignTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scores = {spec.id: compose(spec) for spec in TAPES}

    def test_tapes_differ_from_each_other_and_from_the_radio(self):
        radio_leads = {p.lead for s in PLAYLIST for p in s.form if p.lead}
        for spec in TAPES:
            others = [s for s in CATALOGUE if s is not spec]
            self.assertNotIn(spec.bpm, {s.bpm for s in others}, spec.id)
            self.assertNotIn(spec.family, {s.family for s in others}, spec.id)
            self.assertNotIn(spec.drums, {s.drums for s in others}, spec.id)
            self.assertNotEqual(spec.mood.texture, "vinyl", spec.id)
            leads = {p.lead for p in spec.form if p.lead}
            self.assertFalse(leads & radio_leads, spec.id)
        tapes = {s.id: s for s in TAPES}
        self.assertEqual(min(CATALOGUE, key=lambda s: s.bpm).id, "slow_orbit")
        self.assertEqual(max(CATALOGUE, key=lambda s: s.bpm).id, "after_dark_1")
        self.assertEqual(max(CATALOGUE, key=lambda s: s.mood.warmth_hz).id, "after_dark_1")
        self.assertEqual(tapes["slow_orbit"].keys_voice, "felt")
        self.assertLess(tapes["dust_and_honey"].mood.width, 1.0)

    def test_every_tape_comes_back_round_to_its_first_chord(self):
        for spec in TAPES:
            score = self.scores[spec.id]
            self.assertEqual(spec.ending, "loop")
            self.assertEqual(score.chords[0].chord.root, D_MAJOR.tonic, spec.id)
            self.assertEqual(score.chords[-1].chord.root, A, f"{spec.id} must end on the dominant")
            self.assertEqual(spec.form[-1].opening[1], spec.form[0].opening[0], spec.id)

    def test_after_dark_opens_with_the_station_jingle(self):
        vibes = self.scores["after_dark_1"].parts["lead.vibes"]
        jingle = [pitch for _, _, pitch in QUOTES["jingle"]]
        self.assertEqual([n.pitch for n in vibes[:len(jingle)]], jingle)
        self.assertLess(vibes[len(jingle) - 1].beat, 8.0)

    def test_kenji_thumps_once_while_the_drums_rest(self):
        score = self.scores["dust_and_honey"]
        thumps = [h for h in score.drums if h.voice == "thump"]
        self.assertEqual(len(thumps), 1)
        bar = int(thumps[0].beat // 4)
        same_bar = [h for h in score.drums if h.voice != "thump" and int(h.beat // 4) == bar]
        self.assertEqual(same_bar, [])


if __name__ == "__main__":
    unittest.main()
