#!/usr/bin/env python3
"""
The one command a box runs before it reports. In the box's own worktree, on a trial merge of main (merged without
committing and aborted at the end, so the tests see what landing would produce; skipped when the tip already
contains main), it runs, stopping at the first failure:
  1. compile_check (with lint);
  2. the box's builders, the builders of every domain the branch changes (git diff --name-only main...HEAD, each path
     mapped to its box the way lint.py maps owners; the filter used is printed) and the Main Scene build in one
     headless Unity (unity_batch exec), which catches what only Unity refuses (a Unity object made while a component
     is constructed, a builder throwing, a broken scene wiring) and keeps the tests off stale generated assets;
  3. the box's EditMode test assembly, then its PlayMode assembly (the defaults below; --fast skips PlayMode;
     --filter / --assembly / --platform run one custom tests step instead). PlayMode steps never run the golden-path
     Playthrough (category Playthrough), which is the Director's.
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
  python tools/box_check.py gameplay ui world rover art   # a cross-domain task: one run over several boxes
Several boxes share one run: the builders of all their domains, then their EditMode assemblies together and their
PlayMode assemblies together; the board block goes to <box>+<box>....md.
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

import lint

TOOLS = Path(__file__).resolve().parent
GENERATED = "Assets/_Project/Generated"
DATA_CONTENT = "Assets/_Project/Data/Content"
DATA_TUNING = "Assets/_Project/Data/Tuning"
MAIN_SCENE = "Assets/_Project/Scenes/Main.unity"
BUILDER_OUTPUTS = (GENERATED, DATA_CONTENT, DATA_TUNING, MAIN_SCENE)
UNTRACKED_OUTPUT_PREFIXES = (GENERATED + "/", DATA_CONTENT + "/", DATA_TUNING + "/", "Data/Content/", "Data/Tuning/")
INIT_TEST_SCENE_PREFIX = "Assets/InitTestScene"
MAIN_SCENE_BUILDER = "Main Scene"
RUN_BUILDERS = "MoonProject.Editor.Automation.BuildAutomation.RunBuilders"
# Feature slices span several domains (an alternation inside the builder regex).
BUILDER_DOMAINS = {"art": "Art|Gameplay|Rover", "world": "World", "rover": "Rover", "gameplay": "Gameplay",
                   "audio": "Audio", "ui": "UI", "map": "World|Gameplay|UI", "opening": "Art|World|Rover|UI|Audio",
                   "battery": "Art|World|Rover|Gameplay|UI|Audio", "lab-build": "Art|World",
                   "lab-systems": "Art|World|Gameplay|Rover|UI|Audio", "goals": "Gameplay|UI|Audio",
                   "look": "Art|World|Rover", "cleanup": "Art|World|Rover|Gameplay|UI|Audio",
                   "blender": "Art|UI", "wordmark": "UI",
                   "ui-fit": "UI", "clarity": "Art|World|Rover|Gameplay|UI|Audio", "voice": "Audio|UI|Gameplay",
                   "camera": "Rover|UI"}
# Targeted EditMode tests per box; the full PlayMode suite and the Playthrough are the Director's.
DEFAULT_TESTS = {"art": "MoonProject.Art.Tests;MoonProject.Gameplay.Feel.Tests;MoonProject.Gameplay.Tests;"
                        "MoonProject.Rover.Tests",
                 "world": "MoonProject.World.Tests",
                 "rover": "MoonProject.Rover.Tests", "gameplay": "MoonProject.Gameplay.Tests",
                 "audio": "MoonProject.Audio.Tests", "ui": "MoonProject.UI.Tests",
                 "foundation": "MoonProject.Editor.Tests", "director": "MoonProject.Tests.EditMode",
                 "map": "MoonProject.World.Tests;MoonProject.Gameplay.Tests;MoonProject.UI.Tests",
                 "opening": "MoonProject.Rover.Tests;MoonProject.World.Tests;MoonProject.Audio.Tests;MoonProject.Art.Tests;"
                            "MoonProject.UI.Tests",
                 "battery": "MoonProject.Gameplay.Tests;MoonProject.Rover.Tests;MoonProject.Art.Tests;"
                            "MoonProject.World.Tests;MoonProject.UI.Tests;MoonProject.Audio.Tests",
                 "lab-build": "MoonProject.Art.Tests;MoonProject.World.Tests",
                 "lab-systems": "MoonProject.Gameplay.Tests;MoonProject.Rover.Tests;MoonProject.UI.Tests;"
                                "MoonProject.Audio.Tests",
                 "goals": "MoonProject.Gameplay.Tests;MoonProject.UI.Tests;MoonProject.Audio.Tests",
                 "look": "MoonProject.Art.Tests;MoonProject.World.Tests;MoonProject.Rover.Tests",
                 "cleanup": "MoonProject.Gameplay.Tests;MoonProject.Rover.Tests;MoonProject.Art.Tests;"
                            "MoonProject.World.Tests;MoonProject.UI.Tests;MoonProject.Audio.Tests;"
                            "MoonProject.Editor.Tests",
                 "blender": "MoonProject.Art.Tests;MoonProject.UI.Tests", "wordmark": "MoonProject.UI.Tests",
                 "ui-fit": "MoonProject.UI.Tests", "voice": "MoonProject.Audio.Tests;MoonProject.UI.Tests;MoonProject.Gameplay.Tests",
                 "clarity": "MoonProject.Gameplay.Tests;MoonProject.Rover.Tests;MoonProject.Art.Tests;MoonProject.UI.Tests;"
                            "MoonProject.World.Tests;MoonProject.Audio.Tests",
                 "camera": "MoonProject.Rover.Tests;MoonProject.UI.Tests"}
# Each box's own PlayMode assemblies (art owns the feel kit in Gameplay/Feel, so it runs Gameplay's too; the
# Director's full suite runs in verify).
DEFAULT_PLAYMODE = {"art": "MoonProject.Gameplay.Feel.PlayModeTests;MoonProject.Gameplay.PlayModeTests;"
                           "MoonProject.Rover.PlayModeTests",
                    "world": "MoonProject.World.PlayModeTests", "rover": "MoonProject.Rover.PlayModeTests",
                    "gameplay": "MoonProject.Gameplay.PlayModeTests", "audio": "MoonProject.Audio.PlayModeTests",
                    "ui": "MoonProject.UI.PlayModeTests", "foundation": "MoonProject.Tests.PlayMode",
                    "map": "MoonProject.Gameplay.PlayModeTests;MoonProject.UI.PlayModeTests",
                    "opening": "MoonProject.Rover.PlayModeTests;MoonProject.UI.PlayModeTests",
                    "battery": "MoonProject.Gameplay.PlayModeTests;MoonProject.Rover.PlayModeTests",
                    "lab-build": "MoonProject.World.PlayModeTests",
                    "lab-systems": "MoonProject.Gameplay.PlayModeTests;MoonProject.Rover.PlayModeTests",
                    "goals": "MoonProject.Gameplay.PlayModeTests;MoonProject.UI.PlayModeTests",
                    "look": "MoonProject.World.PlayModeTests;MoonProject.Rover.PlayModeTests",
                    "cleanup": "MoonProject.Gameplay.PlayModeTests;MoonProject.World.PlayModeTests",
                    "blender": "MoonProject.UI.PlayModeTests", "wordmark": "MoonProject.UI.PlayModeTests",
                    "ui-fit": "MoonProject.UI.PlayModeTests", "voice": "MoonProject.Audio.PlayModeTests",
                    "clarity": "MoonProject.Gameplay.PlayModeTests;MoonProject.UI.PlayModeTests",
                    "camera": "MoonProject.Rover.PlayModeTests"}
# The Playthrough is [Explicit], yet an --assembly filter selects it (the runner counts a matching assembly as an
# explicit match), and it outlasts the PlayMode timeout; verify runs it on the merged main.
NO_PLAYTHROUGH = ["--category", "!Playthrough"]
# unity_batch's PlayMode timeout (PLAYMODE_TIMEOUT) fits one box's suites; a run over several boxes gets it per box.
PLAYMODE_SECONDS_PER_BOX = 2400
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
    domains = []
    for box in boxes:
        for domain in BUILDER_DOMAINS.get(box, "").split("|"):
            if domain and domain not in domains:
                domains.append(domain)
    parts = ([f"({'|'.join(domains)})/"] if domains else []) + [f"{MAIN_SCENE_BUILDER}$"]
    return "^(" + "|".join(parts) + ")"


def changed_boxes(paths):
    """Boxes owning <paths>, in first-seen order, by the folder -> box map lint reports its findings with."""
    boxes = []
    for path in paths:
        box = lint.owner_of(path)
        if box not in boxes:
            boxes.append(box)
    return boxes


def branch_changed_boxes(root):
    """Boxes whose folders the branch changes since it left main (git diff --name-only main...HEAD)."""
    return changed_boxes(git(root, "diff", "--name-only", "main...HEAD").splitlines())


def builder_boxes(boxes, changed):
    """The named boxes, then every box the branch changes: a branch that edits the Rover builder must not run another
    box's tests against a stale Generated/Rover."""
    return list(boxes) + [b for b in changed if b not in boxes]


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


