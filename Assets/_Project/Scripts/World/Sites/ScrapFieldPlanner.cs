using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Lays out Kestrel-3's scrap field (0.5.5 §2): seeded hull fragments along the furrow from its far end to the
    /// crater's rim, never inside its clear middle lane, crowding the impact end. Scenery pieces lie out to the sides,
    /// sunk deep and tilted further, a few big curved skins as the wreck's anchors; the first
    /// <see cref="ScrapFieldSettings.Hosts"/> pieces stand upright just beyond the lane's edge, spread along the
    /// furrow, to host the Warm Headlamp's scan fragments. Plain logic, deterministic.
    /// </summary>
    public static class ScrapFieldPlanner
    {
        private const uint PieceSalt = 0x2F6B3A91u;

        // Art's curved hull skin on its ribs: the anchor pieces' variant.
        private const int AnchorVariant = 0;

        // The anchor skins lie in this first share of the furrow, by the impact end.
        private const float AnchorReach = 0.35f;

        // Art's fragments are about 2 m across: a bigger one stands this much further out per unit of extra scale, so
        // it never reaches into the lane.
        private const float FragmentHalfSize = 1f;

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
            int anchors = Mathf.Min(settings.Anchors, settings.Pieces - hosts);
            int scattered = Mathf.Max(1, settings.Pieces - hosts - anchors);
            var pieces = new List<ScrapPlate>(settings.Pieces);
            for (int i = 0; i < settings.Pieces; i++)
            {
                bool host = i < hosts;
                bool anchor = !host && i < hosts + anchors;
                float slot = host ? (i + 1f) / (hosts + 1f)
                    : anchor ? AnchorReach * (i - hosts + Unit(i, 0, pieceSeed)) / Mathf.Max(1, anchors)
                    : Mathf.Pow((i - hosts - anchors + Unit(i, 0, pieceSeed)) / scattered, settings.Crowding);
                float t = Mathf.Lerp(settings.Span.x, settings.Span.y, slot);
                int variant = anchor ? Mathf.Min(AnchorVariant, variants - 1)
                    : Mathf.Min((int)(Unit(i, 4, pieceSeed) * variants), variants - 1);
                Vector2 range = anchor ? settings.AnchorScale : settings.Scale;
                float scale = host ? 1f : Mathf.Lerp(range.x, range.y, Unit(i, 6, pieceSeed));
                float side = Unit(i, 1, pieceSeed) < 0.5f ? -1f : 1f;
                float beyond = settings.HostOffset + (scale - 1f) * FragmentHalfSize
                    + (host ? 0f : settings.Spread * Unit(i, 2, pieceSeed));
                Vector2 spot = kestrel.TrailEnd + along * (length * t)
                    + across * (side * (settings.LaneHalfWidth + beyond));
                float yaw = Unit(i, 3, pieceSeed) * 360f;
                float tilt = host ? 0f : Mathf.Lerp(settings.Tilt.x, settings.Tilt.y, Unit(i, 5, pieceSeed));
                float sink = host ? 0f : settings.Sink * scale;
                var position = new Vector3(spot.x, surface.SampleHeight(spot.x, spot.y) - sink, spot.y);
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
                pieces.Add(new ScrapPlate(position, rotation, variant, scale, host));
            }

            return pieces;
        }

        private static float Unit(int piece, int salt, uint seed)
        {
            return Hashing.ToUnit(Hashing.Hash(piece, salt, seed));
        }
    }
}
