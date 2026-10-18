#!/usr/bin/env python3
"""Write tools/audio/chirp_syllables.json: each chirp's syllable onsets, pitches (0..1) and lengths, from the same
gesture table the sampled voice renders (sampled/picks.py), so 07's eye and antenna move with what is heard.

Usage: python tools/audio/build_chirp_data.py [--check]
"""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from sampled import picks  # noqa: E402

OUT = Path(__file__).resolve().parent / "chirp_syllables.json"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true", help="fail if the committed file is stale")
    args = parser.parse_args()
    text = json.dumps(picks.chirp_bank_data(), indent=2) + "\n"
    if args.check:
        ok = OUT.is_file() and OUT.read_text(encoding="utf-8") == text
        print("chirp data OK" if ok else f"{OUT.name} is stale: run build_chirp_data.py")
        return 0 if ok else 1
    OUT.write_text(text, encoding="utf-8")
    print(f"wrote {OUT.name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
