#!/usr/bin/env python3
"""
Localisation table checker for MoonProject.

Checks Assets/_Project/Data/Localization/en.json and vi.json against each other
and against C# string literals under Assets/_Project/Scripts:
  1. Each table parses as valid JSON and has no duplicate keys.
  2. en and vi have the exact same key set.
  3. Every entry has non-empty text after trimming whitespace.
  4. Placeholders ({0}, {1}, ..., {name}) match as multisets per key between en and vi,
     ignoring escaped '{{' and '}}'.
  5. Every table-shaped whole string literal in C# code exists in the tables.
  6. Reports as INFO (never a failure) table keys that are neither named by a whole
     C# string literal nor covered by an interpolated key pattern (e.g. $"material.{x}.job").

Usage:
  python tools/loc_check.py                 # check the repo containing this script
  python tools/loc_check.py --root <path>   # check another checkout
Tests:
  python -m unittest discover -s tools/tests -t tools
"""
import argparse
from collections import Counter
from dataclasses import dataclass, field
import json
from pathlib import Path
import re
import sys

DEFAULT_ROOT = Path(__file__).resolve().parent.parent
LOC_DIR = Path("Assets/_Project/Data/Localization")
SCRIPTS_DIR = Path("Assets/_Project/Scripts")
LANGUAGES = ("en", "vi")
MAX_INFO_EXAMPLES = 30

# Table keys are two or more dot-separated lowercase alphanumeric/underscore segments
# starting with a letter (e.g. "ui.pause.heading", "upgrade.radio_tower.1.title").
KEY_RE = re.compile(r"^[a-z][a-z0-9]*(?:_[a-z0-9]+)*(?:\.[a-z0-9]+(?:_[a-z0-9]+)*)+$")
KEY_CHARS_RE = re.compile(r"^[a-z0-9_.]*$")
PLACEHOLDER_TOKEN_RE = re.compile(r"\{([A-Za-z0-9_]+)\}")


@dataclass(frozen=True)
class Problem:
    """A single validation failure formatted as `path:key: message`."""
    check: str
    path: str
    key: str
    message: str

    def format(self) -> str:
        return f"{self.path}:{self.key}: {self.message}"


@dataclass
class TableData:
    """Parsed representation of one localisation JSON file."""
    language: str
    path: str
    parsed: bool = False
    entries: dict[str, str] = field(default_factory=dict)
    raw_entries: list[tuple[str, object]] = field(default_factory=list)


@dataclass
class CheckReport:
    """Complete result of running all localisation checks on a root directory."""
    problems: list[Problem] = field(default_factory=list)
    counts_by_check: dict[str, int] = field(default_factory=dict)
    unused_keys: list[str] = field(default_factory=list)

    @property
    def passed(self) -> bool:
        return len(self.problems) == 0


def load_table(root: Path, lang: str) -> tuple[TableData, list[Problem]]:
    """Parse one localisation table and check for structural errors and duplicate keys."""
    rel_path = (LOC_DIR / f"{lang}.json").as_posix()
    full_path = root / rel_path
    table = TableData(language=lang, path=rel_path)
    problems: list[Problem] = []

    if not full_path.is_file():
        problems.append(Problem("parse", rel_path, "<file>", "localisation table file is missing"))
        return table, problems

    try:
        raw_text = full_path.read_text(encoding="utf-8-sig")
    except OSError as exc:
        problems.append(Problem("parse", rel_path, "<file>", f"cannot read file: {exc}"))
        return table, problems

    try:
        data = json.loads(raw_text)
    except json.JSONDecodeError as exc:
        problems.append(
            Problem("parse", rel_path, "<json>", f"invalid JSON at line {exc.lineno} column {exc.colno}: {exc.msg}")
        )
        return table, problems

    if not isinstance(data, dict) or not isinstance(data.get("entries"), list):
        problems.append(Problem("parse", rel_path, "<json>", "table must be a JSON object with an 'entries' list"))
        return table, problems

    table.parsed = True
    seen_counts: Counter[str] = Counter()
    for idx, entry in enumerate(data["entries"]):
        if not isinstance(entry, dict):
            problems.append(Problem("parse", rel_path, f"<entry[{idx}]>", "entry must be a JSON object"))
            continue
        key = entry.get("key")
        if not isinstance(key, str) or not key.strip():
            problems.append(Problem("parse", rel_path, f"<entry[{idx}]>", "entry is missing a non-empty string 'key'"))
            continue
        text = entry.get("text")
        table.raw_entries.append((key, text))
        seen_counts[key] += 1
        if seen_counts[key] == 2:
            problems.append(Problem("parse", rel_path, key, f"duplicate key in {lang}.json"))
        if key not in table.entries:
            table.entries[key] = text if isinstance(text, str) else ""

    return table, problems


