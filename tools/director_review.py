#!/usr/bin/env python3
"""
Fast (no Unity) review sheet for a box branch against main.

Prints seven sections for the Director before landing a box branch:
  1. Commits in main..<branch>, checking that every commit subject has a Conventional Commit type.
  2. Diff stat grouped by domain folder.
  3. Static lint findings (tools/lint.py) on the changed files only.
  4. Tuning / settings .cs files whose serialized fields or initializers changed, warning when no matching .asset
     file changed in the branch.
  5. New or changed .inputactions, .asset and .fbx files.
  6. Worktree status (uncommitted / untracked residue, stale index.lock or MERGE_HEAD).
  7. Gate / verification lines from the worktree's Logs/REPORT.md.

Usage:
  python tools/director_review.py <branch> [--root <path>] [--base <ref>]
"""
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import lint  # noqa: E402

CONVENTIONAL_RE = re.compile(
    r"^(feat|fix|refactor|perf|test|docs|chore|build|ci|style|revert)(\([\w./-]+\))?!?: .+"
)
TUNING_FILE_RE = re.compile(r"(Tuning|Settings)\.cs$", re.I)
WATCHED_ASSET_SUFFIXES = (".inputactions", ".asset", ".fbx")
GATE_LINE_RE = re.compile(
    r"(compile_check|unittest|box_check|RESULT:|PASS|FAIL|lint:|enforced finding|worktree clean)", re.I
)
GATE_HEADER_RE = re.compile(r"^#{1,3}\s+(Gate|Verification|Checks|Test)\b", re.I)


def git(cwd, *args, check=True):
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "core.safecrlf=false", *args], cwd=cwd,
                         capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and res.returncode != 0:
        raise SystemExit(f"git {' '.join(args)} in {cwd}: {(res.stderr or res.stdout).strip()}")
    return res.stdout.rstrip("\r\n")


def ref_exists(root, ref):
    return subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--verify", "--quiet", f"{ref}^{{commit}}"],
                          cwd=root, capture_output=True).returncode == 0


def resolve_branch(root, branch):
    if ref_exists(root, branch):
        return branch
    if not branch.startswith("box/") and ref_exists(root, f"box/{branch}"):
        return f"box/{branch}"
    raise SystemExit(f"unknown branch or ref: {branch}")


def worktrees_by_branch(root):
    mapping = {}
    wt_path = None
    for line in git(root, "worktree", "list", "--porcelain", check=False).splitlines():
        if line.startswith("worktree "):
            wt_path = Path(line[len("worktree "):])
        elif line.startswith("branch refs/heads/") and wt_path is not None:
            mapping[line[len("branch refs/heads/"):]] = wt_path
    return mapping


def check_commits(root, base, branch):
    """Returns [(sha, subject, is_conventional)] for commits in base..branch."""
    out = git(root, "log", "--reverse", "--format=%h%x09%s", f"{base}..{branch}")
    commits = []
    for line in out.splitlines():
        if not line.strip():
            continue
        sha, _, subject = line.partition("\t")
        commits.append((sha, subject, bool(CONVENTIONAL_RE.match(subject))))
    return commits


def domain_of_path(path):
    parts = Path(path).parts
    if not parts:
        return "(root)"
    if parts[:2] == ("Assets", "_Project") and len(parts) >= 3:
        top = parts[2]
        if top in ("Scripts", "Tests", "Data", "Generated", "Prefabs", "Art", "Shaders") and len(parts) >= 4:
            return f"{top}/{parts[3]}"
        return top
    if len(parts) == 1:
        return "(root)"
    return parts[0]


