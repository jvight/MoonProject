#!/usr/bin/env python3
"""
Live status dashboard ("mission control") for the MoonProject agent team.

Scans a worktree root directory (default: D:\\Project\\MoonProject-wt) read-only and writes one
self-contained HTML dashboard to <root>/.board/index.html styled in Variant C ("Terminal hai màu").

Usage:
  python tools/agy_board.py [ROOT] [--watch SECONDS]
"""
import argparse
import ctypes
import datetime
import html
import os
import re
import subprocess
import sys
import time
from dataclasses import dataclass, field
from pathlib import Path

DEFAULT_ROOT = Path(r"D:\Project\MoonProject-wt")
REFRESH_SECONDS = 30
MAX_CAPTURES = 4
MAX_GATE_LINES = 8
MAX_HISTORY_LINES = 30
CLEANUP_AGE_SECONDS = 2 * 86400

HEADING_RE = re.compile(r"^#+\s*(.+)")
ROUND_DIR_RE = re.compile(r"^r\d+$", re.I)
GATE_TOKEN_RE = re.compile(r"RESULT:|(?<!BOARDING )\bPASS\b|\bFAIL\b")
GATE_EXIT_RE = re.compile(r"\bexit(?:ed|s)?(?:\s+with)?(?:\s+code)?\s*[:=]?\s*-?\d+\b|\bexit\s+code\b", re.I)
GATE_FAIL_RE = re.compile(r"\bFAIL\b")
GATE_FAIL_EXIT_RE = re.compile(r"\bexit(?:\s+code)?\s+[1-9]\d*\b", re.I)
TIME_HMS_RE = re.compile(r"^(\d{1,2}):(\d{2}):(\d{2})$")


@dataclass(frozen=True)
class CaptureItem:
    path: Path
    rel_path: str
    uri: str
    mtime: float
    stale: bool


@dataclass(frozen=True)
class GateLine:
    text: str
    is_fail: bool


@dataclass(frozen=True)
class Card:
    name: str
    path: Path
    title: str
    branch: str
    state: str
    landed: bool
    start_time: str
    end_time: str
    elapsed_minutes: int | None
    resumes: tuple[str, ...]
    ahead: int
    behind: int
    last_subject: str
    last_sha: str
    gate_lines: tuple[GateLine, ...]
    captures: tuple[CaptureItem, ...]
    stale_capture_count: int
    rounds: tuple[str, ...]

    @property
    def bucket(self) -> str:
        """Sort bucket: running -> done-unlanded -> failed -> landed/idle."""
        if self.state == "running":
            return "running"
        if self.landed or self.state == "idle":
            return "landed_idle"
        if self.state == "done":
            return "done_unlanded"
        return "failed"

    @property
    def sort_key(self) -> tuple[int, int, str]:
        order = {"running": 0, "done_unlanded": 1, "failed": 2, "landed_idle": 3}[self.bucket]
        sub_order = 0 if not self.landed else 1
        return (order, sub_order, self.name.lower())


@dataclass(frozen=True)
class BoardData:
    root: Path
    generated_at: datetime.datetime
    cards: tuple[Card, ...]
    total_worktrees: int
    cleanup_candidates: tuple[tuple[str, str, int], ...]
    history_lines: tuple[str, ...]


