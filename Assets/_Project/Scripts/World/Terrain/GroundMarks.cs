using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Lays out the marks pressed into the dust (M3-18 V4) into a <see cref="GroundMarkMesh"/>: 07's worn tracks (two
    /// ruts a gauge apart that wander gently, wear in patches and fade in and out at their ends), the lander's
    /// radial landing-blast scar and a short line of crew bootprints. Plain logic, seeded and deterministic.
    /// </summary>
    public static class GroundMarks
    {
        private const uint PatchSalt = 0x1B873593u;
        private const uint ScuffSalt = 0xCC9E2D51u;
        private const uint ScarSalt = 0x85EBCA6Bu;

        // The scar's streaks reach between this share of the scar radius and all of it.
        private const float ShortestStreak = 0.55f;

        // Most gradient noise lies within this much of zero; the wear stretches it to its full range.
        private const float NoiseReach = 0.3f;

        // The four rut edges sample the scuff noise this far apart, so none of them wear alike.
        private const float ScuffEdgeApart = 3.7f;

        // Bootprints step alternately this share of the stride either side of the walking line.
        private const float StepSideShare = 0.18f;

        // Streaks and bootprints vary in strength down to this share: the blast and the years were uneven.
        private const float FaintestStreak = 0.6f;
        private const float FaintestBootprint = 0.5f;

        /// <summary>A worn track from <paramref name="from"/> to <paramref name="to"/> (XZ), on the ground.</summary>
        /// <param name="groundHeight">The ground's height at (x, z).</param>
        public static void AddTrack(GroundMarkMesh marks, Vector2 from, Vector2 to, GroundMarkSettings settings,
            uint seed, Func<float, float, float> groundHeight)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < settings.Step * 2f)
            {
                return;
            }

            Vector2 direction = delta / length;
            var side = new Vector2(direction.y, -direction.x);
            var wander = new GradientNoise(seed);
            var patches = new GradientNoise(Hashing.Mix(seed ^ PatchSalt));
            var scuffs = new GradientNoise(Hashing.Mix(seed ^ ScuffSalt));
            int steps = Mathf.CeilToInt(length / settings.Step);
            var left = new List<Vector3>(steps + 1);
            var right = new List<Vector3>(steps + 1);
            var edges = new List<float>[4];
            for (int e = 0; e < edges.Length; e++)
            {
                edges[e] = new List<float>(steps + 1);
            }

            float halfGauge = settings.TrackGauge * 0.5f;
            for (int i = 0; i <= steps; i++)
            {
                float along = length * i / steps;
                float envelope = Mathf.Sin(Mathf.PI * i / steps);
                float offset = settings.Wander * envelope * wander.Sample(along / settings.WanderWavelength, 0.5f);
                Vector2 centre = from + direction * along + side * offset;
                float fade = Mathf.Min(SmoothMath.Smootherstep(0f, settings.EndFade, along),
                    SmoothMath.Smootherstep(0f, settings.EndFade, length - along));
                float wear = 1f - settings.Patchiness
                    * Contrast(patches.Sample(along / settings.PatchWavelength, 0.5f));
                float worn = settings.Strength * fade * wear;
                for (int e = 0; e < edges.Length; e++)
                {
                    // Each rut edge scuffs on its own at a short scale: the dust is pressed unevenly, along and across.
                    float scuff = scuffs.Sample(along / settings.ScuffWavelength, e * ScuffEdgeApart);
                    edges[e].Add(worn * (1f - settings.Scuff * Contrast(scuff)));
                }

                left.Add(OnGround(centre - side * halfGauge, settings.Lift, groundHeight));
                right.Add(OnGround(centre + side * halfGauge, settings.Lift, groundHeight));
            }

            marks.AddStrip(left, edges[0], edges[1], settings.RutHalfWidth);
            marks.AddStrip(right, edges[2], edges[3], settings.RutHalfWidth);
        }

        /// <summary>The landing-blast scar around <paramref name="centre"/>: streaks swept out radially.</summary>
        public static void AddScar(GroundMarkMesh marks, Vector3 centre, GroundMarkSettings settings, uint seed)
        {
            uint scarSeed = Hashing.Mix(seed ^ ScarSalt);
            var points = new List<Vector3>(2);
            var strength = new List<float> { settings.ScarStrength, 0f };
            for (int i = 0; i < settings.ScarStreaks; i++)
            {
                float angle = (i + 0.8f * Hashing.ToSigned(Hashing.Hash(i, 0, scarSeed)))
                    * Mathf.PI * 2f / settings.ScarStreaks;
                float reach = settings.ScarRadius
                    * Mathf.Lerp(ShortestStreak, 1f, Hashing.ToUnit(Hashing.Hash(i, 1, scarSeed)));
                var ray = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                points.Clear();
                points.Add(centre + ray * settings.ScarInner + Vector3.up * settings.Lift);
                points.Add(centre + ray * reach + Vector3.up * settings.Lift);
                strength[0] = settings.ScarStrength
                    * Mathf.Lerp(FaintestStreak, 1f, Hashing.ToUnit(Hashing.Hash(i, 2, scarSeed)));
                marks.AddStrip(points, strength, settings.ScarStreakHalfWidth);
            }
        }

        /// <summary>A short line of bootprints from <paramref name="start"/> along <paramref name="heading"/>.</summary>
        public static void AddBootprints(GroundMarkMesh marks, Vector3 start, Vector3 heading,
            GroundMarkSettings settings)
        {
            heading.y = 0f;
            heading.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, heading);
            for (int i = 0; i < settings.BootprintCount; i++)
            {
                float step = (i % 2 == 0 ? -1f : 1f) * settings.BootprintStride * StepSideShare;
                Vector3 centre = start + heading * (settings.BootprintStride * i) + side * step
                    + Vector3.up * settings.Lift;
                float age = 1f - (float)i / Mathf.Max(1, settings.BootprintCount);
                marks.AddQuad(centre, heading, settings.BootprintSize.x, settings.BootprintSize.y,
                    settings.BootprintStrength * Mathf.Lerp(FaintestBootprint, 1f, age));
            }
        }

        /// <summary>
        /// Gradient noise mostly stays within a few tenths of zero: stretched to 0..1 so the wear really comes and goes.
        /// </summary>
        private static float Contrast(float noise)
        {
            return SmoothMath.Smootherstep(-NoiseReach, NoiseReach, noise);
        }

        private static Vector3 OnGround(Vector2 point, float lift, Func<float, float, float> groundHeight)
        {
            return new Vector3(point.x, groundHeight(point.x, point.y) + lift, point.y);
        }
    }
}
