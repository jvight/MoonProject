#!/usr/bin/env python3
"""
dead_code: C# types, and the members of Core's event structs and interfaces, that nothing references.

    python tools/dead_code.py [--domain UI] [--show-allowed] [--allowlist FILE] [--root PATH]

A type declared under Assets/_Project/Scripts/** (outside the test assemblies) is a finding when its name appears
nowhere else, by whole-word search, in:
- any Python file under tools/ (the command-line tools name editor methods and types as strings), or
- any C# file under Assets/_Project (comments are ignored; a string literal counts only when it is one identifier or a
  dotted type path, the way reflection names a type), or
- a .uxml or .uss file, or
- an asset, prefab or scene that holds the type's script GUID (MonoBehaviours and ScriptableObjects).
Occurrences inside the type's own body do not count. Occurrences inside another dead type do not count either, so a
type only used by dead code is found too: "level 1" is referenced by nothing, "level 2" by level 1 only, and so on,
and deleting a level makes the next one appear. A type that only tests reference is a finding marked "tests only".

Entry points Unity or the project's tooling reach by reflection are never findings: a type that has or contains an
entry attribute ([MenuItem], [InitializeOnLoad], [MoonBuilder], [CustomEditor], [RuntimeInitializeOnLoadMethod], ...),
is a batch entry (its body calls BatchRunner.Run, so `-executeMethod` reaches it), implements a reflectively discovered interface (ISceneContributor, ICallbacks, IPrebuildSetup, IPostBuildCleanup), or
derives from an editor entry base (AssetPostprocessor, EditorWindow, Editor, ...). tools/dead_code_allowlist.txt lists
the rest, one pattern per line ('#' starts a comment; * and ? match).

Two more checks run on the Core contracts:
- a public member of a struct in Core/Events or of an interface in Core that no code outside it uses (events: by
  `.Member` in a file that mentions the struct; interfaces: by `.Member` anywhere): "Type.Member";
- an event struct nothing constructs: "Type.new".

Output is grouped by domain (the folder under Scripts) and sorted. --domain limits the report, not the search.
Exit code 1 when a finding is not allowlisted. Whole-word search over-counts a name shared with a common word, so a
clean report does not prove a type is used; a finding is a strong hint.
Tests: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import bisect
import fnmatch
import re
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

import lint

SCRIPTS = "Assets/_Project/Scripts/"
ALLOWLIST = Path(__file__).resolve().parent / "dead_code_allowlist.txt"
TEXT_SUFFIXES = (".uxml", ".uss")
ASSET_SUFFIXES = (".unity", ".prefab", ".asset", ".uxml", ".controller", ".playable", ".overrideController")
WORD_RE = re.compile(r"[A-Za-z_]\w*")
IDENT_STRING_RE = re.compile(r'"([A-Za-z_][\w.]*)"')
USING_RE = re.compile(r"^[ \t]*using[ \t]+(?:static[ \t]+)?[\w.= \t]+;", re.M)
NAMESPACE_LINE_RE = re.compile(r"\bnamespace[ \t]+[\w.]+")
GUID_RE = re.compile(r"guid:\s*([0-9a-f]{32})")
EVENTS_FOLDER = "Core/Events/"
ATTRIBUTE_SUFFIX = "Attribute"
ENTRY_ATTRIBUTES = ("MenuItem", "InitializeOnLoad", "InitializeOnLoadMethod", "RuntimeInitializeOnLoadMethod",
                    "MoonBuilder", "CustomEditor", "CustomPropertyDrawer", "ContextMenu", "SettingsProvider",
                    "SettingsProviderGroup", "DidReloadScripts", "OnOpenAsset")
ENTRY_INTERFACES = frozenset({"ISceneContributor", "ICallbacks", "IPrebuildSetup", "IPostBuildCleanup"})
ENTRY_BASES = frozenset({"AssetPostprocessor", "Editor", "EditorWindow", "PropertyDrawer", "ScriptedImporter",
                         "Attribute"})
BATCH_ENTRY = "BatchRunner.Run("
ATTRIBUTE_RE = re.compile(r"\[\s*(?:" + "|".join(ENTRY_ATTRIBUTES) + r")\b")


@dataclass
class Finding:
    id: str            # "Name", "Name.Member" or "Name.new": what the allowlist matches
    domain: str
    kind: str          # the declaration kind, "member" or "never constructed"
    path: str
    line: int
    level: int = 1
    tests_only: bool = False
    allowed: bool = False

    def format(self):
        notes = ([f"level {self.level}"] if self.level > 1 else []) + (["tests only"] if self.tests_only else [])
        suffix = f"  [{', '.join(notes)}]" if notes else ""
        return f"  {self.id}  ({self.kind})  {self.path}:{self.line}{suffix}"


@dataclass
class Index:
    """Everything the searches need, built once from the sources."""
    sources: dict                                  # path -> lint.SourceFile (.cs only)
    structures: dict                               # path -> lint.Structure
    occurrences: dict = field(default_factory=lambda: defaultdict(list))   # word -> [(path, offset)]
    spans: dict = field(default_factory=dict)      # path -> [(start, end, name)]
    kinds: dict = field(default_factory=dict)      # path -> runtime / editor / tests


def domain_of(path):
    rest = path[len(SCRIPTS):] if path.startswith(SCRIPTS) else path
    return rest.split("/", 1)[0] or "(root)"


def blank_namespace_lines(code):
    """The code with 'using X.Y;' and 'namespace X.Y' words blanked: a namespace is not a reference to a type."""
    def blank(match):
        return re.sub(r"\S", " ", match.group(0))
    return NAMESPACE_LINE_RE.sub(blank, USING_RE.sub(blank, code))


def add_words(index, path, text, code):
    for match in WORD_RE.finditer(code):
        index.occurrences[match.group(0)].append((path, match.start()))
    for match in IDENT_STRING_RE.finditer(text):
        if code[match.start()] == '"':
            for word in match.group(1).split("."):
                if word:
                    index.occurrences[word].append((path, match.start(1)))


def build_index(sources, extra_texts=()):
    """Index of the .cs <sources> (lint.SourceFile list) plus (path, text) pairs of .uxml/.uss files."""
    cs = {s.path: s for s in sources if s.path.endswith(".cs")}
    index = Index(cs, {})
    for path, src in cs.items():
        structure = lint.parse_structure(src)
        index.structures[path] = structure
        index.kinds[path] = src.kind
        index.spans[path] = sorted((t.start, t.end, t.name) for t in structure.types if t.end > t.start)
        add_words(index, path, src.text, blank_namespace_lines(src.code))
    for path, text in extra_texts:
        index.kinds[path] = lint.RUNTIME
        index.spans[path] = []
        for match in WORD_RE.finditer(text):
            index.occurrences[match.group(0)].append((path, match.start()))
    return index


def innermost(spans, offset):
    """Name of the smallest type span holding <offset>, or None."""
    best = None
    for start, end, name in spans[:bisect.bisect_right(spans, (offset, float("inf"), ""))]:
        if start <= offset < end and (best is None or end - start < best[1] - best[0]):
            best = (start, end, name)
    return best[2] if best else None


def aliases(name):
    return [name, name[:-len(ATTRIBUTE_SUFFIX)]] if name.endswith(ATTRIBUTE_SUFFIX) and len(name) > len(
        ATTRIBUTE_SUFFIX) else [name]


def base_closure(structures):
    """{type name: every base and interface name it reaches through project types}."""
    direct = defaultdict(set)
    for structure in structures.values():
        for t in structure.types:
            direct[t.name].update(t.bases)
    closure = {}

    def reach(name, seen):
        if name in closure:
            return closure[name]
        out = set()
        for base in direct.get(name, ()):
            if base in seen:
                continue
            out.add(base)
            out |= reach(base, seen | {base})
        closure[name] = out
        return out

    for name in list(direct):
        reach(name, {name})
    return closure


def entry_points(index, closure):
    """Names of types the engine or the tooling reaches by reflection."""
    names = set()
    for path, structure in index.structures.items():
        members_by_owner = defaultdict(list)
        for member in structure.members:
            members_by_owner[id(member.owner)].append(member)
        for t in structure.types:
            reached = closure.get(t.name, set()) | set(t.bases)
            body = index.sources[path].code[t.start:t.end]
            if (ATTRIBUTE_RE.search(t.attrs) or reached & (ENTRY_INTERFACES | ENTRY_BASES) or BATCH_ENTRY in body
                    or any(ATTRIBUTE_RE.search(m.attrs) for m in members_by_owner[id(t)])):
                names.add(t.name)
    return names


def guid_referenced_types(index, root, candidates, closure):
    """Names of Unity-object types whose script GUID an asset, prefab or scene holds."""
    unity_types = {name for name in candidates if closure.get(name, set()) & lint.UNITY_OBJECT_BASES}
    if not unity_types:
        return set()
    held = set()
    for path in Path(root, "Assets").rglob("*"):
        if path.is_file() and path.suffix in ASSET_SUFFIXES:
            held.update(GUID_RE.findall(path.read_text(encoding="utf-8", errors="replace")))
    guids = {}
    for path in index.sources:
        meta = Path(root, path + ".meta")
        if meta.exists():
            match = GUID_RE.search(meta.read_text(encoding="utf-8", errors="replace"))
            if match:
                guids[path] = match.group(1)
    return {t.name for path, structure in index.structures.items() for t in structure.types
            if t.name in unity_types and guids.get(path) in held}


def references(index, name, dead, own_spans):
    """(live, tests) reference counts of <name>: occurrences outside its own bodies and outside dead types."""
    live = tests = 0
    for alias in aliases(name):
        for path, offset in index.occurrences.get(alias, ()):
            spans = index.spans[path]
            holder = innermost(spans, offset)
            if holder == name and any(s <= offset < e for s, e in own_spans.get(path, ())):
                continue
            if holder in dead:
                continue
            if index.kinds[path] == lint.TESTS:
                tests += 1
            else:
                live += 1
    return live, tests


def type_findings(index, root, allow_roots=None):
    candidates = {}
    own_spans = defaultdict(lambda: defaultdict(list))
    for path, structure in index.structures.items():
        for t in structure.types:
            own_spans[t.name][path].append((t.start, t.end))
            if path.startswith(SCRIPTS) and index.kinds[path] != lint.TESTS:
                candidates.setdefault(t.name, t)
    closure = base_closure(index.structures)
    roots = entry_points(index, closure) | guid_referenced_types(index, root, candidates, closure)
    dead, found = {}, []
    level = 1
    while True:
        newly = []
        for name, decl in candidates.items():
            if name in dead or name in roots:
                continue
            live, tests = references(index, name, dead, own_spans[name])
            if not live:
                newly.append((name, decl, tests))
        if not newly:
            break
        for name, decl, tests in newly:
            dead[name] = level
            found.append(Finding(name, domain_of(decl.path), decl.kind, decl.path, decl.line, level, tests > 0))
        level += 1
    return found, dead


def public_member_name(member):
    sig, accessors, _ = lint.signature(member)
    sig = sig.strip()
    words = re.findall(r"\b\w+\b", sig)
    if not words or "operator" in words:
        return None
    paren = sig.find("(")
    head = sig[:paren] if paren >= 0 else sig
    names = re.findall(r"\b\w+\b", head)
    return names[-1] if names else None


def is_public(member):
    kind = member.owner.kind
    sig = lint.signature(member)[0]
    return kind == "interface" or bool(re.search(r"\bpublic\b", sig))


def preceded_by(code, offset, pattern):
    """True when the text just before <offset> (spaces skipped) matches <pattern>, a regex ending in '$'."""
    return re.search(pattern, code[max(0, offset - 16):offset]) is not None


def member_uses(index, owner, name, files=None):
    """(live, tests) counts of '.name' member accesses and nameof(name) outside <owner>'s body, in <files> when given."""
    live = tests = 0
    for path, offset in index.occurrences.get(name, ()):
        if files is not None and path not in files:
            continue
        code = index.sources[path].code if path in index.sources else ""
        if not (preceded_by(code, offset, r"\.\s*$") or preceded_by(code, offset, r"\bnameof\s*\(\s*(?:\w+\.)*$")):
            continue
        if path == owner.path and owner.start <= offset < owner.end:
            continue
        if index.kinds[path] == lint.TESTS:
            tests += 1
        else:
            live += 1
    return live, tests


