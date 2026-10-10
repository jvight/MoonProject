#!/usr/bin/env python3
"""
The one command a box runs before it reports. In the box's own worktree it runs, stopping at the first failure:
  1. compile_check (with lint);
  2. the box's builders and the Main Scene build in one headless Unity (unity_batch exec), which catches what only
     Unity refuses (a Unity object made while a component is constructed, a builder throwing, a broken scene wiring);
  3. the box's targeted tests (the per-box default below, or --filter / --assembly / --platform).
Then it restores what the builders regenerated (Generated/ and Scenes/Main.unity are never hand-edited, the
Director commits them after landing) and moves the untracked batch-made .meta files aside to Logs/box_check/, so
`git merge main` keeps working. Last, it appends a status block to D:/Project/MoonProject-wt/.board/<box>.md, which
`python tools/director.py status` prints.

Usage (from inside the box worktree):
  python tools/box_check.py <box>                         # e.g. world
  python tools/box_check.py <box> --filter "LiftTests"    # other tests instead of the box's default
  python tools/box_check.py <box> --platform playmode --assembly MoonProject.World.PlayModeTests
  python tools/box_check.py <box> --no-tests
Exit code 0 when every step passed.
"""
import argparse
import datetime
import shutil
import subprocess
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent
GENERATED = "Assets/_Project/Generated"
MAIN_SCENE = "Assets/_Project/Scenes/Main.unity"
BUILDER_OUTPUTS = (GENERATED, MAIN_SCENE)
MAIN_SCENE_BUILDER = "Main Scene"
RUN_BUILDERS = "MoonProject.Editor.Automation.BuildAutomation.RunBuilders"
BUILDER_DOMAINS = {"art": "Art", "world": "World", "rover": "Rover", "gameplay": "Gameplay", "audio": "Audio",
                   "ui": "UI"}
# Targeted EditMode tests per box; the full PlayMode suite and the Playthrough are the Director's.
DEFAULT_TESTS = {"art": "MoonProject.Art.Tests", "world": "MoonProject.World.Tests",
                 "rover": "MoonProject.Rover.Tests", "gameplay": "MoonProject.Gameplay.Tests",
                 "audio": "MoonProject.Audio.Tests", "ui": "MoonProject.UI.Tests",
                 "foundation": "MoonProject.Editor.Tests", "director": "MoonProject.Tests.EditMode"}
BOARD_HEADER = "## "


def say(message):
    print(f"[box_check] {message}", flush=True)


def git(cwd, *args, check=True):
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "core.safecrlf=false", *args], cwd=cwd,
                         capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and res.returncode != 0:
        raise SystemExit(f"git {' '.join(args)} in {cwd}: {(res.stderr or res.stdout).strip()}")
    return res.stdout.rstrip()


def main_checkout(root):
    return Path(git(root, "rev-parse", "--path-format=absolute", "--git-common-dir")).resolve().parent


def board_dir(root):
    """MoonProject-wt/.board next to the main checkout, shared by every worktree of the repository."""
    return main_checkout(root).parent / "MoonProject-wt" / ".board"


def builder_filter(boxes):
    """Builder regex for the boxes' own domains plus the Main Scene build, which always runs last (order 1000)."""
    domains = [BUILDER_DOMAINS[b] for b in boxes if b in BUILDER_DOMAINS]
    parts = ([f"({'|'.join(domains)})/"] if domains else []) + [f"{MAIN_SCENE_BUILDER}$"]
    return "^(" + "|".join(parts) + ")"


def run_logged(name, cmd, cwd, log_path):
    """Runs <cmd> in <cwd>, echoing and logging its output; returns True when it exited 0."""
    say(f"{name}: {' '.join(str(c) for c in cmd[1:])}")
    log_path.parent.mkdir(parents=True, exist_ok=True)
    with open(log_path, "w", encoding="utf-8") as log, subprocess.Popen(
            cmd, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8",
            errors="replace") as proc:
        for line in proc.stdout:
            print(line, end="", flush=True)
            log.write(line)
    return proc.returncode == 0


def compile_step(root, log_path):
    return run_logged("compile_check", [sys.executable, str(root / "tools" / "compile_check.py"), "--root",
                                        str(root)], root, log_path)


def builders_step(root, boxes, log_path):
    return run_logged("builders + Main Scene", [sys.executable, str(root / "tools" / "unity_batch.py"), "exec",
                                                "--project", str(root), "--method", RUN_BUILDERS, "--arg",
                                                f"filter={builder_filter(boxes)}"], root, log_path)


def dirty_outputs(root):
    return [line[3:] for line in git(root, "status", "--porcelain", "-uall", "--", *BUILDER_OUTPUTS).splitlines()]


def restore_outputs(root, aside):
    """Puts Generated/ and Main.unity back to HEAD; new untracked builder files go to <aside>, never deleted."""
    changed = dirty_outputs(root)
    git(root, "checkout", "HEAD", "--", *BUILDER_OUTPUTS)
    moved = move_untracked(root, git(root, "ls-files", "--others", "--exclude-standard", "--",
                                     *BUILDER_OUTPUTS).splitlines(), aside)
    return len(changed), moved


