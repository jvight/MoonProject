#!/usr/bin/env python3
"""
Director-only workflow for landing box branches on main and keeping the main editor in step with it
(see CLAUDE.md, "How the team works"). Run from the main project.

Usage:
  python tools/director.py status
  python tools/director.py land <box> [<box> ...] -m MESSAGE [-m MESSAGE ...] [--filter REGEX] [--no-scene]
  python tools/director.py refresh [--filter REGEX] [--scene]
  python tools/director.py outputs -m MESSAGE [-m MESSAGE ...]
  python tools/director.py verify [--skip-playthrough] [--push]

status   every box branch against main (ahead, behind, uncommitted files in its worktree), main against origin and
         the verification worktree, the batch slots and whether the main editor is idle.
land     merges the box branches into main (several boxes land as one merge), requires compile_check PASS before
         the merge is committed (otherwise the merge is aborted), then hot-reloads the main editor: assets-refresh,
         wait until it is idle with no console errors, run the boxes' own builders (e.g. ^(Art|Rover)/, or
         --filter) and rebuild Main.unity. It lists what the builders changed and every tuning or settings type the
         merge touched: saved assets of those types keep their old values until they are reset.
refresh  the same editor hot reload without a merge.
outputs  commits what the builders regenerated under Assets/ and reports any other change it left unstaged.
verify   fast-forwards the verification worktree to main and runs compile_check, EditMode, PlayMode and the
         Playthrough golden path there through tools/unity_batch.py, stopping at the first failure. --push pushes the
         verified commit to origin/main when every step passed.

Each -m is one paragraph of the commit message, as with git.
"""
import argparse
import collections
import datetime
import json
import os
import re
import socket
import subprocess
import sys
import time
import urllib.error
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import unity_mcp  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
VERIFY_WORKTREE = ROOT.parent / "MoonProject-wt" / "director"
BOX_PREFIX = "box/"
BUILDER_DOMAINS = {"art": "Art", "world": "World", "rover": "Rover", "gameplay": "Gameplay", "audio": "Audio",
                   "ui": "UI"}
SCHEMA_TYPE = re.compile(r"(Tuning|Settings)\.cs$")
EDITOR_IDLE_POLLS = 5
EDITOR_POLL_SECONDS = 2
EDITOR_WAIT_SECONDS = 900
EDITOR_STATE_TIMEOUT = 30
BUILD_TIMEOUT = 1800
PLAYTHROUGH_TIMEOUT = 2700
PUSH_TIMEOUT = 300
TAIL_LINES = 12
MCP_ERRORS = (urllib.error.URLError, socket.timeout, TimeoutError, RuntimeError, ConnectionError, OSError,
              json.JSONDecodeError)


def say(message):
    print(f"[director] {message}", flush=True)


def fail(message):
    say(f"ERROR: {message}")
    sys.exit(1)


def git(*args, cwd=ROOT, check=True):
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "core.safecrlf=false", *args], cwd=cwd,
                         capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and res.returncode != 0:
        fail(f"git {' '.join(args)} in {cwd}: {(res.stderr or res.stdout).strip()}")
    return res.stdout.strip()


def tracked_changes(cwd):
    return [line for line in git("status", "--porcelain", cwd=cwd).splitlines() if not line.startswith("??")]


def worktrees():
    """Maps branch name -> worktree path for every worktree of the repository."""
    paths = {}
    path = None
    for line in git("worktree", "list", "--porcelain").splitlines():
        if line.startswith("worktree "):
            path = Path(line[len("worktree "):])
        elif line.startswith("branch refs/heads/") and path is not None:
            paths[line[len("branch refs/heads/"):]] = path
    return paths


def compile_check(cwd):
    res = subprocess.run([sys.executable, str(ROOT / "tools" / "compile_check.py"), "--root", str(cwd)],
                         capture_output=True, text=True, encoding="utf-8", errors="replace")
    lines = res.stdout.strip().splitlines()
    passed = res.returncode == 0 and bool(lines) and lines[-1] == "RESULT: PASS"
    if not passed:
        print(res.stdout + res.stderr)
    return passed


def mcp_text(tool, arguments, timeout):
    result = unity_mcp.McpClient(timeout=timeout).connect().call(tool, arguments)
    text = " ".join(i.get("text", "") for i in result.get("content", []) if i.get("type") == "text")
    if result.get("isError"):
        raise RuntimeError(f"{tool}: {text or 'tool error'}")
    return text


def editor_state():
    """The editor's application state, or None while it is unreachable (closed, or reloading its domain)."""
    try:
        return json.loads(mcp_text("editor-application-get-state", {}, EDITOR_STATE_TIMEOUT)).get("result")
    except MCP_ERRORS:
        return None


def describe_editor(state):
    if state is None:
        return "unreachable"
    busy = [name for name in ("IsPlaying", "IsCompiling", "IsUpdating") if state.get(name)]
    return "busy (" + ", ".join(busy) + ")" if busy else "idle"


