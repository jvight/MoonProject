#!/usr/bin/env python3
"""
Static lint for MoonProject's C# sources: the "Zero technical debt" rules of CLAUDE.md plus the Unity pitfalls the
compiler cannot see (a Unity object built in a MonoBehaviour field initializer compiles, then Unity refuses it).

compile_check.py runs this, so the one gate stays `python tools/compile_check.py` -> "RESULT: PASS". Each rule is
either enforced (its findings fail the gate) or report-only (listed, never failing): a rule main still breaks stays
report-only until the owning boxes have fixed it, then its `enforced` flag flips. There are no allowlists and no
in-code suppressions; a finding is fixed in the code or the rule is wrong.

Usage:
  python tools/lint.py                 # every finding in the worktree you are in, with its owning box
  python tools/lint.py --root <path>   # lint another checkout
  python tools/lint.py --rules         # list the rules and whether each is enforced
Exit code 1 when an enforced rule has findings.
Tests: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import bisect
import json
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

SOURCE_ROOT = Path("Assets/_Project")
CODE_SUFFIXES = (".cs",)
SHADER_SUFFIXES = (".shader", ".hlsl", ".cginc", ".compute")
NAMESPACE_PREFIX = "MoonProject"

RUNTIME, EDITOR, TESTS = "runtime", "editor", "tests"
ALL_KINDS = (RUNTIME, EDITOR, TESTS)


@dataclass(frozen=True)
class Rule:
    id: str
    summary: str
    enforced: bool
    kinds: tuple = ALL_KINDS


RULES = {r.id: r for r in (
    Rule("debt-marker", "TODO/FIXME/HACK left in source", True),
    Rule("find-api", "scene search by name or type (GameObject.Find*, Find*ObjectByType, FindObject(s)OfType)", True,
         (RUNTIME, EDITOR)),
    Rule("banned-api", "SendMessage/BroadcastMessage, Resources.Load, legacy UnityEngine.Input, OnGUI, WheelCollider",
         True),
    Rule("linq-runtime", "using System.Linq in a runtime assembly", True, (RUNTIME,)),
    Rule("static-mutable", "static mutable state (non-readonly static field, static auto-property setter, static "
                           "event)", True),
    Rule("unity-api-in-initializer", "Unity object or API in a MonoBehaviour/ScriptableObject field initializer "
                                     "(Unity refuses it inside the constructor)", True),
    Rule("duplicate-type", "public type name declared in two MoonProject namespaces", False),
)}

# Box that owns each folder under Assets/_Project (docs/ROADMAP.md "Các box (agent) và vùng sở hữu").
SCRIPT_OWNERS = {"Art": "art", "Audio": "audio", "Gameplay": "gameplay", "Rover": "rover", "UI": "ui",
                 "World": "world", "Core": "director", "App": "director", "Editor": "foundation"}
TEST_OWNERS = {"EditMode": "director", "PlayMode": "foundation"}

# UnityEngine.Object types whose constructor Unity refuses while it constructs a scripted object.
UNITY_OBJECT_TYPES = ("MaterialPropertyBlock", "Material", "Mesh", "Texture2D", "Texture3D", "Texture2DArray",
                      "Cubemap", "CubemapArray", "RenderTexture", "CustomRenderTexture", "GameObject",
                      "ComputeBuffer", "GraphicsBuffer", "AudioClip", "Sprite", "PhysicsMaterial", "AnimationClip",
                      "Shader", "ComputeShader", "TerrainData")
UNITY_STATIC_CALLS = (r"Shader\s*\.\s*(PropertyToID|Find)", r"ScriptableObject\s*\.\s*CreateInstance",
                      r"(Object\s*\.\s*)?Instantiate\s*[<(]", r"Camera\s*\.\s*main",
                      r"(?:Time|Application)\s*\.\s*(?:[a-z]\w*|[A-Z]\w*\s*\()", r"Random\s*\.\s*(Range|value|insideUnit\w+|onUnit\w+|rotation\w*)",
                      r"Sprite\s*\.\s*Create", r"AudioClip\s*\.\s*Create", r"Mesh\s*\.\s*\w+Instance",
                      r"Resources\s*\.\s*\w+", r"AssetDatabase\s*\.\s*\w+")
# Bases that make a class a scripted UnityEngine.Object, constructed by Unity off the normal call path.
UNITY_OBJECT_BASES = {"MonoBehaviour", "ScriptableObject", "StateMachineBehaviour", "EditorWindow", "Editor",
                      "ScriptableWizard", "ScriptableSingleton", "ScriptedImporter", "PlayableAsset", "TrackAsset"}

DEBT_RE = re.compile(r"\b(TODO|FIXME|HACK)\b")
FIND_RE = re.compile(r"\bGameObject\s*\.\s*Find(?:WithTag|GameObjectWithTag|GameObjectsWithTag)?\b"
                     r"|\bFind(?:ObjectOfType|ObjectsOfType|ObjectsOfTypeAll|FirstObjectByType|AnyObjectByType"
                     r"|ObjectsByType)\b")
BANNED = (
    (re.compile(r"\b(?:SendMessage|SendMessageUpwards|BroadcastMessage)\s*\("), "SendMessage-style call"),
    (re.compile(r"\bResources\s*\.\s*Load(?:All|Async)?\b"), "Resources.Load"),
    (re.compile(r"\bUnityEngine\s*\.\s*Input\b"), "legacy UnityEngine.Input"),
    (re.compile(r"(?<![\w.])Input\s*\.\s*(?:GetKey\w*|GetAxis\w*|GetButton\w*|GetMouseButton\w*|mousePosition"
                r"|mouseScrollDelta|anyKey\w*|inputString|touchCount|GetTouch|touches|acceleration|gyro|compass"
                r"|ResetInputAxes|mousePresent|touchSupported)\b"), "legacy UnityEngine.Input"),
    (re.compile(r"\bvoid\s+OnGUI\s*\("), "OnGUI"),
    (re.compile(r"\bWheelCollider\b"), "WheelCollider"),
)
LINQ_RE = re.compile(r"^[ \t]*using\s+(?:static\s+)?System\s*\.\s*Linq\b", re.M)
ATTRIBUTES_RE = re.compile(r"^(?:\s*\[[^\[\]]*\])+")
TYPE_HEADER_RE = re.compile(r"(?:^|[\s;])(class|struct|interface|enum|record)\s+(\w+)")
NAMESPACE_RE = re.compile(r"\bnamespace\s+([\w.]+)\s*$")
DELEGATE_RE = re.compile(r"\bdelegate\b[^;]*?\b(\w+)\s*(?:<[^<>]*>)?\s*\($")
TARGET_NEW_RE = re.compile(r"^\s*new\s*\(")
DECLARED_UNITY_RE = re.compile(r"(?:UnityEngine\s*\.\s*)?\b(" + "|".join(UNITY_OBJECT_TYPES) + r")\s*\??\s+\w+\s*$")
STATIC_CALL_RE = re.compile(r"(?<![\w.])(?:UnityEngine\s*\.\s*)?(?:" + "|".join(UNITY_STATIC_CALLS) + r")")
AUTO_SETTER_RE = re.compile(r"\b(?:set|init)\s*;")
LAMBDA_RE = re.compile(r"\s*(?:static\s+)?(?:async\s+)?(?:\([^()]*\)|\w+)\s*=>|\s*delegate\b")


@dataclass(frozen=True)
class Finding:
    path: str
    line: int
    rule: str
    message: str

    @property
    def enforced(self):
        return RULES[self.rule].enforced

    @property
    def owner(self):
        return owner_of(self.path)

    def format(self):
        mode = "" if self.enforced else " (report-only)"
        return f"{self.path}:{self.line}: [{self.rule}]{mode} {self.message} -> box {self.owner}"


@dataclass
class SourceFile:
    """One file to lint; `path` is relative to the checkout root and `kind` is runtime, editor or tests."""
    path: str
    text: str
    kind: str = RUNTIME
    code: str = field(init=False, repr=False)
    _line_starts: list = field(init=False, repr=False)

    def __post_init__(self):
        self.code = strip_comments_and_strings(self.text) if self.path.endswith(CODE_SUFFIXES) else self.text
        self._line_starts = [0] + [m.end() for m in re.finditer("\n", self.text)]

    def line_of(self, offset):
        return bisect.bisect_right(self._line_starts, offset)


def owner_of(path):
    parts = Path(path).parts
    try:
        rest = parts[parts.index("_Project") + 1:]
    except ValueError:
        return "director"
    if len(rest) >= 2 and rest[0] == "Scripts":
        return SCRIPT_OWNERS.get(rest[1], "director")
    if len(rest) >= 2 and rest[0] == "Tests":
        return TEST_OWNERS.get(rest[1], "director")
    if rest and rest[0] == "Shaders":
        return SCRIPT_OWNERS.get(rest[1], "art") if len(rest) > 2 else "art"
    return "director"


def strip_comments_and_strings(text):
    """Blank comments, string/char literals and preprocessor lines, keeping offsets and newlines intact."""
    out = list(text)
    n = len(text)
    i = 0
    line_start = True

    def blank(a, b):
        for k in range(a, b):
            if out[k] != "\n":
                out[k] = " "

    while i < n:
        c = text[i]
        if line_start and c == "#":
            end = text.find("\n", i)
            end = n if end < 0 else end
            blank(i, end)
            i = end
            continue
        if c == "\n":
            line_start = True
            i += 1
            continue
        if not c.isspace():
            line_start = False
        if text.startswith("//", i):
            end = text.find("\n", i)
            end = n if end < 0 else end
            blank(i, end)
            i = end
        elif text.startswith("/*", i):
            end = text.find("*/", i + 2)
            end = n if end < 0 else end + 2
            blank(i, end)
            i = end
        elif c == '"' or (c in "@$" and re.match(r'[@$]{1,2}"', text[i:i + 3])):
            prefix = re.match(r'[@$]*"', text[i:i + 3]).group(0)
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
            blank(i + len(prefix), max(i + len(prefix), j - 1))
            i = j
        elif c == "'":
            m = re.match(r"'(?:\\.[^'\n]*|[^'\\\n])'", text[i:i + 12])
            if m:
                blank(i + 1, i + m.end() - 1)
                i += m.end()
            else:
                i += 1
        else:
            i += 1
    return "".join(out)


@dataclass
class TypeDecl:
    name: str
    kind: str
    namespace: str
    header: str
    path: str
    line: int
    top_level: bool
    bases: tuple


@dataclass
class Member:
    """A declaration at type level: everything from its first token to its ';' or closing '}'."""
    owner: TypeDecl
    start: int
    end: int
    text: str
    head: str
    has_body: bool


@dataclass
class Structure:
    types: list = field(default_factory=list)
    members: list = field(default_factory=list)


def parse_structure(src):
    """Walk the stripped code once and collect type declarations and type-level member declarations."""
    code = src.code
    structure = Structure()
    stack = []  # (kind, payload): ("namespace", name) / ("type", TypeDecl) / ("block", (start, head) or None)
    file_namespace = ""
    seg_start = 0
    pending = None  # (start, head, body_end) of a member whose body closed; a following ';' or '=' continues it
    continued = False  # the pending member went on with '=' (a property or object initializer): it ends at ';'

    def namespace():
        return ".".join(n for n in [file_namespace] + [p for k, p in stack if k == "namespace"] if n)

    def current_type():
        return stack[-1][1] if stack and stack[-1][0] == "type" else None

    def at_namespace_level():
        return not stack or stack[-1][0] == "namespace"

    def emit_member(start, end, head, has_body):
        text = code[start:end]
        stripped = ATTRIBUTES_RE.sub("", text)
        offset = start + len(text) - len(stripped)
        offset += len(stripped) - len(stripped.lstrip())
        structure.members.append(Member(current_type(), offset, end, stripped.strip(),
                                        ATTRIBUTES_RE.sub("", head).strip(), has_body))

    for i, c in enumerate(code):
        if c.isspace():
            continue
        if pending is not None and current_type() is not None and not continued:
            if c == "=":
                continued = True
            elif c != ";":
                emit_member(pending[0], pending[2], pending[1], True)
                seg_start = pending[2]
                pending = None
        if c == "{":
            start = seg_start if pending is None else pending[0]
            header = code[start:i]
            clean = ATTRIBUTES_RE.sub("", header).strip()
            ns = NAMESPACE_RE.search(clean)
            type_match = TYPE_HEADER_RE.search(" " + clean)
            if ns and at_namespace_level():
                stack.append(("namespace", ns.group(1)))
            elif type_match and "(" not in clean[:type_match.start()] and "=" not in clean:
                keyword = TYPE_HEADER_RE.search(" " + header)
                decl = TypeDecl(type_match.group(2), type_match.group(1), namespace(), clean, src.path,
                                src.line_of(start + keyword.start(1) - 1), at_namespace_level(),
                                parse_bases(clean[type_match.end() - 1:]))
                structure.types.append(decl)
                stack.append(("type", decl))
            else:
                stack.append(("block", (start, header) if current_type() is not None else None))
            pending = None
            continued = False
            seg_start = i + 1
        elif c == "}":
            if stack:
                kind, payload = stack.pop()
                if kind == "block" and payload is not None and current_type() is not None:
                    pending = (payload[0], payload[1], i + 1)
            seg_start = i + 1
        elif c == ";":
            if current_type() is not None:
                if pending is not None:
                    emit_member(pending[0], i, pending[1], True)
                    pending = None
                    continued = False
                else:
                    emit_member(seg_start, i, code[seg_start:i], False)
            elif at_namespace_level():
                header = ATTRIBUTES_RE.sub("", code[seg_start:i]).strip()
                ns = re.match(r"namespace\s+([\w.]+)$", header)
                delegate = DELEGATE_RE.search(header[:header.rfind("(") + 1]) if "(" in header else None
                if ns:
                    file_namespace = ns.group(1)
                elif delegate:
                    lead = seg_start + len(code[seg_start:i]) - len(code[seg_start:i].lstrip())
                    structure.types.append(TypeDecl(delegate.group(1), "delegate", namespace(), header, src.path,
                                                    src.line_of(lead), True, ()))
            seg_start = i + 1
    if pending is not None and current_type() is not None:
        emit_member(pending[0], pending[2], pending[1], True)
    return structure


def parse_bases(after_name):
    """Base names after a type name: ': A, B<T> where ...' -> ('A', 'B'); qualified names keep their last part."""
    m = re.match(r"\s*(?:<[^{]*?>)?\s*(?:\([^)]*\))?\s*:\s*(.*)$", after_name, re.S)
    if not m:
        return ()
    text = re.split(r"\bwhere\b", m.group(1))[0]
    depth, parts, cur = 0, [], ""
    for ch in text:
        if ch == "<":
            depth += 1
        elif ch == ">":
            depth -= 1
        elif ch == "," and depth == 0:
            parts.append(cur)
            cur = ""
            continue
        if depth == 0 and ch not in "<>":
            cur += ch
    parts.append(cur)
    return tuple(p.strip().split(".")[-1] for p in parts if p.strip())


def split_initializer(text):
    """('head', 'initializer') of a field declaration at its first top-level '=' that is not '=>' or a comparison."""
    depth = 0
    for k, ch in enumerate(text):
        if ch in "(<[{":
            depth += 1
        elif ch in ")>]}":
            depth -= 1
        elif ch == "=" and depth <= 0:
            prev = text[k - 1] if k else ""
            nxt = text[k + 1] if k + 1 < len(text) else ""
            if nxt in "=>" or prev in "=!<>":
                continue
            return text[:k], text[k + 1:]
    return text, ""


def signature(member):
    """('signature', 'accessors', 'initializer'): a member split at its top-level '=', with any accessor block
    ('{ get; set; }') cut off the signature; parts that are absent are ''."""
    head, init = split_initializer(member.text)
    brace = head.find("{")
    if brace < 0:
        return head, "", init
    return head[:brace], head[brace:], init


def check_debt(src):
    for m in DEBT_RE.finditer(src.text):
        yield Finding(src.path, src.line_of(m.start()), "debt-marker", f"'{m.group(1)}' marker")


def check_find(src):
    for m in FIND_RE.finditer(src.code):
        name = re.sub(r"\s+", "", m.group(0))
        yield Finding(src.path, src.line_of(m.start()), "find-api", f"'{name}': pass the reference in Initialize instead")


def check_banned(src):
    for pattern, what in BANNED:
        for m in pattern.finditer(src.code):
            yield Finding(src.path, src.line_of(m.start()), "banned-api", f"{what} ('{m.group(0).strip()}')")


def check_linq(src):
    for m in LINQ_RE.finditer(src.code):
        yield Finding(src.path, src.line_of(m.start() + len(m.group(0)) - len(m.group(0).lstrip())),
                      "linq-runtime", "System.Linq in a runtime assembly allocates; write the loop")


def check_static_mutable(src, structure):
    for member in structure.members:
        if member.owner.kind == "enum":
            continue
        sig, accessors, _ = signature(member)
        words = set(re.findall(r"\b\w+\b", sig))
        if "static" not in words or words & {"const", "readonly", "using", "delegate", "operator"}:
            continue
        if "(" in sig or "=>" in sig:
            continue
        if accessors:
            if not AUTO_SETTER_RE.search(accessors):
                continue
            what = "static auto-property with a setter"
        elif "event" in words:
            what = "static event"
        else:
            what = "non-readonly static field"
        name = re.findall(r"(\w+)\s*$", sig)
        label = f" '{name[0]}'" if name else ""
        yield Finding(src.path, src.line_of(member.start), "static-mutable",
                      f"{what}{label} in {member.owner.name}: hold the state in an instance passed via GameContext")


def new_pattern(type_names):
    return re.compile(r"\bnew\s+(?:UnityEngine\s*\.\s*)?(" + "|".join(type_names) + r")\s*[({]")


def instance_initializer(member):
    """('signature', 'initializer') of an instance field or auto-property initializer, or None for any other member.
    A lambda initializer runs later, not while the object is constructed."""
    sig, _, init = signature(member)
    if not init.strip() or "=>" in sig or "(" in sig or re.search(r"\b(const|static)\b", sig):
        return None
    if LAMBDA_RE.match(init):
        return None
    return sig, init


def constructor_body(member):
    """The body of an instance constructor, or None."""
    if not member.has_body or re.search(r"\bstatic\b", member.head):
        return None
    if not re.search(r"(?<![\w.])" + re.escape(member.owner.name) + r"\s*\([^()]*\)\s*(?::\s*(?:base|this)\s*\(.*)?$",
                     member.head, re.S):
        return None
    return member.text[len(member.head):]


def constructing_types(structures):
    """Project types whose construction (an instance field initializer or constructor body) creates a Unity object,
    directly or through another such type, so a scripted object's field initializer must not 'new' them either."""
    found = set()
    while True:
        pattern = new_pattern(UNITY_OBJECT_TYPES + tuple(sorted(found)))
        added = set()
        for structure in structures:
            for member in structure.members:
                if member.owner.name in found or member.owner.name in added:
                    continue
                init = instance_initializer(member)
                code = init[1] if init else constructor_body(member)
                if code and pattern.search(code):
                    added.add(member.owner.name)
        if not added:
            return found
        found |= added


