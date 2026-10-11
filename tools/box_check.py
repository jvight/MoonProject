#!/usr/bin/env python3
"""
The one command a box runs before it reports. In the box's own worktree, on a trial merge of main (merged without
committing and aborted at the end, so the tests see what landing would produce; skipped when the tip already
contains main), it runs, stopping at the first failure:
  1. compile_check (with lint);
  2. the box's builders and the Main Scene build in one headless Unity (unity_batch exec), which catches what only
     Unity refuses (a Unity object made while a component is constructed, a builder throwing, a broken scene wiring);
  3. the box's EditMode test assembly, then its PlayMode assembly (the defaults below; --fast skips PlayMode;
     --filter / --assembly / --platform run one custom tests step instead).
Then it restores what the builders regenerated (Generated/ and Scenes/Main.unity are never hand-edited, the
Director commits them after landing) and moves the untracked batch-made .meta files aside to Logs/box_check/, so
`git merge main` keeps working. Last, it appends a status block to D:/Project/MoonProject-wt/.board/<box>.md, which
`python tools/director.py status` prints.

Usage (from inside the box worktree):
  python tools/box_check.py <box>                         # e.g. world: the full gate before a report
  python tools/box_check.py <box> --fast                  # quick iteration: no PlayMode
  python tools/box_check.py <box> --filter "LiftTests"    # other tests instead of the box's defaults
  python tools/box_check.py <box> --platform playmode --assembly MoonProject.World.PlayModeTests
  python tools/box_check.py <box> --no-tests
  python tools/box_check.py <box> --no-merge              # the working tree as it is, without main
The trial merge needs a committed tip (no tracked changes). Each step's time goes into the board block.
Exit code 0 when every step passed.
"""
import argparse
import datetime
import shutil
import subprocess
import sys
import time
from pathlib import Path

TOOLS = Path(__file__).resolve().parent
GENERATED = "Assets/_Project/Generated"
MAIN_SCENE = "Assets/_Project/Scenes/Main.unity"
BUILDER_OUTPUTS = (GENERATED, MAIN_SCENE)
MAIN_SCENE_BUILDER = "Main Scene"
RUN_BUILDERS = "MoonProject.Editor.Automation.BuildAutomation.RunBuilders"
# Feature slices span several domains (an alternation inside the builder regex).
BUILDER_DOMAINS = {"art": "Art", "world": "World", "rover": "Rover", "gameplay": "Gameplay", "audio": "Audio",
                   "ui": "UI", "map": "World|Gameplay|UI", "opening": "World|Rover|Audio",
                   "battery": "Art|World|Rover|Gameplay|UI|Audio", "lab-build": "Art|World",
                   "lab-systems": "Art|World|Gameplay|Rover|UI|Audio", "goals": "Gameplay|UI|Audio"}
# Targeted EditMode tests per box; the full PlayMode suite and the Playthrough are the Director's.
DEFAULT_TESTS = {"art": "MoonProject.Art.Tests", "world": "MoonProject.World.Tests",
                 "rover": "MoonProject.Rover.Tests", "gameplay": "MoonProject.Gameplay.Tests",
                 "audio": "MoonProject.Audio.Tests", "ui": "MoonProject.UI.Tests",
                 "foundation": "MoonProject.Editor.Tests", "director": "MoonProject.Tests.EditMode",
                 "map": "MoonProject.World.Tests;MoonProject.Gameplay.Tests;MoonProject.UI.Tests",
                 "opening": "MoonProject.Rover.Tests;MoonProject.World.Tests;MoonProject.Audio.Tests",
                 "battery": "MoonProject.Gameplay.Tests;MoonProject.Rover.Tests;MoonProject.Art.Tests;"
                            "MoonProject.World.Tests;MoonProject.UI.Tests;MoonProject.Audio.Tests",
                 "lab-build": "MoonProject.Art.Tests;MoonProject.World.Tests",
                 "lab-systems": "MoonProject.Gameplay.Tests;MoonProject.Rover.Tests;MoonProject.UI.Tests;"
                                "MoonProject.Audio.Tests",
                 "goals": "MoonProject.Gameplay.Tests;MoonProject.UI.Tests;MoonProject.Audio.Tests"}