def diff_by_domain(root, base, branch):
    """Returns [(domain, file_count, added, deleted)] sorted by domain."""
    out = git(root, "diff", "--numstat", f"{base}...{branch}")
    stats = {}
    for line in out.splitlines():
        if not line.strip():
            continue
        parts = line.split("\t", 2)
        if len(parts) < 3:
            continue
        add_s, del_s, raw_path = parts
        path = raw_path.split(" -> ", 1)[-1].strip('"')
        if "{" in path and " => " in path:
            path = re.sub(r"\{[^{}]*\s=>\s([^{}]*)\}", r"\1", path)
        dom = domain_of_path(path)
        files, added, deleted = stats.get(dom, (0, 0, 0))
        add_n = int(add_s) if add_s.isdigit() else 0
        del_n = int(del_s) if del_s.isdigit() else 0
        stats[dom] = (files + 1, added + add_n, deleted + del_n)
    return [(dom, f, a, d) for dom, (f, a, d) in sorted(stats.items())]


def changed_files(root, base, branch):
    """Returns [(status, path)] between base and branch."""
    out = git(root, "diff", "--name-status", f"{base}...{branch}")
    items = []
    for line in out.splitlines():
        if not line.strip():
            continue
        parts = line.split("\t")
        status = parts[0][0]
        path = parts[-1].strip('"')
        items.append((status, path))
    return items


def read_blob(root, ref, path):
    res = subprocess.run(["git", "-c", "safe.directory=*", "show", f"{ref}:{path}"],
                         cwd=root, capture_output=True, text=True, encoding="utf-8-sig", errors="replace")
    return res.stdout if res.returncode == 0 else None


def discover_asmdef_kinds(root, ref):
    """Returns [(dir_posix, kind)] sorted deepest-first from the ref's tree."""
    out = git(root, "ls-tree", "-r", "--name-only", ref, "--", "Assets/_Project", check=False)
    owners = []
    for path in out.splitlines():
        if not path.endswith(".asmdef"):
            continue
        raw = read_blob(root, ref, path)
        if not raw:
            continue
        try:
            data = json.loads(raw)
        except json.JSONDecodeError:
            continue
        if "UNITY_INCLUDE_TESTS" in data.get("defineConstraints", []):
            kind = lint.TESTS
        elif data.get("includePlatforms", []) == ["Editor"]:
            kind = lint.EDITOR
        else:
            kind = lint.RUNTIME
        owners.append((Path(path).parent, kind))
    owners.sort(key=lambda t: len(t[0].parts), reverse=True)
    return owners


def kind_for_path(path, owners):
    p = Path(path)
    for d, k in owners:
        if d in p.parents:
            return k
    parts = p.parts
    if "Tests" in parts or "PlayMode" in parts or "EditMode" in parts:
        return lint.TESTS
    if "Editor" in parts:
        return lint.EDITOR
    return lint.RUNTIME


def lint_changed_files(root, branch, changes):
    """Runs tools/lint.py rules on the changed source files at <branch>."""
    owners = discover_asmdef_kinds(root, branch)
    sources = []
    changed_set = set()
    for status, path in changes:
        if status == "D":
            continue
        if not path.endswith(lint.CODE_SUFFIXES + lint.SHADER_SUFFIXES):
            continue
        text = read_blob(root, branch, path)
        if text is None:
            continue
        changed_set.add(path)
        sources.append(lint.SourceFile(path, text, kind_for_path(path, owners)))
    if not sources:
        return []
    return [f for f in lint.lint_sources(sources) if f.path in changed_set]


def serialized_field_signatures(path, text):
    """Returns {field_name: normalized_decl_and_initializer} for serialized fields in C# text."""
    if not text:
        return {}
    src = lint.SourceFile(path, text)
    structure = lint.parse_structure(src)
    fields = {}
    for member in structure.members:
        if member.owner.kind == "enum":
            continue
        sig, accessors, init = lint.signature(member)
        if accessors or "(" in sig or "=>" in sig:
            continue
        words = set(re.findall(r"\b\w+\b", sig))
        if words & {"const", "static", "event", "delegate", "operator", "using"}:
            continue
        attr_words = set(re.findall(r"\b\w+\b", member.attrs))
        is_serialized = ("SerializeField" in attr_words
                         or ("public" in words and "readonly" not in words and "NonSerialized" not in attr_words))
        if not is_serialized:
            continue
        names = re.findall(r"\b(\w+)\s*$", sig.strip())
        if not names:
            continue
        norm_sig = re.sub(r"\s+", " ", sig.strip())
        norm_init = re.sub(r"\s+", " ", init.strip())
        fields[f"{member.owner.name}.{names[0]}"] = f"{norm_sig} = {norm_init}" if norm_init else norm_sig
    return fields


