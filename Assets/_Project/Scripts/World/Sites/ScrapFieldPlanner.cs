using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Lays out Kestrel-3's scrap field (0.5.5 §2): seeded hull plates along the furrow from its far end to the
    /// crater's rim, either side of a clear middle lane, each tilted out of the dust and sunk by a share of its width.
    /// Plain logic, deterministic.
    /// </summary>
    public static class ScrapFieldPlanner
    {
        private const uint PlateSalt = 0x2F6B3A91u;

        public static List<ScrapPlate> Plan(MoonSurface surface, ScrapFieldSettings settings, int seed)
        {
            if (surface == null || settings == null)
            {
                throw new ArgumentNullException(nameof(surface), "The scrap field needs the surface and its settings.");
            }

            KestrelImpact kestrel = surface.Kestrel;
            Vector2 crater = kestrel.Center;
            Vector2 toCrater = crater - kestrel.TrailEnd;
            float length = Mathf.Max(toCrater.magnitude - kestrel.RimRadius, 0f);
            Vector2 along = toCrater.normalized;
            var across = new Vector2(along.y, -along.x);
            uint plateSeed = Hashing.Mix((uint)seed ^ PlateSalt);
            var plates = new List<ScrapPlate>(settings.Plates);
            for (int i = 0; i < settings.Plates; i++)
            {
                float t = Mathf.Lerp(settings.Span.x, settings.Span.y, (i + Unit(i, 0, plateSeed)) / settings.Plates);
                float side = Unit(i, 1, plateSeed) < 0.5f ? -1f : 1f;
                float offset = side * (settings.LaneHalfWidth + settings.Spread * Unit(i, 2, plateSeed));
                Vector2 spot = kestrel.TrailEnd + along * (length * t) + across * offset;
                float plateLength = Mathf.Lerp(settings.Length.x, settings.Length.y, Unit(i, 3, plateSeed));
                float width = plateLength * Mathf.Lerp(settings.WidthShare.x, settings.WidthShare.y,
                    Unit(i, 4, plateSeed));
                float tilt = Mathf.Lerp(settings.Tilt.x, settings.Tilt.y, Unit(i, 5, plateSeed));
                float yaw = Unit(i, 6, plateSeed) * 360f;
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);

                // The plate's centre sinks so the given share of its tilted width lies under the dust.
                float rise = width * 0.5f * Mathf.Sin(tilt * Mathf.Deg2Rad);
                float centreHeight = rise * (1f - 2f * settings.Sink);
                float ground = surface.SampleHeight(spot.x, spot.y);
                plates.Add(new ScrapPlate(new Vector3(spot.x, ground + centreHeight, spot.y), rotation,
                    new Vector3(plateLength, settings.Thickness, width)));
            }

            return plates;
        }

        private static float Unit(int plate, int salt, uint seed)
        {
            return Hashing.ToUnit(Hashing.Hash(plate, salt, seed));
        }
    }
}