def git_dir(root):
    return Path(git(root, "rev-parse", "--absolute-git-dir"))


def clear_index_lock(root):
    """Removes a stale index.lock in the worktree's git dir; returns True when one was removed."""
    lock = git_dir(root) / "index.lock"
    if lock.exists():
        lock.unlink()
        return True
    return False


def parse_porcelain(text):
    """Maps each repo-relative path in `git status --porcelain -uall` output to its two-char XY status."""
    entries = {}
    for line in text.splitlines():
        if len(line) < 4:
            continue
        xy, path = line[:2], line[3:]
        if " -> " in path:
            path = path.split(" -> ", 1)[1]
        entries[path.strip('"')] = xy
    return entries


def snapshot_status(root):
    """Snapshot of `git status --porcelain -uall` before box_check runs."""
    return parse_porcelain(git(root, "status", "--porcelain", "-uall"))


def in_ref(root, ref, path):
    return subprocess.run(["git", "-c", "safe.directory=*", "cat-file", "-e", f"{ref}:{path}"],
                          cwd=root, capture_output=True).returncode == 0


def remove_path(target):
    if target.is_dir():
        shutil.rmtree(target, ignore_errors=True)
    elif target.exists() or target.is_symlink():
        target.unlink()


def prune_empty_dirs(root, paths):
    for rel in paths:
        parent = (root / rel).parent
        while parent != root and parent.exists():
            try:
                parent.rmdir()
            except OSError:
                break
            parent = parent.parent