def wait_editor_idle():
    """Waits until the editor answers idle several polls in a row, so a domain reload in between is not missed."""
    started = time.monotonic()
    idle = 0
    while idle < EDITOR_IDLE_POLLS:
        if time.monotonic() - started > EDITOR_WAIT_SECONDS:
            fail(f"the main editor is not idle after {EDITOR_WAIT_SECONDS}s ({describe_editor(editor_state())})")
        state = editor_state()
        idle = idle + 1 if describe_editor(state) == "idle" else 0
        time.sleep(EDITOR_POLL_SECONDS)


def editor_errors():
    text = mcp_text("console-get-logs", {"logTypeFilter": "Error", "maxEntries": 20}, EDITOR_STATE_TIMEOUT)
    return json.loads(text).get("result") or []


def run_editor(command, filter_pattern=""):
    text, error = unity_mcp.run_editor_command(command, filter_pattern, BUILD_TIMEOUT)
    if text is None:
        fail(f"editor {command}: {error}")
    print(text)
    if not text.rstrip().endswith("RESULT: PASS"):
        fail(f"editor {command} did not pass")


def hot_reload(filter_pattern, scene):
    if editor_state() is None:
        fail("the main editor is unreachable; open the main project in Unity first")
    say("editor: clearing the console and refreshing assets")
    mcp_text("console-clear-logs", {}, EDITOR_STATE_TIMEOUT)
    mcp_text("assets-refresh", {}, BUILD_TIMEOUT)
    wait_editor_idle()
    errors = editor_errors()
    if errors:
        print(json.dumps(errors, indent=2, ensure_ascii=False))
        fail("the main editor logged errors after the refresh")
    if filter_pattern:
        say(f"editor: builders matching {filter_pattern}")
        run_editor("build", filter_pattern)
    if scene:
        say("editor: Main.unity")
        run_editor("scene")


def default_filter(boxes):
    domains = [BUILDER_DOMAINS[box] for box in boxes if box in BUILDER_DOMAINS]
    return f"^({'|'.join(domains)})/" if domains else ""


def commit(messages):
    args = ["commit"]
    for message in messages:
        args += ["-m", message]
    git(*args)
    say(f"committed {git('log', '--oneline', '-1')}")


def cmd_status(_args):
    main = git("rev-parse", "--short", "main")
    say(f"main      {git('log', '--oneline', '-1', 'main')}")
    pushed = git("rev-list", "--count", "origin/main..main")
    say(f"origin    {git('rev-parse', '--short', 'origin/main')} ({pushed} commit(s) on main not pushed)")
    if VERIFY_WORKTREE.exists():
        head = git("rev-parse", "--short", "HEAD", cwd=VERIFY_WORKTREE)
        behind = git("rev-list", "--count", "HEAD..main", cwd=VERIFY_WORKTREE)
        say(f"verify wt {head} ({behind} behind main {main})")
    paths = worktrees()
    say(f"{'box':<10}{'ahead':>6}{'behind':>7}{'dirty':>6}  last commit")
    for branch in git("for-each-ref", "--format=%(refname:short)", "refs/heads/" + BOX_PREFIX).splitlines():
        ahead = git("rev-list", "--count", f"main..{branch}")
        behind = git("rev-list", "--count", f"{branch}..main")
        path = paths.get(branch)
        dirty = str(len(tracked_changes(path))) if path and path.exists() else "-"
        last = git("log", "--format=%h %cr: %s", "-1", branch)
        say(f"{branch[len(BOX_PREFIX):]:<10}{ahead:>6}{behind:>7}{dirty:>6}  {last[:90]}")
    subprocess.run([sys.executable, str(ROOT / "tools" / "unity_batch.py"), "status", "--project", str(ROOT)])
    say(f"editor    {describe_editor(editor_state())}")


def cmd_land(args):
    if tracked_changes(ROOT):
        fail("main has uncommitted changes; commit or stash them first")
    if git("rev-parse", "--abbrev-ref", "HEAD") != "main":
        fail("the main project is not on main")
    branches = [BOX_PREFIX + box for box in args.boxes]
    for branch in branches:
        if git("rev-list", "--count", f"main..{branch}") == "0":
            fail(f"{branch} has nothing to land")
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "core.safecrlf=false", "merge", "--no-ff",
                          "--no-commit", *branches], cwd=ROOT, capture_output=True, text=True, encoding="utf-8",
                         errors="replace")
    if res.returncode != 0:
        print(res.stdout + res.stderr)
        git("merge", "--abort", check=False)
        fail("the merge did not apply cleanly; ask the box to merge main into its branch and resolve it there")
    say("compile_check on the merged tree")
    if not compile_check(ROOT):
        git("merge", "--abort")
        fail("compile_check failed on the merged tree; the merge was aborted")
    commit(args.message)
    schema = [path for path in git("diff", "--name-only", "HEAD^1", "HEAD").splitlines() if SCHEMA_TYPE.search(path)]
    hot_reload(args.filter if args.filter is not None else default_filter(args.boxes), not args.no_scene)
    changed = git("status", "--porcelain")
    say("builder output to review and commit with `outputs`:" if changed else "the builders changed nothing")
    if changed:
        print(changed)
    for path in schema:
        say(f"NOTE: the merge touched {path}; reset its saved assets if fields or defaults changed")


