"""Feel coverage: which domains answer each Core event (docs/design/feel-pass.md, the owner's polish bar).

Every Core event is a state change the player may live through. The owner's bar is that each one the player can
notice is seen AND heard. This lists every readonly struct in Core/Events and, per domain, whether runtime code
(tests excluded) answers it: a `Subscribe<Event>` call or a handler taking the event as its parameter.

    python tools/feel_coverage.py            # markdown table of all events
    python tools/feel_coverage.py --silent   # only events nobody in Audio, or nobody in Rover/UI/Gameplay, answers
"""

import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SCRIPTS = ROOT / "Assets" / "_Project" / "Scripts"
EVENTS = SCRIPTS / "Core" / "Events"
DOMAINS = ("Audio", "Rover", "UI", "Gameplay", "World", "App")
VISUAL = ("Rover", "UI", "Gameplay", "World")

STRUCT = re.compile(r"public readonly struct (\w+)")


def events():
    found = []
    for path in sorted(EVENTS.glob("*.cs")):
        for name in STRUCT.findall(path.read_text(encoding="utf-8")):
            found.append((name, path.stem))
    return found


def runtime_sources():
    sources = {domain: [] for domain in DOMAINS}
    for domain in DOMAINS:
        folder = SCRIPTS / domain
        if not folder.exists():
            continue
        for path in folder.rglob("*.cs"):
            parts = {part.lower() for part in path.relative_to(folder).parts}
            if "tests" in parts or "editor" in parts:
                continue
            sources[domain].append(path.read_text(encoding="utf-8"))
    return sources


def answers(text, name):
    if f"Subscribe<{name}>" in text:
        return True
    return re.search(rf"\((?:in )?{name} \w+\)", text) is not None


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--silent", action="store_true", help="only events missing a sound or a visual answer")
    args = parser.parse_args()

    sources = runtime_sources()
    rows = []
    for name, group in events():
        hit = {domain: any(answers(text, name) for text in sources[domain]) for domain in DOMAINS}
        heard = hit["Audio"]
        seen = any(hit[domain] for domain in VISUAL)
        rows.append((name, group, hit, heard, seen))

    shown = [row for row in rows if not args.silent or not (row[3] and row[4])]
    print("| Event | File | " + " | ".join(DOMAINS) + " | Heard | Seen |")
    print("|---|---|" + "---|" * len(DOMAINS) + "---|---|")
    for name, group, hit, heard, seen in shown:
        marks = " | ".join("x" if hit[domain] else "" for domain in DOMAINS)
        print(f"| {name} | {group} | {marks} | {'yes' if heard else 'NO'} | {'yes' if seen else 'NO'} |")
    both = sum(1 for row in rows if row[3] and row[4])
    print(f"\n{both}/{len(rows)} events are both heard and seen.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
