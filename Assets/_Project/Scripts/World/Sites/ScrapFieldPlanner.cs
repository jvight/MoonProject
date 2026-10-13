using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Lays out Kestrel-3's scrap field (0.5.5 §2): seeded hull fragments along the furrow from its far end to the
    /// crater's rim, never inside its clear middle lane, crowding the impact end. Scenery pieces lie out to the sides,
    /// sunk deep and tilted further, a few big curved skins as the wreck's anchors. Plain logic, deterministic.
    /// </summary>
    public static class ScrapFieldPlanner
    {
        private const uint PieceSalt = 0x2F6B3A91u;
        private const uint CuttableSalt = 0x6C1D84E3u;

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
            int anchors = Mathf.Min(settings.Anchors, settings.Pieces);
            int scattered = Mathf.Max(1, settings.Pieces - anchors);
            var pieces = new List<ScrapPlate>(settings.Pieces);
            for (int i = 0; i < settings.Pieces; i++)
            {
                bool anchor = i < anchors;
                float slot = anchor ? AnchorReach * (i + Unit(i, 0, pieceSeed)) / Mathf.Max(1, anchors)
                    : Mathf.Pow((i - anchors + Unit(i, 0, pieceSeed)) / scattered, settings.Crowding);
                float t = Mathf.Lerp(settings.Span.x, settings.Span.y, slot);
                int variant = anchor ? Mathf.Min(AnchorVariant, variants - 1)
                    : Mathf.Min((int)(Unit(i, 4, pieceSeed) * variants), variants - 1);
                Vector2 range = anchor ? settings.AnchorScale : settings.Scale;
                float scale = Mathf.Lerp(range.x, range.y, Unit(i, 6, pieceSeed));
                float side = Unit(i, 1, pieceSeed) < 0.5f ? -1f : 1f;
                float beyond = settings.LaneMargin + (scale - 1f) * FragmentHalfSize
                    + settings.Spread * Unit(i, 2, pieceSeed);
                Vector2 spot = kestrel.TrailEnd + along * (length * t)
                    + across * (side * (settings.LaneHalfWidth + beyond));
                float yaw = Unit(i, 3, pieceSeed) * 360f;
                float tilt = Mathf.Lerp(settings.Tilt.x, settings.Tilt.y, Unit(i, 5, pieceSeed));
                float sink = settings.Sink * scale;
                var position = new Vector3(spot.x, surface.SampleHeight(spot.x, spot.y) - sink, spot.y);
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
                pieces.Add(new ScrapPlate(position, rotation, variant, scale));
            }

            return pieces;
        }

        /// <summary>
        /// The cuttable slots in the clear lane (Gameplay's salvage pieces): evenly spaced down the furrow, a little
        /// off its middle, shallow and gently tilted, at scale 1. Never Art's anchor skin.
        /// </summary>
        public static List<ScrapPlate> PlanCuttables(MoonSurface surface, ScrapFieldSettings settings, int variants,
            int seed)
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
            uint slotSeed = Hashing.Mix((uint)seed ^ CuttableSalt);
            float offset = Mathf.Min(settings.CuttableOffset, settings.LaneHalfWidth);
            int first = variants > 1 ? AnchorVariant + 1 : AnchorVariant;
            var slots = new List<ScrapPlate>(settings.Cuttables);
            for (int i = 0; i < settings.Cuttables; i++)
            {
                float share = settings.Cuttables > 1 ? i / (float)(settings.Cuttables - 1) : 0.5f;
                float t = Mathf.Lerp(settings.CuttableSpan.x, settings.CuttableSpan.y, share);
                float side = (Unit(i, 1, slotSeed) * 2f - 1f) * offset;
                Vector2 spot = kestrel.TrailEnd + along * (length * t) + across * side;
                int variant = first + i % (variants - first);
                float yaw = Unit(i, 3, slotSeed) * 360f;
                float tilt = Mathf.Lerp(settings.CuttableTilt.x, settings.CuttableTilt.y, Unit(i, 5, slotSeed));
                var position = new Vector3(spot.x, surface.SampleHeight(spot.x, spot.y) - settings.CuttableSink,
                    spot.y);
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
                slots.Add(new ScrapPlate(position, rotation, variant, 1f));
            }

            return slots;
        }

        private static float Unit(int piece, int salt, uint seed)
        {
            return Hashing.ToUnit(Hashing.Hash(piece, salt, seed));
        }
    }
}