def check_initializers(src, structure, unity_types, constructing):
    creates = new_pattern(UNITY_OBJECT_TYPES + tuple(sorted(constructing)))
    for member in structure.members:
        if member.owner.name not in unity_types:
            continue
        split = instance_initializer(member)
        if split is None:
            continue
        head, init = split
        init_offset = member.start + member.text.rfind(init)
        found = creates.search(init)
        if found:
            name = found.group(1)
            what = f"new {name}" + ("" if name in UNITY_OBJECT_TYPES else ", which creates a Unity object,")
        elif TARGET_NEW_RE.match(init) and DECLARED_UNITY_RE.search(head):
            what = f"new {DECLARED_UNITY_RE.search(head).group(1)}"
            found = TARGET_NEW_RE.match(init)
        else:
            found = STATIC_CALL_RE.search(init)
            if not found:
                continue
            what = re.sub(r"\s+", "", found.group(0)).rstrip("(<")
        yield Finding(src.path, src.line_of(init_offset + found.start()), "unity-api-in-initializer",
                      f"'{what}' in a field initializer of {member.owner.name}: create it in Awake/Initialize")


def unity_object_types(structures):
    """Names of every declared class that derives, directly or through project classes, from a scripted Unity base."""
    bases = {}
    for structure in structures:
        for t in structure.types:
            if t.kind in ("class", "record"):
                bases.setdefault(t.name, set()).update(t.bases)
    resolved = {}

    def derives(name, seen):
        if name in resolved:
            return resolved[name]
        if name in UNITY_OBJECT_BASES:
            return True
        if name in seen or name not in bases:
            return False
        seen.add(name)
        result = any(derives(b, seen) for b in bases[name])
        resolved[name] = result
        return result

    return {name for name in bases if derives(name, set())}


