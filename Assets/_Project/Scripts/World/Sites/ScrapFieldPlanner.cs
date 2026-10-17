using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Plans Kestrel-3's scrap field (0.5.5 §2): the slots down the furrow's clear lane where Gameplay stands its
    /// cuttable hull fragments. Plain logic, deterministic.
    /// </summary>
    public static class ScrapFieldPlanner
    {
        private const uint CuttableSalt = 0x6C1D84E3u;

        // Art's curved hull skin on its ribs: never a cuttable slot's variant.
        private const int AnchorVariant = 0;

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
