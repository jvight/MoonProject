#!/usr/bin/env python3
"""
Fast compile check for MoonProject assemblies WITHOUT opening Unity.

Compiles every MoonProject.* .asmdef under Assets/_Project with Unity's own Roslyn
compiler against the Unity engine/editor DLLs and the package assemblies that the
main Unity project has already built into Library/ScriptAssemblies.

Two configurations are compiled:
  editor  - UNITY_EDITOR defined, every assembly (runtime + editor + tests)
  player  - UNITY_EDITOR undefined, runtime assemblies only, NO UnityEditor refs
            (catches editor API leaking into code that ships in a build)

Warnings are errors: the project keeps a zero-warning policy.

Usage:
  python tools/compile_check.py                 # check the git worktree you are in
  python tools/compile_check.py --root <path>   # check another checkout
  python tools/compile_check.py --config player

Requires the MAIN Unity project (git common dir parent) to have been opened once
with the project's Unity version so Library/ScriptAssemblies exists.
"""
import argparse
import hashlib
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ASM_PREFIX = "MoonProject"
SOURCE_ROOT = Path("Assets/_Project")
BASE_DEFINES = [
    "UNITY_6000_0_OR_NEWER", "UNITY_2023_1_OR_NEWER", "UNITY_2022_3_OR_NEWER",
    "UNITY_2021_3_OR_NEWER", "UNITY_2020_3_OR_NEWER", "UNITY_5_3_OR_NEWER",
    "UNITY_STANDALONE_WIN", "UNITY_STANDALONE", "PLATFORM_STANDALONE_WIN", "PLATFORM_STANDALONE",
    "ENABLE_INPUT_SYSTEM", "ENABLE_MONO", "NET_STANDARD_2_0", "NET_STANDARD", "NET_STANDARD_2_1",
    "NETSTANDARD", "NETSTANDARD2_1", "CSHARP_7_3_OR_NEWER", "TRACE",
]
EDITOR_DEFINES = ["UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_EDITOR_64", "UNITY_INCLUDE_TESTS"]
# CS0649: [SerializeField] private fields are assigned by Unity serialization (Unity suppresses it too).
NO_WARN = ["0649"]


def run(cmd, cwd=None):
    return subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")


def git(root, *args):
    res = run(["git", "-c", f"safe.directory={Path(root).as_posix()}", "-c", "safe.directory=*", *args], cwd=root)
    if res.returncode != 0:
        sys.exit(f"git {' '.join(args)} failed: {res.stderr.strip()}")
    return res.stdout.strip()


def resolve_paths(args):
    root = Path(args.root).resolve() if args.root else Path(git(Path.cwd(), "rev-parse", "--show-toplevel")).resolve()
    if args.unity_project:
        main = Path(args.unity_project).resolve()
    else:
        common = Path(git(root, "rev-parse", "--path-format=absolute", "--git-common-dir")).resolve()
        main = common.parent
    version_file = root / "ProjectSettings" / "ProjectVersion.txt"
    match = re.search(r"m_EditorVersion:\s*(\S+)", version_file.read_text(encoding="utf-8"))
    if not match:
        sys.exit(f"Cannot read editor version from {version_file}")
    editor = Path(args.unity_editor) if args.unity_editor else Path(
        rf"C:\Program Files\Unity\Hub\Editor\{match.group(1)}\Editor")
    if not editor.exists():
        sys.exit(f"Unity editor not found at {editor} (pass --unity-editor)")
    script_asm = main / "Library" / "ScriptAssemblies"
    if not script_asm.exists():
        sys.exit(f"{script_asm} missing - open the main project in Unity once first")
    return root, main, editor


class Asmdef:
    def __init__(self, path: Path):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        self.path = path
        self.dir = path.parent
        self.name = data["name"]
        self.references = data.get("references", [])
        self.include_platforms = data.get("includePlatforms", [])
        self.define_constraints = data.get("defineConstraints", [])
        self.precompiled = data.get("precompiledReferences", []) if data.get("overrideReferences") else []
        self.unsafe = data.get("allowUnsafeCode", False)
        self.sources = []

    @property
    def editor_only(self):
        return self.include_platforms == ["Editor"]


def discover(root: Path):
    src_root = root / SOURCE_ROOT
    asmdefs = {}
    for p in sorted(src_root.rglob("*.asmdef")):
        a = Asmdef(p)
        if not a.name.startswith(ASM_PREFIX):
            continue
        for ref in a.references:
            if ref.startswith("GUID:"):
                sys.exit(f"{p}: reference assemblies by name, not GUID ({ref})")
        asmdefs[a.name] = a
    owner_dirs = sorted(((a.dir, a) for a in asmdefs.values()), key=lambda t: len(t[0].parts), reverse=True)
    for cs in src_root.rglob("*.cs"):
        for d, a in owner_dirs:
            if d in cs.parents:
                a.sources.append(cs)
                break
        else:
            sys.exit(f"{cs} is not covered by any {ASM_PREFIX}.* asmdef")
    return asmdefs


