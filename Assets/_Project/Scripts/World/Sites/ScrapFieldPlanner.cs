using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Lays out Kestrel-3's scrap field (0.5.5 §2): seeded hull fragments along the furrow from its far end to the
    /// crater's rim, either side of a clear middle lane, each on the ground with a yaw only (Art models the tilt and the
    /// buried part) and one of Art's variants. Plain logic, deterministic.
    /// </summary>
    public static class ScrapFieldPlanner
    {
        private const uint PieceSalt = 0x2F6B3A91u;

        public static List<ScrapPlate> Plan(MoonSurface surface, ScrapFieldSettings settings, int variants, int seed)
        {
            if (surface == null || settings == null)
            {
                throw new ArgumentNullException(nameof(surface), "The scrap field needs the surface and its settings.");
            }

            if (variants < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(variants), variants, "The scrap field needs a variant.");
            }

            KestrelImpact kestrel = surface.Kestrel;
            Vector2 toCrater = kestrel.Center - kestrel.TrailEnd;
            float length = Mathf.Max(toCrater.magnitude - kestrel.RimRadius, 0f);
            Vector2 along = toCrater.normalized;
            var across = new Vector2(along.y, -along.x);
            uint pieceSeed = Hashing.Mix((uint)seed ^ PieceSalt);
            var pieces = new List<ScrapPlate>(settings.Pieces);
            for (int i = 0; i < settings.Pieces; i++)
            {
                float t = Mathf.Lerp(settings.Span.x, settings.Span.y, (i + Unit(i, 0, pieceSeed)) / settings.Pieces);
                float side = Unit(i, 1, pieceSeed) < 0.5f ? -1f : 1f;
                float offset = side * (settings.LaneHalfWidth + settings.Spread * Unit(i, 2, pieceSeed));
                Vector2 spot = kestrel.TrailEnd + along * (length * t) + across * offset;
                float yaw = Unit(i, 3, pieceSeed) * 360f;
                int variant = Mathf.Min((int)(Unit(i, 4, pieceSeed) * variants), variants - 1);
                var position = new Vector3(spot.x, surface.SampleHeight(spot.x, spot.y), spot.y);
                pieces.Add(new ScrapPlate(position, Quaternion.Euler(0f, yaw, 0f), variant));
            }

            return pieces;
        }

        private static float Unit(int piece, int salt, uint seed)
        {
            return Hashing.ToUnit(Hashing.Hash(piece, salt, seed));
        }
    }
}
