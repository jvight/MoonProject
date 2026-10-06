#!/usr/bin/env python3
"""
Windows playtest build of YOUR worktree (git toplevel of the current directory; never the main project).

  python tools/build_player.py [--out Builds/LofiLunar-Windows] [--skip-builders] [--no-smoke] [--no-build]

Steps:
  1. tools/unity_batch.py exec MoonProject.Editor.Build.PlayerBuild.BuildWindows: runs every builder (Main Scene
     last) so the build reflects source, then builds Main.unity for StandaloneWindows64 (Mono, release) and writes
     build_info.txt (version = <bundleVersion>-<commit date>-<short hash>[-dirty]) next to the exe.
  2. Zips the build to Builds/LofiLunar-<version>.zip (Builds/ is git-ignored) and prints its size and path.
  3. Smoke check: launches the exe windowed (1280x720) with -logFile and -saveSlot smoke for --smoke-seconds,
     requires the bootstrap to confirm the smoke slot and the WorldSystem boot line, and no exceptions/errors in the
     player log, then closes it and reports boot time and sizes. The smoke slot's files are deleted before and
     after the run, so a smoke run never reads or writes the player's progress.

Exit code 0 = build, zip and smoke check all passed.
"""
import argparse
import os
import re
import subprocess
import sys
import time
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from moonbatch import logscan, procutil  # noqa: E402

BUILD_METHOD = "MoonProject.Editor.Build.PlayerBuild.BuildWindows"
INFO_FILE = "build_info.txt"
BOOT_MARKER = re.compile(r"^WorldSystem: terrain")
# Release players log engine errors as bare lines (no severity, no stack), so these words count as failures.
PLAYER_PROBLEM = re.compile(r"(?i)\b(couldn't|could not|cannot|can't|failed|failure|error|invalid|missing|"
                            r"exception|crash)\b")
BUILD_TIMEOUT = 5400
SMOKE_SLOT = "smoke"
SLOT_MARKER = re.compile(r"^GameBootstrap: save slot '(?P<slot>[^']*)'")


def say(message):
    print(f"[build_player] {message}", flush=True)


def git_toplevel():
    res = subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--show-toplevel"], capture_output=True,
                         text=True)
    if res.returncode != 0:
        sys.exit(f"[build_player] ERROR: not inside a git worktree: {res.stderr.strip()}")
    return Path(res.stdout.strip()).resolve()


def read_info(out_dir):
    info = {}
    for line in (out_dir / INFO_FILE).read_text(encoding="utf-8").splitlines():
        if "=" in line:
            key, value = line.split("=", 1)
            info[key.strip()] = value.strip()
    return info


def player_identity(root):
    text = (root / "ProjectSettings" / "ProjectSettings.asset").read_text(encoding="utf-8")
    company = re.search(r"^\s*companyName: (.*)$", text, re.MULTILINE).group(1).strip()
    product = re.search(r"^\s*productName: (.*)$", text, re.MULTILINE).group(1).strip()
    return company, product


def folder_size(path):
    return sum(f.stat().st_size for f in path.rglob("*") if f.is_file())


def mb(size):
    return f"{size / (1024 * 1024):.1f} MB"


def build(root, out_dir, skip_builders, timeout):
    cmd = [sys.executable, str(Path(__file__).resolve().parent / "unity_batch.py"), "exec", "--method", BUILD_METHOD,
           "--project", str(root), "--timeout", str(timeout), "--arg", f"out={out_dir}",
           "--arg", f"builders={'false' if skip_builders else 'true'}"]
    say("building the player through unity_batch (first builds compile player shaders and take several minutes)")
    return subprocess.run(cmd).returncode == 0


def make_zip(root, out_dir, version):
    builds = root / "Builds"
    builds.mkdir(exist_ok=True)
    name = f"LofiLunar-{version}"
    archive = builds / f"{name}.zip"
    if archive.exists():
        archive.unlink()
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
        for file in sorted(p for p in out_dir.rglob("*") if p.is_file()):
            zf.write(file, f"{name}/{file.relative_to(out_dir).as_posix()}")
    return archive


def delete_slot_files(saves_dir, slot):
    """Removes every file of a save slot (save, backup, temp, set-aside corrupt copies)."""
    if saves_dir.is_dir():
        for file in saves_dir.glob(f"{slot}.json*"):
            file.unlink()


