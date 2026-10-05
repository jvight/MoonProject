"""synth - Lofi Lunar's shared, deterministic DSP core (numpy + scipy + soundfile; see tools/requirements.txt).

Used by ``tools/audio`` (SFX and ambience) and ``tools/music`` (radio tracks). Import it by putting
``<repo>/tools/audio`` on ``sys.path``, then ``import synth`` or ``from synth import osc, filters``.

Conventions (stable API)
------------------------
* Fixed sample rate :data:`SAMPLE_RATE` = 48000 Hz (no per-call sample-rate arguments).
* Signals: ``numpy.float64``, nominal -1..1. Mono = shape ``(n,)``; stereo = shape ``(n, 2)`` (frames x channels).
* Times in seconds, frequencies in Hz, gains linear unless the name ends in ``_db``. Where a frequency may vary,
  pass a per-sample array of length ``n``.
* Randomness only through :func:`rng` (``numpy.random.Generator``, PCG64 seeded from sha256 of the keys).

Modules
-------
core        SAMPLE_RATE, samples/seconds/time_axis, db_to_gain/gain_to_db, rng, note_to_midi/midi_to_freq/
            note_freq/pentatonic/loop_freq, silence/channels/map_channels/to_mono/to_stereo/pan/place/mix/
            normalize_peak
osc         sine, saw (polyBLEP), square (polyBLEP pulse), triangle (polyBLAMP), additive, glide, vibrato,
            phase_cycles
noise       white, pink, brown, pink_filter, brown_filter, event_times
envelope    ar, adsr (linear | exp), segments (linear | smooth), exp_decay, curve, fade_in, fade_out, lfo,
            decay_coefficient
filters     biquad + lowpass/highpass/bandpass/notch/peaking/lowshelf/highshelf (RBJ), coefficients, butter,
            swept (time-varying), onepole_lp/onepole_hp, dc_block, k_weight
fm          operator (phase modulation, optional self-feedback), two_op
pluck       karplus_strong
effects     reverb (Freeverb, stereo out), chorus (stereo out), wow_flutter, tremolo, echo, saturate,
            soft_limit, compressor, limiter (lookahead, true peak)
loop        periodic (steady-state processing of loops), crossfade_loop, seam_error
analysis    peak_db, true_peak_db, rms_db, dc_offset, loudness_integrated (BS.1770-4 gated),
            loudness_momentary_max, spectral_stats, seam_stats, analyze
io          write_wav (deterministic PCM 16/24), write_ogg (Vorbis), read_audio, audio_digest, quantize
instruments kalimba, glass_chime, soft_bell, soft_pad, partial, grain, scatter

Determinism: the same code, numpy/scipy versions and CPU produce bit-identical float output; WAV files are then
byte-identical. Ogg files differ in their random stream serial only - compare them with ``io.audio_digest``.
"""
from . import analysis, core, effects, envelope, filters, fm, instruments, io, loop, noise, osc, pluck
from .core import SAMPLE_RATE, rng

__all__ = ["SAMPLE_RATE", "analysis", "core", "effects", "envelope", "filters", "fm", "instruments", "io", "loop",
           "noise", "osc", "pluck", "rng"]

API_VERSION = 1
