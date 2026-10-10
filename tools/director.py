#!/usr/bin/env python3
"""
Director-only workflow for landing box branches on main and keeping the main editor in step with it
(see CLAUDE.md, "How the team works"). Run from the main project.

Usage:
  python tools/director.py status
  python tools/director.py land <box> [<box> ...] -m MESSAGE [-m MESSAGE ...] [--filter REGEX] [--no-scene]
  python tools/director.py land --batch <box> [<box> ...] -m MESSAGE [-m MESSAGE ...] [--filter REGEX] [--no-scene]
                                [--dry-run] [--worktree PATH]
  python tools/director.py refresh [--filter REGEX] [--scene]
  python tools/director.py outputs -m MESSAGE [-m MESSAGE ...]
  python tools/director.py verify [--skip-playthrough] [--parallel] [--shards N] [--push]

status   every box branch against main (ahead, behind, uncommitted files in its worktree), main against origin and
         the verification worktree, the batch slots, whether the main editor is idle, and the last status block of
         each board in MoonProject-wt/.board (written by tools/box_check.py).
land     merges the box branches into main (several boxes land as one merge), requires compile_check PASS before
         the merge is committed (otherwise the merge is aborted), then hot-reloads the main editor: assets-refresh,
         wait until it is idle with no console errors, run the boxes' own builders (e.g. ^(Art|Rover)/, or
         --filter) and rebuild Main.unity. It lists what the builders changed and every tuning or settings type the
         merge touched: saved assets of those types keep their old values until they are reset.
         --batch lands without the main editor: the merge, compile_check, the boxes' builders and the Main Scene
         build all run in the verification worktree (unity_batch exec, which also writes the new scripts' metas);
         only if all pass is the merge committed there, with a second commit for the regenerated outputs and
         metas, and main fast-forwarded to it. On any failure the verification worktree goes back to main and main
         is untouched. --dry-run stops before main; --worktree lands in another worktree (created on first use).
refresh  the same editor hot reload without a merge.
outputs  commits what the builders regenerated under Assets/ and reports any other change it left unstaged.
verify   fast-forwards the verification worktree to main and runs compile_check, EditMode, PlayMode and the
         Playthrough golden path there through tools/unity_batch.py, stopping at the first failure. --parallel runs
         the Playthrough in a second worktree (MoonProject-wt/director2, created on first use) alongside EditMode and
         PlayMode, using both batch slots (~25 min instead of ~45). PlayMode is sharded by test assembly across
         the shard worktrees (director, then director2 unless it runs the Playthrough, then landtest; each must
         exist, except director2 which is created on first use), balanced on the assembly times of the last full
         run, so it takes the longest shard's time, not the sum; --shards N caps it (1 = one Unity, as before).
         --push pushes the verified commit to origin/main when every step passed.

Each -m is one paragraph of the commit message, as with git.
"""
import argparse
import collections
import contextlib
import datetime
import json
import os
import re
import socket
import subprocess
import sys
import threading
import time
import urllib.error
import xml.etree.ElementTree as ElementTree
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import box_check  # noqa: E402
import unity_mcp  # noqa: E402
from moonbatch import procutil  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
VERIFY_WORKTREE = ROOT.parent / "MoonProject-wt" / "director"
VERIFY_BRANCH = "director/verify"
VERIFY_WORKTREE_2 = ROOT.parent / "MoonProject-wt" / "director2"
VERIFY_BRANCH_2 = "director/verify2"
WORKTREE_LOCK = Path("Logs") / "director.lock"
SPARE_WORKTREE = ROOT.parent / "MoonProject-wt" / "landtest"
MAX_SHARDS = 3
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


