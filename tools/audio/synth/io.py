"""Audio file I/O (soundfile / libsndfile).

* :func:`write_wav` - PCM WAV, byte-for-byte deterministic (plain RIFF header, no timestamps).
* :func:`write_ogg` - Ogg Vorbis. NOT byte-deterministic: libsndfile picks a random Ogg stream serial number
  per file. The decoded audio is identical, so verify OGG renders with :func:`audio_digest`.
* :func:`read_audio` - any format libsndfile reads -> ``(float64 array, sample_rate)``.
"""
import hashlib

import numpy as np
import soundfile as sf

from .core import SAMPLE_RATE

_PCM_SUBTYPES = {16: ("PCM_16", np.int16, 32767.0), 24: ("PCM_24", np.int32, 8388607.0)}

#: Frames per encoder call in :func:`write_ogg` (small enough for any thread's stack).
OGG_BLOCK_FRAMES = 16384


def quantize(x: np.ndarray, bits: int = 16) -> np.ndarray:
    """Clamp to -1..1 and round to the nearest integer code (16-bit -> int16; 24-bit -> int32 holding 24-bit
    values, scaled to the top of the int32 range for soundfile)."""
    _, dtype, scale = _PCM_SUBTYPES[bits]
    codes = np.rint(np.clip(x, -1.0, 1.0) * scale)
    if bits == 24:
        return (codes * 256.0).astype(np.int32)
    return codes.astype(dtype)


def write_wav(path, x: np.ndarray, bits: int = 16, sample_rate: int = SAMPLE_RATE) -> None:
    """Writes mono ``(n,)`` or stereo ``(n, 2)`` PCM WAV (16 or 24 bit)."""
    subtype = _PCM_SUBTYPES[bits][0]
    sf.write(str(path), quantize(x, bits), sample_rate, subtype=subtype, format="WAV")


def write_ogg(path, x: np.ndarray, quality: float = 0.6, sample_rate: int = SAMPLE_RATE) -> None:
    """Writes Ogg Vorbis; ``quality`` 0..1 (higher = better, ~0.6 is transparent for lofi).

    Frames are fed to the encoder in blocks of :data:`OGG_BLOCK_FRAMES`: handing libsndfile's Vorbis encoder a
    whole track in one call overflows the stack of worker threads on Windows (process dies, exit code 127)."""
    data = np.clip(np.asarray(x, dtype=np.float64), -1.0, 1.0)
    chans = 1 if data.ndim == 1 else data.shape[1]
    with sf.SoundFile(str(path), "w", sample_rate, chans, format="OGG", subtype="VORBIS",
                      compression_level=min(max(1.0 - quality, 0.0), 1.0)) as out:
        for start in range(0, data.shape[0], OGG_BLOCK_FRAMES):
            out.write(data[start:start + OGG_BLOCK_FRAMES])


def read_audio(path) -> tuple:
    """``(samples, sample_rate)``; mono files give ``(n,)``, multichannel ``(n, channels)``, float64 -1..1."""
    data, rate = sf.read(str(path), dtype="float64", always_2d=False)
    return data, rate


def audio_digest(path) -> str:
    """sha256 of the decoded 16-bit sample codes (format- and container-independent fingerprint)."""
    data, rate = read_audio(path)
    h = hashlib.sha256(str(rate).encode("ascii"))
    h.update(quantize(data, 16).tobytes())
    return h.hexdigest()
