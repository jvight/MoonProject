#!/usr/bin/env python3
"""
Plays the real game with the Autoplay bot, without touching the owner's mouse, keyboard or screen.

  python tools/autoplay.py [--build] [--scenario opening] [--until B3] [--out DIR] [--timeout 600]
                           [--shot-seconds 20] [--exe PATH] [--visible] [--sound]

The bot lives in the development build (python tools/build_player.py --autoplay, into Builds/Lunarion-Autoplay of
your worktree; --build makes it first). It feeds the Input System from inside the process (a virtual keyboard and
mouse; the real devices are disabled for the run), so the OS never sees an input. The window opens windowed,
unfocused, behind the other windows in the bottom-right corner of the screen, and plays the scenario on its own
save slot ('autoplay-<hash of the worktree>', cleared before and after the run).

Outputs in --out (default Logs/autoplay/<timestamp>): report.md, screenshots/*.png (one per beat and every
--shot-seconds), autoplay.log (the Unity log), player.log (the engine log). Exit code 0 = every beat passed.
"""
import argparse
import ctypes
import hashlib
import os
import subprocess
import sys
import time
from ctypes import wintypes
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import build_player  # noqa: E402
from moonbatch import procutil  # noqa: E402

SLOT_PREFIX = "autoplay"
DEFAULT_OUT = "Logs/autoplay"
BUILD_DIR = "Builds/Lunarion-Autoplay"
WINDOW_WIDTH = 960
WINDOW_HEIGHT = 540
CORNER_MARGIN = 16
WINDOW_WAIT_SECONDS = 60
GRACE_SECONDS = 90
HWND_BOTTOM = 1
SWP_NOSIZE = 0x0001
SWP_NOACTIVATE = 0x0010
STARTF_USESHOWWINDOW = 0x0001
SW_SHOWNOACTIVATE = 4
SM_CXSCREEN = 0
SM_CYSCREEN = 1


def say(message):
    print(f"[autoplay] {message}", flush=True)


def slot_for(root):
    """The save slot of this worktree: runs in two worktrees at once must not read or clear each other's save."""
    return f"{SLOT_PREFIX}-{hashlib.sha1(str(root).lower().encode()).hexdigest()[:8]}"


def launch_arguments(slot, out_dir, log_path, scenario, until, timeout, shot_seconds, lines=False, strict_guidance=True, naive=False, sound=False):
    """The player's command line: window size, own save slot, own log, and the bot's flags."""
    args = ["-screen-width", str(WINDOW_WIDTH), "-screen-height", str(WINDOW_HEIGHT), "-screen-fullscreen", "0",
            "-saveSlot", slot, "-logFile", str(log_path), "-autoplay", scenario, "-autoplay-out", str(out_dir),
            "-autoplay-timeout", str(timeout), "-autoplay-shot-seconds", str(shot_seconds)]
    if until:
        args += ["-autoplay-until", until]
    if lines:
        args += ["-autoplay-lines", "1"]
    if not strict_guidance:
        args += ["-autoplay-strict-guidance", "0"]
    if naive:
        args += ["-autoplay-naive", "1"]
    if not sound:
        args.append("-autoplay-mute")
    return args


def verdict(out_dir):
    """('PASS' | 'FAIL' | None, report path): what the bot's report.md says; None when it wrote none."""
    report = Path(out_dir) / "report.md"
    if not report.exists():
        return None, report
    for line in report.read_text(encoding="utf-8").splitlines():
        if line.startswith("**Result:"):
            return ("PASS" if "PASS" in line else "FAIL"), report
    return None, report


def window_of(pid):
    """The player's main top-level window handle (0 until it exists)."""
    user32 = ctypes.windll.user32
    found = []

    @ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
    def visit(handle, _):
        owner = wintypes.DWORD()
        user32.GetWindowThreadProcessId(handle, ctypes.byref(owner))
        if owner.value == pid and user32.IsWindowVisible(handle):
            found.append(handle)
            return False
        return True

    user32.EnumWindows(visit, 0)
    return found[0] if found else 0


