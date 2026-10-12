# tools/voice: the radio's recorded voices

Text lines (`lines.json`) -> offline TTS (sherpa-onnx running Piper voices) -> an old-tape or radio treatment
(`radio_filter.py`) -> one WAV per line in `Assets/_Project/Audio/Music/Voice/` plus `voice_manifest.json`, which the
Unity builder `Audio/Voice Lines` turns into `Data/Audio/VoiceLibrary.asset`. The ticker line with the same key is the
subtitle (en and vi); the voices are English only (owner, 2026-10-12).

| Voice | Who | Model | Licence |
|---|---|---|---|
| `ro` | Ro's letters and her last recording (old cassette) | Piper `en_GB-cori-high` | Public domain (LibriVox) |
| `station` | The automated station (narrow radio band, machine cadence) | Piper `en_US-libritts_r-medium`, speaker 460 | CC BY 4.0: credit in `voice_manifest.json` |

Engine: sherpa-onnx 1.13.8 (Apache-2.0). The generated audio ships in the game; all three licences allow that.

## Setup (once per machine, never system-wide)
The engine lives in `D:/Tools/sherpa-onnx` (or `MOON_TTS_HOME`):
1. `python -m venv D:/Tools/sherpa-onnx/venv`, then install the two hash-pinned wheels from PyPI:
   `venv/Scripts/python -m pip install --only-binary :all: --require-hashes --no-deps -r requirements.lock` with
   - `sherpa-onnx==1.13.8 --hash=sha256:195455d8d6cc49f616e9d459b7d08f10a32daf2ddcbe90d4d479a8ba9aab28bf`
   - `sherpa-onnx-core==1.13.8 --hash=sha256:5579e80196d516e6dae23c8f629292ce3142ab8869925d32b94612fd86f93733`
2. Download each model archive named in `voices.json` from its `source` (the k2-fsa/sherpa-onnx `tts-models`
   release), check `archiveSha256`, and extract it into `D:/Tools/sherpa-onnx/models/`.

## Use
- `python tools/voice/build_voice.py` renders every line; `--only <key>` one line; `--check` re-renders into a temp
  dir and fails unless every clip is byte-identical to the committed one and passes the loudness and peak gates.
- Tests (no engine needed): `python -m unittest discover -s tools/voice/tests -v`.

Determinism: Piper samples noise inside its ONNX graph; `onnx_seed.py` writes a cached copy of each model whose random
nodes carry a fixed seed, and every line gets a fresh engine, so takes keep their natural prosody and still repeat
exactly. The station uses no noise at all (an even, machine cadence).