def check_key_parity(en_table: TableData, vi_table: TableData) -> list[Problem]:
    """Verify that en.json and vi.json define the exact same set of keys."""
    if not (en_table.parsed and vi_table.parsed):
        return []
    problems: list[Problem] = []
    en_keys = set(en_table.entries)
    vi_keys = set(vi_table.entries)

    for key in sorted(en_keys - vi_keys):
        problems.append(Problem("parity", vi_table.path, key, "key missing from vi.json (present in en.json)"))
    for key in sorted(vi_keys - en_keys):
        problems.append(Problem("parity", en_table.path, key, "key missing from en.json (present in vi.json)"))
    return problems


def check_non_empty(tables: list[TableData]) -> list[Problem]:
    """Verify that every entry in each parsed table has non-empty text after trimming."""
    problems: list[Problem] = []
    for table in tables:
        if not table.parsed:
            continue
        reported: set[str] = set()
        for key, text in table.raw_entries:
            if key in reported:
                continue
            if not isinstance(text, str) or not text.strip():
                problems.append(Problem("non_empty", table.path, key, "entry text is empty after trimming"))
                reported.add(key)
    return problems


def extract_placeholders(text: str) -> list[str]:
    """
    Extract positional ({0}, {1}, ...) and named ({name}) placeholders from text.

    Escaped double braces ('{{' and '}}') are skipped from left to right before
    matching placeholder tokens so sequences like '{{{0}}}' or '{0}}}' yield '{0}'.
    """
    tokens: list[str] = []
    n = len(text)
    i = 0
    while i < n:
        if text.startswith("{{", i) or text.startswith("}}", i):
            i += 2
            continue
        if text[i] == "{":
            match = PLACEHOLDER_TOKEN_RE.match(text, i)
            if match:
                close_pos = match.end() - 1
                close_run = 0
                while close_pos + close_run < n and text[close_pos + close_run] == "}":
                    close_run += 1
                if close_run % 2 == 1:
                    tokens.append(match.group(0))
                    i = match.end()
                    continue
        i += 1
    return tokens


def check_placeholders(en_table: TableData, vi_table: TableData) -> list[Problem]:
    """Verify that shared keys have the same multiset of placeholders in en and vi."""
    if not (en_table.parsed and vi_table.parsed):
        return []
    problems: list[Problem] = []
    shared_keys = sorted(set(en_table.entries) & set(vi_table.entries))
    for key in shared_keys:
        en_tokens = sorted(extract_placeholders(en_table.entries[key]))
        vi_tokens = sorted(extract_placeholders(vi_table.entries[key]))
        if en_tokens != vi_tokens:
            en_desc = ", ".join(en_tokens) if en_tokens else "none"
            vi_desc = ", ".join(vi_tokens) if vi_tokens else "none"
            problems.append(
                Problem(
                    "placeholders",
                    vi_table.path,
                    key,
                    f"placeholder mismatch between en ({en_desc}) and vi ({vi_desc})",
                )
            )
    return problems


def is_key_shaped(value: str) -> bool:
    """Return True when a literal has the shape of a localisation table key."""
    return bool(KEY_RE.match(value))


def _unescape_regular_string(raw: str) -> str:
    """Decode common C# escapes in a non-verbatim string literal."""
    out: list[str] = []
    i = 0
    n = len(raw)
    escapes = {"n": "\n", "r": "\r", "t": "\t", "\\": "\\", '"': '"', "'": "'", "0": "\0"}
    while i < n:
        if raw[i] == "\\" and i + 1 < n:
            nxt = raw[i + 1]
            out.append(escapes.get(nxt, "\\" + nxt))
            i += 2
        else:
            out.append(raw[i])
            i += 1
    return "".join(out)


def _skip_interpolation_hole(text: str, start: int) -> int:
    """
    Advance from the opening '{' of an interpolation hole to the index after its matching '}'.

    Nested braces, character literals, and string literals inside the hole are skipped so
    inner quotes do not terminate the enclosing interpolated string early.
    """
    n = len(text)
    i = start + 1
    depth = 1
    while i < n and depth > 0:
        c = text[i]
        if text.startswith("//", i):
            end = text.find("\n", i)
            i = n if end < 0 else end + 1
        elif text.startswith("/*", i):
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
        elif c == '"' or (c in "@$" and re.match(r'[@$]{1,2}"', text[i:i + 3])):
            prefix_match = re.match(r'[@$]*"', text[i:i + 3])
            prefix = prefix_match.group(0)
            verbatim = "@" in prefix
            j = i + len(prefix)
            while j < n:
                if verbatim and text.startswith('""', j):
                    j += 2
                elif not verbatim and text[j] == "\\":
                    j += 2
                elif text[j] == '"':
                    j += 1
                    break
                elif not verbatim and text[j] == "\n":
                    break
                else:
                    j += 1
            i = j
        elif c == "'":
            char_match = re.match(r"'(?:\\.[^'\n]*|[^'\\\n])'", text[i:i + 12])
            i += char_match.end() if char_match else 1
        elif c == "{":
            depth += 1
            i += 1
        elif c == "}":
            depth -= 1
            i += 1
        else:
            i += 1
    return i


