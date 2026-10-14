"""
Unit tests for tools/loc_check.py using temporary directory fixtures.
Run: python -m unittest discover -s tools/tests -t tools
"""
import contextlib
import io
import json
import tempfile
import unittest
from pathlib import Path

import loc_check


def write_table(root: Path, lang: str, entries: list[dict], extra_top: dict | None = None) -> Path:
    loc_dir = root / "Assets/_Project/Data/Localization"
    loc_dir.mkdir(parents=True, exist_ok=True)
    payload = {"language": lang, "name": lang.upper(), "entries": entries}
    if extra_top:
        payload.update(extra_top)
    path = loc_dir / f"{lang}.json"
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    return path


def write_script(root: Path, rel_path: str, content: str) -> Path:
    path = root / "Assets/_Project/Scripts" / rel_path
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    return path


class TableParseAndDuplicateTests(unittest.TestCase):
    def test_valid_tables_with_extra_entry_fields_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            entries = [{"key": "ui.pause.heading", "text": "Paused", "context": "pause menu"}]
            write_table(root, "en", entries)
            write_table(root, "vi", [{"key": "ui.pause.heading", "text": "Tạm dừng", "notes": "ok"}])
            write_script(root, "UI/Pause.cs", 'class P { const string K = "ui.pause.heading"; }\n')

            report = loc_check.check_root(root)
            self.assertTrue(report.passed)
            self.assertEqual(report.problems, [])

    def test_malformed_json_is_reported(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            loc_dir = root / "Assets/_Project/Data/Localization"
            loc_dir.mkdir(parents=True)
            (loc_dir / "en.json").write_text("{not valid json", encoding="utf-8")
            write_table(root, "vi", [{"key": "ui.pause.heading", "text": "Tạm dừng"}])

            report = loc_check.check_root(root)
            self.assertFalse(report.passed)
            self.assertEqual(report.counts_by_check["parse"], 1)
            self.assertEqual(report.problems[0].path, "Assets/_Project/Data/Localization/en.json")
            self.assertIn("invalid JSON", report.problems[0].message)

    def test_duplicate_keys_are_reported_for_each_table(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(
                root,
                "en",
                [
                    {"key": "ui.pause.resume", "text": "Resume"},
                    {"key": "ui.pause.resume", "text": "Continue"},
                ],
            )
            write_table(root, "vi", [{"key": "ui.pause.resume", "text": "Tiếp tục"}])
            write_script(root, "UI/Pause.cs", 'class P { const string K = "ui.pause.resume"; }\n')

            report = loc_check.check_root(root)
            self.assertFalse(report.passed)
            self.assertEqual(report.counts_by_check["parse"], 1)
            self.assertEqual(
                report.problems[0].format(),
                "Assets/_Project/Data/Localization/en.json:ui.pause.resume: duplicate key in en.json",
            )


class KeyParityTests(unittest.TestCase):
    def test_matching_key_sets_in_different_order_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(
                root,
                "en",
                [
                    {"key": "hint.scan", "text": "Scan"},
                    {"key": "hint.hold", "text": "Hold"},
                ],
            )
            write_table(
                root,
                "vi",
                [
                    {"key": "hint.hold", "text": "Giữ"},
                    {"key": "hint.scan", "text": "Quét"},
                ],
            )

            report = loc_check.check_root(root)
            self.assertTrue(report.passed)
            self.assertEqual(report.counts_by_check["parity"], 0)

    def test_keys_missing_on_either_side_are_reported(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(
                root,
                "en",
                [
                    {"key": "ui.shared.key", "text": "Shared"},
                    {"key": "ui.only.en", "text": "English only"},
                ],
            )
            write_table(
                root,
                "vi",
                [
                    {"key": "ui.shared.key", "text": "Chung"},
                    {"key": "ui.only.vi", "text": "Chỉ tiếng Việt"},
                ],
            )

            report = loc_check.check_root(root)
            self.assertFalse(report.passed)
            self.assertEqual(report.counts_by_check["parity"], 2)
            formatted = [p.format() for p in report.problems]
            self.assertEqual(
                formatted,
                [
                    "Assets/_Project/Data/Localization/vi.json:ui.only.en: key missing from vi.json (present in en.json)",
                    "Assets/_Project/Data/Localization/en.json:ui.only.vi: key missing from en.json (present in vi.json)",
                ],
            )


class NonEmptyTextTests(unittest.TestCase):
    def test_trimmed_empty_or_whitespace_text_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(
                root,
                "en",
                [
                    {"key": "ui.empty.one", "text": ""},
                    {"key": "ui.empty.two", "text": "Good"},
                ],
            )
            write_table(
                root,
                "vi",
                [
                    {"key": "ui.empty.one", "text": "Có chữ"},
                    {"key": "ui.empty.two", "text": "   \t\n  "},
                ],
            )

            report = loc_check.check_root(root)
            self.assertFalse(report.passed)
            self.assertEqual(report.counts_by_check["non_empty"], 2)
            self.assertEqual(
                [(p.path, p.key) for p in report.problems],
                [
                    ("Assets/_Project/Data/Localization/en.json", "ui.empty.one"),
                    ("Assets/_Project/Data/Localization/vi.json", "ui.empty.two"),
                ],
            )


class PlaceholderMatchTests(unittest.TestCase):
    def test_matching_positional_named_and_escaped_braces_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(
                root,
                "en",
                [
                    {"key": "ui.tape.count", "text": "{0} of {1} — {speaker} {{literal}} {{{2}}} {0}}}"},
                ],
            )
            write_table(
                root,
                "vi",
                [
                    {"key": "ui.tape.count", "text": "{{escaped}} {speaker}: {1} trên {0} ({2}) / {0}"},
                ],
            )

            report = loc_check.check_root(root)
            self.assertTrue(report.passed)
            self.assertEqual(report.counts_by_check["placeholders"], 0)

    def test_mismatched_placeholder_multiset_or_escaped_braces_fail(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(
                root,
                "en",
                [
                    {"key": "ui.mismatch.count", "text": "{0} and {0}"},
                    {"key": "ui.mismatch.named", "text": "Hello {name}"},
                    {"key": "ui.mismatch.escaped", "text": "Literal {{0}}"},
                    {"key": "ui.mismatch.unclosed", "text": "Unclosed {0}}"},
                ],
            )
            write_table(
                root,
                "vi",
                [
                    {"key": "ui.mismatch.count", "text": "Chỉ một {0}"},
                    {"key": "ui.mismatch.named", "text": "Xin chào {other}"},
                    {"key": "ui.mismatch.escaped", "text": "Thật {0}"},
                    {"key": "ui.mismatch.unclosed", "text": "Thật {0}"},
                ],
            )

            report = loc_check.check_root(root)
            self.assertFalse(report.passed)
            self.assertEqual(report.counts_by_check["placeholders"], 4)
            keys = [p.key for p in report.problems]
            self.assertEqual(
                keys,
                ["ui.mismatch.count", "ui.mismatch.escaped", "ui.mismatch.named", "ui.mismatch.unclosed"],
            )


class CSharpKeyLiteralsTests(unittest.TestCase):
    def test_existing_keys_non_key_literals_comments_and_interpolations_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(
                root,
                "en",
                [
                    {"key": "ui.pause.heading", "text": "Paused"},
                    {"key": "hint.scan", "text": "Scan"},
                ],
            )
            write_table(
                root,
                "vi",
                [
                    {"key": "ui.pause.heading", "text": "Tạm dừng"},
                    {"key": "hint.scan", "text": "Quét"},
                ],
            )
            write_script(
                root,
                "UI/Sample.cs",
                """
                #region "missing.in.directive"
                // "missing.in.line_comment"
                /* "missing.in.block_comment" */
                class Sample
                {
                    const string A = "ui.pause.heading";
                    const string B = @"hint.scan";
                    const string NotAKey1 = "Plain Words";
                    const string NotAKey2 = "singleword";
                    const string NotAKey3 = "Upper.Case";
                    const string NotAKey4 = "1.0";
                    string Built(string id) => $"material.{id}.job";
                }
                """,
            )

            report = loc_check.check_root(root)
            self.assertTrue(report.passed)
            self.assertEqual(report.counts_by_check["csharp_keys"], 0)

    def test_missing_key_shaped_literal_in_csharp_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_table(root, "en", [{"key": "ui.pause.heading", "text": "Paused"}])
            write_table(root, "vi", [{"key": "ui.pause.heading", "text": "Tạm dừng"}])
            write_script(
                root,
                "UI/Missing.cs",
                """
                class Missing
                {
                    const string Ok = "ui.pause.heading";
                    const string Bad = "ui.pause.missing_key";
                    const string BadAgain = "ui.pause.missing_key";
                }
                """,
            )

            report = loc_check.check_root(root)
            self.assertFalse(report.passed)
            self.assertEqual(report.counts_by_check["csharp_keys"], 1)
            self.assertEqual(
                report.problems[0].format(),
                "Assets/_Project/Scripts/UI/Missing.cs:ui.pause.missing_key: key named in C# literal does not exist in tables",
            )


class UnusedKeysInfoTests(unittest.TestCase):
    def test_interpolated_prefix_covers_matching_keys_and_unused_is_info_only(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            entries_en = [
                {"key": "ui.pause.heading", "text": "Paused"},
                {"key": "material.metal.job", "text": "Frames"},
                {"key": "material.optics.job", "text": "Lenses"},
                {"key": "hint.scan", "text": "Scan"},
                {"key": "unused.alpha.one", "text": "One"},
                {"key": "unused.beta.two", "text": "Two"},
            ]
            entries_vi = [
                {"key": e["key"], "text": f"VI {e['text']}"}
                for e in entries_en
            ]
            write_table(root, "en", entries_en)
            write_table(root, "vi", entries_vi)
            write_script(
                root,
                "UI/Refs.cs",
                """
                class Refs
                {
                    const string Direct = "ui.pause.heading";
                    string MatJob(string x) => $"material.{x}.job";
                    string HintKey(string k) => $"hint.{k}";
                }
                """,
            )

            report = loc_check.check_root(root)
            self.assertTrue(report.passed)
            self.assertEqual(report.unused_keys, ["unused.alpha.one", "unused.beta.two"])

            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                code = loc_check.main(["--root", str(root)])
            output = buf.getvalue()
            self.assertEqual(code, 0)
            self.assertIn("2 key(s) not named by C# literal or interpolated prefix", output)
            self.assertIn("unused.alpha.one, unused.beta.two", output)
            self.assertIn("RESULT: PASS", output)

    def test_info_examples_are_capped_at_thirty(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            entries_en = [{"key": f"unused.item.k{i:02d}", "text": f"Item {i}"} for i in range(35)]
            entries_vi = [{"key": f"unused.item.k{i:02d}", "text": f"Mục {i}"} for i in range(35)]
            write_table(root, "en", entries_en)
            write_table(root, "vi", entries_vi)

            buf = io.StringIO()
            with contextlib.redirect_stdout(buf):
                code = loc_check.main(["--root", str(root)])
            output = buf.getvalue()
            self.assertEqual(code, 0)
            self.assertIn("INFO examples (30/35):", output)
            self.assertIn("... (+5 more)", output)
            self.assertIn("unused.item.k29", output)
            self.assertNotIn("unused.item.k30", output)


if __name__ == "__main__":
    unittest.main()