def dirty_outputs(root):
    return [line[3:] for line in git(root, "status", "--porcelain", "-uall", "--", GENERATED, MAIN_SCENE).splitlines()]


def restore_outputs(root, aside=None, before=None):
    """Puts builder outputs back to HEAD; new untracked builder files go to <aside> (or are removed when None)."""
    before = {} if before is None else before
    restored, moved, _, _ = cleanup_worktree(root, before, aside.parent if aside is not None else None)
    return restored, moved


def move_untracked(root, paths, aside):
    for rel in paths:
        target = aside / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(root / rel), str(target))
    return len(paths)


def move_untracked_metas(root, aside, before=None):
    """Batch Unity writes a .meta for every script or folder it has not seen; boxes never commit them."""
    before = {} if before is None else before
    metas = [p for p in git(root, "ls-files", "--others", "--exclude-standard", "--", "Assets").splitlines()
             if p.endswith(".meta") and p not in before]
    res = move_untracked(root, metas, aside)
    prune_empty_dirs(root, metas)
    return res


def cleanup_worktree(root, before=None, logs=None):
    """Restores every trial-merge and builder residue introduced since the `before` status snapshot:
    staged files from the main merge, modified Main.unity / builder outputs, new untracked files under
    Assets/_Project/Generated/, Data/Content, Data/Tuning, Assets/InitTestScene*, untracked .metas, and stale
    index.lock in the worktree's git dir. Files present in `before` are never touched."""
    before = {} if before is None else before
    clear_index_lock(root)
    abort_problem = abort_trial_merge(root, before)
    after = snapshot_status(root)
    restored, new_outputs, metas = 0, 0, 0
    touched_untracked = []
    for path, xy in after.items():
        if path in before:
            continue
        if xy != "??":
            if in_ref(root, "HEAD", path):
                if xy[0] != " ":
                    git(root, "reset", "-q", "HEAD", "--", path, check=False)
                git(root, "checkout", "-q", "HEAD", "--", path, check=False)
            else:
                git(root, "rm", "-f", "-q", "--", path, check=False)
                remove_path(root / path)
                touched_untracked.append(path)
            restored += 1
        elif path.startswith(INIT_TEST_SCENE_PREFIX):
            remove_path(root / path)
            touched_untracked.append(path)
            new_outputs += 1
        elif path.startswith(UNTRACKED_OUTPUT_PREFIXES):
            if logs is not None:
                move_untracked(root, [path], logs / "outputs")
            else:
                remove_path(root / path)
            touched_untracked.append(path)
            new_outputs += 1
        elif path.startswith("Assets/") and path.endswith(".meta"):
            if logs is not None:
                move_untracked(root, [path], logs / "metas")
            else:
                remove_path(root / path)
            touched_untracked.append(path)
            metas += 1
        elif in_ref(root, "main", path) and not in_ref(root, "HEAD", path):
            remove_path(root / path)
            touched_untracked.append(path)
            restored += 1
    prune_empty_dirs(root, touched_untracked)
    clear_index_lock(root)
    return restored, new_outputs, metas, abort_problem


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