# Each box's own PlayMode assembly (art has none; the Director's full suite runs in verify).
DEFAULT_PLAYMODE = {"world": "MoonProject.World.PlayModeTests", "rover": "MoonProject.Rover.PlayModeTests",
                    "gameplay": "MoonProject.Gameplay.PlayModeTests", "audio": "MoonProject.Audio.PlayModeTests",
                    "ui": "MoonProject.UI.PlayModeTests", "foundation": "MoonProject.Tests.PlayMode",
                    "map": "MoonProject.Gameplay.PlayModeTests;MoonProject.UI.PlayModeTests",
                    "opening": "MoonProject.Rover.PlayModeTests",
                    "battery": "MoonProject.Gameplay.PlayModeTests;MoonProject.Rover.PlayModeTests",
                    "lab-build": "MoonProject.World.PlayModeTests",
                    "lab-systems": "MoonProject.Gameplay.PlayModeTests;MoonProject.Rover.PlayModeTests",
                    "goals": "MoonProject.Gameplay.PlayModeTests;MoonProject.UI.PlayModeTests"}
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


def format_block(stamp, box, tested, steps, notes):
    """The board's status block: a '## ' header line with the verdict and what was tested, then one line per step
    (verdict, name, seconds, log) and note."""
    verdict = "PASS" if steps and all(ok for _, ok, _, _ in steps) else "FAIL"
    lines = [f"{BOARD_HEADER}{stamp} box_check {box}: {verdict} ({tested})"]
    lines += [f"- {'PASS' if ok else 'FAIL'} {name} {seconds:.0f} s" + (f" (log {log})" if log else "")
              for name, ok, log, seconds in steps]
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


def test_commands(root, args):
    """[(label, command)] of the tests steps: the box's EditMode then PlayMode assembly (no PlayMode with --fast), or
    one custom step for --filter / --assembly; [] when the box has no default tests."""
    base = [sys.executable, str(root / "tools" / "unity_batch.py"), "tests", "--project", str(root), "--platform"]
    if args.filter:
        return [(f"{args.platform} --filter {args.filter}", base + [args.platform, "--filter", args.filter])]
    if args.assembly:
        return [(f"{args.platform} {' '.join(args.assembly)}",
                 base + [args.platform] + [arg for name in args.assembly for arg in ("--assembly", name)])]
    defaults = [("editmode", DEFAULT_TESTS.get(args.box))]
    if not args.fast:
        defaults.append(("playmode", DEFAULT_PLAYMODE.get(args.box)))
    return [(f"{platform} {name}", base + [platform, "--assembly", name]) for platform, name in defaults if name]


def needs_merge(root):
    return subprocess.run(["git", "-c", "safe.directory=*", "merge-base", "--is-ancestor", "main", "HEAD"],
                          cwd=root, capture_output=True).returncode != 0