def move_untracked(root, paths, aside):
    for rel in paths:
        target = aside / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(root / rel), str(target))
    return len(paths)


def move_untracked_metas(root, aside):
    """Batch Unity writes a .meta for every script or folder it has not seen; boxes never commit them."""
    metas = [p for p in git(root, "ls-files", "--others", "--exclude-standard", "--", "Assets").splitlines()
             if p.endswith(".meta")]
    return move_untracked(root, metas, aside)


def format_block(stamp, box, branch, sha, steps, notes):
    """The board's status block: a '## ' header line with the verdict, then one line per step and note."""
    verdict = "PASS" if steps and all(ok for _, ok, _ in steps) else "FAIL"
    lines = [f"{BOARD_HEADER}{stamp} box_check {box}: {verdict} ({branch} @ {sha})"]
    lines += [f"- {'PASS' if ok else 'FAIL'} {name}" + (f" (log {log})" if log else "") for name, ok, log in steps]
    lines += [f"- {note}" for note in notes]
    return "\n".join(lines) + "\n"


def append_block(board, block):
    board.parent.mkdir(parents=True, exist_ok=True)
    with open(board, "a", encoding="utf-8") as f:
        f.write("\n" + block)


def last_block(text):
    """The last '## ' block of a board file, or '' when it has none."""
    start = text.rfind("\n" + BOARD_HEADER)
    if start < 0:
        return text.strip() if text.startswith(BOARD_HEADER) else ""
    return text[start + 1:].strip()


def test_command(root, args):
    cmd = [sys.executable, str(root / "tools" / "unity_batch.py"), "tests", "--project", str(root), "--platform",
           args.platform]
    if args.filter:
        return cmd + ["--filter", args.filter], f"{args.platform} --filter {args.filter}"
    assemblies = args.assembly or ([DEFAULT_TESTS[args.box]] if args.box in DEFAULT_TESTS else [])
    if not assemblies:
        return None, f"no default tests for box {args.box}; pass --filter or --assembly"
    for name in assemblies:
        cmd += ["--assembly", name]
    return cmd, f"{args.platform} {' '.join(assemblies)}"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("box", help="box name (art, world, rover, gameplay, audio, ui, foundation, ...)")
    ap.add_argument("--filter", help="test filter regex instead of the box's default test assembly")
    ap.add_argument("--assembly", action="append", help="test assembly instead of the default (repeatable)")
    ap.add_argument("--platform", default="editmode", choices=["editmode", "playmode"], type=str.lower)
    ap.add_argument("--no-tests", action="store_true", help="skip the tests step")
    args = ap.parse_args()

    root = Path(git(Path.cwd(), "rev-parse", "--show-toplevel")).resolve()
    if root == main_checkout(root):
        raise SystemExit("run box_check in your box worktree, not in the main checkout")
    if dirty_outputs(root):
        raise SystemExit(f"{GENERATED} or {MAIN_SCENE} has changes; box_check restores them afterwards, so start "
                         "from a clean copy (git checkout HEAD -- them)")
    branch = git(root, "rev-parse", "--abbrev-ref", "HEAD")
    sha = git(root, "rev-parse", "--short", "HEAD")
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    logs = root / "Logs" / "box_check" / stamp
    steps, notes = [], []

    def step(name, ok, log):
        steps.append((name, ok, log.relative_to(root).as_posix()))
        return ok

    ok = step("compile_check", compile_step(root, logs / "compile.log"), logs / "compile.log")
    if ok:
        ok = step(f"builders + Main Scene {builder_filter([args.box])}",
                  builders_step(root, [args.box], logs / "builders.log"), logs / "builders.log")
    if ok and not args.no_tests:
        cmd, label = test_command(root, args)
        if cmd is None:
            notes.append(label)
            steps.append(("tests", False, ""))
        else:
            step(f"tests {label}", run_logged("tests", cmd, root, logs / "tests.log"), logs / "tests.log")
    restored, new_outputs = restore_outputs(root, logs / "outputs")
    metas = move_untracked_metas(root, logs / "metas")
    notes.append(f"restored {restored} builder output(s), moved {new_outputs} new output(s) and {metas} untracked "
                 f".meta(s) to {logs.relative_to(root).as_posix()}")
    leftover = [line[3:] for line in git(root, "status", "--porcelain").splitlines()]
    if leftover:
        notes.append(f"left in the worktree: {len(leftover)} change(s): " + ", ".join(leftover[:5]))
    block = format_block(stamp, args.box, branch, sha, steps, notes)
    board = board_dir(root) / f"{args.box}.md"
    append_block(board, block)
    print(block, end="")
    say(f"board: {board}")
    sys.exit(0 if all(s[1] for s in steps) and steps else 1)


if __name__ == "__main__":
    main()