def box_assemblies(table, boxes):
    """The boxes' assemblies from a DEFAULT_TESTS-style table as one ';' list (each once, in box order), or None."""
    names = []
    for box in boxes:
        for name in (table.get(box) or "").split(";"):
            if name and name not in names:
                names.append(name)
    return ";".join(names) or None


def test_commands(root, args):
    """[(label, command)] of the tests steps: the boxes' EditMode then PlayMode assemblies (no PlayMode with --fast),
    or one custom step for --filter / --assembly; [] when the boxes have no default tests. PlayMode excludes the
    Playthrough."""
    base = [sys.executable, str(root / "tools" / "unity_batch.py"), "tests", "--project", str(root), "--platform"]

    def command(platform, selection):
        return base + [platform] + selection + (NO_PLAYTHROUGH if platform == "playmode" else [])

    if args.filter:
        return [(f"{args.platform} --filter {args.filter}", command(args.platform, ["--filter", args.filter]))]
    if args.assembly:
        return [(f"{args.platform} {' '.join(args.assembly)}",
                 command(args.platform, [arg for name in args.assembly for arg in ("--assembly", name)]))]
    defaults = [("editmode", box_assemblies(DEFAULT_TESTS, args.boxes))]
    if not args.fast:
        defaults.append(("playmode", box_assemblies(DEFAULT_PLAYMODE, args.boxes)))
    budget = ["--timeout", str(PLAYMODE_SECONDS_PER_BOX * len(args.boxes))] if len(args.boxes) > 1 else []
    return [(f"{platform} {name}",
             command(platform, (budget if platform == "playmode" else []) + ["--assembly", name]))
            for platform, name in defaults if name]


def needs_merge(root):
    return subprocess.run(["git", "-c", "safe.directory=*", "merge-base", "--is-ancestor", "main", "HEAD"],
                          cwd=root, capture_output=True).returncode != 0


def trial_merge(root):
    """Merges main without committing; returns '' or why it could not."""
    clear_index_lock(root)
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "core.safecrlf=false", "merge", "--no-commit",
                          "--no-ff", "main"], cwd=root, capture_output=True, text=True, encoding="utf-8",
                         errors="replace")
    if res.returncode != 0:
        git(root, "merge", "--abort", check=False)
        return (res.stdout + res.stderr).strip().splitlines()[-1] if (res.stdout + res.stderr).strip() else "failed"
    return ""


def merge_in_progress(root):
    return (git_dir(root) / "MERGE_HEAD").exists()