def park_window(pid):
    """Puts the player's window in the bottom-right corner, behind every other window, without activating it."""
    user32 = ctypes.windll.user32
    deadline = time.monotonic() + WINDOW_WAIT_SECONDS
    handle = 0
    while not handle and time.monotonic() < deadline:
        handle = window_of(pid)
        if not handle:
            time.sleep(0.25)
    if not handle:
        say("the player's window did not appear; leaving it where the system put it")
        return
    x = user32.GetSystemMetrics(SM_CXSCREEN) - WINDOW_WIDTH - CORNER_MARGIN
    y = user32.GetSystemMetrics(SM_CYSCREEN) - WINDOW_HEIGHT - CORNER_MARGIN
    user32.SetWindowPos(handle, HWND_BOTTOM, max(x, 0), max(y, 0), 0, 0, SWP_NOSIZE | SWP_NOACTIVATE)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--scenario", default="opening")
    ap.add_argument("--until", default="", help="last beat to play (for example B3); default: the scenario's end")
    ap.add_argument("--build", action="store_true", help="build the autoplay variant first (python tools/build_player.py --autoplay)")
    ap.add_argument("--exe", help="the autoplay exe (default: Builds/Lunarion-Autoplay of this worktree)")
    ap.add_argument("--out", help="output folder (default Logs/autoplay/<timestamp>)")
    ap.add_argument("--timeout", type=int, default=1500, help="seconds the bot may play before it fails the run")
    ap.add_argument("--shot-seconds", type=int, default=20)
    ap.add_argument("--lenient-guidance", action="store_true",
                    help="two guidance elements on screen together only warn (default: they fail the run)")
    ap.add_argument("--naive", action="store_true",
                    help="first-time player check: after each beat the bot stands passive and a readable next-action "
                         "cue must be on screen within 5 s of game time, or the run fails")
    ap.add_argument("--lines", action="store_true",
                    help="the opening shows objective lines and the bot checks them on screen (default: it shows "
                         "none, and a line on screen is a stall reminder that fails the run)")
    ap.add_argument("--backend", choices=("mono", "il2cpp"), default=build_player.DEFAULT_BACKEND,
                    help="scripting backend of the --build player; the default is the one owner builds ship with")
    ap.add_argument("--visible", action="store_true", help="leave the window where the system puts it")
    ap.add_argument("--sound", action="store_true", help="play with audio on (default: muted with -autoplay-mute)")
    args = ap.parse_args()

    root = build_player.git_toplevel()
    build_dir = (root / BUILD_DIR).resolve()
    if args.build and not build_player.build(root, build_dir, False, build_player.BUILD_TIMEOUT, autoplay=True,
                                               backend=args.backend):
        say("RESULT: FAIL (the autoplay build failed)")
        sys.exit(1)
    exe = Path(args.exe).resolve() if args.exe else None
    if exe is None:
        if not (build_dir / build_player.INFO_FILE).exists():
            say(f"RESULT: FAIL (no autoplay build in {build_dir}; run with --build)")
            sys.exit(1)
        exe = build_dir / build_player.read_info(build_dir)["exe"]
    out_dir = Path(args.out).resolve() if args.out else (root / DEFAULT_OUT / time.strftime("%Y%m%d-%H%M%S")).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    company, product = build_player.player_identity(root)
    saves_dir = Path(os.environ["USERPROFILE"]) / "AppData" / "LocalLow" / company / product / "Saves"
    slot = slot_for(root)
    build_player.delete_slot_files(saves_dir, slot)
    command = [str(exe)] + launch_arguments(slot, out_dir, out_dir / "player.log", args.scenario, args.until, args.timeout,
                                            args.shot_seconds, args.lines, not args.lenient_guidance, args.naive,
                                            args.sound)
    say(f"launching {exe.name} on save slot '{slot}', output {out_dir}")
    guard = procutil.ProcessTreeGuard()
    # The window shows without taking focus, so a run never jumps in front of what the owner is doing.
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= STARTF_USESHOWWINDOW
    startup.wShowWindow = SW_SHOWNOACTIVATE
    proc = subprocess.Popen(command, cwd=str(exe.parent), startupinfo=startup)
    guard.attach(proc)
    try:
        if not args.visible:
            park_window(proc.pid)
        try:
            proc.wait(timeout=args.timeout + GRACE_SECONDS)
        except subprocess.TimeoutExpired:
            say("the player did not finish in time; stopping it")
    finally:
        guard.kill()
        proc.wait()
        guard.close()
        build_player.delete_slot_files(saves_dir, slot)

    result, report = verdict(out_dir)
    if result is None:
        say(f"RESULT: FAIL (the bot wrote no report; see {out_dir / 'player.log'}); exit code {proc.returncode}")
        sys.exit(1)
    say(f"report: {report}")
    say(f"RESULT: {result}")
    sys.exit(0 if result == "PASS" and proc.returncode == 0 else 1)


if __name__ == "__main__":
    main()
