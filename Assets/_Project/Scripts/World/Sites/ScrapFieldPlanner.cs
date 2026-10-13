using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Lays out Kestrel-3's scrap field (0.5.5 §2): seeded hull fragments along the furrow from its far end to the
    /// crater's rim, never inside its clear middle lane. Scenery pieces lie out to the sides, sunk deep and tilted
    /// further; the first <see cref="ScrapFieldSettings.Hosts"/> pieces stand upright just beyond the lane's edge,
    /// spread along the furrow, to host the Warm Headlamp's scan fragments. Plain logic, deterministic.
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
            int hosts = Mathf.Min(settings.Hosts, settings.Pieces);
            var pieces = new List<ScrapPlate>(settings.Pieces);
            for (int i = 0; i < settings.Pieces; i++)
            {
                bool host = i < hosts;
                float slot = host ? (i + 1f) / (hosts + 1f)
                    : (i - hosts + Unit(i, 0, pieceSeed)) / Mathf.Max(1, settings.Pieces - hosts);
                float t = Mathf.Lerp(settings.Span.x, settings.Span.y, slot);
                float side = Unit(i, 1, pieceSeed) < 0.5f ? -1f : 1f;
                float beyond = settings.HostOffset + (host ? 0f : settings.Spread * Unit(i, 2, pieceSeed));
                Vector2 spot = kestrel.TrailEnd + along * (length * t)
                    + across * (side * (settings.LaneHalfWidth + beyond));
                float yaw = Unit(i, 3, pieceSeed) * 360f;
                int variant = Mathf.Min((int)(Unit(i, 4, pieceSeed) * variants), variants - 1);
                float tilt = host ? 0f : Mathf.Lerp(settings.Tilt.x, settings.Tilt.y, Unit(i, 5, pieceSeed));
                float sink = host ? 0f : settings.Sink;
                var position = new Vector3(spot.x, surface.SampleHeight(spot.x, spot.y) - sink, spot.y);
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
                pieces.Add(new ScrapPlate(position, rotation, variant, host));
            }

            return pieces;
        }

        private static float Unit(int piece, int salt, uint seed)
        {
            return Hashing.ToUnit(Hashing.Hash(piece, salt, seed));
        }
    }
}
