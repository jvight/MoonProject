"""
Unit tests for tools/agy_board.py on a temporary git repository and fake worktree root.
Run: python -m unittest discover -s tools/tests -t tools
"""
import datetime
import os
import subprocess
import tempfile
import unittest
from pathlib import Path

import agy_board


class AgyBoardTests(unittest.TestCase):
    def git(self, cwd: Path, *args: str, env: dict | None = None) -> str:
        run_env = dict(os.environ, **(env or {}))
        res = subprocess.run(
            ["git", "-c", "safe.directory=*", "-c", "user.name=test", "-c", "user.email=test@example.com", *args],
            cwd=cwd,
            env=run_env,
            check=True,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )
        return res.stdout.strip()

    def write_task(self, wt: Path, heading: str) -> Path:
        logs = wt / "Logs"
        logs.mkdir(parents=True, exist_ok=True)
        task = logs / "TASK.md"
        task.write_text(f"# {heading}\n\nDetails for {wt.name}.\n", encoding="utf-8")
        return task

    def snapshot_worktree(self, wt: Path) -> dict[str, bytes]:
        snap: dict[str, bytes] = {}
        for p in sorted(wt.rglob("*")):
            if p.is_file() and ".git" not in p.parts:
                snap[p.relative_to(wt).as_posix()] = p.read_bytes()
        return snap

    def test_board_classifies_states_captures_merges_and_renders_html_readonly(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp).resolve()
            main_repo = base / "main-repo"
            wt_root = base / "wt-root"
            main_repo.mkdir()
            wt_root.mkdir()

            now_dt = datetime.datetime(2026, 10, 16, 16, 30, 0)
            now_ts = now_dt.timestamp()
            three_days_ago_ts = int(now_ts - 3 * 86400)
            old_date_env = {
                "GIT_AUTHOR_DATE": f"{three_days_ago_ts} +0000",
                "GIT_COMMITTER_DATE": f"{three_days_ago_ts} +0000",
            }

            self.git(main_repo, "init", "-q", "-b", "main")
            (main_repo / "README.md").write_text("lunarion\n", encoding="utf-8")
            self.git(main_repo, "add", "README.md")
            self.git(main_repo, "commit", "-q", "-m", "chore: initial commit", env=old_date_env)

            # 1. Landed worktree: branch merged into main via --no-ff > 2 days ago (cleanup candidate).
            wt_landed = wt_root / "agy-landed"
            self.git(main_repo, "worktree", "add", "-q", "-b", "box/agy-landed", str(wt_landed), "main")
            (wt_landed / "landed.txt").write_text("landed feature\n", encoding="utf-8")
            self.git(wt_landed, "add", "landed.txt")
            self.git(wt_landed, "commit", "-q", "-m", "feat(rover): landed suspension tweak", env=old_date_env)
            self.git(
                main_repo,
                "merge",
                "--no-ff",
                "-q",
                "-m",
                "merge: box/agy-landed",
                "box/agy-landed",
                env=old_date_env,
            )
            self.write_task(wt_landed, "Task LANDED-BOX: landed feature on main")
            (wt_landed / "Logs" / "agy-time.txt").write_text(
                "start 10:00:00\nstatus done\nend 10:25:00\nminutes 25\n", encoding="utf-8"
            )
            (wt_landed / "Logs" / "REPORT.md").write_text("# Report\nRESULT: PASS\n", encoding="utf-8")
            (wt_landed / "Logs" / "DONE").write_text("", encoding="utf-8")
            os.utime(wt_landed, (three_days_ago_ts, three_days_ago_ts))

            # 2. Running worktree: start in agy-time.txt without status, rate-limit resume, fresh capture, 2 rounds.
            wt_running = wt_root / "agy-running"
            self.git(main_repo, "worktree", "add", "-q", "-b", "box/agy-running", str(wt_running), "main")
            self.write_task(wt_running, "Task RUNNING-BOX: active telemetry work")
            time_running = wt_running / "Logs" / "agy-time.txt"
            time_running.write_text(
                "start 16:00:00\nrate-limited: waiting 120s, resume #1 (16:10:00)\n",
                encoding="utf-8",
            )
            start_running_ts = datetime.datetime(2026, 10, 16, 16, 0, 0).timestamp()
            os.utime(time_running, (start_running_ts, start_running_ts))
            fresh_png = wt_running / "Logs" / "captures" / "fresh_shot.png"
            fresh_png.parent.mkdir(parents=True, exist_ok=True)
            fresh_png.write_bytes(b"\x89PNG\r\n\x1a\n")
            os.utime(fresh_png, (start_running_ts + 600, start_running_ts + 600))
            (wt_running / "Logs" / "r1").mkdir()
            (wt_running / "Logs" / "r2").mkdir()

            # 3. Done (unlanded) worktree: DONE + REPORT, commits ahead of main.
            wt_done = wt_root / "agy-done"
            self.git(main_repo, "worktree", "add", "-q", "-b", "box/agy-done", str(wt_done), "main")
            (wt_done / "work.txt").write_text("step 1\n", encoding="utf-8")
            self.git(wt_done, "add", "work.txt")
            self.git(wt_done, "commit", "-q", "-m", "feat(ui): first step")
            (wt_done / "work.txt").write_text("step 2\n", encoding="utf-8")
            self.git(wt_done, "add", "work.txt")
            self.git(wt_done, "commit", "-q", "-m", "feat(ui): finish terminal two-color layout")
            self.write_task(wt_done, "Task DONE-BOX: review queue item")
            done_time = wt_done / "Logs" / "agy-time.txt"
            done_time.write_text("start 14:00:00\nstatus done\nend 14:42:00\nminutes 42\n", encoding="utf-8")
            (wt_done / "Logs" / "REPORT.md").write_text(
                "# Report\n- `python tools/compile_check.py` -> RESULT: PASS\n- `box_check` exit code 0\n",
                encoding="utf-8",
            )
            (wt_done / "Logs" / "DONE").write_text("", encoding="utf-8")

            # 4. Timeout worktree: status timeout-killed and FAIL gate line in REPORT.md.
            wt_timeout = wt_root / "agy-timeout"
            self.git(main_repo, "worktree", "add", "-q", "-b", "box/agy-timeout", str(wt_timeout), "main")
            (wt_timeout / "wip.txt").write_text("wip\n", encoding="utf-8")
            self.git(wt_timeout, "add", "wip.txt")
            self.git(wt_timeout, "commit", "-q", "-m", "feat(world): partial crater pass")
            self.write_task(wt_timeout, "Task TIMEOUT-BOX: heavy bake that timed out")
            (wt_timeout / "Logs" / "agy-time.txt").write_text(
                "start 11:00:00\nstatus timeout-killed\nend 12:30:00\nminutes 90\n", encoding="utf-8"
            )
            (wt_timeout / "Logs" / "REPORT.md").write_text(
                "# Partial Report\nRESULT: FAIL (2 problem(s))\n", encoding="utf-8"
            )

            # 5. Stale-captures worktree: exited-without-DONE + PNG older than job start.
            wt_stale = wt_root / "agy-stale"
            self.git(main_repo, "worktree", "add", "-q", "-b", "box/agy-stale", str(wt_stale), "main")
            self.write_task(wt_stale, "Task STALE-BOX: round 2 with old round 1 capture")
            stale_time = wt_stale / "Logs" / "agy-time.txt"
            stale_time.write_text(
                "start 15:00:00\nstatus exited-without-DONE\nend 15:20:00\nminutes 20\n", encoding="utf-8"
            )
            stale_job_ts = datetime.datetime(2026, 10, 16, 15, 20, 0).timestamp()
            os.utime(stale_time, (stale_job_ts, stale_job_ts))
            stale_png = wt_stale / "Logs" / "old_capture.png"
            stale_png.write_bytes(b"\x89PNG\r\n\x1a\n")
            old_png_ts = datetime.datetime(2026, 10, 16, 12, 0, 0).timestamp()
            os.utime(stale_png, (old_png_ts, old_png_ts))

            # 6. Idle worktree: TASK.md present, not started yet.
            wt_idle = wt_root / "agy-idle"
            self.git(main_repo, "worktree", "add", "-q", "-b", "box/agy-idle", str(wt_idle), "main")
            self.write_task(wt_idle, "Task IDLE-BOX: queued for next slot")

            # History file with 35 entries (only last 30 should be rendered).
            board_dir = wt_root / ".board"
            board_dir.mkdir(parents=True, exist_ok=True)
            history_lines = [f"12:{i:02d}:00 /d/wt/job-{i:02d} done {i}" for i in range(1, 36)]
            (board_dir / "agy-jobs.md").write_text("\n".join(history_lines) + "\n", encoding="utf-8")

            worktrees = [wt_landed, wt_running, wt_done, wt_timeout, wt_stale, wt_idle]
            before_snapshots = {wt.name: self.snapshot_worktree(wt) for wt in worktrees}

            out_file, board = agy_board.write_board(wt_root, now=now_dt, check_processes=False)
            self.assertTrue(out_file.is_file())
            self.assertEqual(out_file, wt_root / ".board" / "index.html")

            # Verify no worktree was modified.
            for wt in worktrees:
                self.assertEqual(before_snapshots[wt.name], self.snapshot_worktree(wt), f"{wt.name} was modified")

            by_name = {c.name: c for c in board.cards}
            self.assertEqual(set(by_name), {"agy-landed", "agy-running", "agy-done", "agy-timeout", "agy-stale", "agy-idle"})

            # Check state classification and fields.
            self.assertEqual(by_name["agy-running"].state, "running")
            self.assertFalse(by_name["agy-running"].landed)
            self.assertEqual(by_name["agy-running"].elapsed_minutes, 30)
            self.assertEqual(len(by_name["agy-running"].resumes), 1)
            self.assertEqual(by_name["agy-running"].rounds, ("r1", "r2"))
            self.assertEqual(len(by_name["agy-running"].captures), 1)
            self.assertFalse(by_name["agy-running"].captures[0].stale)
            self.assertTrue(by_name["agy-running"].captures[0].uri.startswith("file:///"))

            self.assertEqual(by_name["agy-done"].state, "done")
            self.assertFalse(by_name["agy-done"].landed)
            self.assertEqual(by_name["agy-done"].ahead, 2)
            self.assertEqual(by_name["agy-done"].last_subject, "feat(ui): finish terminal two-color layout")

            self.assertEqual(by_name["agy-timeout"].state, "timeout-killed")
            self.assertFalse(by_name["agy-timeout"].landed)
            self.assertTrue(any(g.is_fail for g in by_name["agy-timeout"].gate_lines))

            self.assertEqual(by_name["agy-stale"].state, "exited-without-DONE")
            self.assertEqual(len(by_name["agy-stale"].captures), 1)
            self.assertTrue(by_name["agy-stale"].captures[0].stale)

            self.assertEqual(by_name["agy-landed"].state, "done")
            self.assertTrue(by_name["agy-landed"].landed)

            self.assertEqual(by_name["agy-idle"].state, "idle")
            self.assertFalse(by_name["agy-idle"].landed)

            # Check card sort order: running -> done_unlanded -> failed -> landed_idle.
            buckets = [c.bucket for c in board.cards]
            self.assertEqual(
                buckets,
                ["running", "done_unlanded", "failed", "failed", "landed_idle", "landed_idle"],
            )

            # Check disk cleanup candidates (>2d merged).
            self.assertEqual(board.total_worktrees, 6)
            cand_names = [name for name, _, _ in board.cleanup_candidates]
            self.assertIn("agy-landed", cand_names)
            self.assertNotIn("agy-done", cand_names)

            # Check history truncation to last 30 lines.
            self.assertEqual(len(board.history_lines), 30)
            self.assertEqual(board.history_lines[0], "12:06:00 /d/wt/job-06 done 6")
            self.assertEqual(board.history_lines[-1], "12:35:00 /d/wt/job-35 done 35")

            # Check rendered HTML content.
            html_text = out_file.read_text(encoding="utf-8")
            self.assertIn('<meta http-equiv="refresh" content="30">', html_text)
            self.assertIn('"IBM Plex Mono", Consolas, monospace', html_text)
            self.assertIn("#FFB547", html_text)
            self.assertIn("#5FF3FF", html_text)
            for wt_name in by_name:
                self.assertIn(f'data-worktree="{wt_name}"', html_text)
            self.assertIn("Task RUNNING-BOX: active telemetry work", html_text)
            self.assertIn("Task DONE-BOX: review queue item", html_text)
            self.assertIn("Task TIMEOUT-BOX: heavy bake that timed out", html_text)
            self.assertIn("Task STALE-BOX: round 2 with old round 1 capture", html_text)
            self.assertIn("Task LANDED-BOX: landed feature on main", html_text)
            self.assertIn("Task IDLE-BOX: queued for next slot", html_text)
            self.assertIn('<span class="stale-flag">stale</span>', html_text)
            self.assertIn('<span class="token-fail">FAIL</span>', html_text)
            self.assertIn("12:35:00 /d/wt/job-35 done 35", html_text)
            self.assertNotIn("12:01:00 /d/wt/job-01 done 1", html_text)


if __name__ == "__main__":
    unittest.main()