def contract_member_findings(index, dead):
    """Unused public members of Core event structs and Core interfaces."""
    found = []
    for path, structure in index.structures.items():
        if not path.startswith(SCRIPTS + "Core/") or index.kinds[path] == lint.TESTS:
            continue
        for member in structure.members:
            owner = member.owner
            events = owner.kind == "struct" and EVENTS_FOLDER in path
            if owner.name in dead or not (events or owner.kind == "interface") or not is_public(member):
                continue
            name = public_member_name(member)
            if not name or name == owner.name:
                continue
            files = {p for p, _ in index.occurrences.get(owner.name, ())} if events else None
            live, tests = member_uses(index, owner, name, files)
            if not live:
                found.append(Finding(f"{owner.name}.{name}", domain_of(path), "member", path,
                                     index.sources[path].line_of(member.start), 1, tests > 0))
    return found


def never_constructed(index, dead):
    """Core event structs no non-test code constructs."""
    found = []
    for path, structure in index.structures.items():
        if not path.startswith(SCRIPTS) or EVENTS_FOLDER not in path or index.kinds[path] == lint.TESTS:
            continue
        for t in structure.types:
            if t.kind != "struct" or t.name in dead:
                continue
            live = tests = 0
            for other, offset in index.occurrences.get(t.name, ()):
                code = index.sources[other].code if other in index.sources else ""
                if not (preceded_by(code, offset, r"\bnew\s+$") or preceded_by(code, offset, r"\bdefault\s*\(\s*$")):
                    continue
                if other == path and t.start <= offset < t.end:
                    continue
                if index.kinds[other] == lint.TESTS:
                    tests += 1
                else:
                    live += 1
            if not live:
                found.append(Finding(f"{t.name}.new", domain_of(path), "never constructed", path, t.line, 1,
                                     tests > 0))
    return found


