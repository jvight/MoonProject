"""
tools/moonbatch/defines.py: a define added to the Standalone scripting symbols for one run, and the file put back.
Run: python -m unittest discover -s tools/tests -t tools
"""
import tempfile
import unittest
from pathlib import Path

from moonbatch import defines

SETTINGS = (
    "PlayerSettings:\r\n"
    "  scriptingBackend:\r\n"
    "    Standalone: 1\r\n"
    "  scriptingDefineSymbols:\r\n"
    "    Android: UNITY_MCP_READY\r\n"
    "    Standalone: UNITY_MCP_READY;UNITY_MCP_DEPS_6\r\n"
    "  other:\r\n"
    "    Standalone: 0\r\n"
)


class DefinesTests(unittest.TestCase):
    def test_a_symbol_joins_the_standalone_scripting_line_only(self):
        text = defines.add_defines(SETTINGS, ["LUNARION_AUTOPLAY"])
        self.assertIn("    Standalone: UNITY_MCP_READY;UNITY_MCP_DEPS_6;LUNARION_AUTOPLAY\r\n", text)
        self.assertIn("    Android: UNITY_MCP_READY\r\n", text)
        self.assertIn("    Standalone: 1\r\n", text)
        self.assertIn("    Standalone: 0\r\n", text)

    def test_a_symbol_already_there_is_not_added_twice(self):
        once = defines.add_defines(SETTINGS, ["A"])
        self.assertEqual(once, defines.add_defines(once, ["A"]))

    def test_the_settings_file_is_put_back_byte_for_byte_even_when_the_run_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / defines.SETTINGS
            path.parent.mkdir(parents=True)
            path.write_bytes(SETTINGS.encode("utf-8"))
            with self.assertRaises(RuntimeError):
                with defines.defined(tmp, ["LUNARION_AUTOPLAY"]):
                    self.assertIn("LUNARION_AUTOPLAY", path.read_text(encoding="utf-8"))
                    raise RuntimeError("the run failed")
            self.assertEqual(path.read_bytes(), SETTINGS.encode("utf-8"))

    def test_no_symbols_leave_the_file_alone(self):
        with tempfile.TemporaryDirectory() as tmp:
            with defines.defined(tmp, []):
                pass


if __name__ == "__main__":
    unittest.main()
