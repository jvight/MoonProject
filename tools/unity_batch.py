#!/usr/bin/env python3
"""
Headless Unity on YOUR git worktree (the git toplevel of the current directory). Never runs on the main project,
which is open in the Director's editor.

Usage (from anywhere inside your worktree):
  python tools/unity_batch.py tests --platform editmode [--filter REGEX] [--assembly NAME] [--category NAME]
  python tools/unity_batch.py tests --platform playmode [--filter REGEX] [--assembly NAME]
  python tools/unity_batch.py exec --method Full.Type.Method [--arg key=value ...]
  python tools/unity_batch.py warmup      # import + compile the worktree once, then quit
  python tools/unity_batch.py status      # who holds the machine-wide batch slots

Common options:
  --timeout SECONDS   kill Unity after this long (default 900, or 3600 when the worktree has no Library yet)
  --nographics        no GPU device (faster startup; never use it for captures)
  --no-wait           fail instead of waiting when all batch slots are busy
  --project PATH      run on another worktree of this repository instead of the current one
  --unity PATH        Unity.exe to use (default: Hub install matching ProjectSettings/ProjectVersion.txt)

Outputs: Logs/batch/<stamp>-<cmd>.log is the full Unity log; Logs/batch/<stamp>-<cmd>/ holds results.xml,
captures and anything else the run writes. `exec` forwards -moonOutDir <that folder> and each --arg as
`-moonArg key=value`; read them in C# with MoonProject.Editor.Automation.BatchArgs.

Exit code: 0 = success; 1 = failure (tests failed, compile errors, timeout, or the exec method's own exit code).
At most MOON_BATCH_SLOTS (default 2) batch editors run machine-wide; extra runs wait for a free slot.
"""
import argparse
import datetime
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from moonbatch import logscan, mcpguard, nunit, procutil, slots  # noqa: E402

DEFAULT_TIMEOUT = 900
FIRST_RUN_TIMEOUT = 3600
FIRST_RUN_MEASURED = "measured: ~2 min for ~4000 assets on the 16-thread dev machine"
HEARTBEAT_SECONDS = 30
KEEP_RUNS = 30
LOCK_DIR_NAME = "moon-batch"


def say(message):
    print(f"[unity_batch] {message}", flush=True)


def fail(message, code=1):
    say(f"ERROR: {message}")
    sys.exit(code)


def git(cwd, *args):
    res = subprocess.run(["git", "-c", "safe.directory=*", *args], cwd=cwd, capture_output=True, text=True,
                         encoding="utf-8", errors="replace")
    if res.returncode != 0:
        fail(f"git {' '.join(args)} failed in {cwd}: {res.stderr.strip()}")
    return res.stdout.strip()


def same_path(a, b):
    return os.path.normcase(os.path.realpath(a)) == os.path.normcase(os.path.realpath(b))


def resolve_project(args):
    start = Path(args.project).resolve() if args.project else Path.cwd()
    root = Path(git(start, "rev-parse", "--show-toplevel")).resolve()
    common = Path(git(root, "rev-parse", "--path-format=absolute", "--git-common-dir")).resolve()
    main_checkout = common.parent
    if same_path(root, main_checkout):
        fail(f"{root} is the main checkout, which is open in the Director's Unity editor. "
             "Run unity_batch from your own worktree (D:/Project/MoonProject-wt/<box>).")
    return root, common


def ensure_not_open_elsewhere(root):
    lockfile = root / "Temp" / "UnityLockfile"
    if not lockfile.exists():
        return
    try:
        with open(lockfile, "a"):
            pass
    except OSError:
        fail(f"{root} is open in another Unity editor ({lockfile} is locked); close it first.")


def resolve_unity(root, args):
    explicit = args.unity or os.environ.get("MOON_UNITY_EXE")
    if explicit:
        unity = Path(explicit)
    else:
        version_file = root / "ProjectSettings" / "ProjectVersion.txt"
        match = re.search(r"m_EditorVersion:\s*(\S+)", version_file.read_text(encoding="utf-8"))
        if not match:
            fail(f"cannot read m_EditorVersion from {version_file}")
        unity = Path(rf"C:\Program Files\Unity\Hub\Editor\{match.group(1)}\Editor\Unity.exe")
    if not unity.exists():
        fail(f"Unity not found at {unity} (pass --unity or set MOON_UNITY_EXE)")
    return unity


def command_arguments(args, out_dir):
    if args.cmd == "tests":
        platform = {"editmode": "EditMode", "playmode": "PlayMode"}[args.platform]
        extra = ["-runTests", "-testPlatform", platform, "-testResults", str(out_dir / "results.xml")]
        if args.filter:
            extra += ["-testFilter", args.filter]
        if args.assembly:
            extra += ["-assemblyNames", ";".join(args.assembly)]
        if args.category:
            extra += ["-testCategory", ";".join(args.category)]
        return extra
    if args.cmd == "exec":
        extra = ["-executeMethod", args.method, "-quit", "-moonOutDir", str(out_dir)]
        for pair in args.arg or []:
            if "=" not in pair:
                fail(f"--arg must be key=value, got '{pair}'")
            extra += ["-moonArg", pair]
        return extra
    return ["-quit"]