def smoke(root, out_dir, info, seconds):
    exe = out_dir / info["exe"]
    log_path = root / "Logs" / "batch" / f"{time.strftime('%Y%m%d-%H%M%S')}-smoke-player.log"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    company, product = player_identity(root)
    saves_dir = Path(os.environ["USERPROFILE"]) / "AppData" / "LocalLow" / company / product / "Saves"
    delete_slot_files(saves_dir, SMOKE_SLOT)
    say(f"smoke: launching {exe.name} windowed 1280x720, save slot '{SMOKE_SLOT}', for {seconds}s (log {log_path})")
    boot_seconds = None
    exited_early = None
    boot_line = ""
    slot_in_use = None
    guard = procutil.ProcessTreeGuard()
    started = time.monotonic()
    proc = subprocess.Popen([str(exe), "-screen-width", "1280", "-screen-height", "720", "-screen-fullscreen", "0",
                             "-saveSlot", SMOKE_SLOT, "-logFile", str(log_path)], cwd=str(out_dir))
    guard.attach(proc)
    try:
        while time.monotonic() - started < seconds:
            code = proc.poll()
            if code is not None:
                exited_early = code
                break
            if boot_seconds is None and log_path.exists():
                for line in log_path.read_text(encoding="utf-8", errors="replace").splitlines():
                    slot_match = SLOT_MARKER.match(line)
                    if slot_match:
                        slot_in_use = slot_match.group("slot")
                    if BOOT_MARKER.match(line):
                        boot_seconds = time.monotonic() - started
                        boot_line = line
                        break
                if boot_seconds is not None and slot_in_use != SMOKE_SLOT:
                    break
            time.sleep(0.25)
    finally:
        guard.kill()
        proc.wait()
        guard.close()
        delete_slot_files(saves_dir, SMOKE_SLOT)
    text = log_path.read_text(encoding="utf-8", errors="replace") if log_path.exists() else ""
    report = logscan.analyse(text, out_dir)
    logscan.print_report(report)
    native = player_problems(text)
    for line, count in native:
        say(f"smoke: player log problem x{count}: {line}")
    ok = True
    if slot_in_use != SMOKE_SLOT:
        say(f"smoke: FAIL - the player did not confirm save slot '{SMOKE_SLOT}' (got {slot_in_use!r}); "
            "the build predates -saveSlot or ignored it, so it was stopped as soon as it booted")
        ok = False
    else:
        say(f"smoke: the player used save slot '{SMOKE_SLOT}' (deleted again)")
    if exited_early is not None:
        say(f"smoke: FAIL - the player exited by itself after {time.monotonic() - started:.1f}s (code {exited_early})")
        ok = False
    if boot_seconds is None:
        say("smoke: FAIL - no 'WorldSystem: terrain' boot line in the player log")
        ok = False
    else:
        say(f"smoke: world booted {boot_seconds:.1f}s after launch: {boot_line.strip()}")
    if report.has_problems or native:
        say("smoke: FAIL - the player log has errors or exceptions (listed above)")
        ok = False
    return ok, boot_seconds, log_path


def player_problems(text):
    """Distinct problem lines of a player log with how often each occurred (numbers folded together)."""
    counts = {}
    for line in text.splitlines():
        line = line.strip()
        if line and PLAYER_PROBLEM.search(line):
            key = re.sub(r"\d+", "#", line)
            first, count = counts.get(key, (line, 0))
            counts[key] = (first, count + 1)
    return sorted(counts.values(), key=lambda item: -item[1])


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", help="build folder (default Builds/LofiLunar-Windows in the worktree)")
    ap.add_argument("--skip-builders", action="store_true", help="build the player without re-running builders")
    ap.add_argument("--no-build", action="store_true", help="reuse the existing build folder (zip + smoke only)")
    ap.add_argument("--no-smoke", action="store_true", help="skip launching the built player")
    ap.add_argument("--smoke-seconds", type=int, default=20)
    ap.add_argument("--timeout", type=int, default=BUILD_TIMEOUT, help="seconds before the Unity build is killed")
    args = ap.parse_args()

    root = git_toplevel()
    out_dir = (Path(args.out) if args.out and Path(args.out).is_absolute() else root / (args.out or
               "Builds/LofiLunar-Windows")).resolve()
    if not args.no_build and not build(root, out_dir, args.skip_builders, args.timeout):
        say("RESULT: FAIL (player build failed; see the unity_batch output above)")
        sys.exit(1)
    if not (out_dir / INFO_FILE).exists():
        say(f"RESULT: FAIL ({out_dir / INFO_FILE} missing; build first)")
        sys.exit(1)

    info = read_info(out_dir)
    exe = out_dir / info["exe"]
    archive = make_zip(root, out_dir, info["version"])
    say(f"version {info['version']}  exe {mb(exe.stat().st_size)}  build folder {mb(folder_size(out_dir))}")
    say(f"zip {mb(archive.stat().st_size)}: {archive}")

    ok = True
    if not args.no_smoke:
        ok, _, _ = smoke(root, out_dir, info, args.smoke_seconds)
    say("RESULT: " + ("PASS" if ok else "FAIL"))
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
