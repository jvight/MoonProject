"""
Scripting define symbols for one batch run: the editor compiles the Standalone group's symbols from
ProjectSettings.asset, so a run that needs a symbol (the development-only Autoplay module) adds it there for the
run and puts the file back byte for byte afterwards.
"""
import re
from contextlib import contextmanager
from pathlib import Path

SETTINGS = Path("ProjectSettings") / "ProjectSettings.asset"
_BLOCK = re.compile(r"^  scriptingDefineSymbols:\r?\n((?:    [^\r\n]*\r?\n)+)", re.MULTILINE)
_STANDALONE = re.compile(r"^(    Standalone:)([^\r\n]*)(?=\r?\n|\Z)", re.MULTILINE)


def add_defines(text, symbols):
    """`text` (ProjectSettings.asset) with `symbols` on the Standalone scripting-define line, each once."""
    block = _BLOCK.search(text)
    if block is None:
        raise ValueError("ProjectSettings has no scriptingDefineSymbols block")
    line = _STANDALONE.search(block.group(1))
    if line is None:
        raise ValueError("the scriptingDefineSymbols block has no Standalone line")
    present = [s for s in line.group(2).strip().split(";") if s]
    merged = present + [s for s in symbols if s not in present]
    start = block.start(1) + line.start(2)
    end = block.start(1) + line.end(2)
    return text[:start] + " " + ";".join(merged) + text[end:]


@contextmanager
def defined(root, symbols):
    """Inside the block the worktree at `root` compiles with `symbols` defined; the settings file is restored on exit."""
    if not symbols:
        yield
        return
    path = Path(root) / SETTINGS
    original = path.read_bytes()
    try:
        path.write_bytes(add_defines(original.decode("utf-8"), symbols).encode("utf-8"))
        yield
    finally:
        path.write_bytes(original)