CSHARP_TOKEN_RE = re.compile(
    r'(?m)^[ \t]*#[^\n]*|//[^\n]*|/\*[\s\S]*?\*/|\'(?:\\.[^\'\\\n]*|[^\'\\\n])\'|[@$]{1,2}"|"'
)
REGULAR_STRING_END_RE = re.compile(r'(?:\\.|[^"\\\n])*"')
VERBATIM_STRING_END_RE = re.compile(r'(?:""|[^"])*"')


def extract_csharp_strings(text: str) -> tuple[list[str], list[list[str]]]:
    """
    Extract whole string literals and interpolated string templates from C# source text.

    Comments ('//', '/* */'), preprocessor directives ('#...'), and character literals are
    ignored. Returns:
      - literals: list of plain/verbatim string values
      - interpolated_segments: list of static segment lists for each interpolated string
        (e.g. `$"material.{x}.job"` -> `["material.", ".job"]`)
    """
    literals: list[str] = []
    interpolated_segments: list[list[str]] = []
    n = len(text)
    i = 0

    while i < n:
        match = CSHARP_TOKEN_RE.search(text, i)
        if match is None:
            break
        token = match.group(0)
        if not token.endswith('"'):
            i = match.end()
            continue

        prefix = token
        verbatim = "@" in prefix
        interpolated = "$" in prefix
        j = match.end()

        if not interpolated:
            end_re = VERBATIM_STRING_END_RE if verbatim else REGULAR_STRING_END_RE
            end_match = end_re.match(text, j)
            if end_match is None:
                i = n
                break
            raw = text[j:end_match.end() - 1]
            value = raw.replace('""', '"') if verbatim else _unescape_regular_string(raw)
            literals.append(value)
            i = end_match.end()
        else:
            segments: list[str] = []
            current: list[str] = []
            while j < n:
                if verbatim and text.startswith('""', j):
                    current.append('"')
                    j += 2
                elif not verbatim and text[j] == "\\" and j + 1 < n:
                    current.append(_unescape_regular_string(text[j:j + 2]))
                    j += 2
                elif text.startswith("{{", j):
                    current.append("{")
                    j += 2
                elif text.startswith("}}", j):
                    current.append("}")
                    j += 2
                elif text[j] == "{":
                    segments.append("".join(current))
                    current = []
                    hole_end = _skip_interpolation_hole(text, j)
                    inner_lits, inner_interps = extract_csharp_strings(text[j + 1:max(j + 1, hole_end - 1)])
                    literals.extend(inner_lits)
                    interpolated_segments.extend(inner_interps)
                    j = hole_end
                elif text[j] == '"':
                    segments.append("".join(current))
                    if len(segments) > 1:
                        interpolated_segments.append(segments)
                    else:
                        literals.append(segments[0])
                    j += 1
                    break
                elif not verbatim and text[j] == "\n":
                    break
                else:
                    current.append(text[j])
                    j += 1
            i = j

    return literals, interpolated_segments


def build_interpolated_pattern(segments: list[str]) -> re.Pattern[str] | None:
    """
    Convert static segments of an interpolated string into a key-matching regex if key-like.

    For example, `["material.", ".job"]` from `$"material.{x}.job"` becomes
    `^material\\.[a-z0-9_.]+\\.job$`, and `["hint.", ""]` from `$"hint.{kind}"` becomes
    `^hint\\.[a-z0-9_.]+$`. Interpolated strings whose static parts contain non-key
    characters or no dot at all return None.
    """
    if len(segments) < 2 or not segments[0]:
        return None
    if not ("a" <= segments[0][0] <= "z"):
        return None
    if not all(KEY_CHARS_RE.match(seg) for seg in segments):
        return None
    if not any("." in seg for seg in segments):
        return None
    pattern = "^" + r"[a-z0-9_.]+".join(re.escape(seg) for seg in segments) + "$"
    return re.compile(pattern)


