"""Synthesises voice takes with sherpa-onnx. Runs inside the TTS venv (``D:/Tools/sherpa-onnx/venv``), stdlib only.

Usage (called by build_voice.py, never by hand):
  <venv>/python -I tts_worker.py <jobs.json>

Each job: ``{"model", "tokens", "dataDir", "speaker", "noiseScale", "noiseW", "lengthScale", "text", "out"}``.
Every job gets a fresh engine, so a seeded model draws the same noise for each line whatever order lines render in.
Writes the samples as little-endian float32 to ``out`` and prints one JSON line per job with its sample rate.
"""
import array
import json
import sys

import sherpa_onnx


def synthesise(job: dict) -> dict:
    vits = sherpa_onnx.OfflineTtsVitsModelConfig(
        model=job["model"], tokens=job["tokens"], data_dir=job["dataDir"],
        noise_scale=job["noiseScale"], noise_scale_w=job["noiseW"], length_scale=job["lengthScale"])
    config = sherpa_onnx.OfflineTtsConfig(
        model=sherpa_onnx.OfflineTtsModelConfig(vits=vits, num_threads=1, provider="cpu"))
    audio = sherpa_onnx.OfflineTts(config).generate(job["text"], sid=job["speaker"], speed=1.0)
    samples = array.array("f", audio.samples)
    if sys.byteorder != "little":
        samples.byteswap()
    with open(job["out"], "wb") as handle:
        samples.tofile(handle)
    return {"out": job["out"], "sampleRate": audio.sample_rate, "frames": len(samples)}


def main() -> int:
    with open(sys.argv[1], encoding="utf-8") as handle:
        jobs = json.load(handle)
    for job in jobs:
        print(json.dumps(synthesise(job)), flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