def editor_errors(since):
    """Errors the editor logged since <since> (time.monotonic()), rounded up to whole minutes."""
    minutes = int((time.monotonic() - since) // 60) + 1
    text = mcp_text("console-get-logs", {"logTypeFilter": "Error", "maxEntries": 20, "lastMinutes": minutes},
                    EDITOR_STATE_TIMEOUT)
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
    say("editor: refreshing assets")
    started = time.monotonic()
    mcp_text("assets-refresh", {}, BUILD_TIMEOUT)
    wait_editor_idle()
    errors = editor_errors(started)
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


def commit(messages, cwd=ROOT):
    args = ["commit"]
    for message in messages:
        args += ["-m", message]
    git(*args, cwd=cwd)
    say(f"committed {git('log', '--oneline', '-1', cwd=cwd)}")


def main_checkout():
    return Path(git("rev-parse", "--path-format=absolute", "--git-common-dir")).resolve().parent


class WorktreeLock:
    """One director command (verify or land --batch) at a time in a verification worktree."""

    def __init__(self, path, command):
        self.file = path / WORKTREE_LOCK
        self.command = command

    def __enter__(self):
        if self.file.exists():
            holder = self.file.read_text(encoding="utf-8").split(maxsplit=1)
            if holder and holder[0].isdigit() and procutil.pid_alive(int(holder[0])):
                fail(f"{self.file.parent.parent} is in use by pid {' '.join(holder)}")
        self.file.parent.mkdir(parents=True, exist_ok=True)
        self.file.write_text(f"{os.getpid()} {self.command}", encoding="utf-8")
        return self

    def __exit__(self, *_):
        self.file.unlink(missing_ok=True)


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
    for board in sorted(box_check.board_dir(ROOT).glob("*.md")):
        block = box_check.last_block(board.read_text(encoding="utf-8"))
        if block:
            say(f"board {board.stem}:")
            print(block)


def cmd_land(args):
    if args.batch:
        cmd_land_batch(args)
        return
    if args.dry_run or args.worktree:
        fail("--dry-run and --worktree only apply to land --batch")
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


def batch_builder_filter(args):
    """The boxes' builders (or --filter) and, unless --no-scene, the Main Scene build, as one regex."""
    domains = args.filter if args.filter is not None else default_filter(args.boxes)
    scene = "" if args.no_scene else f"^{box_check.MAIN_SCENE_BUILDER}$"
    return "|".join(f"(?:{part})" for part in (domains, scene) if part)


def discard(path, base):
    """Puts a verification worktree back on <base>, dropping a merge in progress and untracked builder output."""
    git("merge", "--abort", cwd=path, check=False)
    git("reset", "--hard", base, cwd=path)
    git("clean", "-fdq", "--", "Assets", cwd=path)


def cmd_land_batch(args):
    worktree = Path(args.worktree).resolve() if args.worktree else VERIFY_WORKTREE
    branch = VERIFY_BRANCH if worktree == VERIFY_WORKTREE else f"director/{worktree.name}"
    checkout = main_checkout()
    if not args.dry_run:
        if tracked_changes(checkout):
            fail(f"{checkout} has uncommitted changes; commit or stash them first")
        if git("rev-parse", "--abbrev-ref", "HEAD", cwd=checkout) != "main":
            fail(f"{checkout} is not on main")
    branches = [BOX_PREFIX + box for box in args.boxes]
    for box_branch in branches:
        if git("rev-list", "--count", f"main..{box_branch}") == "0":
            fail(f"{box_branch} has nothing to land")
    if not worktree.exists():
        git("worktree", "add", "-B", branch, str(worktree), "main")
    with WorktreeLock(worktree, "land --batch"):
        base = ready_worktree(worktree, branch)
        landed = merge_and_build(args, worktree, branches, base)
        if args.dry_run:
            print(git("log", "--stat", "--format=%h %s", f"{base}..{landed}", "--", "Assets", cwd=worktree))
            discard(worktree, base)
            say(f"dry run: {landed[:7]} passed in {worktree.name}; main untouched, {worktree.name} back on main")
            return
        res = subprocess.run(["git", "-c", "safe.directory=*", "merge", "--ff-only", landed], cwd=checkout,
                             capture_output=True, text=True, encoding="utf-8", errors="replace")
        if res.returncode != 0:
            print(res.stdout + res.stderr)
            discard(worktree, base)
            fail("main moved or its checkout refused the fast-forward; nothing landed, run land --batch again")
    say(f"main fast-forwarded to {git('log', '--oneline', '-1', 'main')}")
    say("the main editor, if open, picks the change up on its next refresh (`refresh` forces it)")
    for path in git("diff", "--name-only", base, landed).splitlines():
        if SCHEMA_TYPE.search(path):
            say(f"NOTE: the merge touched {path}; reset its saved assets if fields or defaults changed")


def merge_and_build(args, worktree, branches, base):
    """Merges <branches> in <worktree>, gates it on compile_check and the batch builders, commits the merge and the
    regenerated outputs there and returns the resulting sha; on failure the worktree goes back to <base>."""
    res = subprocess.run(["git", "-c", "safe.directory=*", "-c", "core.safecrlf=false", "merge", "--no-ff",
                          "--no-commit", *branches], cwd=worktree, capture_output=True, text=True, encoding="utf-8",
                         errors="replace")
    if res.returncode != 0:
        print(res.stdout + res.stderr)
        discard(worktree, base)
        fail("the merge did not apply cleanly; ask the box to merge main into its branch and resolve it there")
    say(f"compile_check on the merged tree in {worktree.name}")
    if not compile_check(worktree):
        discard(worktree, base)
        fail("compile_check failed on the merged tree; nothing landed")
    commit(args.message, cwd=worktree)
    pattern = batch_builder_filter(args)
    if pattern:
        log_dir = ROOT / "Logs" / "director"
        log_dir.mkdir(parents=True, exist_ok=True)
        log_path = log_dir / f"{datetime.datetime.now():%Y%m%d-%H%M%S}-land-batch.log"
        results = []
        run_chain([("builders", [sys.executable, "tools/unity_batch.py", "exec", "--method",
                                 box_check.RUN_BUILDERS, "--arg", f"filter={pattern}"])],
                  worktree, log_path, True, results)
        if not results[-1][1]:
            discard(worktree, base)
            fail(f"the builders or the Main Scene build failed (log {log_path}); nothing landed")
    git("add", "-A", "--", "Assets", cwd=worktree)
    outside = [line for line in git("status", "--porcelain", cwd=worktree).splitlines()
               if not line[3:].startswith("Assets/")]
    if outside:
        print("\n".join(outside))
        discard(worktree, base)
        fail("the builders changed files outside Assets/; nothing landed")
    staged = git("diff", "--cached", "--name-only", cwd=worktree).splitlines()
    if staged:
        say(f"{len(staged)} regenerated output(s) and meta(s)")
        trailers = [m for m in args.message if m.startswith("Co-Authored-By")]
        commit([f"build: outputs and metas for {', '.join(args.boxes)} from the batch land"] + trailers,
               cwd=worktree)
    else:
        say("the builders changed nothing")
    return git("rev-parse", "HEAD", cwd=worktree)


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


def run_step(name, cmd, log, cwd, echo):
    """Runs one verification step in <cwd>, logging (and optionally echoing) its output; returns (passed, tail)."""
    say(f"verify: {name} ({cwd.name})")
    tail = collections.deque(maxlen=TAIL_LINES)
    with subprocess.Popen(cmd, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                          encoding="utf-8", errors="replace") as proc:
        for line in proc.stdout:
            if echo:
                print(line, end="", flush=True)
            log.write(line)
            tail.append(line.rstrip())
    return proc.returncode == 0, list(tail)


def run_chain(steps, cwd, log_path, echo, results):
    """Runs <steps> in order in <cwd>, stopping at the first failure; appends (name, passed, tail) to <results>."""
    with open(log_path, "w", encoding="utf-8") as log:
        for name, cmd in steps:
            ok, tail = run_step(name, cmd, log, cwd, echo)
            results.append((name, ok, tail))
            if not ok:
                return


def ready_worktree(path, branch):
    """Fast-forwards a clean verification worktree to main (creating it on <branch> if missing); returns its HEAD."""
    if not path.exists():
        git("worktree", "add", "-B", branch, str(path), "main")
    if tracked_changes(path):
        fail(f"{path} has local changes")
    git("merge", "--ff-only", "main", cwd=path)
    return git("rev-parse", "HEAD", cwd=path)


def playmode_assemblies(worktree):
    """Names of the PlayMode test assemblies: test asmdefs for every platform that reference the test runner."""
    names = []
    for path in sorted((worktree / "Assets" / "_Project").rglob("*.asmdef")):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        if ("UNITY_INCLUDE_TESTS" in data.get("defineConstraints", []) and not data.get("includePlatforms")
                and "UnityEngine.TestRunner" in data.get("references", [])):
            names.append(data["name"])
    return names


def assembly_seconds(results_xml):
    """{assembly name: seconds} from an NUnit results.xml, or {} when it cannot be read."""
    try:
        root = ElementTree.parse(results_xml).getroot()
    except (OSError, ElementTree.ParseError):
        return {}
    return {suite.get("name", "").removesuffix(".dll"): float(suite.get("duration", 0))
            for suite in root.iter("test-suite") if suite.get("type") == "Assembly"}


def last_playmode_seconds(worktree):
    """Assembly times of the newest PlayMode run in <worktree> that covered more than one assembly."""
    for run in sorted((worktree / "Logs" / "batch").glob("*-tests-playmode"), reverse=True):
        seconds = assembly_seconds(run / "results.xml")
        if len(seconds) > 1:
            return seconds
    return {}


def shard(assemblies, seconds, count):
    """Splits <assemblies> into at most <count> non-empty shards, longest first onto the lightest shard. Unknown
    assemblies weigh the median known time (1 s when none is known)."""
    known = sorted(seconds[a] for a in assemblies if a in seconds)
    default = known[len(known) // 2] if known else 1.0
    weights = {a: seconds.get(a, default) for a in assemblies}
    shards = [[] for _ in range(max(1, min(count, len(assemblies))))]
    totals = [0.0] * len(shards)
    for name in sorted(assemblies, key=lambda a: (-weights[a], a)):
        lightest = totals.index(min(totals))
        shards[lightest].append(name)
        totals[lightest] += weights[name]
    return [sorted(s) for s in shards]


def shard_worktrees(args):
    """Worktrees PlayMode shards run in: director first, director2 when it is not running the Playthrough, then the
    spare landtest worktree when it exists."""
    pool = [(VERIFY_WORKTREE, VERIFY_BRANCH)]
    if not (args.parallel and not args.skip_playthrough):
        pool.append((VERIFY_WORKTREE_2, VERIFY_BRANCH_2))
    if SPARE_WORKTREE.exists():
        pool.append((SPARE_WORKTREE, f"director/{SPARE_WORKTREE.name}"))
    return pool[:max(1, min(args.shards, MAX_SHARDS))]


def cmd_verify(args):
    if not VERIFY_WORKTREE.exists():
        fail(f"no verification worktree at {VERIFY_WORKTREE}")
    with contextlib.ExitStack() as locks:
        used = [path for path, _ in shard_worktrees(args)]
        for path in dict.fromkeys(used + [VERIFY_WORKTREE_2]):
            if path.exists():
                locks.enter_context(WorktreeLock(path, "verify"))
        verify(args)


def verify(args):
    sha = ready_worktree(VERIFY_WORKTREE, VERIFY_BRANCH)
    batch = [sys.executable, "tools/unity_batch.py", "tests", "--platform"]
    pool = shard_worktrees(args)
    for path, branch in pool[1:]:
        if ready_worktree(path, branch) != sha:
            fail(f"{path} is not at {sha[:7]}")
    shards = shard(playmode_assemblies(VERIFY_WORKTREE), last_playmode_seconds(VERIFY_WORKTREE), len(pool))
    playmode = [(f"playmode {' '.join(a.removeprefix('MoonProject.') for a in names)}",
                 batch + ["playmode"] + [arg for name in names for arg in ("--assembly", name)]) for names in shards]
    playthrough = [("playthrough", batch + ["playmode", "--category", "Playthrough", "--timeout",
                                            str(PLAYTHROUGH_TIMEOUT)])]
    out_dir = ROOT / "Logs" / "director"
    out_dir.mkdir(parents=True, exist_ok=True)
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    base = out_dir / f"{stamp}-verify-{sha[:7]}"
    results = []
    run_chain([("compile_check", [sys.executable, "tools/compile_check.py"])], VERIFY_WORKTREE,
              base.with_suffix(".compile.log"), True, results)
    if results[-1][1]:
        parallel = args.parallel and not args.skip_playthrough
        chains = [(path, [step], base.with_name(f"{base.name}.shard{i}.log"))
                  for i, ((path, _), step) in enumerate(zip(pool, playmode))]
        chains[0] = (VERIFY_WORKTREE, [("editmode", batch + ["editmode"])] + chains[0][1],
                     base.with_suffix(".tests.log"))
        if parallel:
            if ready_worktree(VERIFY_WORKTREE_2, VERIFY_BRANCH_2) != sha:
                fail(f"{VERIFY_WORKTREE_2} is not at {sha[:7]}")
            chains.append((VERIFY_WORKTREE_2, playthrough, base.with_suffix(".playthrough.log")))
        say(f"verify: EditMode, then PlayMode in {len(playmode)} shard(s) on "
            f"{', '.join(path.name for path, _ in pool[:len(playmode)])}"
            + (f", alongside the Playthrough in {VERIFY_WORKTREE_2.name}" if parallel else "")
            + f" (logs {base.name}.*.log)")
        threads = [threading.Thread(target=run_chain, args=(steps, path, log, len(chains) == 1, results))
                   for path, steps, log in chains]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
        if not parallel and not args.skip_playthrough and all(ok for _, ok, _ in results):
            run_chain(playthrough, VERIFY_WORKTREE, base.with_suffix(".playthrough.log"), True, results)
    summary = [f"verify {sha} ({stamp})"]
    for name, ok, tail in results:
        summary.append(f"{'PASS' if ok else 'FAIL'}  {name}")
        if not ok:
            summary += ["      " + line for line in tail]
    base.with_suffix(".txt").write_text("\n".join(summary) + "\n", encoding="utf-8")
    print("\n".join(summary))
    expected = 2 + len(playmode) + (0 if args.skip_playthrough else 1)
    if len(results) != expected or not all(ok for _, ok, _ in results):
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
    land.add_argument("--batch", action="store_true",
                      help="merge, build and gate in the verification worktree; no main editor needed")
    land.add_argument("--dry-run", action="store_true", help="with --batch: everything but the fast-forward of main")
    land.add_argument("--worktree", help="with --batch: worktree to land in (default: the verification worktree)")
    refresh = sub.add_parser("refresh", help="hot-reload the main editor without merging")
    refresh.add_argument("--filter", help="builder regex to run after the refresh")
    refresh.add_argument("--scene", action="store_true", help="also rebuild Main.unity")
    outputs = sub.add_parser("outputs", help="commit regenerated builder output under Assets/")
    outputs.add_argument("-m", "--message", action="append", required=True, help="commit message paragraph")
    verify = sub.add_parser("verify", help="run the verification ladder on main in the verification worktree")
    verify.add_argument("--skip-playthrough", action="store_true", help="stop after the full PlayMode suite")
    verify.add_argument("--push", action="store_true", help="push the verified commit to origin/main")
    verify.add_argument("--parallel", action="store_true",
                        help="run the Playthrough in a second verification worktree alongside the suites")
    verify.add_argument("--shards", type=int, default=MAX_SHARDS,
                        help=f"PlayMode shards, one Unity each (default and maximum {MAX_SHARDS}; 1 = unsharded)")
    args = ap.parse_args()
    {"status": cmd_status, "land": cmd_land, "refresh": cmd_refresh, "outputs": cmd_outputs,
     "verify": cmd_verify}[args.cmd](args)


if __name__ == "__main__":
    main()