def run_label(args):
    if args.cmd == "tests":
        return f"tests-{args.platform}"
    if args.cmd == "exec":
        return "exec-" + args.method.rsplit(".", 1)[-1]
    return args.cmd


def prune_old_runs(batch_dir):
    logs = sorted(batch_dir.glob("[0-9]*-*.log"))
    for log in logs[:-KEEP_RUNS]:
        try:
            log.unlink()
        except OSError:
            continue
        run_dir = log.with_suffix("")
        if run_dir.is_dir():
            shutil.rmtree(run_dir, ignore_errors=True)


def remove_test_runner_leftovers(root):
    """The test runner can leave Assets/InitTestScene<guid>.unity behind when a run is interrupted."""
    for scene in sorted((root / "Assets").glob("InitTestScene*.unity")):
        for path in (scene, scene.with_name(scene.name + ".meta")):
            if path.exists():
                path.unlink()
        say(f"removed test-runner leftover {scene.relative_to(root).as_posix()}")


def format_duration(seconds):
    minutes, secs = divmod(int(seconds), 60)
    return f"{minutes}m{secs:02d}s" if minutes else f"{secs}s"


class LogEcho:
    """Reads what Unity appended to its log since the last call; echoes complete [moon] lines, feeds the scanner."""

    def __init__(self, log_path):
        self._path = log_path
        self._offset = 0
        self._partial = ""

    def drain(self, scanner):
        if not self._path.exists():
            return
        with open(self._path, "rb") as f:
            f.seek(self._offset)
            chunk = f.read()
        self._offset += len(chunk)
        text = chunk.decode("utf-8", errors="replace")
        scanner.feed(text)
        lines = (self._partial + text).split("\n")
        self._partial = lines.pop()
        for line in lines:
            if line.startswith("[moon] "):
                print(line.rstrip(), flush=True)


def run_unity(cmdline, env, log_path, timeout, slot, first_run):
    guard = procutil.ProcessTreeGuard()
    scanner = logscan.Scanner()
    started = time.monotonic()
    last_heartbeat = started
    last_phase = None
    timed_out = False
    proc = subprocess.Popen(cmdline, env=env, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                            stderr=subprocess.DEVNULL)
    if not guard.attach(proc):
        say("warning: could not put Unity in a job object; a killed unity_batch may leave Unity running")
    slot.update(unity_pid=proc.pid)
    say(f"Unity started (pid {proc.pid}); log: {log_path}")
    echo = LogEcho(log_path)
    try:
        while True:
            try:
                code = proc.wait(timeout=1.0)
                echo.drain(scanner)
                break
            except subprocess.TimeoutExpired:
                code = None
            echo.drain(scanner)
            now = time.monotonic()
            if scanner.phase != last_phase and (first_run or now - started > 10):
                say(f"{format_duration(now - started)}: {scanner.describe()}")
                last_phase = scanner.phase
                last_heartbeat = now
            elif now - last_heartbeat >= HEARTBEAT_SECONDS:
                say(f"{format_duration(now - started)}: still working ({scanner.describe()})")
                last_heartbeat = now
            if now - started > timeout:
                timed_out = True
                say(f"TIMEOUT after {format_duration(timeout)}: killing Unity")
                guard.kill()
                code = proc.wait()
                break
    except KeyboardInterrupt:
        say("interrupted: killing Unity")
        guard.kill()
        proc.wait()
        guard.close()
        raise
    guard.close()
    return code, time.monotonic() - started, timed_out


def cmd_status(args):
    root = Path(git(Path(args.project).resolve() if args.project else Path.cwd(), "rev-parse", "--show-toplevel"))
    common = Path(git(root, "rev-parse", "--path-format=absolute", "--git-common-dir"))
    count = slot_count()
    for index, held, info in slots.status(str(common / LOCK_DIR_NAME), count):
        say(f"slot {index}: " + (slots.describe(info) if held else "free"))


