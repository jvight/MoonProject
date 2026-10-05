"""
Reads a Unity editor log and pulls out what a human needs: compile errors, exceptions, Debug.LogError/Assert
messages, batchmode aborts, crashes, executeMethod failures and the `[moon]` summary lines written by
MoonProject.Editor.Automation. Also turns new log lines into short progress hints while Unity runs.
"""
import re

COMPILE_ERROR = re.compile(r"^(?P<loc>.+?\(\d+,\d+\)): error (?P<code>CS\d+): (?P<msg>.+)$")
EXCEPTION = re.compile(r"^(?:[A-Za-z_][\w.`]*\.)?[A-Za-z_]\w*Exception(?:: .*)?$")
LOG_ERROR_FRAME = re.compile(r"^UnityEngine\.Debug:(LogError|LogException|LogAssertion|LogErrorFormat)")
STACK_FRAME = re.compile(r"^(?:\s+at |\S+:\S+ \(.*\)|UnityEngine\.|UnityEditor\.|System\.|\(Filename:)")
MOON_LINE = re.compile(r"^\[moon\] ")
EXECUTE_METHOD_FAILURE = re.compile(r"executeMethod (?:method|class) .*(?:does not exist|could not be found)|"
                                    r"Invalid -executeMethod")
CRASH = re.compile(r"^(?:Crash!!!|Received signal|=+ Native Crash Reporting|Unity Editor has crashed)")
ABORT = "Aborting batchmode due to failure:"
SCRIPTS_HAVE_ERRORS = "Scripts have compiler errors."

PROGRESS = [
    (re.compile(r"Resolving packages|\[Package Manager\]|Registered \d+ packages"), "resolving packages"),
    (re.compile(r"\[ScriptCompilation\]|bee_backend|Csc Library/Bee"), "compiling scripts"),
    (re.compile(r"Start importing (.+?) using Guid"), "importing assets"),
    (re.compile(r"Compiling shader|Compiled shader|ShaderCompilation"), "compiling shaders"),
    (re.compile(r"Asset Pipeline Refresh|Refresh completed"), "asset refresh done"),
    (re.compile(r"Executing \S+ for executeMethod|Invoking executeMethod|\[moon\]"), "running method"),
    (re.compile(r"Running tests|TestRunner|\[TestRunner\]|Test run"), "running tests"),
]


class Scanner:
    """Incremental progress tracker fed with new log text while Unity runs."""

    def __init__(self):
        self.phase = "starting Unity"
        self.imported = 0
        self.compile_errors = 0
        self._partial = ""

    def feed(self, text):
        data = self._partial + text
        lines = data.split("\n")
        self._partial = lines.pop()
        for line in lines:
            line = line.rstrip("\r")
            if line.startswith("Start importing "):
                self.imported += 1
            if COMPILE_ERROR.match(line):
                self.compile_errors += 1
            for pattern, phase in PROGRESS:
                if pattern.search(line):
                    self.phase = phase
                    break

    def describe(self):
        parts = [self.phase]
        if self.imported:
            parts.append(f"{self.imported} assets imported so far")
        if self.compile_errors:
            parts.append(f"{self.compile_errors} compile error lines seen")
        return ", ".join(parts)


class Report:
    def __init__(self):
        self.compile_errors = []
        self.scripts_have_errors = False
        self.abort_reason = None
        self.exceptions = []
        self.logged_errors = []
        self.method_failures = []
        self.crashes = []
        self.moon_lines = []

    @property
    def has_problems(self):
        return bool(self.compile_errors or self.scripts_have_errors or self.abort_reason or self.exceptions
                    or self.logged_errors or self.method_failures or self.crashes)


def _strip_root(text, root):
    for form in (str(root) + "\\", str(root).replace("\\", "/") + "/"):
        text = text.replace(form, "")
    return text


def analyse(log_text, root, max_items=40):
    """Builds a Report from a complete Unity log."""
    report = Report()
    lines = [line.rstrip("\r") for line in log_text.split("\n")]
    seen_compile = set()
    seen_exceptions = set()
    seen_errors = set()
    for i, line in enumerate(lines):
        match = COMPILE_ERROR.match(line)
        if match:
            text = _strip_root(line, root)
            if text not in seen_compile:
                seen_compile.add(text)
                report.compile_errors.append(text)
            continue
        if line.strip() == SCRIPTS_HAVE_ERRORS:
            report.scripts_have_errors = True
        if line.startswith(ABORT):
            following = lines[i + 1].strip() if i + 1 < len(lines) else ""
            report.abort_reason = following or line
        if MOON_LINE.match(line):
            report.moon_lines.append(line)
        if EXECUTE_METHOD_FAILURE.search(line):
            report.method_failures.append(line.strip())
        if CRASH.match(line):
            report.crashes.append(line.strip())
        if EXCEPTION.match(line) and not line.startswith(" "):
            key = line.strip()
            if key not in seen_exceptions:
                seen_exceptions.add(key)
                stack = [s.strip() for s in lines[i + 1:i + 5] if s.strip() and STACK_FRAME.match(s)]
                report.exceptions.append([_strip_root(key, root)] + [_strip_root(s, root) for s in stack[:3]])
            continue
        frame = LOG_ERROR_FRAME.match(line)
        if frame and frame.group(1) != "LogException":
            message = _message_before(lines, i)
            key = message
            if key and key not in seen_errors:
                seen_errors.add(key)
                caller = lines[i + 1].strip() if i + 1 < len(lines) else ""
                report.logged_errors.append([_strip_root(message, root), _strip_root(caller, root)])
    for name in ("compile_errors", "exceptions", "logged_errors", "method_failures", "crashes"):
        items = getattr(report, name)
        if len(items) > max_items:
            setattr(report, name, items[:max_items] + [f"... and {len(items) - max_items} more (see log)"])
    return report


def _message_before(lines, index):
    """The message text of a Debug.LogError: the non-stack lines right above its stack frames."""
    j = index - 1
    while j >= 0 and lines[j].strip() and STACK_FRAME.match(lines[j].strip()):
        j -= 1
    message = []
    while j >= 0 and len(message) < 3:
        text = lines[j].strip()
        if not text or STACK_FRAME.match(text):
            break
        message.insert(0, text)
        j -= 1
    return " | ".join(message)


def print_report(report, out=print, verbose_moon=True):
    if verbose_moon:
        for line in report.moon_lines:
            out(line)
    if report.compile_errors:
        out(f"COMPILE ERRORS ({len(report.compile_errors)}):")
        for line in report.compile_errors:
            out(f"  {line}")
    elif report.scripts_have_errors:
        out("COMPILE ERRORS: Unity reported 'Scripts have compiler errors.' (see log)")
    if report.method_failures:
        out("EXECUTE METHOD FAILURES:")
        for line in report.method_failures:
            out(f"  {line}")
    if report.exceptions:
        out(f"EXCEPTIONS ({len(report.exceptions)} unique):")
        for item in report.exceptions:
            if isinstance(item, str):
                out(f"  {item}")
                continue
            out(f"  {item[0]}")
            for frame in item[1:]:
                out(f"      {frame}")
    if report.logged_errors:
        out(f"LOGGED ERRORS ({len(report.logged_errors)} unique):")
        for item in report.logged_errors:
            if isinstance(item, str):
                out(f"  {item}")
                continue
            out(f"  {item[0]}")
            if item[1]:
                out(f"      at {item[1]}")
    if report.crashes:
        out("CRASH:")
        for line in report.crashes:
            out(f"  {line}")
    if report.abort_reason:
        out(f"UNITY ABORTED: {report.abort_reason}")