def check_csharp_keys(
    root: Path, table_keys: set[str]
) -> tuple[list[Problem], set[str], list[re.Pattern[str]]]:
    """
    Scan C# files under Assets/_Project/Scripts for table-shaped string literals and patterns.

    Every whole string literal that is key-shaped must exist in `table_keys`. Returns:
      - problems for missing key-shaped literals
      - the set of all key-shaped whole literals seen in C#
      - compiled regex patterns for key-like interpolated strings seen in C#
    """
    scripts_root = root / SCRIPTS_DIR
    problems: list[Problem] = []
    named_keys: set[str] = set()
    interp_patterns: list[re.Pattern[str]] = []
    seen_patterns: set[str] = set()

    if not scripts_root.exists():
        return problems, named_keys, interp_patterns

    for cs_path in sorted(scripts_root.rglob("*.cs")):
        if not cs_path.is_file():
            continue
        rel_path = cs_path.relative_to(root).as_posix()
        text = cs_path.read_text(encoding="utf-8-sig")
        literals, interpolated_list = extract_csharp_strings(text)

        reported_in_file: set[str] = set()
        for literal in literals:
            if not is_key_shaped(literal):
                continue
            named_keys.add(literal)
            if literal not in table_keys and literal not in reported_in_file:
                problems.append(
                    Problem("csharp_keys", rel_path, literal, "key named in C# literal does not exist in tables")
                )
                reported_in_file.add(literal)

        for segments in interpolated_list:
            regex = build_interpolated_pattern(segments)
            if regex is not None and regex.pattern not in seen_patterns:
                seen_patterns.add(regex.pattern)
                interp_patterns.append(regex)

    return problems, named_keys, interp_patterns


def find_unused_table_keys(
    table_keys: set[str], named_keys: set[str], interp_patterns: list[re.Pattern[str]]
) -> list[str]:
    """
    Return sorted table keys that are neither named by a whole C# literal nor matched by
    any interpolated key pattern in C# code.
    """
    unused: list[str] = []
    for key in sorted(table_keys):
        if key in named_keys:
            continue
        if any(pattern.match(key) for pattern in interp_patterns):
            continue
        unused.append(key)
    return unused


def check_root(root: Path) -> CheckReport:
    """Run all localisation table checks against `root` and return a structured report."""
    root = Path(root)
    en_lang, vi_lang = LANGUAGES
    en_table, en_parse_problems = load_table(root, en_lang)
    vi_table, vi_parse_problems = load_table(root, vi_lang)

    parse_problems = en_parse_problems + vi_parse_problems
    parity_problems = check_key_parity(en_table, vi_table)
    non_empty_problems = check_non_empty([en_table, vi_table])
    placeholder_problems = check_placeholders(en_table, vi_table)

    all_table_keys = set(en_table.entries) | set(vi_table.entries)
    csharp_problems, named_keys, interp_patterns = check_csharp_keys(root, all_table_keys)
    unused_keys = find_unused_table_keys(all_table_keys, named_keys, interp_patterns)

    problems = (
        parse_problems
        + parity_problems
        + non_empty_problems
        + placeholder_problems
        + csharp_problems
    )
    counts = {
        "parse": len(parse_problems),
        "parity": len(parity_problems),
        "non_empty": len(non_empty_problems),
        "placeholders": len(placeholder_problems),
        "csharp_keys": len(csharp_problems),
    }
    return CheckReport(problems=problems, counts_by_check=counts, unused_keys=unused_keys)


def print_report(root: Path, report: CheckReport) -> None:
    """Print problem lines, per-check summaries, INFO unused keys, and final RESULT line."""
    print(f"loc_check: {root}")
    for problem in report.problems:
        print(problem.format())

    labels = (
        ("parse", "1. table parse & duplicate keys"),
        ("parity", "2. en/vi key set parity"),
        ("non_empty", "3. non-empty text"),
        ("placeholders", "4. placeholder match"),
        ("csharp_keys", "5. C# literal keys in tables"),
    )
    for check_id, label in labels:
        count = report.counts_by_check.get(check_id, 0)
        status = "OK" if count == 0 else f"FAIL ({count} problem(s))"
        print(f"  [{label}] {status}")

    unused_count = len(report.unused_keys)
    examples = report.unused_keys[:MAX_INFO_EXAMPLES]
    print(
        f"  [6. INFO unreferenced table keys] {unused_count} key(s) not named by C# literal or interpolated prefix"
    )
    if examples:
        shown = ", ".join(examples)
        suffix = f" ... (+{unused_count - len(examples)} more)" if unused_count > len(examples) else ""
        print(f"  INFO examples ({len(examples)}/{unused_count}): {shown}{suffix}")

    total = len(report.problems)
    print("RESULT: " + ("PASS" if total == 0 else f"FAIL ({total} problem(s))"))


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", help="checkout root to check (default: script's parent's parent)")
    args = ap.parse_args(argv)

    root = Path(args.root).resolve() if args.root else DEFAULT_ROOT
    report = check_root(root)
    print_report(root, report)
    return 0 if report.passed else 1


if __name__ == "__main__":
    sys.exit(main())
