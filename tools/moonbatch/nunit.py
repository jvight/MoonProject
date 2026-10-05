"""Parses the NUnit 3 XML that Unity's test runner writes with -testResults."""
import xml.etree.ElementTree as ET


class TestResults:
    def __init__(self):
        self.total = 0
        self.passed = 0
        self.failed = 0
        self.skipped = 0
        self.inconclusive = 0
        self.duration = 0.0
        self.result = "Unknown"
        self.failures = []

    @property
    def ok(self):
        return self.total > 0 and self.failed == 0 and self.result.startswith("Passed")


class Failure:
    def __init__(self, name, label, message, stack, output):
        self.name = name
        self.label = label
        self.message = message
        self.stack = stack
        self.output = output


def _text(node, path):
    found = node.find(path)
    return (found.text or "").strip() if found is not None and found.text else ""


def parse(path):
    root = ET.parse(path).getroot()
    results = TestResults()
    results.total = int(root.get("total", 0))
    results.passed = int(root.get("passed", 0))
    results.failed = int(root.get("failed", 0))
    results.skipped = int(root.get("skipped", 0))
    results.inconclusive = int(root.get("inconclusive", 0))
    results.duration = float(root.get("duration", 0.0))
    results.result = root.get("result", "Unknown")
    for case in root.iter("test-case"):
        if case.get("result") != "Failed":
            continue
        results.failures.append(Failure(
            name=case.get("fullname", case.get("name", "?")),
            label=case.get("label", ""),
            message=_text(case, "failure/message"),
            stack=_text(case, "failure/stack-trace"),
            output=_text(case, "output"),
        ))
    # Fixture-level failures (OneTimeSetUp, assembly load problems) carry no failed test-case of their own.
    for suite in root.iter("test-suite"):
        if suite.get("result") == "Failed" and suite.get("site") in ("SetUp", "TearDown"):
            results.failures.append(Failure(
                name=suite.get("fullname", suite.get("name", "?")) + f" [{suite.get('site')}]",
                label=suite.get("label", ""),
                message=_text(suite, "failure/message"),
                stack=_text(suite, "failure/stack-trace"),
                output=_text(suite, "output"),
            ))
    return results


def print_results(results, out=print, stack_lines=6):
    for failure in results.failures:
        label = f" ({failure.label})" if failure.label and failure.label != "Failed" else ""
        out(f"FAILED{label}: {failure.name}")
        for line in failure.message.splitlines()[:12]:
            out(f"    {line}")
        stack = [s for s in failure.stack.splitlines() if s.strip()]
        for line in stack[:stack_lines]:
            out(f"      {line.strip()}")
        if failure.output:
            out("    output:")
            for line in failure.output.splitlines()[:10]:
                out(f"      {line}")
    out(f"TESTS: {results.total} total, {results.passed} passed, {results.failed} failed, "
        f"{results.skipped} skipped, {results.inconclusive} inconclusive "
        f"({results.duration:.1f}s) -> {results.result}")
