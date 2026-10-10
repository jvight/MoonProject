using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Where rust runs hang along one seam of a weathered wall (VISION ruling 12, M3-15 §3): only a few, at random
    /// gaps, each from a cause (the seam itself or a bolt a little under it), their lengths and widths varied widely
    /// and some only a stain at the bolt. Two neighbours never share a length, and two neighbouring gaps never share a
    /// spacing, so a row of runs never reads as a printed trim.
    /// </summary>
    public static class RustRunPlan
    {
        /// <summary>
        /// Neighbouring runs' lengths, and neighbouring gaps, differ by at least this share of the larger.
        /// </summary>
        public const float MinContrast = 0.3f;

        private const float Margin = 0.05f;

        // Gaps between runs, as shares of the mean spacing a profile asks for: few runs, at random gaps.
        private const float MinGapShare = 0.8f;
        private const float MaxGapShare = 3.2f;
        private const float WiderGap = 1.6f;
        private const float NarrowerGap = 0.6f;
        private const float StainShare = 0.25f;
        private const float MinStain = 0.03f;
        private const float MaxStain = 0.08f;
        private const float MinRunShare = 0.12f;
        private const float LengthSkew = 1.7f;
        private const float BoltShare = 0.5f;
        private const float MinBoltDrop = 0.02f;
        private const float MaxBoltDrop = 0.1f;
        private const float MinWidth = 0.025f;
        private const float MaxWidth = 0.12f;
        private const float Longer = 2.2f;
        private const float Shorter = 0.45f;

        /// <summary>One run: its centre across the wall, its width, where it starts and how far it hangs.</summary>
        public readonly struct Run
        {
            public Run(float x, float width, float top, float length)
            {
                X = x;
                Width = width;
                Top = top;
                Length = length;
            }

            public float X { get; }

            public float Width { get; }

            public float Top { get; }

            public float Length { get; }
        }

        /// <summary>
        /// Plans the runs along a seam at height <paramref name="seam"/> from <paramref name="left"/> to
        /// <paramref name="right"/> (wall coordinates), hanging at most <paramref name="drop"/>, about
        /// <paramref name="perMetre"/> causes per metre once the stain-free gaps are counted, seeded by
        /// <paramref name="seed"/>; adds them to <paramref name="runs"/> left to right.
        /// </summary>
        public static void Plan(float left, float right, float seam, float drop, float perMetre, int seed,
            List<Run> runs)
        {
            var random = new Xorshift(seed);
            float minGap = MinGapShare / perMetre;
            float maxGap = MaxGapShare / perMetre;
            float x = left + Margin + random.Range(0f, maxGap * 0.5f);
            float previousLength = -1f;
            float previousGap = -1f;
            float shortest = Mathf.Min(MinStain, drop);
            while (x < right - Margin && drop > shortest)
            {
                float length = random.Next01() < StainShare
                    ? random.Range(MinStain, MaxStain)
                    : Mathf.Lerp(MinRunShare, 1f, Mathf.Pow(random.Next01(), LengthSkew)) * drop;
                length = Mathf.Clamp(length, shortest, drop);
                if (previousLength > 0f && !Contrasts(length, previousLength))
                {
                    bool canGrow = previousLength * Longer <= drop;
                    bool canShrink = previousLength * Shorter >= shortest;
                    length = canGrow && (!canShrink || random.Next01() < 0.5f)
                        ? previousLength * Longer
                        : previousLength * Shorter;
                }

                float top = seam - (random.Next01() < BoltShare ? random.Range(MinBoltDrop, MaxBoltDrop) : 0f);
                runs.Add(new Run(x, random.Range(MinWidth, MaxWidth), top, length));
                previousLength = length;

                float gap = random.Range(minGap, maxGap);
                if (previousGap > 0f && !Contrasts(gap, previousGap))
                {
                    gap = previousGap * WiderGap <= maxGap ? previousGap * WiderGap : previousGap * NarrowerGap;
                }

                previousGap = gap;
                x += gap;
            }
        }

        /// <summary>Whether two lengths (or gaps) differ by at least <see cref="MinContrast"/> of the larger.</summary>
        public static bool Contrasts(float a, float b)
        {
            return Mathf.Abs(a - b) >= MinContrast * Mathf.Max(a, b);
        }
        /// <summary>A tiny repeatable random sequence (xorshift32) for one seam.</summary>
        private sealed class Xorshift
        {
            private uint _state;

            public Xorshift(int seed)
            {
                _state = (uint)seed * 0x9E3779B1u | 1u;
            }

            public float Next01()
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return (_state & 0xFFFFFF) / 16777216f;
            }

            public float Range(float min, float max)
            {
                return min + (max - min) * Next01();
            }
        }
    }
}