def topo_order(asmdefs):
    order, state = [], {}

    def visit(name, chain):
        if state.get(name) == "done":
            return
        if state.get(name) == "visiting":
            sys.exit(f"asmdef cycle: {' -> '.join(chain + [name])}")
        state[name] = "visiting"
        for ref in asmdefs[name].references:
            if ref in asmdefs:
                visit(ref, chain + [name])
        state[name] = "done"
        order.append(name)

    for n in sorted(asmdefs):
        visit(n, [])
    return order


def engine_refs(editor: Path, player: bool):
    managed = editor / "Data" / "Managed" / "UnityEngine"
    refs = [p for p in managed.glob("*.dll") if not (player and p.name.startswith("UnityEditor"))]
    ns = editor / "Data" / "NetStandard"
    refs.append(ns / "ref" / "2.1.0" / "netstandard.dll")
    refs += list((ns / "compat" / "2.1.0" / "shims" / "netfx").glob("*.dll"))
    return refs


def find_precompiled(main: Path, filename: str):
    hits = list((main / "Library" / "PackageCache").rglob(filename)) + list((main / "Assets").rglob(filename))
    if not hits:
        sys.exit(f"precompiled reference {filename} not found")
    return hits[0]


def compile_config(config, root, main, editor, asmdefs, out_dir):
    player = config == "player"
    script_asm = main / "Library" / "ScriptAssemblies"
    pkg_dlls = {p.stem: p for p in script_asm.glob("*.dll")
                if not p.stem.startswith(ASM_PREFIX) and not p.stem.startswith("Assembly-CSharp")}
    base = engine_refs(editor, player)
    defines = BASE_DEFINES + ([] if player else EDITOR_DEFINES)
    built = {}
    problems = 0
    csc = [str(editor / "Data" / "NetCoreRuntime" / "dotnet.exe"), str(editor / "Data" / "DotNetSdkRoslyn" / "csc.dll")]

    for name in topo_order(asmdefs):
        a = asmdefs[name]
        if player and a.editor_only:
            continue
        if any(c not in defines for c in a.define_constraints):
            continue
        if not a.sources:
            continue
        refs = list(base)
        for ref in a.references:
            if ref in asmdefs:
                if ref not in built:
                    print(f"  [{config}] {name}: skipped, dependency {ref} unavailable in this config")
                    problems += 1
                    break
                refs.append(built[ref])
            elif ref in pkg_dlls:
                if player and ("Editor" in ref.split(".") or ref.startswith("UnityEditor")):
                    print(f"  [{config}] {name}: runtime assembly references editor assembly {ref}")
                    problems += 1
                    continue
                refs.append(pkg_dlls[ref])
            else:
                print(f"  [{config}] {name}: unknown reference '{ref}' (not a MoonProject asmdef or built package assembly)")
                problems += 1
        else:
            refs += [find_precompiled(main, f) for f in a.precompiled]
            out = out_dir / config / f"{name}.dll"
            out.parent.mkdir(parents=True, exist_ok=True)
            rsp = out.with_suffix(".rsp")
            lines = ["-nologo", "-nostdlib+", "-target:library", "-langversion:9.0", "-deterministic",
                     "-warn:4", "-warnaserror+", f"-nowarn:{','.join(NO_WARN)}", f"-out:{out}",
                     f"-define:{';'.join(defines)}", "-unsafe+" if a.unsafe else "-unsafe-"]
            lines += [f'-r:"{r}"' for r in refs]
            lines += [f'"{s}"' for s in sorted(a.sources)]
            rsp.write_text("\n".join(lines), encoding="utf-8")
            res = run(csc + ["-noconfig", f"@{rsp}"])
            output = (res.stdout + res.stderr).strip()
            if res.returncode == 0:
                built[name] = out
                print(f"  [{config}] {name}: OK ({len(a.sources)} files)")
            else:
                problems += 1
                print(f"  [{config}] {name}: FAILED")
            for line in output.splitlines():
                if line.strip():
                    print("      " + line.replace(str(root) + "\\", "").replace(str(root) + "/", ""))
    return problems


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", help="checkout to compile (default: current git worktree)")
    ap.add_argument("--unity-project", help="main Unity project with a built Library (default: git common dir parent)")
    ap.add_argument("--unity-editor", help="path to the Unity 'Editor' folder")
    ap.add_argument("--config", choices=["editor", "player", "both"], default="both")
    args = ap.parse_args()

    root, main_proj, editor = resolve_paths(args)
    asmdefs = discover(root)
    if not asmdefs:
        sys.exit(f"no {ASM_PREFIX}.* asmdefs under {root / SOURCE_ROOT}")
    out_dir = Path(tempfile.gettempdir()) / "moon_compile_check" / hashlib.sha1(str(root).encode()).hexdigest()[:10]
    print(f"compile_check: {root}  (Unity {editor.parent.name}, refs from {main_proj})")
    configs = ["editor", "player"] if args.config == "both" else [args.config]
    problems = sum(compile_config(c, root, main_proj, editor, asmdefs, out_dir) for c in configs)
    print("RESULT: " + ("PASS" if problems == 0 else f"FAIL ({problems} problem(s))"))
    sys.exit(0 if problems == 0 else 1)


if __name__ == "__main__":
    main()