def trial_merge(root):
    """Merges main without committing; returns '' or why it could not."""
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "core.safecrlf=false", "merge", "--no-commit",
                          "--no-ff", "main"], cwd=root, capture_output=True, text=True, encoding="utf-8",
                         errors="replace")
    if res.returncode != 0:
        git(root, "merge", "--abort", check=False)
        return (res.stdout + res.stderr).strip().splitlines()[-1] if (res.stdout + res.stderr).strip() else "failed"
    return ""


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("box", help="box name (art, world, rover, gameplay, audio, ui, foundation, ...)")
    ap.add_argument("--fast", action="store_true", help="skip the PlayMode step (quick iterations)")
    ap.add_argument("--filter", help="one tests step with this filter instead of the box's defaults")
    ap.add_argument("--assembly", action="append", help="one tests step on these assemblies (repeatable)")
    ap.add_argument("--platform", default="editmode", choices=["editmode", "playmode"], type=str.lower,
                    help="platform of the --filter / --assembly step")
    ap.add_argument("--no-tests", action="store_true", help="skip the tests steps")
    ap.add_argument("--no-merge", action="store_true", help="test the working tree as it is, without main")
    args = ap.parse_args()

    root = Path(git(Path.cwd(), "rev-parse", "--show-toplevel")).resolve()
    if root == main_checkout(root):
        raise SystemExit("run box_check in your box worktree, not in the main checkout")
    if (Path(git(root, "rev-parse", "--absolute-git-dir")) / "MERGE_HEAD").exists():
        raise SystemExit("a merge is in progress in this worktree; finish it or run git merge --abort first")
    if dirty_outputs(root):
        raise SystemExit(f"{GENERATED} or {MAIN_SCENE} has changes; box_check restores them afterwards, so start "
                         "from a clean copy (git checkout HEAD -- them)")
    merge = not args.no_merge and needs_merge(root)
    if merge and git(root, "status", "--porcelain", "--untracked-files=no"):
        raise SystemExit("commit your changes first: box_check tests your tip merged with main (or pass --no-merge)")
    branch = git(root, "rev-parse", "--abbrev-ref", "HEAD")
    sha = git(root, "rev-parse", "--short", "HEAD")
    main_sha = git(root, "rev-parse", "--short", "main")
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    logs = root / "Logs" / "box_check" / stamp
    steps, notes = [], []
    if merge:
        tested = f"{branch} @ {sha} + main @ {main_sha}, trial merge"
    else:
        tested = f"{branch} @ {sha}, " + ("without main @ " + main_sha if needs_merge(root) else "contains main")

    def step(name, log_name, run):
        log = logs / log_name
        started = time.monotonic()
        ok = run(log)
        steps.append((name, ok, log.relative_to(root).as_posix(), time.monotonic() - started))
        return ok

    if merge:
        say(f"trial merge of main ({main_sha}) into {branch} ({sha}), aborted at the end")
        problem = trial_merge(root)
        if problem:
            steps.append((f"merge main ({main_sha})", False, "", 0.0))
            notes.append(f"main does not merge cleanly ({problem}); merge main into your branch and resolve it")
            merge = False
    try:
        ok = not steps and step("compile_check", "compile.log", lambda log: compile_step(root, log))
        if ok:
            ok = step(f"builders + Main Scene {builder_filter([args.box])}", "builders.log",
                      lambda log: builders_step(root, [args.box], log))
        if ok and not args.no_tests:
            planned = test_commands(root, args)
            if not planned:
                notes.append(f"no default tests for box {args.box}; pass --filter or --assembly")
            for label, cmd in planned:
                if not ok:
                    break
                ok = step(f"tests {label}", f"tests-{label.split()[0]}.log",
                          lambda log, c=cmd: run_logged("tests", c, root, log))
        restored, new_outputs = restore_outputs(root, logs / "outputs")
        metas = move_untracked_metas(root, logs / "metas")
    finally:
        if merge:
            git(root, "merge", "--abort", check=False)
    notes.append(f"restored {restored} builder output(s), moved {new_outputs} new output(s) and {metas} untracked "
                 f".meta(s) to {logs.relative_to(root).as_posix()}")
    leftover = [line[3:] for line in git(root, "status", "--porcelain").splitlines()]
    if leftover:
        notes.append(f"left in the worktree: {len(leftover)} change(s): " + ", ".join(leftover[:5]))
    block = format_block(stamp, args.box, tested, steps, notes)
    board = board_dir(root) / f"{args.box}.md"
    append_block(board, block)
    print(block, end="")
    say(f"board: {board}")
    sys.exit(0 if steps and all(s[1] for s in steps) else 1)


if __name__ == "__main__":
    main()