def load_allowlist(path):
    patterns = []
    if Path(path).exists():
        for line in Path(path).read_text(encoding="utf-8").splitlines():
            entry = line.split("#", 1)[0].strip()
            if entry:
                patterns.append(entry)
    return patterns


def analyse(root, allowlist=()):
    """Every finding under <root>, with .allowed set from <allowlist> patterns, sorted by domain then id."""
    root = Path(root)
    sources = [s for s in lint.load_sources(root) if s.path.endswith(".cs")]
    texts = [(p.relative_to(root).as_posix(), p.read_text(encoding="utf-8", errors="replace"))
             for p in (root / "Assets" / "_Project").rglob("*") if p.is_file() and p.suffix in TEXT_SUFFIXES]
    scripts = [(p.relative_to(root).as_posix(), p.read_text(encoding="utf-8", errors="replace"))
               for p in (root / "tools").rglob("*.py") if "tests" not in p.parts and p.name != Path(__file__).name]
    index = build_index(sources, texts + scripts)
    findings, dead = type_findings(index, root)
    findings += contract_member_findings(index, dead) + never_constructed(index, dead)
    for finding in findings:
        finding.allowed = any(fnmatch.fnmatchcase(finding.id, pattern) for pattern in allowlist)
    return sorted(findings, key=lambda f: (f.domain, f.id))


