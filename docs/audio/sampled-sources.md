# Sampled sources (Sonniss GDC 2026)

07's voice, the POST boot sounds and the build sounds are processed from real recorded mechanical sources
(Ben Burtt's method: a machine sound carried along a pitch contour, layered and filtered).

## The library

- **What:** the Sonniss *#GameAudioGDC Bundle 2026* (the free GDC sampler: 347 WAV files from 125 libraries, 7.5 GB).
- **Where:** on the owner's machine, extracted at `C:/Users/jvigh/AudioLibrary/Sonniss-GDC2026/extracted/`.
  It is read only, and **never copied into the repository**. The tools take its folder as an argument
  (`--library`).
- **Licence** (`License - GDC Game Audio.pdf` in the bundle): royalty-free; commercial use, no attribution, unlimited
  projects. **Not allowed:** reselling or redistributing the raw or edited files as sound effects or sample packs,
  and using them to train AI or ML models. Using processed results inside the game is allowed.

## What lives in the repository

- `tools/audio/sampled/sources.py`: each source by its path inside the library, the slice used (start, length)
  and, for tonal sources, the measured fundamental.
- `tools/audio/sampled/`: the recipes (`voice.py`, `boot.py`, `build.py`, shared `parts.py` and `dsp.py`) and
  `render_audition.py`, which writes level-matched A/B OGGs for a listening pass.
- The processed game cues only (WAVs under `Assets/_Project/Audio/`, once the owner has picked them), never a
  folder of library files or anything that works as a sample pack.

## Rendering

```
python tools/audio/sampled/render_audition.py --library <library folder> --out <folder>
```

Re-rendering a sampled cue needs the library on disk. Without it, the committed processed files are still checked
against the manifest's hashes; the check says that the sampled cues were not re-rendered.
