"""
Machine-wide limit on concurrent batch Unity instances.

Each slot is a lock file in <git common dir>/moon-batch/ that is held with an OS byte-range lock
(msvcrt.locking on Windows, fcntl.flock elsewhere). The OS drops the lock when the holder dies, so a crashed
holder can never leave a stale slot behind; unity_batch.py additionally puts Unity in a job object that dies with
it. Next to each lock a small JSON sidecar records who holds the slot (pid, worktree, command, start time) so
waiters can print a useful message; a sidecar whose pid is no longer alive is reported as stale and ignored.
"""
import datetime
import json
import os
import sys
import time

from . import procutil

if sys.platform == "win32":
    import msvcrt

    def _try_lock(fd):
        os.lseek(fd, 0, os.SEEK_SET)
        try:
            msvcrt.locking(fd, msvcrt.LK_NBLCK, 1)
            return True
        except OSError:
            return False

    def _unlock(fd):
        os.lseek(fd, 0, os.SEEK_SET)
        msvcrt.locking(fd, msvcrt.LK_UNLCK, 1)
else:
    import fcntl

    def _try_lock(fd):
        try:
            fcntl.flock(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
            return True
        except OSError:
            return False

    def _unlock(fd):
        fcntl.flock(fd, fcntl.LOCK_UN)


class Slot:
    """A held batch slot. Release it (or use it as a context manager) when Unity has exited."""

    def __init__(self, index, fd, info_path):
        self.index = index
        self._fd = fd
        self._info_path = info_path

    def update(self, **fields):
        info = _read_json(self._info_path) or {}
        info.update(fields)
        _write_json(self._info_path, info)

    def release(self):
        if self._fd is None:
            return
        try:
            os.remove(self._info_path)
        except OSError:
            pass
        _unlock(self._fd)
        os.close(self._fd)
        self._fd = None

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        self.release()


def _read_json(path):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError):
        return None


def _write_json(path, data):
    tmp = f"{path}.{os.getpid()}.tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    os.replace(tmp, path)


def _describe(info):
    if not info:
        return "unknown holder (no info yet)"
    pid = info.get("pid")
    alive = procutil.pid_alive(pid)
    unity_pid = info.get("unity_pid")
    state = "alive" if alive else "STALE: holder pid is gone"
    unity = f", unity pid {unity_pid}" if unity_pid else ""
    return (f"{info.get('worktree', '?')} [{info.get('command', '?')}] pid {pid} ({state}{unity}), "
            f"since {info.get('since', '?')}")


def acquire(lock_dir, count, worktree, command, wait=True, log=print, poll_seconds=2.0, report_seconds=60.0):
    """Blocks until one of `count` slots is free and returns it. Returns None if wait is False and all are busy."""
    os.makedirs(lock_dir, exist_ok=True)
    started = time.monotonic()
    last_report = None
    while True:
        busy = []
        for index in range(count):
            lock_path = os.path.join(lock_dir, f"slot-{index}.lock")
            info_path = os.path.join(lock_dir, f"slot-{index}.json")
            fd = os.open(lock_path, os.O_RDWR | os.O_CREAT, 0o644)
            if _try_lock(fd):
                stale = _read_json(info_path)
                if stale and stale.get("pid") != os.getpid():
                    log(f"[unity_batch] slot {index}: clearing stale record ({_describe(stale)})")
                slot = Slot(index, fd, info_path)
                _write_json(info_path, {
                    "pid": os.getpid(),
                    "worktree": worktree,
                    "command": command,
                    "since": datetime.datetime.now().isoformat(timespec="seconds"),
                })
                return slot
            os.close(fd)
            busy.append((index, _read_json(info_path)))
        if not wait:
            for index, info in busy:
                log(f"[unity_batch] slot {index} busy: {_describe(info)}")
            return None
        now = time.monotonic()
        if last_report is None or now - last_report >= report_seconds:
            waited = int(now - started)
            log(f"[unity_batch] all {count} batch Unity slots are busy (waited {waited}s); waiting for one to free up:")
            for index, info in busy:
                log(f"[unity_batch]   slot {index}: {_describe(info)}")
            last_report = now
        time.sleep(poll_seconds)


def status(lock_dir, count):
    """Returns [(index, held, info)] for display."""
    result = []
    for index in range(count):
        lock_path = os.path.join(lock_dir, f"slot-{index}.lock")
        info_path = os.path.join(lock_dir, f"slot-{index}.json")
        if not os.path.exists(lock_path):
            result.append((index, False, None))
            continue
        fd = os.open(lock_path, os.O_RDWR)
        try:
            held = not _try_lock(fd)
            if not held:
                _unlock(fd)
        finally:
            os.close(fd)
        result.append((index, held, _read_json(info_path) if held else None))
    return result


def describe(info):
    return _describe(info)
