using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// One mix for being alone on the moon (feel pillar 6): from how far 07 is past the radio's signal, how still it
    /// is, how deep in Whispering Canyon and whether the radio is on Quiet Hours, the gain of every layer.
    /// <list type="bullet">
    /// <item><b>Farness</b> 0..1 eases in over the silence width past the signal's edge.</item>
    /// <item><b>Solitude</b> 0..1 combines farness, Quiet Hours and the canyon (each weighted): how alone 07 is.</item>
    /// <item><b>Radio</b> (music and static): fades to near-silence with farness, thins in the canyon.</item>
    /// <item><b>Basin bed</b>: recedes with farness and in the canyon, swells a little on Quiet Hours.</item>
    /// <item><b>Room tone</b>: from barely there at home to the main bed when alone (less in the canyon, whose
    /// whisper is its air).</item>
    /// <item><b>07's small sounds</b>: come forward with solitude.</item>
    /// <item><b>Stillness</b> pulls the world (radio, basin and canyon beds) back a few dB; the room tone and 07's
    /// own sounds stay, so the space feels wider.</item>
    /// </list>
    /// Every contribution is added in dB, so the layers move smoothly and independently. Allocation-free.
    /// </summary>
    public sealed class SoundscapeModel
    {
        private const float DecibelsPerAmplitudeDecade = 20f;
        private const float MinLinear = 1e-5f;

        private readonly SoundscapeTuning _tuning;
        private readonly CanyonAudioTuning _canyon;
        private readonly EasedValue _quietHours = new EasedValue(0f);

        public SoundscapeModel(SoundscapeTuning tuning, CanyonAudioTuning canyon)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _canyon = canyon != null ? canyon : throw new ArgumentNullException(nameof(canyon));
            Step(0f, 1f, 0f, 0f, false, 0f);
        }

        /// <summary>0 within the radio's signal .. 1 past the silence width beyond it.</summary>
        public float Farness { get; private set; }

        /// <summary>0 at home with the radio on .. 1 utterly alone.</summary>
        public float Solitude { get; private set; }

        /// <summary>Quiet Hours, eased 0..1.</summary>
        public float QuietHours => _quietHours.Value;

        public float Stillness { get; private set; }

        public float Canyon { get; private set; }

        /// <summary>Radio music and static gain.</summary>
        public float RadioGain { get; private set; }

        /// <summary>The basin's ambience bed gain.</summary>
        public float BasinBedGain { get; private set; }

        /// <summary>Whispering Canyon's whisper and trough beds gain.</summary>
        public float CanyonBedGain { get; private set; }

        public float RoomToneGain { get; private set; }

        /// <summary>07's lamp hum, servos and cooling ticks gain.</summary>
        public float SmallSoundsGain { get; private set; }

        /// <summary>
        /// Recomputes every gain. <paramref name="distance"/> is 07's horizontal distance from the base,
        /// <paramref name="signalEdge"/> where the radio's signal is lost entirely (both metres);
        /// <paramref name="stillness"/> and <paramref name="canyonInside"/> are 0..1 (already eased);
        /// <paramref name="quietHours"/> eases in and out here.
        /// </summary>
        public void Step(float distance, float signalEdge, float stillness, float canyonInside, bool quietHours,
            float deltaTime)
        {
            _quietHours.Step(quietHours ? 1f : 0f, Mathf.Max(0f, deltaTime), _tuning.QuietHoursEase);
            Farness = Smooth01((distance - signalEdge) / _tuning.SilenceWidth);
            Stillness = Mathf.Clamp01(stillness);
            Canyon = Mathf.Clamp01(canyonInside);
            Solitude = 1f - (1f - Farness) * (1f - QuietHours * _tuning.QuietHoursSolitude) *
                       (1f - Canyon * _tuning.CanyonSolitude);

            float still = Stillness * _tuning.StillDuckDb;
            RadioGain = FromDb(Farness * _tuning.FarRadioDb + Canyon * ToDb(_canyon.RadioMusicInside) + still);
            BasinBedGain = FromDb(Farness * _tuning.FarBasinDb + Canyon * ToDb(_canyon.BasinBedInside) +
                                  QuietHours * _tuning.QuietHoursBasinDb + still);
            CanyonBedGain = FromDb(still);
            RoomToneGain = FromDb(Mathf.Lerp(_tuning.RoomToneNearDb, _tuning.RoomToneFarDb, Solitude) +
                                  Canyon * _tuning.RoomToneInCanyonDb);
            SmallSoundsGain = FromDb(Mathf.Lerp(_tuning.SmallSoundsNearDb, 0f, Solitude));
        }

        /// <summary>The radio's low-pass from its open <paramref name="cutoffHz"/>: thinner in the canyon (eased
        /// geometrically towards the canyon's thin set).</summary>
        public float RadioCutoff(float cutoffHz)
        {
            if (Canyon <= 0f || cutoffHz <= 0f)
            {
                return cutoffHz;
            }

            return cutoffHz * Mathf.Pow(Mathf.Min(1f, _canyon.RadioCutoffInside / cutoffHz), Canyon);
        }

        /// <summary>Linear gain of <paramref name="decibels"/>.</summary>
        public static float FromDb(float decibels)
        {
            return Mathf.Pow(10f, decibels / DecibelsPerAmplitudeDecade);
        }

        /// <summary>Decibels of a linear <paramref name="gain"/> (floored at -100 dB).</summary>
        public static float ToDb(float gain)
        {
            return DecibelsPerAmplitudeDecade * Mathf.Log10(Mathf.Max(gain, MinLinear));
        }

        private static float Smooth01(float x)
        {
            float t = Mathf.Clamp01(x);
            return t * t * (3f - 2f * t);
        }
    }
}
