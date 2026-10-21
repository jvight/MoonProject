"""
tools/autoplay.py helpers: the player's command line and reading the bot's verdict from its report.
Run: python -m unittest discover -s tools/tests -t tools
"""
import tempfile
import unittest
from pathlib import Path

import autoplay


class LaunchArgumentTests(unittest.TestCase):
    def test_the_command_line_names_slot_log_and_the_bots_flags(self):
        args = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "B3", 600, 20)
        pairs = dict(zip(args[::2], args[1::2]))
        self.assertEqual(pairs["-saveSlot"], "autoplay-slot")
        self.assertEqual(pairs["-autoplay"], "opening")
        self.assertEqual(pairs["-autoplay-out"], str(Path("out")))
        self.assertEqual(pairs["-autoplay-until"], "B3")
        self.assertEqual(pairs["-autoplay-timeout"], "600")
        self.assertEqual(pairs["-autoplay-shot-seconds"], "20")
        self.assertEqual(pairs["-screen-fullscreen"], "0")

    def test_without_a_last_beat_the_scenario_plays_to_its_end(self):
        args = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600, 20)
        self.assertNotIn("-autoplay-until", args)

    def test_the_lines_flag_is_sent_only_when_the_opening_is_checked_with_its_lines(self):
        none = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600, 20)
        self.assertNotIn("-autoplay-lines", none)
        shown = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600, 20,
                                          lines=True)
        pairs = dict(zip(shown[::2], shown[1::2]))
        self.assertEqual(pairs["-autoplay-lines"], "1")

    def test_the_naive_flag_is_sent_only_when_asked_for(self):
        plain = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600, 20)
        self.assertNotIn("-autoplay-naive", plain)
        naive = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600, 20,
                                          naive=True)
        pairs = dict(zip(naive[::2], naive[1::2]))
        self.assertEqual(pairs["-autoplay-naive"], "1")

    def test_guidance_is_strict_unless_made_lenient(self):
        strict = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600, 20)
        self.assertNotIn("-autoplay-strict-guidance", strict)
        lenient = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600,
                                            20, strict_guidance=False)
        pairs = dict(zip(lenient[::2], lenient[1::2]))
        self.assertEqual(pairs["-autoplay-strict-guidance"], "0")

    def test_sound_is_muted_by_default_and_kept_when_sound_flag_is_on(self):
        muted = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600, 20)
        self.assertIn("-autoplay-mute", muted)
        with_sound = autoplay.launch_arguments("autoplay-slot", Path("out"), Path("out/player.log"), "opening", "", 600,
                                               20, sound=True)
        self.assertNotIn("-autoplay-mute", with_sound)


class VerdictTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.out = Path(self._tmp.name)

    def test_no_report_has_no_verdict(self):
        self.assertIsNone(autoplay.verdict(self.out)[0])

    def test_the_result_line_decides(self):
        (self.out / "report.md").write_text("# Autoplay report: opening\n\n**Result: PASS**\n", encoding="utf-8")
        self.assertEqual(autoplay.verdict(self.out)[0], "PASS")
        (self.out / "report.md").write_text("# Autoplay report: opening\n\n**Result: FAIL**\n", encoding="utf-8")
        self.assertEqual(autoplay.verdict(self.out)[0], "FAIL")

    def test_a_report_without_a_result_line_has_no_verdict(self):
        (self.out / "report.md").write_text("# Autoplay report: opening\n", encoding="utf-8")
        self.assertIsNone(autoplay.verdict(self.out)[0])


class CompileConstraintTests(unittest.TestCase):
    def test_unity_define_constraints_support_or_and_not(self):
        import compile_check
        self.assertTrue(compile_check.constraint_met("A || B", ["B"]))
        self.assertFalse(compile_check.constraint_met("A || B", ["C"]))
        self.assertTrue(compile_check.constraint_met("!A", ["B"]))
        self.assertFalse(compile_check.constraint_met("!A", ["A"]))
        self.assertTrue(compile_check.constraint_met("UNITY_INCLUDE_TESTS", ["UNITY_INCLUDE_TESTS"]))


if __name__ == "__main__":
    unittest.main()