def render(findings, domains=None, show_allowed=False):
    """The report text and the number of findings that are neither filtered out nor allowlisted."""
    wanted = {d.lower() for d in domains} if domains else None
    shown = [f for f in findings if (wanted is None or f.domain.lower() in wanted)]
    open_ = [f for f in shown if not f.allowed]
    lines = []
    for domain in sorted({f.domain for f in shown}):
        group = [f for f in shown if f.domain == domain and (show_allowed or not f.allowed)]
        if group:
            lines.append(f"{domain} ({len(group)})")
            lines += [f.format() + ("  [allowlisted]" if f.allowed else "") for f in group]
    allowed = len(shown) - len(open_)
    lines.append(f"dead_code: {len(open_)} finding(s)" + (f", {allowed} allowlisted" if allowed else ""))
    return "\n".join(lines), len(open_)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", help="checkout to scan (default: current git worktree)")
    ap.add_argument("--domain", action="append", help="only report this domain (repeatable), e.g. UI, Core")
    ap.add_argument("--allowlist", default=str(ALLOWLIST), help="allowlist file")
    ap.add_argument("--show-allowed", action="store_true", help="also list allowlisted findings")
    args = ap.parse_args()
    root = Path(args.root).resolve() if args.root else lint.default_root()
    report, open_count = render(analyse(root, load_allowlist(args.allowlist)), args.domain, args.show_allowed)
    print(report)
    return 1 if open_count else 0


if __name__ == "__main__":
    sys.exit(main())