def check_duplicate_types(structures):
    declared = {}
    for structure in structures:
        for t in structure.types:
            if not t.top_level or not t.namespace.startswith(NAMESPACE_PREFIX):
                continue
            if "public" not in re.findall(r"\b\w+\b", t.header.split(t.name)[0]):
                continue
            declared.setdefault(t.name, {}).setdefault(t.namespace, t)
    for name, by_namespace in sorted(declared.items()):
        if len(by_namespace) < 2:
            continue
        decls = sorted(by_namespace.values(), key=lambda d: (d.path, d.line))
        for decl in decls:
            others = ", ".join(f"{o.namespace} ({o.path}:{o.line})" for o in decls if o is not decl)
            yield Finding(decl.path, decl.line, "duplicate-type",
                          f"public {decl.kind} '{name}' in {decl.namespace} is also declared in {others}")


def lint_sources(sources):
    """Every finding over in-memory sources, sorted by path and line."""
    findings = []
    parsed = []
    for src in sources:
        if src.kind in RULES["debt-marker"].kinds:
            findings += check_debt(src)
        if not src.path.endswith(CODE_SUFFIXES):
            continue
        structure = parse_structure(src)
        parsed.append((src, structure))
        if src.kind in RULES["find-api"].kinds:
            findings += check_find(src)
        findings += check_banned(src)
        if src.kind in RULES["linq-runtime"].kinds:
            findings += check_linq(src)
        findings += check_static_mutable(src, structure)
    structures = [s for _, s in parsed]
    unity_types = unity_object_types(structures)
    constructing = constructing_types(structures)
    for src, structure in parsed:
        findings += check_initializers(src, structure, unity_types, constructing)
    findings += check_duplicate_types(structures)
    return sorted(set(findings), key=lambda f: (f.path, f.line, f.rule, f.message))