def abort_trial_merge(root, before=None):
    """Ends a merge left in progress, whatever stopped the run; returns '' or why it is still there. Decided by
    MERGE_HEAD, not by a flag: a half-aborted trial once left a box worktree with main's 93 files staged."""
    clear_index_lock(root)
    if not merge_in_progress(root):
        return ""
    git(root, "merge", "--abort", check=False)
    if merge_in_progress(root):
        git(root, "reset", "--merge", check=False)
    if merge_in_progress(root):
        if not before:
            # The builders rewrite tracked assets the merge also touched, so both aborts refuse ("not uptodate"); the
            # run started from a clean tracked tree, so HEAD is all there is to go back to.
            git(root, "reset", "--hard", "HEAD", check=False)
        else:
            for flag in ("MERGE_HEAD", "MERGE_MODE", "MERGE_MSG", "AUTO_MERGE"):
                (git_dir(root) / flag).unlink(missing_ok=True)
    return "the trial merge of main is still in progress; run git merge --abort" if merge_in_progress(root) else ""


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("boxes", nargs="+", metavar="box",
                    help="box name(s) (art, world, rover, gameplay, audio, ui, foundation, ...); several share one run")
    ap.add_argument("--fast", action="store_true", help="skip the PlayMode step (quick iterations)")
    ap.add_argument("--filter", help="one tests step with this filter instead of the boxes' defaults")
    ap.add_argument("--assembly", action="append", help="one tests step on these assemblies (repeatable)")
    ap.add_argument("--platform", default="editmode", choices=["editmode", "playmode"], type=str.lower,
                    help="platform of the --filter / --assembly step")
    ap.add_argument("--no-tests", action="store_true", help="skip the tests steps")
    ap.add_argument("--no-merge", action="store_true", help="test the working tree as it is, without main")
    args = ap.parse_args()
    box = "+".join(args.boxes)

    root = Path(git(Path.cwd(), "rev-parse", "--show-toplevel")).resolve()
    if root == main_checkout(root):
        raise SystemExit("run box_check in your box worktree, not in the main checkout")
    clear_index_lock(root)
    if merge_in_progress(root):
        raise SystemExit("a merge is in progress in this worktree; finish it or run git merge --abort first")
    if dirty_outputs(root):
        raise SystemExit(f"{GENERATED} or {MAIN_SCENE} has changes; box_check restores them afterwards, so start "
                         "from a clean copy (git checkout HEAD -- them)")
    merge = not args.no_merge and needs_merge(root)
    if merge and git(root, "status", "--porcelain", "--untracked-files=no"):
        raise SystemExit("commit your changes first: box_check tests your tip merged with main (or pass --no-merge)")
    before = snapshot_status(root)
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
    changed = branch_changed_boxes(root)
    build_boxes = builder_boxes(args.boxes, changed)
    say(f"builder filter {builder_filter(build_boxes)}: box {box} + boxes the branch changes "
        f"(git diff --name-only main...HEAD): {', '.join(changed) or 'none'}")

    def step(name, log_name, run):
        log = logs / log_name
        started = time.monotonic()
        ok = run(log)
        steps.append((name, ok, log.relative_to(root).as_posix(), time.monotonic() - started))
        return ok

    try:
        if merge:
            say(f"trial merge of main ({main_sha}) into {branch} ({sha}), aborted at the end")
            problem = trial_merge(root)
            if problem:
                steps.append((f"merge main ({main_sha})", False, "", 0.0))
                notes.append(f"main does not merge cleanly ({problem}); merge main into your branch and resolve it")
        ok = not steps and step("compile_check", "compile.log", lambda log: compile_step(root, log))
        if ok:
            ok = step(f"builders + Main Scene {builder_filter(build_boxes)}", "builders.log",
                      lambda log: builders_step(root, build_boxes, log))
        if ok and not args.no_tests:
            planned = test_commands(root, args)
            if not planned:
                notes.append(f"no default tests for box {box}; pass --filter or --assembly")
            for label, cmd in planned:
                if not ok:
                    break
                ok = step(f"tests {label}", f"tests-{label.split()[0]}.log",
                          lambda log, c=cmd: run_logged("tests", c, root, log))
    finally:
        restored, new_outputs, metas, abort_problem = cleanup_worktree(root, before, logs)
    if abort_problem:
        steps.append(("abort trial merge", False, "", 0.0))
        notes.append(abort_problem)
    notes.append(f"restored {restored} builder output(s), moved {new_outputs} new output(s) and {metas} untracked "
                 f".meta(s) to {logs.relative_to(root).as_posix()}")
    leftover = [p for p in snapshot_status(root) if p not in before]
    if leftover:
        notes.append(f"left in the worktree: {len(leftover)} change(s): " + ", ".join(leftover[:5]))
    else:
        notes.append("worktree clean")
    block = format_block(stamp, box, tested, steps, notes)
    board = board_dir(root) / f"{box}.md"
    append_block(board, block)
    print(block, end="")
    say(f"board: {board}")
    if not leftover:
        say("worktree clean")
    sys.exit(0 if steps and all(s[1] for s in steps) else 1)


if __name__ == "__main__":
    main()