def is_tuning_script(path, text):
    if TUNING_FILE_RE.search(path) or "/Tuning/" in path:
        return True
    if not text:
        return False
    return bool(re.search(r":\s*ScriptableObject\b", text))


def check_tuning_changes(root, base, branch, changes):
    """Detects tuning .cs files whose serialized fields or initializers changed and checks if matching .asset files
    changed too. Returns [(cs_path, changed_field_names, matching_assets, warned)]."""
    changed_assets = [path for status, path in changes if status != "D" and path.lower().endswith(".asset")]
    asset_blobs = {a: (read_blob(root, branch, a) or "") for a in changed_assets}
    results = []
    for status, path in changes:
        if status == "D" or not path.endswith(".cs"):
            continue
        after_text = read_blob(root, branch, path) or ""
        before_text = read_blob(root, base, path) or ""
        if not (is_tuning_script(path, after_text) or is_tuning_script(path, before_text)):
            continue
        before_fields = serialized_field_signatures(path, before_text)
        after_fields = serialized_field_signatures(path, after_text)
        if before_fields == after_fields:
            continue
        diff_keys = sorted(
            k.split(".", 1)[-1]
            for k in (set(before_fields) | set(after_fields))
            if before_fields.get(k) != after_fields.get(k)
        )
        stem = Path(path).stem
        base_stem = re.sub(r"(Tuning|Settings)$", "", stem)
        matched = []
        for asset in changed_assets:
            asset_stem = Path(asset).stem
            if (asset_stem.lower() == stem.lower()
                    or (base_stem and base_stem.lower() in asset_stem.lower())
                    or any(re.search(rf"\b{re.escape(f)}\b", asset_blobs[asset]) for f in diff_keys)):
                matched.append(asset)
        results.append((path, diff_keys, matched, len(matched) == 0))
    return results


def watched_asset_changes(changes):
    """Returns [(status, path)] for new or changed .inputactions, .asset and .fbx files."""
    return [(status, path) for status, path in changes if path.lower().endswith(WATCHED_ASSET_SUFFIXES)]


def check_worktree_status(root, branch):
    """Returns (worktree_path_or_None, residue_lines)."""
    wt = worktrees_by_branch(root).get(branch)
    if wt is None:
        current = git(root, "rev-parse", "--abbrev-ref", "HEAD", check=False)
        if current == branch:
            wt = Path(root)
    if wt is None or not wt.exists():
        return None, []
    residue = [line for line in git(wt, "status", "--porcelain", "-uall", check=False).splitlines() if line.strip()]
    git_dir_out = git(wt, "rev-parse", "--absolute-git-dir", check=False)
    if git_dir_out:
        gdir = Path(git_dir_out)
        if (gdir / "index.lock").exists():
            residue.append("!! stale index.lock in git dir")
        if (gdir / "MERGE_HEAD").exists():
            residue.append("!! MERGE_HEAD present (trial merge still in progress)")
    return wt, residue


def extract_gate_lines(wt):
    """Extracts gate / verification lines from <wt>/Logs/REPORT.md."""
    if wt is None:
        return None, []
    report = Path(wt) / "Logs" / "REPORT.md"
    if not report.exists():
        return report, []
    lines = report.read_text(encoding="utf-8", errors="replace").splitlines()
    picked = []
    in_gate_section = False
    for line in lines:
        stripped = line.strip()
        if GATE_HEADER_RE.match(stripped):
            in_gate_section = True
            continue
        if stripped.startswith("#") and in_gate_section:
            in_gate_section = False
        if in_gate_section and stripped:
            picked.append(stripped)
        elif GATE_LINE_RE.search(stripped):
            picked.append(stripped)
    deduped = []
    for item in picked:
        if item not in deduped:
            deduped.append(item)
    return report, deduped[:15]