def cmd_refresh(args):
    hot_reload(args.filter or "", args.scene)


def cmd_outputs(args):
    git("add", "-A", "--", "Assets")
    staged = git("diff", "--cached", "--name-only")
    if not staged:
        fail("nothing changed under Assets/")
    print(staged)
    commit(args.message)
    leftover = git("status", "--porcelain")
    if leftover:
        say("left unstaged (outside Assets/):")
        print(leftover)


def run_step(name, cmd, log):
    """Runs one verification step in the verification worktree, echoing its output; returns (passed, tail)."""
    say(f"verify: {name}")
    tail = collections.deque(maxlen=TAIL_LINES)
    with subprocess.Popen(cmd, cwd=VERIFY_WORKTREE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                          encoding="utf-8", errors="replace") as proc:
        for line in proc.stdout:
            print(line, end="", flush=True)
            log.write(line)
            tail.append(line.rstrip())
    return proc.returncode == 0, list(tail)


def cmd_verify(args):
    if not VERIFY_WORKTREE.exists():
        fail(f"no verification worktree at {VERIFY_WORKTREE}")
    if tracked_changes(VERIFY_WORKTREE):
        fail(f"{VERIFY_WORKTREE} has local changes")
    git("merge", "--ff-only", "main", cwd=VERIFY_WORKTREE)
    sha = git("rev-parse", "HEAD", cwd=VERIFY_WORKTREE)
    batch = [sys.executable, "tools/unity_batch.py", "tests", "--platform"]
    steps = [("compile_check", [sys.executable, "tools/compile_check.py"]),
             ("editmode", batch + ["editmode"]),
             ("playmode", batch + ["playmode"])]
    if not args.skip_playthrough:
        steps.append(("playthrough", batch + ["playmode", "--category", "Playthrough", "--timeout",
                                              str(PLAYTHROUGH_TIMEOUT)]))
    out_dir = ROOT / "Logs" / "director"
    out_dir.mkdir(parents=True, exist_ok=True)
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    summary = [f"verify {sha} ({stamp})"]
    passed = True
    with open(out_dir / f"{stamp}-verify-{sha[:7]}.log", "w", encoding="utf-8") as log:
        for name, cmd in steps:
            ok, tail = run_step(name, cmd, log)
            summary.append(f"{'PASS' if ok else 'FAIL'}  {name}")
            if not ok:
                summary += ["      " + line for line in tail]
                passed = False
                break
    (out_dir / f"{stamp}-verify-{sha[:7]}.txt").write_text("\n".join(summary) + "\n", encoding="utf-8")
    print("\n".join(summary))
    if not passed:
        fail(f"{sha[:7]} did not verify")
    say(f"VERIFIED {sha[:7]}")
    if args.push:
        push(sha)


def push(sha):
    env = dict(os.environ, GIT_TERMINAL_PROMPT="0")
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "credential.interactive=never", "push", "origin",
                          f"{sha}:main"], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8",
                         errors="replace", timeout=PUSH_TIMEOUT)
    print(res.stdout + res.stderr)
    if res.returncode != 0:
        fail(f"push of {sha[:7]} failed")
    say(f"pushed {sha[:7]} to origin/main")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("status", help="boxes, main, slots and editor at a glance")
    land = sub.add_parser("land", help="merge box branches into main and hot-reload the editor")
    land.add_argument("boxes", nargs="+", help="box names (branch box/<name>)")
    land.add_argument("-m", "--message", action="append", required=True, help="commit message paragraph")
    land.add_argument("--filter", help="builder regex (default: the boxes' own domains; empty string = none)")
    land.add_argument("--no-scene", action="store_true", help="do not rebuild Main.unity")
    refresh = sub.add_parser("refresh", help="hot-reload the main editor without merging")
    refresh.add_argument("--filter", help="builder regex to run after the refresh")
    refresh.add_argument("--scene", action="store_true", help="also rebuild Main.unity")
    outputs = sub.add_parser("outputs", help="commit regenerated builder output under Assets/")
    outputs.add_argument("-m", "--message", action="append", required=True, help="commit message paragraph")
    verify = sub.add_parser("verify", help="run the verification ladder on main in the verification worktree")
    verify.add_argument("--skip-playthrough", action="store_true", help="stop after the full PlayMode suite")
    verify.add_argument("--push", action="store_true", help="push the verified commit to origin/main")
    args = ap.parse_args()
    {"status": cmd_status, "land": cmd_land, "refresh": cmd_refresh, "outputs": cmd_outputs,
     "verify": cmd_verify}[args.cmd](args)


if __name__ == "__main__":
    main()
