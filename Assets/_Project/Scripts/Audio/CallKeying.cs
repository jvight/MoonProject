using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Lumen's call sign as baked into the call's loops (tools/audio/sampled/call.py: LEAD_S, SHORT_S, LONG_S, GAP_S,
    /// three short and one long with soft edges), so the game can read where the sign is at a loop's playhead: the
    /// light beside Earth keys in phase with what is heard (<see cref="MoonProject.Core.ICallSignal"/>). The loops
    /// carry the call's sign at the same offset; relay.1's reply in the answered loop is not the sky's light.
    /// <c>CallKeyingTests</c> checks these against call.py.
    /// </summary>
    public static class CallKeying
    {
        /// <summary>Seconds of silence before the sign starts in each loop.</summary>
        public const float Lead = 0.2f;

        /// <summary>Length of each short tone, seconds.</summary>
        public const float Short = 0.16f;

        /// <summary>Length of the long tone, seconds.</summary>
        public const float Long = 0.7f;

        /// <summary>Silence between tones, seconds.</summary>
        public const float Gap = 0.14f;

        /// <summary>Longest soft edge of a tone, seconds (shorter for short tones: 30% of the tone).</summary>
        public const float MaxEdge = 0.03f;

        /// <summary>Share of a tone's length its soft edge may take at most.</summary>
        public const float EdgeShare = 0.3f;

        private const int Shorts = 3;

        /// <summary>
        /// How strongly the sign keys at <paramref name="playhead"/> seconds into a loop: 1 inside a tone, easing over
        /// its soft edges, 0 between tones, before the lead and after the long.
        /// </summary>
        public static float Envelope(float playhead)
        {
            float t = playhead - Lead;
            for (int i = 0; i <= Shorts; i++)
            {
                float start = i * (Short + Gap);
                float length = i < Shorts ? Short : Long;
                if (t >= start && t <= start + length)
                {
                    return Tone(t - start, length);
                }
            }

            return 0f;
        }

        private static float Tone(float into, float length)
        {
            float edge = Mathf.Min(MaxEdge, length * EdgeShare);
            float rise = Mathf.Clamp01(into / edge);
            float fall = Mathf.Clamp01((length - into) / edge);
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(rise, fall));
        }
    }
}
