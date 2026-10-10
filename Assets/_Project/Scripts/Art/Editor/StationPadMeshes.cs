using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// A station's pad (M3-16, from M3-15 §4): a worn orange circle painted on the dust, broken into arcs of uneven
    /// length with uneven gaps, dust drifted over parts of it, and small flush pad lamps round its rim that light only
    /// while the station is awake and has something for 07 to do. Built in pad space: centre on the ground, +Y up.
    /// </summary>
    internal static class StationPadMeshes
    {
        public const int LampCount = 6;

        private const float PaintWidth = 0.12f;
        private const float PaintLift = 0.004f;
        private const float PaintThickness = 0.006f;
        private const float StepDegrees = 6f;
        private const float MinArc = 20f;
        private const float MaxArc = 75f;
        private const float MinGap = 6f;
        private const float MaxGap = 22f;
        private const float LampInset = 0.22f;
        private const float LampRadius = 0.045f;

        /// <summary>
        /// Where pad lamp <paramref name="index"/> sits on a pad of <paramref name="radius"/> (pad space).
        /// </summary>
        public static Vector3 Lamp(float radius, int index)
        {
            float angle = (index + 0.5f) * 360f / LampCount * Mathf.Deg2Rad;
            float r = radius - LampInset;
            return new Vector3(Mathf.Sin(angle) * r, 0.02f, Mathf.Cos(angle) * r);
        }

        /// <summary>The painted circle, its drifts and the lamps' flush housings, for a pad of this radius.</summary>
        public static LowPolyMeshBuilder Pad(float radius, int seed)
        {
            var b = new LowPolyMeshBuilder(600);
            var random = new Hash(seed);
            float at = random.Range(0f, 40f);
            while (at < 360f - MinGap)
            {
                float arc = Mathf.Min(random.Range(MinArc, MaxArc), 360f - at);
                int steps = Mathf.Max(1, Mathf.RoundToInt(arc / StepDegrees));
                for (int k = 0; k < steps; k++)
                {
                    float a0 = (at + arc * k / steps) * Mathf.Deg2Rad;
                    float a1 = (at + arc * (k + 1) / steps) * Mathf.Deg2Rad;
                    var from = new Vector3(Mathf.Sin(a0) * radius, PaintLift, Mathf.Cos(a0) * radius);
                    var to = new Vector3(Mathf.Sin(a1) * radius, PaintLift, Mathf.Cos(a1) * radius);
                    b.Box(Along(from, to), new Vector3(PaintWidth, PaintThickness, Vector3.Distance(from, to) + 0.01f),
                        PaletteSwatch.FadedAccent);
                }

                at += arc + random.Range(MinGap, MaxGap);
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = random.Range(0f, 360f) * Mathf.Deg2Rad;
                var centre = new Vector3(Mathf.Sin(angle) * radius, PaintLift + 0.004f, Mathf.Cos(angle) * radius);
                b.Box(At(centre, new Vector3(0f, angle * Mathf.Rad2Deg + 90f, 0f)),
                    new Vector3(PaintWidth + 0.08f, 0.012f, random.Range(0.25f, 0.6f)), PaletteSwatch.CakedDust,
                    0.004f);
            }

            for (int i = 0; i < LampCount; i++)
            {
                b.Prism(At(Lamp(radius, i) + Vector3.down * 0.01f), LampRadius + 0.02f, 0.02f, 8,
                    PaletteSwatch.Metal);
            }

            return b;
        }

        /// <summary>One pad lamp's glass, a low dome centred on its lamp point.</summary>
        public static LowPolyMeshBuilder LampGlass()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(Matrix4x4.identity, LampRadius, 0.022f, 8, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>A tiny repeatable sequence for one pad's paint (xorshift32).</summary>
        private sealed class Hash
        {
            private uint _state;

            public Hash(int seed)
            {
                _state = (uint)seed * 0x9E3779B1u | 1u;
            }

            public float Range(float min, float max)
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return min + (max - min) * ((_state & 0xFFFFFF) / 16777216f);
            }
        }
    }
}