def slot_count():
    try:
        return max(1, int(os.environ.get("MOON_BATCH_SLOTS", "2")))
    except ValueError:
        return 2


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)

    def common(p):
        p.add_argument("--timeout", type=int, help="seconds before Unity is killed")
        p.add_argument("--nographics", action="store_true", help="start Unity with -nographics (no captures)")
        p.add_argument("--no-wait", action="store_true", help="fail if no batch slot is free")
        p.add_argument("--project", help="worktree to run on (default: git toplevel of cwd)")
        p.add_argument("--unity", help="path to Unity.exe")
        p.add_argument("--show-log-errors", action="store_true",
                       help="for tests: list exceptions / logged errors found in the log (often expected by tests)")

    tests = sub.add_parser("tests", help="run EditMode or PlayMode tests")
    tests.add_argument("--platform", required=True, choices=["editmode", "playmode"], type=str.lower)
    tests.add_argument("--filter", help="regex / semicolon list matched against full test names")
    tests.add_argument("--assembly", action="append", help="test assembly name (repeatable)")
    tests.add_argument("--category", action="append", help="NUnit category (repeatable)")
    common(tests)

    exe = sub.add_parser("exec", help="run a static editor method via -executeMethod")
    exe.add_argument("--method", required=True, help="Full.Namespace.Type.Method")
    exe.add_argument("--arg", action="append", help="key=value forwarded as -moonArg (repeatable)")
    common(exe)

    warm = sub.add_parser("warmup", help="open the worktree once (import + compile) and quit")
    common(warm)

    status = sub.add_parser("status", help="show the machine-wide batch slots")
    status.add_argument("--project", help="any worktree of the repository")

    args = ap.parse_args()
    if args.cmd == "status":
        cmd_status(args)
        return

    root, common_dir = resolve_project(args)
    unity = resolve_unity(root, args)
    label = run_label(args)
    batch_dir = root / "Logs" / "batch"
    batch_dir.mkdir(parents=True, exist_ok=True)

    worktree_lock = slots.acquire(str(batch_dir / ".lock"), 1, str(root), label, wait=not args.no_wait, log=print)
    if worktree_lock is None:
        fail("another unity_batch run is using this worktree (--no-wait)")
    try:
        ensure_not_open_elsewhere(root)
        slot = slots.acquire(str(common_dir / LOCK_DIR_NAME), slot_count(), str(root), label,
                             wait=not args.no_wait, log=print)
        if slot is None:
            fail("all batch slots are busy (--no-wait)")
        try:
            stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
            out_dir = batch_dir / f"{stamp}-{label}"
            log_path = batch_dir / f"{stamp}-{label}.log"
            out_dir.mkdir(parents=True, exist_ok=True)
            first_run = not (root / "Library" / "ArtifactDB").exists()
            timeout = args.timeout or (FIRST_RUN_TIMEOUT if first_run else DEFAULT_TIMEOUT)
            cmdline = [str(unity), "-batchmode", "-projectPath", str(root), "-logFile", str(log_path),
                       "-silent-crashes"]
            if args.nographics:
                cmdline.append("-nographics")
            cmdline += command_arguments(args, out_dir)

            config = mcpguard.write_worktree_config(root)
            env = mcpguard.child_environment(os.environ)
            say(f"project: {root}")
            say(f"command: {label}  (timeout {format_duration(timeout)}; batch slot {slot.index}; "
                f"MCP isolated via {config.name} + env)")
            if first_run:
                say("first run on this worktree (no Library): Unity resolves packages, compiles every script and "
                    f"imports every asset before running anything ({FIRST_RUN_MEASURED}). "
                    "Later runs start in seconds.")
            code, elapsed, timed_out = run_unity(cmdline, env, log_path, timeout, slot, first_run)
        finally:
            slot.release()
    except KeyboardInterrupt:
        sys.exit(130)
    finally:
        worktree_lock.release()

    log_text = log_path.read_text(encoding="utf-8", errors="replace") if log_path.exists() else ""
    report = logscan.analyse(log_text, root)
    say(f"Unity exited with code {code} after {format_duration(elapsed)}")
    if args.cmd == "tests" and not args.show_log_errors:
        noted = len(report.exceptions) + len(report.logged_errors)
        if noted:
            say(f"log has {len(report.exceptions)} exception(s) and {len(report.logged_errors)} logged error(s), "
                "usually expected by tests (LogAssert); --show-log-errors lists them")
        report.exceptions = []
        report.logged_errors = []
    logscan.print_report(report, out=print, verbose_moon=False)

    ok = not timed_out
    if args.cmd == "tests":
        results_path = out_dir / "results.xml"
        if results_path.exists():
            results = nunit.parse(results_path)
            nunit.print_results(results)
            if results.total == 0:
                say("no tests matched (check --filter / --assembly)")
            ok = ok and results.ok
        else:
            say("no test results were written (Unity failed before running tests; see errors above / the log)")
            ok = False
    else:
        ok = ok and code == 0 and not report.compile_errors and not report.method_failures

    if args.cmd == "tests":
        remove_test_runner_leftovers(root)
    prune_old_runs(batch_dir)
    say(f"log: {log_path}")
    say(f"outputs: {out_dir}")
    say("RESULT: " + ("PASS" if ok else "FAIL"))
    if args.cmd == "exec" and code not in (None, 0):
        sys.exit(code)
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