def assembly_kind(asmdef_path):
    data = json.loads(asmdef_path.read_text(encoding="utf-8-sig"))
    if "UNITY_INCLUDE_TESTS" in data.get("defineConstraints", []):
        return TESTS
    return EDITOR if data.get("includePlatforms", []) == ["Editor"] else RUNTIME


def load_sources(root):
    """Every C# and shader file under Assets/_Project with the kind of the assembly that compiles it."""
    root = Path(root)
    src_root = root / SOURCE_ROOT
    owners = sorted(((p.parent, assembly_kind(p)) for p in src_root.rglob("*.asmdef")),
                    key=lambda t: len(t[0].parts), reverse=True)
    sources = []
    for path in sorted(src_root.rglob("*")):
        if not path.is_file() or path.suffix not in CODE_SUFFIXES + SHADER_SUFFIXES:
            continue
        kind = next((k for d, k in owners if d in path.parents), RUNTIME)
        sources.append(SourceFile(path.relative_to(root).as_posix(), path.read_text(encoding="utf-8-sig"), kind))
    return sources


def lint_root(root):
    return lint_sources(load_sources(root))


def summarize(findings):
    """'rule N, rule M' over the given findings, in rule-table order."""
    counts = {}
    for f in findings:
        counts[f.rule] = counts.get(f.rule, 0) + 1
    return ", ".join(f"{r} {counts[r]}" for r in RULES if r in counts)


def default_root():
    res = subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--show-toplevel"],
                         capture_output=True, text=True)
    if res.returncode != 0:
        sys.exit(f"not inside a git worktree: {res.stderr.strip()}")
    return Path(res.stdout.strip())


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", help="checkout to lint (default: current git worktree)")
    ap.add_argument("--rules", action="store_true", help="list the rules and exit")
    args = ap.parse_args()
    if args.rules:
        for r in RULES.values():
            print(f"{r.id:26} {'enforced' if r.enforced else 'report-only':12} {'/'.join(r.kinds):22} {r.summary}")
        return
    root = Path(args.root).resolve() if args.root else default_root()
    findings = lint_root(root)
    for f in findings:
        print(f.format())
    enforced = [f for f in findings if f.enforced]
    print(f"lint: {len(enforced)} enforced finding(s), {len(findings) - len(enforced)} report-only")
    sys.exit(1 if enforced else 0)


if __name__ == "__main__":
    main()