def running_agy_worktrees() -> set[Path]:
    """Returns normalized Paths of working directories for any running agy.exe processes on Windows."""
    if sys.platform != "win32" or ctypes.sizeof(ctypes.c_void_p) != 8:
        return set()
    try:
        from ctypes import wintypes

        k32 = ctypes.WinDLL("kernel32", use_last_error=True)
        ntdll = ctypes.WinDLL("ntdll")

        class ProcessEntry32W(ctypes.Structure):
            _fields_ = [
                ("dwSize", wintypes.DWORD),
                ("cntUsage", wintypes.DWORD),
                ("th32ProcessID", wintypes.DWORD),
                ("th32DefaultHeapID", ctypes.c_size_t),
                ("th32ModuleID", wintypes.DWORD),
                ("cntThreads", wintypes.DWORD),
                ("th32ParentProcessID", wintypes.DWORD),
                ("pcPriClassBase", wintypes.LONG),
                ("dwFlags", wintypes.DWORD),
                ("szExeFile", ctypes.c_wchar * 260),
            ]

        class ProcessBasicInfo(ctypes.Structure):
            _fields_ = [
                ("Reserved1", ctypes.c_void_p),
                ("PebBaseAddress", ctypes.c_void_p),
                ("Reserved2", ctypes.c_void_p * 2),
                ("UniqueProcessId", ctypes.c_void_p),
                ("Reserved3", ctypes.c_void_p),
            ]

        class UnicodeString(ctypes.Structure):
            _fields_ = [
                ("Length", wintypes.USHORT),
                ("MaximumLength", wintypes.USHORT),
                ("Buffer", ctypes.c_void_p),
            ]

        snap = k32.CreateToolhelp32Snapshot(2, 0)
        if snap == wintypes.HANDLE(-1).value or not snap:
            return set()
        cwds: set[Path] = set()
        try:
            pe = ProcessEntry32W(dwSize=ctypes.sizeof(ProcessEntry32W))
            if k32.Process32FirstW(snap, ctypes.byref(pe)):
                while True:
                    if pe.szExeFile.lower() == "agy.exe":
                        h = k32.OpenProcess(0x0410, False, pe.th32ProcessID)
                        if h:
                            try:
                                pbi = ProcessBasicInfo()
                                status = ntdll.NtQueryInformationProcess(
                                    h, 0, ctypes.byref(pbi), ctypes.sizeof(pbi), None
                                )
                                if status == 0 and pbi.PebBaseAddress:
                                    pp = ctypes.c_void_p()
                                    if (
                                        k32.ReadProcessMemory(
                                            h, ctypes.c_void_p(pbi.PebBaseAddress + 0x20), ctypes.byref(pp), 8, None
                                        )
                                        and pp.value
                                    ):
                                        us = UnicodeString()
                                        if (
                                            k32.ReadProcessMemory(
                                                h,
                                                ctypes.c_void_p(pp.value + 0x38),
                                                ctypes.byref(us),
                                                ctypes.sizeof(us),
                                                None,
                                            )
                                            and us.Length
                                            and us.Buffer
                                        ):
                                            buf = ctypes.create_unicode_buffer(us.Length // 2)
                                            if k32.ReadProcessMemory(
                                                h, ctypes.c_void_p(us.Buffer), buf, us.Length, None
                                            ):
                                                raw = buf.value.rstrip("\\/")
                                                if raw:
                                                    cwds.add(Path(raw).resolve())
                            finally:
                                k32.CloseHandle(h)
                    if not k32.Process32NextW(snap, ctypes.byref(pe)):
                        break
        finally:
            k32.CloseHandle(snap)
        return cwds
    except Exception:
        return set()


def run_git(cwd: Path, *args: str) -> str:
    res = subprocess.run(
        ["git", "-c", "safe.directory=*", *args],
        cwd=cwd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    return res.stdout.strip() if res.returncode == 0 else ""


def resolve_common_git_dir(wt: Path) -> Path | None:
    dot_git = wt / ".git"
    if not dot_git.exists():
        return None
    if dot_git.is_dir():
        return dot_git.resolve()
    try:
        text = dot_git.read_text(encoding="utf-8", errors="replace").strip()
    except OSError:
        return None
    if not text.startswith("gitdir:"):
        return None
    gitdir_raw = text[len("gitdir:"):].strip()
    gitdir = Path(gitdir_raw)
    if not gitdir.is_absolute():
        gitdir = (wt / gitdir).resolve()
    else:
        gitdir = gitdir.resolve()
    commondir_file = gitdir / "commondir"
    if commondir_file.is_file():
        try:
            rel = commondir_file.read_text(encoding="utf-8", errors="replace").strip()
            return (gitdir / rel).resolve()
        except OSError:
            pass
    if gitdir.parent.name == "worktrees":
        return gitdir.parent.parent.resolve()
    return gitdir


@dataclass
class GitBranchInfo:
    branch: str = ""
    head_sha: str = ""
    ahead: int = 0
    behind: int = 0
    is_ancestor_of_main: bool = False
    on_main_first_parent: bool = False
    is_main_tip: bool = False
    main_has_parents: bool = False
    commit_ts: int = 0
    subject: str = ""


def collect_git_info(worktrees: list[Path]) -> dict[Path, GitBranchInfo]:
    """Collects branch, ahead/behind, merge status, and last commit info for all git worktrees in batch."""
    by_common: dict[Path, list[Path]] = {}
    for wt in worktrees:
        common = resolve_common_git_dir(wt)
        if common is not None:
            by_common.setdefault(common, []).append(wt)

    result: dict[Path, GitBranchInfo] = {}
    for common, group in by_common.items():
        rep_wt = group[0]
        first_parents_out = run_git(rep_wt, "rev-list", "--first-parent", "main")
        first_parents = first_parents_out.splitlines() if first_parents_out else []
        first_parent_set = set(first_parents)
        main_tip = first_parents[0] if first_parents else ""
        main_has_parents = len(first_parents) > 1

        wt_heads: dict[Path, tuple[str, str]] = {}
        cur_wt: Path | None = None
        cur_head = ""
        cur_branch = ""
        for line in run_git(rep_wt, "worktree", "list", "--porcelain").splitlines():
            if line.startswith("worktree "):
                if cur_wt is not None:
                    wt_heads[cur_wt] = (cur_head, cur_branch)
                cur_wt = Path(line[len("worktree "):]).resolve()
                cur_head = ""
                cur_branch = ""
            elif line.startswith("HEAD "):
                cur_head = line[len("HEAD "):].strip()
            elif line.startswith("branch refs/heads/"):
                cur_branch = line[len("branch refs/heads/"):].strip()
        if cur_wt is not None:
            wt_heads[cur_wt] = (cur_head, cur_branch)

        refs_info: dict[str, tuple[str, int, int, int, str]] = {}
        ref_fmt = "%(refname:short)%09%(objectname)%09%(ahead-behind:main)%09%(committerdate:unix)%09%(subject)"
        for line in run_git(rep_wt, "for-each-ref", f"--format={ref_fmt}", "refs/heads/").splitlines():
            parts = line.split("\t", 4)
            if len(parts) < 5:
                continue
            ref_name, obj_sha, ab_str, ts_str, subj = parts
            ab_parts = ab_str.split()
            ahead = int(ab_parts[0]) if len(ab_parts) == 2 and ab_parts[0].isdigit() else 0
            behind = int(ab_parts[1]) if len(ab_parts) == 2 and ab_parts[1].isdigit() else 0
            ts = int(ts_str) if ts_str.isdigit() else 0
            refs_info[ref_name] = (obj_sha, ahead, behind, ts, subj)

        for wt in group:
            wt_resolved = wt.resolve()
            head_sha, branch = wt_heads.get(wt_resolved, ("", ""))
            if not branch and not head_sha:
                branch = run_git(wt, "rev-parse", "--abbrev-ref", "HEAD")
                head_sha = run_git(wt, "rev-parse", "HEAD")
            if branch in refs_info:
                obj_sha, ahead, behind, ts, subj = refs_info[branch]
                head_sha = head_sha or obj_sha
                is_anc = bool(main_tip) and ahead == 0
            else:
                ab_out = run_git(wt, "rev-list", "--left-right", "--count", "main...HEAD")
                ab_parts = ab_out.split()
                behind = int(ab_parts[0]) if len(ab_parts) == 2 and ab_parts[0].isdigit() else 0
                ahead = int(ab_parts[1]) if len(ab_parts) == 2 and ab_parts[1].isdigit() else 0
                log_out = run_git(wt, "log", "-1", "--format=%ct%x09%s")
                ts_part, _, subj = log_out.partition("\t")
                ts = int(ts_part) if ts_part.isdigit() else 0
                is_anc = bool(main_tip) and ahead == 0

            result[wt] = GitBranchInfo(
                branch=branch or "(detached)",
                head_sha=head_sha,
                ahead=ahead,
                behind=behind,
                is_ancestor_of_main=is_anc,
                on_main_first_parent=head_sha in first_parent_set,
                is_main_tip=bool(main_tip and head_sha == main_tip),
                main_has_parents=main_has_parents,
                commit_ts=ts,
                subject=subj,
            )
    return result


def is_branch_merged(git_info: GitBranchInfo, state: str = "") -> bool:
    """True when the worktree's branch has already landed into main."""
    branch = git_info.branch
    if not branch or branch in ("main", "HEAD", "(detached)") or branch.startswith("director/"):
        return False
    if not git_info.is_ancestor_of_main or git_info.ahead > 0:
        return False
    if state == "running":
        return False
    if not git_info.on_main_first_parent:
        return True
    if git_info.is_main_tip and git_info.main_has_parents and state == "done":
        return True
    return False


def parse_hms_timestamp(hms: str, anchor_ts: float) -> float | None:
    match = TIME_HMS_RE.match(hms.strip())
    if not match:
        return None
    hour, minute, second = (int(g) for g in match.groups())
    if not (0 <= hour < 24 and 0 <= minute < 60 and 0 <= second < 60):
        return None
    anchor_dt = datetime.datetime.fromtimestamp(anchor_ts)
    candidate = anchor_dt.replace(hour=hour, minute=minute, second=second, microsecond=0)
    if candidate > anchor_dt + datetime.timedelta(minutes=2):
        candidate -= datetime.timedelta(days=1)
    return candidate.timestamp()


def parse_agy_time(logs_dir: Path) -> tuple[str, str, str, int | None, list[str], bool, float | None]:
    """Parses Logs/agy-time.txt.
    Returns (start_str, status_str, end_str, minutes, resumes, has_exit, start_epoch).
    """
    time_file = logs_dir / "agy-time.txt"
    if not time_file.is_file():
        return "", "", "", None, [], False, None
    try:
        stat = time_file.stat()
        lines = time_file.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return "", "", "", None, [], False, None

    start_str = ""
    status_str = ""
    end_str = ""
    minutes_val: int | None = None
    resumes: list[str] = []
    has_exit = False

    for raw in lines:
        line = raw.strip()
        if not line:
            continue
        if line.startswith("start "):
            start_str = line[len("start "):].strip()
        elif line.startswith("status "):
            status_str = line[len("status "):].strip()
        elif line.startswith("end "):
            end_str = line[len("end "):].strip()
        elif line.startswith("minutes "):
            val = line[len("minutes "):].strip()
            if val.lstrip("-").isdigit():
                minutes_val = max(0, int(val))
        elif line.startswith("rate-limited:") or "resume #" in line:
            resumes.append(line)
        elif line.startswith("EXIT "):
            has_exit = True

    start_epoch = parse_hms_timestamp(start_str, stat.st_mtime) if start_str else stat.st_mtime
    if start_epoch is None:
        start_epoch = stat.st_mtime
    return start_str, status_str, end_str, minutes_val, resumes, has_exit, start_epoch


def classify_state(
    logs_dir: Path,
    start_str: str,
    status_str: str,
    end_str: str,
    has_exit: bool,
    agy_process_running: bool = False,
) -> str:
    """Derives card state from Logs/ files and optional active agy.exe working directory."""
    done_file = logs_dir / "DONE"
    report_file = logs_dir / "REPORT.md"
    time_file = logs_dir / "agy-time.txt"
    has_done_and_report = done_file.is_file() and report_file.is_file()

    if status_str in ("timeout-killed", "exited-without-DONE"):
        if has_done_and_report:
            try:
                if done_file.stat().st_mtime >= time_file.stat().st_mtime - 1.0:
                    return "done"
            except OSError:
                pass
        return status_str

    if status_str == "timeout":
        return "timeout-killed"

    if has_done_and_report:
        if start_str and not status_str and not end_str and time_file.is_file():
            try:
                if done_file.stat().st_mtime < time_file.stat().st_mtime - 1.0:
                    return "running"
            except OSError:
                pass
        return "done"

    if (start_str and not status_str and not end_str) or agy_process_running:
        return "running"

    if status_str == "done":
        return "done" if has_done_and_report else "exited-without-DONE"

    if end_str or has_exit:
        return "exited-without-DONE"

    return "idle"


def parse_task_title(task_file: Path) -> str:
    try:
        for line in task_file.read_text(encoding="utf-8", errors="replace").splitlines():
            match = HEADING_RE.match(line.strip())
            if match:
                return match.group(1).strip()
    except OSError:
        pass
    return task_file.parent.parent.name


def parse_gate_lines(report_file: Path) -> list[GateLine]:
    if not report_file.is_file():
        return []
    try:
        lines = report_file.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return []

    picked: list[GateLine] = []
    seen: set[str] = set()
    for raw in lines:
        stripped = raw.strip()
        if not stripped or stripped.startswith("#"):
            continue
        if GATE_TOKEN_RE.search(stripped) or GATE_EXIT_RE.search(stripped):
            if stripped in seen:
                continue
            seen.add(stripped)
            is_fail = bool(GATE_FAIL_RE.search(stripped) or GATE_FAIL_EXIT_RE.search(stripped))
            picked.append(GateLine(text=stripped, is_fail=is_fail))

    result_lines = [g for g in picked if "RESULT:" in g.text or g.is_fail]
    other_lines = [g for g in picked if g not in result_lines]
    combined = result_lines + other_lines
    return combined[:MAX_GATE_LINES]


def find_rounds(logs_dir: Path) -> list[str]:
    if not logs_dir.is_dir():
        return []
    rounds = []
    try:
        for child in logs_dir.iterdir():
            if child.is_dir() and ROUND_DIR_RE.match(child.name):
                rounds.append(child.name)
    except OSError:
        return []
    return sorted(rounds, key=lambda name: int(name[1:]))


def find_captures(logs_dir: Path, job_start_ts: float | None) -> tuple[list[CaptureItem], int]:
    """Finds up to MAX_CAPTURES newest PNGs under Logs/ (fresh first, then stale if < MAX_CAPTURES fresh)."""
    if not logs_dir.is_dir():
        return [], 0
    threshold = (job_start_ts - 1.0) if job_start_ts is not None else 0.0
    fresh: list[CaptureItem] = []
    stale: list[CaptureItem] = []
    try:
        for png in logs_dir.rglob("*.png"):
            if not png.is_file():
                continue
            try:
                mtime = png.stat().st_mtime
            except OSError:
                continue
            rel = png.relative_to(logs_dir).as_posix()
            uri = png.resolve().as_uri()
            is_stale = job_start_ts is not None and mtime < threshold
            item = CaptureItem(path=png, rel_path=rel, uri=uri, mtime=mtime, stale=is_stale)
            if is_stale:
                stale.append(item)
            else:
                fresh.append(item)
    except OSError:
        return [], 0

    fresh.sort(key=lambda c: (c.mtime, c.rel_path), reverse=True)
    stale.sort(key=lambda c: (c.mtime, c.rel_path), reverse=True)
    selected = (fresh + stale)[:MAX_CAPTURES]
    return selected, len(stale)


def compute_elapsed_minutes(
    state: str,
    minutes_val: int | None,
    start_epoch: float | None,
    end_str: str,
    logs_dir: Path,
    now_ts: float,
) -> int | None:
    if minutes_val is not None:
        return minutes_val
    if start_epoch is None:
        return None
    if state == "running":
        return max(0, int((now_ts - start_epoch) // 60))
    if end_str:
        end_epoch = parse_hms_timestamp(end_str, now_ts)
        if end_epoch is not None:
            if end_epoch < start_epoch:
                end_epoch += 86400
            return max(0, int((end_epoch - start_epoch) // 60))
    done_file = logs_dir / "DONE"
    if done_file.is_file():
        try:
            return max(0, int((done_file.stat().st_mtime - start_epoch) // 60))
        except OSError:
            pass
    return None


def scan_board(root: Path, now: datetime.datetime | None = None, check_processes: bool = True) -> BoardData:
    root = Path(root).resolve()
    now_dt = now or datetime.datetime.now()
    now_ts = now_dt.timestamp()

    subdirs = sorted([p for p in root.iterdir() if p.is_dir() and not p.name.startswith(".")], key=lambda p: p.name.lower())
    git_worktrees = [p for p in subdirs if (p / ".git").exists()]
    task_worktrees = [p for p in subdirs if (p / "Logs" / "TASK.md").is_file()]

    all_git_targets = list(dict.fromkeys(git_worktrees + [p for p in task_worktrees if (p / ".git").exists()]))
    git_map = collect_git_info(all_git_targets)
    active_procs = running_agy_worktrees() if check_processes else set()

    cards: list[Card] = []
    for wt in task_worktrees:
        logs_dir = wt / "Logs"
        task_file = logs_dir / "TASK.md"
        title = parse_task_title(task_file)
        start_str, status_str, end_str, minutes_val, resumes, has_exit, start_epoch = parse_agy_time(logs_dir)
        if start_epoch is None:
            try:
                start_epoch = task_file.stat().st_mtime
            except OSError:
                start_epoch = None

        proc_running = wt.resolve() in active_procs
        state = classify_state(logs_dir, start_str, status_str, end_str, has_exit, proc_running)
        git_info = git_map.get(wt, GitBranchInfo())
        landed = is_branch_merged(git_info, state)
        elapsed = compute_elapsed_minutes(state, minutes_val, start_epoch, end_str, logs_dir, now_ts)
        gate_lines = parse_gate_lines(logs_dir / "REPORT.md")
        captures, stale_count = find_captures(logs_dir, start_epoch)
        rounds = find_rounds(logs_dir)

        cards.append(
            Card(
                name=wt.name,
                path=wt,
                title=title,
                branch=git_info.branch or "—",
                state=state,
                landed=landed,
                start_time=start_str,
                end_time=end_str,
                elapsed_minutes=elapsed,
                resumes=tuple(resumes),
                ahead=git_info.ahead,
                behind=git_info.behind,
                last_subject=git_info.subject,
                last_sha=git_info.head_sha[:8] if git_info.head_sha else "",
                gate_lines=tuple(gate_lines),
                captures=tuple(captures),
                stale_capture_count=stale_count,
                rounds=tuple(rounds),
            )
        )

    cards.sort(key=lambda c: c.sort_key)

    card_by_path = {c.path: c for c in cards}
    cleanup_candidates: list[tuple[str, str, int]] = []
    for wt in git_worktrees:
        git_info = git_map.get(wt, GitBranchInfo())
        card = card_by_path.get(wt)
        state = card.state if card else ""
        if not is_branch_merged(git_info, state):
            continue
        try:
            wt_mtime = wt.stat().st_mtime
        except OSError:
            wt_mtime = now_ts
        age_anchor = git_info.commit_ts if git_info.commit_ts > 0 else wt_mtime
        age_Anchor = min(age_anchor, wt_mtime) if git_info.commit_ts > 0 else wt_mtime
        age_seconds = now_ts - age_Anchor
        if age_seconds >= CLEANUP_AGE_SECONDS:
            age_days = int(age_seconds // 86400)
            cleanup_candidates.append((wt.name, git_info.branch, age_days))

    jobs_file = root / ".board" / "agy-jobs.md"
    history_lines: list[str] = []
    if jobs_file.is_file():
        try:
            raw_lines = [ln.rstrip() for ln in jobs_file.read_text(encoding="utf-8", errors="replace").splitlines() if ln.strip()]
            history_lines = raw_lines[-MAX_HISTORY_LINES:]
        except OSError:
            history_lines = []

    return BoardData(
        root=root,
        generated_at=now_dt,
        cards=tuple(cards),
        total_worktrees=len(git_worktrees),
        cleanup_candidates=tuple(cleanup_candidates),
        history_lines=tuple(history_lines),
    )


def format_gate_html(gate: GateLine) -> str:
    escaped = html.escape(gate.text)
    escaped = re.sub(r"\bFAIL\b", '<span class="token-fail">FAIL</span>', escaped)
    escaped = re.sub(r"\bPASS\b", '<span class="token-pass">PASS</span>', escaped)
    cls = "gate-line is-fail" if gate.is_fail else "gate-line"
    return f'<li class="{cls}">{escaped}</li>'


def render_card(card: Card) -> str:
    classes = ["card", f"state-{card.state}"]
    if card.landed:
        classes.append("landed")

    badges = [f'<span class="badge badge-state state-{html.escape(card.state)}">{html.escape(card.state)}</span>']
    if card.landed:
        badges.append('<span class="badge badge-landed">landed</span>')
    if card.rounds:
        rounds_label = f"{len(card.rounds)} round{'s' if len(card.rounds) != 1 else ''} ({', '.join(card.rounds)})"
        badges.append(f'<span class="badge badge-rounds">{html.escape(rounds_label)}</span>')

    start_text = f"start {card.start_time}" if card.start_time else "start —"
    if card.end_time:
        start_text += f" → {card.end_time}"
    elapsed_text = f"{card.elapsed_minutes} min" if card.elapsed_minutes is not None else "— min"
    resume_count = len(card.resumes)
    resume_summary = f"{resume_count} resume{'s' if resume_count != 1 else ''}"

    resume_items = ""
    if card.resumes:
        items = "".join(f"<li>{html.escape(r)}</li>" for r in card.resumes)
        resume_items = f'<ul class="resume-list">{items}</ul>'

    commit_badge = (
        '<span class="commit-merged">merged into main (landed)</span>'
        if card.landed
        else f'<span class="commit-ahead">{card.ahead} ahead of main</span>'
    )
    sha_prefix = f'<span class="sha">{html.escape(card.last_sha)}</span> ' if card.last_sha else ""
    subject_html = f"{sha_prefix}{html.escape(card.last_subject)}" if card.last_subject else "—"

    if card.gate_lines:
        gate_items = "".join(format_gate_html(g) for g in card.gate_lines)
        gate_html = f'<div class="section"><span class="sec-label">GATE</span><ul class="gate-list">{gate_items}</ul></div>'
    else:
        gate_html = '<div class="section"><span class="sec-label">GATE</span><span class="muted">no gate lines in REPORT.md</span></div>'

    if card.captures:
        thumbs = []
        for cap in card.captures:
            stale_cls = " is-stale" if cap.stale else ""
            stale_tag = '<span class="stale-flag">stale</span>' if cap.stale else ""
            thumbs.append(
                f'<a class="thumb{stale_cls}" href="{html.escape(cap.uri)}" target="_blank" '
                f'title="{html.escape(cap.rel_path)}">'
                f'<img src="{html.escape(cap.uri)}" alt="{html.escape(cap.rel_path)}" loading="lazy">'
                f'<span class="thumb-meta">{stale_tag}<span class="thumb-name">{html.escape(Path(cap.rel_path).name)}</span></span>'
                f"</a>"
            )
        captures_html = (
            f'<div class="section"><span class="sec-label">CAPTURES ({len(card.captures)})</span>'
            f'<div class="thumbs">{"".join(thumbs)}</div></div>'
        )
    else:
        captures_html = '<div class="section"><span class="sec-label">CAPTURES</span><span class="muted">none</span></div>'

    return f"""
    <article class="{' '.join(classes)}" data-worktree="{html.escape(card.name)}" data-state="{html.escape(card.state)}" data-landed="{str(card.landed).lower()}">
      <header class="card-head">
        <div class="card-id">
          <span class="wt-name">{html.escape(card.name)}</span>
          <span class="branch-name">{html.escape(card.branch)}</span>
        </div>
        <div class="badges">{''.join(badges)}</div>
      </header>
      <h3 class="card-title">{html.escape(card.title)}</h3>
      <div class="telemetry-row">
        <span>{html.escape(start_text)}</span>
        <span class="sep">·</span>
        <span>{html.escape(elapsed_text)}</span>
        <span class="sep">·</span>
        <span>{html.escape(resume_summary)}</span>
      </div>
      {resume_items}
      <div class="commits-row">
        <div class="commits-meta">{commit_badge}</div>
        <div class="commit-subj">{subject_html}</div>
      </div>
      {gate_html}
      {captures_html}
    </article>
    """.strip()


def render_html(board: BoardData) -> str:
    running_cards = [c for c in board.cards if c.bucket == "running"]
    review_cards = [c for c in board.cards if c.bucket == "done_unlanded"]
    failed_cards = [c for c in board.cards if c.bucket == "failed"]
    collapsed_cards = [c for c in board.cards if c.bucket == "landed_idle"]

    landed_count = sum(1 for c in board.cards if c.landed)
    idle_count = sum(1 for c in board.cards if c.state == "idle" and not c.landed)

    if board.cleanup_candidates:
        cand_items = ", ".join(f"{html.escape(name)} ({html.escape(branch)}, {days}d)" for name, branch, days in board.cleanup_candidates)
        cleanup_html = f"{len(board.cleanup_candidates)} merged &gt;2d cleanup candidate(s) (list only): {cand_items}"
    else:
        cleanup_html = "0 merged &gt;2d cleanup candidates"

    if board.history_lines:
        hist_rows = "\n".join(html.escape(line) for line in board.history_lines)
        history_section = f"""
        <section class="panel history-panel">
          <header class="section-head">
            <h2>HISTORY · .board/agy-jobs.md (last {len(board.history_lines)})</h2>
          </header>
          <pre class="history-log">{hist_rows}</pre>
        </section>
        """.strip()
    else:
        history_section = """
        <section class="panel history-panel">
          <header class="section-head">
            <h2>HISTORY · .board/agy-jobs.md</h2>
          </header>
          <p class="muted">No .board/agy-jobs.md file recorded yet.</p>
        </section>
        """.strip()

    def render_group(title: str, subtitle: str, group_cards: list[Card], group_cls: str = "") -> str:
        if not group_cards:
            return ""
        cards_markup = "\n".join(render_card(c) for c in group_cards)
        return f"""
        <section class="card-group {group_cls}">
          <header class="section-head">
            <h2>{html.escape(title)} <span class="count-pill">{len(group_cards)}</span></h2>
            <span class="section-sub">{html.escape(subtitle)}</span>
          </header>
          <div class="grid">
            {cards_markup}
          </div>
        </section>
        """.strip()

    active_sections = "\n".join(
        s
        for s in (
            render_group("RUNNING", "Active worker boxes", running_cards, "group-running"),
            render_group("DONE · UNLANDED", "Director's review queue", review_cards, "group-review"),
            render_group("FAILED", "Timeout or exited without DONE", failed_cards, "group-failed"),
        )
        if s
    )

    collapsed_markup = "\n".join(render_card(c) for c in collapsed_cards)
    collapsed_section = f"""
    <details class="card-group group-collapsed">
      <summary class="section-head">
        <h2>LANDED / IDLE (COLLAPSED) <span class="count-pill">{len(collapsed_cards)}</span></h2>
        <span class="section-sub">{landed_count} landed · {idle_count} idle — click to expand</span>
      </summary>
      <div class="grid">
        {collapsed_markup}
      </div>
    </details>
    """.strip()

    stamp = board.generated_at.strftime("%Y-%m-%d %H:%M:%S")
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta http-equiv="refresh" content="{REFRESH_SECONDS}">
<title>AGY-BOARD · Lunarion Mission Control</title>
<style>
:root {{
  --bg: #0e0b09;
  --panel: #16110d;
  --panel-telemetry: #0c161a;
  --amber: #FFB547;
  --amber-bright: #FFD27A;
  --amber-muted: #B88235;
  --amber-dim: #5c4018;
  --cyan: #5FF3FF;
  --cyan-soft: rgba(95, 243, 255, 0.14);
  --cyan-border: rgba(95, 243, 255, 0.45);
  --fail: #FF5C4D;
  --fail-soft: rgba(255, 92, 77, 0.16);
  --pass: #7CE38B;
  --landed: #7c7469;
  --border: rgba(255, 181, 71, 0.36);
  --border-soft: rgba(255, 181, 71, 0.16);
  --mono: "IBM Plex Mono", Consolas, monospace;
}}
* {{ box-sizing: border-box; }}
body {{
  margin: 0;
  padding: 20px 24px 40px;
  background-color: var(--bg);
  background-image: repeating-linear-gradient(
    0deg,
    rgba(255, 181, 71, 0.018) 0px,
    rgba(255, 181, 71, 0.018) 1px,
    transparent 1px,
    transparent 4px
  );
  color: var(--amber);
  font-family: var(--mono);
  font-size: 13px;
  line-height: 1.45;
}}
.shell {{
  max-width: 1680px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 18px;
}}
.top-bar {{
  background: var(--panel);
  border: 1.5px solid var(--amber);
  box-shadow: inset 0 0 0 1px rgba(255, 181, 71, 0.12);
  border-radius: 5px;
  padding: 14px 18px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}}
.title-row {{
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
}}
h1 {{
  margin: 0;
  font-size: 18px;
  font-weight: 700;
  color: var(--amber-bright);
  letter-spacing: 0.04em;
}}
.stamp {{
  color: var(--cyan);
  font-size: 12px;
}}
.counts-row {{
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}}
.stat-pill {{
  background: var(--panel-telemetry);
  border: 1px solid var(--cyan-border);
  color: var(--cyan);
  padding: 4px 10px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}}
.stat-pill.amber {{
  background: rgba(255, 181, 71, 0.1);
  border-color: var(--border);
  color: var(--amber-bright);
}}
.stat-pill.fail {{
  background: var(--fail-soft);
  border-color: var(--fail);
  color: var(--fail);
}}
.stat-pill.muted-pill {{
  background: rgba(124, 116, 105, 0.12);
  border-color: rgba(124, 116, 105, 0.4);
  color: var(--landed);
}}
.disk-row {{
  padding-top: 8px;
  border-top: 1px solid var(--border-soft);
  color: var(--amber-muted);
  font-size: 12px;
}}
.disk-row strong {{
  color: var(--amber-bright);
}}
.card-group, .panel {{
  background: rgba(22, 17, 13, 0.72);
  border: 1px solid var(--border);
  border-radius: 5px;
  padding: 14px 16px;
}}
.section-head {{
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 12px;
  padding-bottom: 6px;
  border-bottom: 1px solid var(--border-soft);
}}
details.group-collapsed > summary.section-head {{
  cursor: pointer;
  margin-bottom: 0;
  border-bottom: none;
  list-style: none;
}}
details.group-collapsed[open] > summary.section-head {{
  margin-bottom: 12px;
  border-bottom: 1px solid var(--border-soft);
}}
h2 {{
  margin: 0;
  font-size: 14px;
  font-weight: 700;
  color: var(--cyan);
  letter-spacing: 0.05em;
}}
.count-pill {{
  display: inline-block;
  background: var(--cyan-soft);
  border: 1px solid var(--cyan-border);
  color: var(--cyan);
  padding: 1px 7px;
  border-radius: 3px;
  font-size: 12px;
  margin-left: 6px;
}}
.section-sub {{
  color: var(--amber-muted);
  font-size: 12px;
}}
.grid {{
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(390px, 1fr));
  gap: 14px;
}}
.card {{
  background: var(--panel);
  border: 1.5px solid var(--amber);
  box-shadow: inset 0 0 0 1px rgba(255, 181, 71, 0.1);
  border-radius: 5px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}}
.card.state-running {{
  border-color: var(--cyan);
  box-shadow: inset 0 0 0 1px rgba(95, 243, 255, 0.18);
}}
.card.state-timeout-killed, .card.state-exited-without-DONE {{
  border-color: var(--fail);
}}
.card.landed {{
  border-color: rgba(124, 116, 105, 0.45);
  color: var(--landed);
  opacity: 0.68;
}}
.card.landed .card-title,
.card.landed .wt-name,
.card.landed .commit-subj {{
  color: var(--landed);
}}
.card-head {{
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
}}
.card-id {{
  display: flex;
  align-items: baseline;
  gap: 8px;
  min-width: 0;
}}
.wt-name {{
  font-size: 15px;
  font-weight: 700;
  color: var(--amber-bright);
}}
.branch-name {{
  font-size: 12px;
  color: var(--cyan);
}}
.badges {{
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
}}
.badge {{
  font-size: 11px;
  font-weight: 600;
  padding: 2px 7px;
  border-radius: 3px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}}
.badge-state {{
  background: var(--panel-telemetry);
  color: var(--cyan);
  border: 1px solid var(--cyan-border);
}}
.badge-state.state-timeout-killed,
.badge-state.state-exited-without-DONE {{
  background: var(--fail-soft);
  color: var(--fail);
  border-color: var(--fail);
}}
.badge-state.state-idle {{
  background: rgba(124, 116, 105, 0.15);
  color: var(--landed);
  border-color: rgba(124, 116, 105, 0.45);
}}
.badge-landed {{
  background: rgba(124, 116, 105, 0.18);
  color: var(--landed);
  border: 1px solid rgba(124, 116, 105, 0.5);
}}
.badge-rounds {{
  background: rgba(255, 181, 71, 0.1);
  color: var(--amber-bright);
  border: 1px solid var(--border);
  text-transform: none;
}}
.card-title {{
  margin: 0;
  font-size: 13px;
  font-weight: 600;
  color: var(--amber);
  overflow-wrap: anywhere;
}}
.telemetry-row {{
  background: var(--panel-telemetry);
  border: 1px solid rgba(95, 243, 255, 0.25);
  color: var(--cyan);
  padding: 5px 8px;
  border-radius: 4px;
  font-size: 12px;
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}}
.sep {{ opacity: 0.6; }}
.resume-list {{
  margin: 0;
  padding-left: 18px;
  font-size: 11px;
  color: var(--amber-muted);
}}
.commits-row {{
  font-size: 12px;
  border-top: 1px solid var(--border-soft);
  padding-top: 6px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}}
.commit-ahead {{ color: var(--amber-bright); font-weight: 600; }}
.commit-merged {{ color: var(--landed); font-style: italic; }}
.commit-subj {{ color: var(--amber); overflow-wrap: anywhere; }}
.sha {{ color: var(--cyan); }}
.section {{
  border-top: 1px solid var(--border-soft);
  padding-top: 6px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}}
.sec-label {{
  font-size: 11px;
  font-weight: 700;
  color: var(--cyan);
  letter-spacing: 0.05em;
}}
.gate-list {{
  margin: 0;
  padding-left: 16px;
  font-size: 11.5px;
  max-height: 130px;
  overflow-y: auto;
}}
.gate-line {{
  color: var(--amber);
  overflow-wrap: anywhere;
  margin-bottom: 2px;
}}
.gate-line.is-fail, .token-fail {{
  color: var(--fail);
  font-weight: 700;
}}
.token-pass {{
  color: var(--pass);
  font-weight: 600;
}}
.thumbs {{
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 6px;
}}
.thumb {{
  display: flex;
  flex-direction: column;
  border: 1px solid var(--border);
  border-radius: 4px;
  overflow: hidden;
  background: #090706;
  color: var(--amber);
  text-decoration: none;
  font-size: 10px;
}}
.thumb:hover {{
  border-color: var(--cyan);
}}
.thumb.is-stale {{
  border-color: var(--fail);
  opacity: 0.82;
}}
.thumb img {{
  width: 100%;
  height: 64px;
  object-fit: cover;
  display: block;
  background: #050403;
}}
.thumb-meta {{
  padding: 2px 4px;
  display: flex;
  align-items: center;
  gap: 4px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}}
.stale-flag {{
  background: var(--fail);
  color: #0e0b09;
  font-weight: 700;
  padding: 0 4px;
  border-radius: 2px;
  text-transform: uppercase;
  flex-shrink: 0;
}}
.thumb-name {{
  overflow: hidden;
  text-overflow: ellipsis;
}}
.history-log {{
  margin: 0;
  font-family: var(--mono);
  font-size: 12px;
  color: var(--amber);
  background: #0a0806;
  border: 1px solid var(--border-soft);
  border-radius: 4px;
  padding: 10px 12px;
  max-height: 260px;
  overflow-y: auto;
  white-space: pre-wrap;
}}
.muted {{ color: var(--amber-muted); font-size: 12px; margin: 0; }}
</style>
</head>
<body>
<div class="shell">
  <header class="top-bar">
    <div class="title-row">
      <h1>LUNARION · AGY MISSION CONTROL</h1>
      <span class="stamp">Updated {html.escape(stamp)} · auto-refresh {REFRESH_SECONDS}s</span>
    </div>
    <div class="counts-row">
      <span class="stat-pill">running: {len(running_cards)}</span>
      <span class="stat-pill amber">done (unlanded): {len(review_cards)}</span>
      <span class="stat-pill fail">failed: {len(failed_cards)}</span>
      <span class="stat-pill muted-pill">landed: {landed_count}</span>
      <span class="stat-pill muted-pill">idle: {idle_count}</span>
      <span class="stat-pill amber">cards: {len(board.cards)}</span>
    </div>
    <div class="disk-row">
      <strong>DISK:</strong> {board.total_worktrees} worktrees under {html.escape(str(board.root))} · {cleanup_html}
    </div>
  </header>

  {active_sections}
  {collapsed_section}
  {history_section}
</div>
</body>
</html>
"""


def write_board(root: Path, now: datetime.datetime | None = None, check_processes: bool = True) -> tuple[Path, BoardData]:
    board = scan_board(root, now=now, check_processes=check_processes)
    out_dir = Path(root).resolve() / ".board"
    out_dir.mkdir(parents=True, exist_ok=True)
    out_file = out_dir / "index.html"
    tmp_file = out_dir / f".index.{os.getpid()}.tmp"
    html_text = render_html(board)
    tmp_file.write_text(html_text, encoding="utf-8")
    os.replace(tmp_file, out_file)
    return out_file, board


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("root", nargs="?", default=str(DEFAULT_ROOT), help="worktree root folder to scan")
    parser.add_argument("--watch", type=float, default=0.0, metavar="SECONDS", help="regenerate in a loop every N seconds")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    if not root.is_dir():
        sys.exit(f"root directory does not exist: {root}")

    if args.watch and args.watch > 0:
        try:
            while True:
                out_file, board = write_board(root)
                print(
                    f"[agy_board] wrote {out_file} ({len(board.cards)} cards, "
                    f"{sum(1 for c in board.cards if c.state == 'running')} running)",
                    flush=True,
                )
                time.sleep(args.watch)
        except KeyboardInterrupt:
            return
    else:
        out_file, board = write_board(root)
        print(
            f"[agy_board] wrote {out_file} ({len(board.cards)} cards, "
            f"{sum(1 for c in board.cards if c.state == 'running')} running)"
        )


if __name__ == "__main__":
    main()
