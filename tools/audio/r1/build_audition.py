"""Build the R1 Audio Audition Set (`Logs/audition/`).

Synthesizes Variant A (closer to C2 "Radio bench") and Variant B (bolder
electro-mechanical / acoustic idea) for all 37 R1 cues, normalizes each to its
target loudness in `tools/audio/r1/catalog.py`, verifies all quality /
pentatonic / loop-seam / loudness gates via `tools/audio/analyze_audio.py`, and
writes:
- `Logs/audition/A/<cue_id>.wav` (37 Variant A WAVs)
- `Logs/audition/B/<cue_id>.wav` (37 Variant B WAVs)
- `Logs/audition/<cue_id>.wav` (37 recommended pick WAVs at root)
- `Logs/audition/index.html` (grouped A/B audition page with Vietnamese descriptions & picks)
- `Logs/audition/loudness.md` (loudness & acoustic verification table)
- `Logs/audition/manifest.json` (machine-readable metrics & metadata)

Usage:
    python -m tools.audio.r1.build_audition
"""

from __future__ import annotations

import argparse
from dataclasses import asdict
import html
import json
import math
from pathlib import Path
import shutil
import sys
import time
from typing import Any

import numpy as np

_REPO_ROOT = Path(__file__).resolve().parents[3]
_AUDIO_ROOT = _REPO_ROOT / "tools" / "audio"
for _p in (str(_REPO_ROOT), str(_AUDIO_ROOT)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from tools.audio.analyze_audio import MOMENTARY_WINDOW_S, analyze_file, evaluate
from tools.audio.r1.catalog import (
    CATALOG,
    GROUPS,
    GUIDE_MAX_EFFECTIVE_DB,
    GUIDE_MAX_TARGET_LUFS,
    CueSpec,
)
from synth import analysis, core, effects, envelope
from synth import io as synth_io
from synth.analysis import loudness_integrated, loudness_momentary_max, rms_db


REPO_ROOT = _REPO_ROOT
DEFAULT_OUT_DIR = REPO_ROOT / "Logs" / "audition"
HEADROOM_DBFS = -1.0


def _remove_dc_keep_edges(x: np.ndarray) -> np.ndarray:
    """Subtract mean weighted by a Hann window so fade-in/out endpoints stay at 0."""
    n = x.shape[0]
    if n < 8:
        return x
    w = np.hanning(n)
    w_mean = float(np.mean(w))
    if w_mean <= 1e-12:
        return x
    if x.ndim == 1:
        dc = float(np.mean(x))
        return x - dc * (w / w_mean)
    dc = np.mean(x, axis=0, keepdims=True)
    return x - dc * (w[:, None] / w_mean)


def _measure_cue_lufs(x: np.ndarray, is_loop: bool) -> float:
    if is_loop:
        val = loudness_integrated(x)
        if math.isfinite(val):
            return val
    val = loudness_momentary_max(x, window=MOMENTARY_WINDOW_S)
    if math.isfinite(val):
        return val
    val = loudness_integrated(x)
    if math.isfinite(val):
        return val
    return rms_db(x)


def condition_and_normalize(raw: np.ndarray, spec: CueSpec) -> np.ndarray:
    """Apply DC removal, endpoint conditioning, and exact LUFS normalization."""
    x = np.asarray(raw, dtype=np.float64).copy()

    if spec.loop:
        x = x - np.mean(x, axis=0)
        current = _measure_cue_lufs(x, is_loop=True)
        if math.isfinite(current):
            gain_db = spec.target_lufs - current
            x = x * (10.0 ** (gain_db / 20.0))
        x = x - np.mean(x, axis=0)
    else:
        fade_in_s = min(0.0005, max(0.0001, (x.shape[0] / core.SAMPLE_RATE) * 0.02))
        fade_out_s = min(0.008, max(0.001, (x.shape[0] / core.SAMPLE_RATE) * 0.08))
        x = envelope.fade_in(x, fade_in_s)
        x = envelope.fade_out(x, fade_out_s)
        x = _remove_dc_keep_edges(x)

        current = _measure_cue_lufs(x, is_loop=False)
        if math.isfinite(current):
            gain_db = spec.target_lufs - current
            x = x * (10.0 ** (gain_db / 20.0))

    # Peak safety check against -1.0 dBFS
    peak = float(np.max(np.abs(x))) if x.size else 0.0
    ceiling_lin = 10.0 ** (HEADROOM_DBFS / 20.0)
    if peak > ceiling_lin:
        x = effects.limiter(x, ceiling_db=HEADROOM_DBFS - 0.2, release=0.030)
        if not spec.loop:
            x = envelope.fade_in(x, 0.0005)
            x = envelope.fade_out(x, 0.008)
            x = _remove_dc_keep_edges(x)
            current = _measure_cue_lufs(x, is_loop=False)
            if math.isfinite(current):
                gain_db = spec.target_lufs - current
                if (float(np.max(np.abs(x))) * (10.0 ** (gain_db / 20.0))) <= ceiling_lin:
                    x = x * (10.0 ** (gain_db / 20.0))
        else:
            x = x - np.mean(x, axis=0)

    if not spec.loop:
        x = envelope.fade_in(x, 0.0005)
        x = envelope.fade_out(x, 0.006)
        x = _remove_dc_keep_edges(x)
    else:
        x = x - np.mean(x, axis=0)

    return x


def render_variant(spec: CueSpec, variant: str, out_dir: Path) -> tuple[Path, dict[str, Any], float]:
    """Render either Variant 'A' or 'B' for ``spec`` and return (wav_path, metrics, measured_lufs)."""
    if variant == "A":
        variant_idx = 0
    elif variant == "B":
        variant_idx = 1
    else:
        raise ValueError(f"Unknown variant {variant!r}")

    gen = core.rng("r1", spec.cue_id, variant)
    raw = spec.fn(variant_idx, gen)

    audio = condition_and_normalize(raw, spec)
    wav_path = out_dir / variant / f"{spec.cue_id}.wav"
    synth_io.write_wav(wav_path, audio)
    metrics = analyze_file(wav_path, loop=spec.loop, hf_cutoff=8000.0, tonal=True)
    loudness_mode = "integrated" if spec.loop else (
        "momentary" if math.isfinite(metrics["lufs_momentary_max"]) else "integrated"
    )
    measured_lufs = (
        metrics["lufs_integrated"]
        if loudness_mode == "integrated" and math.isfinite(metrics["lufs_integrated"])
        else (
            metrics["lufs_momentary_max"]
            if math.isfinite(metrics["lufs_momentary_max"])
            else metrics["rms_db"]
        )
    )
    issues = evaluate(
        metrics,
        hf_max_db=-30.0,
        target_lufs=spec.target_lufs if math.isfinite(metrics["lufs_momentary_max"]) or spec.loop else None,
        loudness_mode=loudness_mode if (math.isfinite(metrics["lufs_momentary_max"]) or spec.loop) else None,
    )
    metrics["issues"] = issues
    metrics["ok"] = len(issues) == 0
    metrics["measured_lufs"] = round(float(measured_lufs), 2)
    metrics["pitch_note"] = metrics.get("pitch", "-")
    metrics["duration_s"] = round(float(metrics["duration"]), 3)
    return wav_path, metrics, float(measured_lufs)


def _generate_index_html(
    entries: list[dict[str, Any]],
    out_path: Path,
) -> None:
    """Write `Logs/audition/index.html` grouped by cue with A/B players, Vietnamese descriptions, and picks."""
    by_group: dict[str, list[dict[str, Any]]] = {gid: [] for gid, _, _ in GROUPS}
    for item in entries:
        by_group[item["group_id"]].append(item)

    total_cues = len(entries)
    picks_a = sum(1 for e in entries if e["pick"] == "A")
    picks_b = sum(1 for e in entries if e["pick"] == "B")

    group_nav_html = []
    sections_html = []

    for gid, gtitle, gsub in GROUPS:
        group_items = by_group.get(gid, [])
        group_nav_html.append(
            f'<a class="nav-pill" href="#group-{html.escape(gid)}">'
            f'{html.escape(gtitle)} <span class="badge-count">{len(group_items)}</span></a>'
        )

        cards_html = []
        for item in group_items:
            cid = item["cue_id"]
            pick = item["pick"]
            loop_attr = " loop" if item["loop"] else ""
            loop_badge = (
                '<span class="tag tag-loop">LOOP</span>'
                if item["loop"]
                else '<span class="tag tag-oneshot">ONE-SHOT</span>'
            )
            dur_s = item["A"]["duration_s"]
            target_lufs = item["target_lufs"]
            eff_db = item["effective_db"]
            lufs_a = item["A"]["measured_lufs"]
            lufs_b = item["B"]["measured_lufs"]
            note_a = item["A"]["pitch_note"] or "—"
            note_b = item["B"]["pitch_note"] or "—"

            a_win_cls = " variant-picked" if pick == "A" else ""
            b_win_cls = " variant-picked" if pick == "B" else ""
            a_badge = '<span class="pick-pill">★ Pick A</span>' if pick == "A" else ""
            b_badge = '<span class="pick-pill">★ Pick B</span>' if pick == "B" else ""

            cards_html.append(f"""
        <article class="cue-card" id="cue-{html.escape(cid)}">
          <header class="cue-header">
            <div class="cue-title-wrap">
              <h3 class="cue-id"><code>{html.escape(cid)}</code></h3>
              {loop_badge}
              <span class="tag tag-meta">{dur_s:.2f}s</span>
              <span class="tag tag-meta">Target {target_lufs:.1f} LUFS (Eff {eff_db:.1f} dB)</span>
            </div>
            <div class="cue-pick-header">
              <span class="our-pick-badge">Đề xuất chọn: <strong>Phương án {html.escape(pick)}</strong></span>
            </div>
          </header>

          <div class="variants-grid">
            <div class="variant-box{a_win_cls}">
              <div class="variant-top">
                <span class="variant-label">Variant A — C2 &ldquo;Radio bench&rdquo;</span>
                {a_badge}
              </div>
              <p class="vi-desc">{html.escape(item["desc_a_vi"])}</p>
              <audio controls preload="none"{loop_attr} src="A/{html.escape(cid)}.wav"></audio>
              <div class="variant-metrics">
                <span>LUFS: <strong>{lufs_a:.1f}</strong></span>
                <span>Peak: <strong>{item["A"]["peak_db"]:.1f} dBFS</strong></span>
                <span>Pitch: <strong>{html.escape(str(note_a))}</strong></span>
                <a class="dl-link" href="A/{html.escape(cid)}.wav" download="{html.escape(cid)}_A.wav">WAV A</a>
              </div>
            </div>

            <div class="variant-box{b_win_cls}">
              <div class="variant-top">
                <span class="variant-label">Variant B — Cơ điện / Âm học táo bạo</span>
                {b_badge}
              </div>
              <p class="vi-desc">{html.escape(item["desc_b_vi"])}</p>
              <audio controls preload="none"{loop_attr} src="B/{html.escape(cid)}.wav"></audio>
              <div class="variant-metrics">
                <span>LUFS: <strong>{lufs_b:.1f}</strong></span>
                <span>Peak: <strong>{item["B"]["peak_db"]:.1f} dBFS</strong></span>
                <span>Pitch: <strong>{html.escape(str(note_b))}</strong></span>
                <a class="dl-link" href="B/{html.escape(cid)}.wav" download="{html.escape(cid)}_B.wav">WAV B</a>
              </div>
            </div>
          </div>

          <footer class="pick-reason">
            <strong>Lý do chọn {html.escape(pick)}:</strong> {html.escape(item["pick_reason"])}
          </footer>
        </article>""")

        sections_html.append(f"""
      <section class="cue-group" id="group-{html.escape(gid)}">
        <div class="group-header">
          <h2>{html.escape(gtitle)}</h2>
          <p class="group-sub">{html.escape(gsub)}</p>
        </div>
        <div class="cue-list">
          {"".join(cards_html)}
        </div>
      </section>""")

    page_html = f"""<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>MoonProject R1 Audio Audition — C2 &ldquo;Radio Bench&rdquo;</title>
  <style>
    :root {{
      --bg: #12100e;
      --panel: #1c1814;
      --panel-alt: #241f19;
      --border: #3a3126;
      --amber: #e6a84c;
      --amber-soft: #c98e38;
      --green-crt: #7ad491;
      --text: #efe6d8;
      --muted: #b0a390;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      padding: 0;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      background: var(--bg);
      color: var(--text);
      line-height: 1.5;
    }}
    .container {{
      max-width: 1180px;
      margin: 0 auto;
      padding: 28px 20px 64px;
    }}
    header.page-hero {{
      background: linear-gradient(180deg, #211b15 0%, #17130f 100%);
      border: 1px solid var(--border);
      border-radius: 12px;
      padding: 24px 28px;
      margin-bottom: 24px;
    }}
    header.page-hero h1 {{
      margin: 0 0 8px;
      font-size: 1.65rem;
      color: var(--amber);
    }}
    header.page-hero p {{
      margin: 6px 0;
      color: var(--muted);
    }}
    .summary-stats {{
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      margin-top: 14px;
    }}
    .stat-chip {{
      background: var(--panel-alt);
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 6px 12px;
      font-size: 0.88rem;
    }}
    .stat-chip strong {{
      color: var(--amber);
    }}
    nav.group-nav {{
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 28px;
      position: sticky;
      top: 0;
      z-index: 10;
      background: rgba(18, 16, 14, 0.94);
      backdrop-filter: blur(6px);
      padding: 10px 0;
      border-bottom: 1px solid var(--border);
    }}
    .nav-pill {{
      text-decoration: none;
      color: var(--text);
      background: var(--panel);
      border: 1px solid var(--border);
      padding: 6px 12px;
      border-radius: 999px;
      font-size: 0.85rem;
      transition: border-color 0.15s, color 0.15s;
    }}
    .nav-pill:hover {{
      border-color: var(--amber);
      color: var(--amber);
    }}
    .badge-count {{
      background: var(--border);
      color: var(--amber);
      border-radius: 999px;
      padding: 1px 7px;
      margin-left: 4px;
      font-size: 0.78rem;
    }}
    .cue-group {{
      margin-bottom: 42px;
    }}
    .group-header {{
      border-bottom: 2px solid var(--border);
      margin-bottom: 16px;
      padding-bottom: 6px;
    }}
    .group-header h2 {{
      margin: 0;
      color: var(--amber);
      font-size: 1.35rem;
    }}
    .group-sub {{
      margin: 4px 0 0;
      color: var(--muted);
      font-size: 0.92rem;
    }}
    .cue-list {{
      display: flex;
      flex-direction: column;
      gap: 16px;
    }}
    .cue-card {{
      background: var(--panel);
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 16px 18px;
    }}
    .cue-header {{
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      gap: 10px;
      margin-bottom: 12px;
    }}
    .cue-title-wrap {{
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
    }}
    .cue-id {{
      margin: 0;
      font-size: 1.08rem;
    }}
    .cue-id code {{
      color: var(--amber);
      background: #14110e;
      padding: 3px 8px;
      border-radius: 6px;
      border: 1px solid var(--border);
    }}
    .tag {{
      font-size: 0.75rem;
      padding: 2px 8px;
      border-radius: 4px;
      font-weight: 600;
    }}
    .tag-loop {{
      background: #1b3324;
      color: var(--green-crt);
      border: 1px solid #2d593c;
    }}
    .tag-oneshot {{
      background: #2b2319;
      color: var(--amber);
      border: 1px solid #4a3a26;
    }}
    .tag-meta {{
      background: #181410;
      color: var(--muted);
      border: 1px solid var(--border);
      font-weight: 400;
    }}
    .our-pick-badge {{
      background: #1e2f23;
      color: var(--green-crt);
      border: 1px solid #355c40;
      padding: 4px 10px;
      border-radius: 6px;
      font-size: 0.85rem;
    }}
    .variants-grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 14px;
    }}
    .variant-box {{
      background: var(--panel-alt);
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 12px 14px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }}
    .variant-picked {{
      border-color: var(--green-crt);
      box-shadow: inset 0 0 0 1px rgba(122, 212, 145, 0.25);
    }}
    .variant-top {{
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 8px;
    }}
    .variant-label {{
      font-weight: 600;
      font-size: 0.92rem;
      color: var(--amber-soft);
    }}
    .pick-pill {{
      background: var(--green-crt);
      color: #0f1c13;
      font-weight: 700;
      font-size: 0.74rem;
      padding: 2px 8px;
      border-radius: 999px;
    }}
    .vi-desc {{
      margin: 0;
      font-size: 0.93rem;
      color: var(--text);
    }}
    audio {{
      width: 100%;
      height: 36px;
      margin-top: 2px;
    }}
    .variant-metrics {{
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      gap: 8px;
      font-size: 0.78rem;
      color: var(--muted);
      padding-top: 4px;
      border-top: 1px dashed var(--border);
    }}
    .dl-link {{
      color: var(--amber);
      text-decoration: none;
      font-weight: 600;
    }}
    .dl-link:hover {{
      text-decoration: underline;
    }}
    .pick-reason {{
      margin-top: 12px;
      padding-top: 10px;
      border-top: 1px solid var(--border);
      font-size: 0.88rem;
      color: var(--muted);
    }}
    .pick-reason strong {{
      color: var(--green-crt);
    }}
  </style>
</head>
<body>
  <div class="container">
    <header class="page-hero">
      <h1>MoonProject R1 Audio Audition — Direction C2 &ldquo;Radio Bench&rdquo;</h1>
      <p>
        Bộ mẫu âm thanh R1 tổng hợp bằng Python có hạt giống xác định (deterministic seeded synthesis).
        Màu âm bàn vô tuyến đèn cổ điển (bóng đèn ấm, màng loa giấy, sôi băng từ nhẹ) kết hợp cơ khí vật lý
        (rơ-le, công tắc bakelite, chốt nỉ, khung thép). Toàn bộ phần có cao độ đều thuộc thang <strong>Ngũ cung Rê trưởng (D E F# A B)</strong>.
      </p>
      <div class="summary-stats">
        <span class="stat-chip">Tổng số cue: <strong>{total_cues}</strong> (78 file WAV A/B)</span>
        <span class="stat-chip">Đề xuất chọn A (Radio bench): <strong>{picks_a}</strong></span>
        <span class="stat-chip">Đề xuất chọn B (Cơ điện / Âm học): <strong>{picks_b}</strong></span>
        <span class="stat-chip">Trần độ lớn: <strong>&le; -18.0 LUFS</strong> (Nhóm UI nhỏ nhất: <strong>-28.5 .. -30.5 LUFS</strong>)</span>
      </div>
    </header>

    <nav class="group-nav">
      {"".join(group_nav_html)}
    </nav>

    <main>
      {"".join(sections_html)}
    </main>
  </div>
  <script>
    // Pause other one-shot players when starting a new player so A/B comparison is clean
    document.addEventListener('play', function(e) {{
      const audios = document.getElementsByTagName('audio');
      for (let i = 0; i < audios.length; i++) {{
        if (audios[i] !== e.target) {{
          audios[i].pause();
        }}
      }}
    }}, true);
  </script>
</body>
</html>
"""
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(page_html, encoding="utf-8")


def _generate_loudness_md(entries: list[dict[str, Any]], out_path: Path) -> None:
    """Write `Logs/audition/loudness.md` summarizing loudness & quality verification."""
    lines = [
        "# R1 Audio Audition — Loudness & Quality Report",
        "",
        "Verified against `tools/audio/feel_loudness.md` (`GUIDE_MAX_TARGET_LUFS = -18.0 LUFS`, `GUIDE_MAX_EFFECTIVE_DB = -19.9 dB`).",
        "All CRT UI cues (`ui_*`) are mixed strictly quieter than all non-UI cues.",
        "",
        "| Cue | Group | Loop | Target LUFS | Vol | Effective dB | Meas A (LUFS) | Meas B (LUFS) | Peak A (dB) | Peak B (dB) | Pick |",
        "| --- | --- | :---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | :---: |",
    ]
    for e in entries:
        lines.append(
            f"| `{e['cue_id']}` | `{e['group_id']}` | {'yes' if e['loop'] else 'no'} "
            f"| {e['target_lufs']:.1f} | {e['volume']:.2f} | {e['effective_db']:.1f} "
            f"| {e['A']['measured_lufs']:.1f} | {e['B']['measured_lufs']:.1f} "
            f"| {e['A']['peak_db']:.1f} | {e['B']['peak_db']:.1f} | **{e['pick']}** |"
        )
    lines.append("")
    out_path.write_text("\n".join(lines), encoding="utf-8")


def build_all(out_dir: Path = DEFAULT_OUT_DIR, verbose: bool = True) -> dict[str, Any]:
    """Render all 37 cues (Variants A & B), run quality checks, and write audition outputs."""
    t_start = time.perf_counter()
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "A").mkdir(parents=True, exist_ok=True)
    (out_dir / "B").mkdir(parents=True, exist_ok=True)

    entries: list[dict[str, Any]] = []
    failures: list[str] = []

    for spec in CATALOG:
        wav_a, rep_a, lufs_a = render_variant(spec, "A", out_dir)
        wav_b, rep_b, lufs_b = render_variant(spec, "B", out_dir)

        # Copy the picked variant to root Logs/audition/<cue_id>.wav for convenience
        picked_src = wav_a if spec.pick == "A" else wav_b
        shutil.copyfile(picked_src, out_dir / f"{spec.cue_id}.wav")

        for label, rep, meas_lufs in [("A", rep_a, lufs_a), ("B", rep_b, lufs_b)]:
            if not rep["ok"]:
                failures.append(f"{spec.cue_id} [{label}] analyze_file issues: {rep['issues']}")
            if abs(meas_lufs - spec.target_lufs) > 0.6:
                failures.append(
                    f"{spec.cue_id} [{label}] LUFS drift: measured {meas_lufs:.2f} vs target {spec.target_lufs:.2f}"
                )
            if meas_lufs > GUIDE_MAX_TARGET_LUFS + 0.15:
                failures.append(
                    f"{spec.cue_id} [{label}] exceeds feel_loudness.md ceiling: {meas_lufs:.2f} > {GUIDE_MAX_TARGET_LUFS:.1f}"
                )
            if spec.effective_db > GUIDE_MAX_EFFECTIVE_DB:
                failures.append(
                    f"{spec.cue_id} [{label}] effective_db {spec.effective_db:.2f} > {GUIDE_MAX_EFFECTIVE_DB:.1f}"
                )

        def _clean_dict(d: dict[str, Any]) -> dict[str, Any]:
            cleaned: dict[str, Any] = {}
            for k, v in d.items():
                if isinstance(v, (float, np.floating)):
                    cleaned[k] = round(float(v), 4) if math.isfinite(float(v)) else None
                elif isinstance(v, (bool, np.bool_)):
                    cleaned[k] = bool(v)
                elif isinstance(v, (int, np.integer)):
                    cleaned[k] = int(v)
                else:
                    cleaned[k] = v
            return cleaned

        entry = {
            "cue_id": spec.cue_id,
            "group_id": spec.group_id,
            "group_title": spec.group_title,
            "category": spec.category,
            "loop": spec.loop,
            "target_lufs": spec.target_lufs,
            "volume": spec.volume,
            "effective_db": round(spec.effective_db, 2),
            "desc_a_vi": spec.desc_a_vi,
            "desc_b_vi": spec.desc_b_vi,
            "pick": spec.pick,
            "pick_reason": spec.pick_reason,
            "A": _clean_dict(rep_a),
            "B": _clean_dict(rep_b),
        }
        entries.append(entry)
        if verbose:
            print(
                f"  [{spec.group_id:8s}] {spec.cue_id:28s} "
                f"A={lufs_a:5.1f} LUFS ({rep_a['pitch_note'] or '-':3s}) | "
                f"B={lufs_b:5.1f} LUFS ({rep_b['pitch_note'] or '-':3s}) | "
                f"Pick={spec.pick}"
            )

    # Verify UI cues are strictly the quietest family
    ui_entries = [e for e in entries if e["group_id"] == "ui"]
    non_ui_entries = [e for e in entries if e["group_id"] != "ui"]
    max_ui_lufs = max(max(e["A"]["measured_lufs"], e["B"]["measured_lufs"]) for e in ui_entries)
    min_non_ui_lufs = min(min(e["A"]["measured_lufs"], e["B"]["measured_lufs"]) for e in non_ui_entries)
    max_ui_eff = max(e["effective_db"] for e in ui_entries)
    min_non_ui_eff = min(e["effective_db"] for e in non_ui_entries)

    if max_ui_lufs >= min_non_ui_lufs:
        failures.append(
            f"UI cues are not quietest in LUFS: max UI ({max_ui_lufs:.2f}) >= min non-UI ({min_non_ui_lufs:.2f})"
        )
    if max_ui_eff >= min_non_ui_eff:
        failures.append(
            f"UI cues are not quietest in effective dB: max UI ({max_ui_eff:.2f}) >= min non-UI ({min_non_ui_eff:.2f})"
        )

    _generate_index_html(entries, out_dir / "index.html")
    _generate_loudness_md(entries, out_dir / "loudness.md")

    elapsed_s = time.perf_counter() - t_start
    manifest = {
        "direction": "C2 Radio bench",
        "scale": "D major pentatonic (D E F# A B)",
        "sample_rate": core.SAMPLE_RATE,
        "cue_count": len(entries),
        "variant_wav_count": len(entries) * 2,
        "max_ui_lufs": round(max_ui_lufs, 2),
        "min_non_ui_lufs": round(min_non_ui_lufs, 2),
        "max_ui_effective_db": round(max_ui_eff, 2),
        "min_non_ui_effective_db": round(min_non_ui_eff, 2),
        "elapsed_s": round(elapsed_s, 2),
        "failures": failures,
        "cues": entries,
    }
    (out_dir / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")

    if verbose:
        print(
            f"\nBuilt {len(entries)} cues ({len(entries) * 2} A/B WAVs) in {elapsed_s:.2f}s -> {out_dir}"
        )
        print(
            f"Loudness hierarchy check: max UI = {max_ui_lufs:.2f} LUFS (eff {max_ui_eff:.2f} dB) < "
            f"min non-UI = {min_non_ui_lufs:.2f} LUFS (eff {min_non_ui_eff:.2f} dB)"
        )
        if failures:
            print("FAILURES:")
            for f in failures:
                print(f"  - {f}")
        else:
            print("ALL QUALITY, PENTATONIC, LOOP-SEAM, AND LOUDNESS CHECKS PASSED.")

    return manifest


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR, help="Output directory for audition files")
    ap.add_argument("--quiet", action="store_true", help="Suppress per-cue progress output")
    args = ap.parse_args(argv)

    manifest = build_all(out_dir=args.out_dir, verbose=not args.quiet)
    return 1 if manifest["failures"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
