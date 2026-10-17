#!/usr/bin/env python3
"""
Automated PlayMode feel-capture runner (Task CORE FEEL AUDIT).

Usage:
  python tools/feel_capture.py --scenario <name|all> [--out Logs/audit] [--timeout SECONDS]

Scenarios:
  driving        Scenario 1: Flat accelerate, cruise, coast, reverse, low/high-speed turns, skid release
  slopes         Scenario 2: 10° / 20° / steep climbs, downhill coast, crest airtime, 15° & 35° side-slope
  leap           Scenario 3: Hover-Jump charge crouch, release, lunar arc, air pitch/roll, landing cushion
  collisions     Scenario 4: Head-on boulder impact, glancing scrape, angled ruin wall pinning
  camera         Scenario 5: Orbit 360°, free-look damping loss, auto-recenter, rock occlusion, downhill lift, zoom
  verbs          Scenario 6: Scan, Cutter (wreck + sealed crate), Tether & Winch, Build mode, Face Inventory, Dock
  first_minutes  Scenario 7: Title -> New Game -> Opening wake -> first control -> tower -> depot -> ridge -> ruins -> chasm
  night_dawn     Scenario 8: Night headlamp + eye glow + silhouettes, sun-shadow drop at sunlit=0.5, dawn grazing light
  all            Run all 8 scenarios in a single PlayMode batch invocation
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw
except ImportError:
    Image = None
    ImageDraw = None

SCENARIO_FILTERS = {
    "driving": "MoonProject.Rover.PlayModeTests.FeelCaptureRoverTests.Scenario01_Driving",
    "slopes": "MoonProject.Rover.PlayModeTests.FeelCaptureRoverTests.Scenario02_Slopes",
    "leap": "MoonProject.Rover.PlayModeTests.FeelCaptureRoverTests.Scenario03_Leap",
    "collisions": "MoonProject.Rover.PlayModeTests.FeelCaptureRoverTests.Scenario04_Collisions",
    "camera": "MoonProject.Rover.PlayModeTests.FeelCaptureRoverTests.Scenario05_Camera",
    "verbs": "MoonProject.Gameplay.PlayModeTests.FeelCaptureGameplayTests.Scenario06_Verbs",
    "first_minutes": "MoonProject.Gameplay.PlayModeTests.FeelCaptureGameplayTests.Scenario07_FirstMinutes",
    "night_dawn": "MoonProject.Rover.PlayModeTests.FeelCaptureRoverTests.Scenario08_NightDawn",
}

ALL_SCENARIOS = list(SCENARIO_FILTERS.keys())


def say(msg: str) -> None:
    print(f"[feel_capture] {msg}", flush=True)


def build_labeled_contact_sheet(scenario_dir: Path, metrics: dict) -> None:
    """Renders an annotated contact sheet strip.png with frame number, timestamp, speed, and label."""
    if Image is None or ImageDraw is None:
        return

    captures = metrics.get("captures", [])
    if not captures:
        return

    thumb_w = 320
    thumb_h = 180
    banner_h = 24
    cell_w = thumb_w
    cell_h = thumb_h + banner_h
    pad = 8
    cols = min(4, len(captures))
    rows = (len(captures) + cols - 1) // cols

    sheet_w = cols * cell_w + (cols + 1) * pad
    sheet_h = rows * cell_h + (rows + 1) * pad
    sheet = Image.new("RGB", (sheet_w, sheet_h), (16, 18, 24))
    draw = ImageDraw.Draw(sheet)

    for idx, cap in enumerate(captures):
        file_name = cap.get("file", "")
        frame_path = scenario_dir / file_name
        if not frame_path.exists():
            continue

        col = idx % cols
        row = idx // cols
        x0 = pad + col * (cell_w + pad)
        y0 = pad + row * (cell_h + pad)

        try:
            with Image.open(frame_path) as img:
                thumb = img.convert("RGB").resize((thumb_w, thumb_h), Image.Resampling.BILINEAR)
                sheet.paste(thumb, (x0, y0))
        except Exception as exc:
            say(f"warning: could not read {frame_path}: {exc}")
            continue

        # Draw label banner beneath thumbnail
        draw.rectangle([x0, y0 + thumb_h, x0 + thumb_w, y0 + cell_h], fill=(26, 30, 40))
        frame_num = cap.get("index", idx + 1)
        t_sec = cap.get("time", cap.get("timeSeconds", 0.0))
        speed = cap.get("speed", 0.0)
        label = cap.get("label", "")
        caption = f"#{frame_num:02d}  t={t_sec:5.1f}s  v={speed:4.1f}m/s  {label}"
        draw.text((x0 + 6, y0 + thumb_h + 5), caption[:48], fill=(225, 232, 245))

    strip_path = scenario_dir / "strip.png"
    sheet.save(strip_path, format="PNG", optimize=True)


def try_encode_mp4(scenario_dir: Path, scenario: str) -> None:
    """Optionally encodes <scenario>.mp4 from captured frames if ffmpeg is on PATH."""
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        return

    frames = sorted(scenario_dir.glob("frame_*.png"))
    if len(frames) < 2:
        return

    concat_file = scenario_dir / "_ffmpeg_frames.txt"
    try:
        with open(concat_file, "w", encoding="utf-8") as f:
            for frame in frames:
                f.write(f"file '{frame.name}'\n")
                f.write("duration 0.5\n")
            f.write(f"file '{frames[-1].name}'\n")

        mp4_path = scenario_dir / f"{scenario}.mp4"
        subprocess.run(
            [
                ffmpeg,
                "-y",
                "-f",
                "concat",
                "-safe",
                "0",
                "-i",
                str(concat_file.name),
                "-vf",
                "fps=10,format=yuv420p",
                str(mp4_path.name),
            ],
            cwd=str(scenario_dir),
            capture_output=True,
            check=False,
        )
    finally:
        if concat_file.exists():
            concat_file.unlink()


def main() -> int:
    parser = argparse.ArgumentParser(description="Run PlayMode feel-capture scenarios and output frames + metrics.")
    parser.add_argument(
        "--scenario",
        default="all",
        choices=["all", *ALL_SCENARIOS],
        help="Scenario name to capture, or 'all' (default: all).",
    )
    parser.add_argument(
        "--out",
        default="Logs/audit",
        help="Output folder for scenario captures and metrics.json (default: Logs/audit).",
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=1800,
        help="Timeout in seconds for unity_batch.py (default: 1800).",
    )
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent.parent
    out_dir = Path(args.out)
    if not out_dir.is_absolute():
        out_dir = (repo_root / out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    if args.scenario == "all":
        scenarios = ALL_SCENARIOS
        test_filter = "FeelCaptureRoverTests|FeelCaptureGameplayTests"
    else:
        scenarios = [args.scenario]
        test_filter = SCENARIO_FILTERS[args.scenario]

    say(f"scenario={args.scenario} -> out={out_dir}")
    env = os.environ.copy()
    env["MOON_FEEL_OUT"] = str(out_dir)

    cmd = [
        sys.executable,
        str(repo_root / "tools" / "unity_batch.py"),
        "tests",
        "--platform",
        "playmode",
        "--filter",
        test_filter,
        "--timeout",
        str(args.timeout),
    ]
    say("running: " + " ".join(cmd))
    proc = subprocess.run(cmd, cwd=str(repo_root), env=env)
    if proc.returncode != 0:
        say(f"ERROR: unity_batch.py exited with code {proc.returncode}")
        print("RESULT: FAIL (PlayMode feel capture batch failed)", flush=True)
        return proc.returncode

    missing = []
    for sc in scenarios:
        sc_dir = out_dir / sc
        metrics_path = sc_dir / "metrics.json"
        if not metrics_path.exists():
            missing.append(sc)
            continue

        with open(metrics_path, "r", encoding="utf-8") as f:
            metrics = json.load(f)

        build_labeled_contact_sheet(sc_dir, metrics)
        try_encode_mp4(sc_dir, sc)
        captures = metrics.get("captures", [])
        fps_mean = metrics.get("fps", {}).get("mean", 0.0)
        gc_mean = metrics.get("gcAllocationBytesPerFrame", {}).get("mean", 0.0)
        say(
            f"  [OK] {sc:<14} frames={len(captures):<3} strip={(sc_dir / 'strip.png').exists()} "
            f"fps_mean={fps_mean:.1f} gc_mean={gc_mean:.1f}B"
        )

    if missing:
        say(f"ERROR: missing metrics.json for scenarios: {', '.join(missing)}")
        print(f"RESULT: FAIL (missing scenarios: {', '.join(missing)})", flush=True)
        return 1

    print(f"RESULT: PASS ({len(scenarios)} scenario(s) captured to {out_dir})", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