def build_review_sheet(root, branch, base="main"):
    root = Path(root).resolve()
    resolved = resolve_branch(root, branch)
    commits = check_commits(root, base, resolved)
    domains = diff_by_domain(root, base, resolved)
    changes = changed_files(root, base, resolved)
    findings = lint_changed_files(root, resolved, changes)
    tuning = check_tuning_changes(root, base, resolved, changes)
    assets = watched_asset_changes(changes)
    wt, residue = check_worktree_status(root, resolved)
    report_path, gate_lines = extract_gate_lines(wt or root)

    out = [f"# Director Review: {resolved} (vs {base})"]

    bad_commits = [c for c in commits if not c[2]]
    commit_Note = "all conventional" if commits and not bad_commits else (
        f"{len(bad_commits)} non-conventional [WARN]" if bad_commits else "none"
    )
    out.append(f"\n## Commits ({len(commits)}, {commit_Note})")
    if commits:
        for sha, subj, ok in commits:
            tag = "OK" if ok else "WARN: missing Conventional type"
            out.append(f"- [{tag}] {sha} {subj}")
    else:
        out.append("- (no commits ahead of base)")

    out.append("\n## Diff Stat by Domain")
    if domains:
        for dom, count, added, deleted in domains:
            out.append(f"- {dom}: {count} file(s), +{added} / -{deleted}")
    else:
        out.append("- (no changed files)")

    enforced = [f for f in findings if f.enforced]
    out.append(f"\n## Lint Findings on Changed Files ({len(enforced)} enforced, "
               f"{len(findings) - len(enforced)} report-only)")
    if findings:
        for f in findings:
            out.append(f"- {f.format()}")
    else:
        out.append("- clean")

    out.append("\n## Tuning Serialized Fields & Matching Assets")
    if tuning:
        for cs_path, fields, matched, warned in tuning:
            field_list = ", ".join(fields)
            if warned:
                out.append(f"- [WARN] {cs_path} changed ({field_list}) but no matching .asset changed")
            else:
                out.append(f"- [OK] {cs_path} changed ({field_list}); matching .asset: {', '.join(matched)}")
    else:
        out.append("- none")

    out.append("\n## New or Changed .inputactions, .asset, .fbx")
    if assets:
        for status, path in assets:
            out.append(f"- {status} {path}")
    else:
        out.append("- none")

    out.append("\n## Worktree Status (Residue)")
    if wt is None:
        out.append(f"- no worktree checked out for {resolved}")
    elif residue:
        out.append(f"- [WARN] {wt} has {len(residue)} residue item(s):")
        for item in residue[:20]:
            out.append(f"  {item}")
    else:
        out.append(f"- clean ({wt})")

    out.append("\n## Gate Lines from Logs/REPORT.md")
    if report_path is None or not report_path.exists():
        out.append("- Logs/REPORT.md not found")
    elif gate_lines:
        for line in gate_lines:
            out.append(f"- {line}")
    else:
        out.append("- (no gate lines found in Logs/REPORT.md)")

    return "\n".join(out) + "\n"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("branch", help="box branch name (e.g. box/agy-rover or agy-rover)")
    ap.add_argument("--root", help="git repository or worktree root (default: current directory)")
    ap.add_argument("--base", default="main", help="base ref to compare against (default: main)")
    args = ap.parse_args()
    cwd = Path(args.root).resolve() if args.root else Path.cwd()
    root = Path(git(cwd, "rev-parse", "--show-toplevel")).resolve()
    print(build_review_sheet(root, args.branch, args.base), end="")


if __name__ == "__main__":
    main()
