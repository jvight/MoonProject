using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// One mix for being alone on the moon (feel pillar 6): from how far 07 is past the radio's signal, how still it
    /// is and whether the camera's wide shot is open, the gain of every layer.
    /// <list type="bullet">
    /// <item><b>Farness</b> 0..1 eases in over the silence width past the signal's edge, measured from the base; it
    /// follows with a short time constant.</item>
    /// <item><b>Solitude</b> 0..1 is how alone 07 is (its farness).</item>
    /// <item><b>Radio</b> (music and static): fades to near-silence with farness.</item>
    /// <item><b>Basin bed</b>: recedes with farness.</item>
    /// <item><b>Room tone</b>: from barely there at the base to the main bed when alone.</item>
    /// <item><b>07's small sounds</b>: come forward with solitude.</item>
    /// <item><b>Stillness</b> pulls the world (radio and basin bed) back a few dB; the room tone and 07's own sounds
    /// stay, so the space feels wider.</item>
    /// <item><b>The wide shot</b> breathes out a little further with the frame: the radio and beds back again, the
    /// room tone up a touch.</item>
    /// </list>
    /// Every contribution is added in dB, so the layers move smoothly and independently. Allocation-free.
    /// </summary>
    public sealed class SoundscapeModel
    {
        private const float DecibelsPerAmplitudeDecade = 20f;
        private const float MinLinear = 1e-5f;

        private readonly SoundscapeTuning _tuning;
        private readonly EasedValue _farness = new EasedValue(0f);

        public SoundscapeModel(SoundscapeTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            Step(0f, 1f, 0f, 0f, 0f);
        }

        /// <summary>0 within the radio's signal .. 1 past the silence width beyond it (eased).</summary>
        public float Farness => _farness.Value;

        /// <summary>0 at the base .. 1 utterly alone.</summary>
        public float Solitude { get; private set; }

        public float Stillness { get; private set; }

        /// <summary>The camera's wide shot, 0 closed .. 1 fully open (eased by the caller).</summary>
        public float Wide { get; private set; }

        /// <summary>Radio music and static gain.</summary>
        public float RadioGain { get; private set; }

        /// <summary>The basin's ambience bed gain.</summary>
        public float BasinBedGain { get; private set; }

        public float RoomToneGain { get; private set; }

        /// <summary>07's lamp hum, servos and cooling ticks gain.</summary>
        public float SmallSoundsGain { get; private set; }

        /// <summary>
        /// Recomputes every gain. <paramref name="distance"/> is 07's horizontal distance from the base,
        /// <paramref name="signalEdge"/> where the radio's signal is lost entirely (both metres);
        /// <paramref name="stillness"/> and <paramref name="wide"/> are 0..1 (already eased).
        /// </summary>
        public void Step(float distance, float signalEdge, float stillness, float wide, float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            _farness.Step(FarnessAt(distance, signalEdge), dt, _tuning.FarnessEase);
            Stillness = Mathf.Clamp01(stillness);
            Wide = Mathf.Clamp01(wide);
            Solitude = Farness;

            float still = Stillness * _tuning.StillDuckDb + Wide * _tuning.WideDuckDb;
            RadioGain = FromDb(Farness * _tuning.FarRadioDb + still);
            BasinBedGain = FromDb(Farness * _tuning.FarBasinDb + still);
            RoomToneGain = FromDb(Mathf.Lerp(_tuning.RoomToneNearDb, _tuning.RoomToneFarDb, Solitude) +
                                  Wide * _tuning.WideRoomToneDb);
            SmallSoundsGain = FromDb(Mathf.Lerp(_tuning.SmallSoundsNearDb, 0f, Solitude));
        }

        /// <summary>07 was set down somewhere new unseen (a recovery): the distance takes effect at once.</summary>
        public void SettleAt(float distance, float signalEdge)
        {
            _farness.Snap(FarnessAt(distance, signalEdge));
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

        private float FarnessAt(float distance, float signalEdge)
        {
            return Smooth01((distance - signalEdge) / _tuning.SilenceWidth);
        }

        private static float Smooth01(float x)
        {
            float t = Mathf.Clamp01(x);
            return t * t * (3f - 2f * t);
        }
    }
}
