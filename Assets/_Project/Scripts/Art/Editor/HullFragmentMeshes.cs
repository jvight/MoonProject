using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kestrel-3's hull fragments for its scrap field (0.5.5 §2): pieces of a spacecraft's skin that read as such from
    /// 20 m, a curved skin section on its ribs, a torn plate with a jagged edge, a panel with its hatch, and a bare rib
    /// cage with a scrap of skin. Fragment space: the origin where the dust line crosses the piece's middle, +Y up,
    /// +Z its exposed side; everything below y 0 is meant to sit under the dust.
    /// </summary>
    internal static class HullFragmentMeshes
    {
        public const int Variants = 4;

        private const float SkinThickness = 0.04f;
        private const float RibSection = 0.07f;

        /// <summary>The cut point on each fragment's exposed edge (fragment space), +Z out of the cut face.</summary>
        public static Vector3 CutPoint(int variant)
        {
            switch (variant)
            {
                case 0:
                    return new Vector3(0.2f, 0.75f, 0.55f);
                case 1:
                    return new Vector3(0.1f, 0.42f, 0.55f);
                case 2:
                    return new Vector3(-0.35f, 0.5f, 0.12f);
                default:
                    return new Vector3(0f, 0.85f, 0.2f);
            }
        }

        public static LowPolyMeshBuilder Fragment(int variant)
        {
            switch (variant)
            {
                case 0:
                    return CurvedSkin();
                case 1:
                    return TornPlate();
                case 2:
                    return HatchPanel();
                case 3:
                    return RibCage();
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(variant), variant, "Unknown hull fragment.");
            }
        }

        /// <summary>A curved section of the hull, its outer skin in faded enamel with the orange band, three ribs
        /// inside, lying on its side half-sunk.</summary>
        private static LowPolyMeshBuilder CurvedSkin()
        {
            var b = new LowPolyMeshBuilder(900);
            Matrix4x4 lie = At(new Vector3(-0.2f, -0.45f, 0f), new Vector3(0f, 0f, 12f));
            const float radius = 1.3f;
            const float length = 2.2f;
            const int steps = 6;
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.Lerp(-5f, 95f, (i + 0.5f) / steps);
                float width = 2f * radius * Mathf.Sin(100f / steps * 0.5f * Mathf.Deg2Rad) + 0.01f;
                Matrix4x4 panel = lie * At(Polar(a, radius), new Vector3(0f, 0f, a - 90f));
                MeshRange skin = b.Box(panel, new Vector3(width, SkinThickness, length),
                    i == 3 ? PaletteSwatch.FadedAccent : PaletteSwatch.FadedPaint);
                SiteKit.DustCap(b, skin);
            }

            for (int rib = -1; rib <= 1; rib++)
            {
                Vector3 previous = Polar(-5f, radius - 0.06f) + Vector3.forward * rib * 0.85f;
                for (int i = 1; i <= steps; i++)
                {
                    Vector3 next = Polar(Mathf.Lerp(-5f, 95f, (float)i / steps), radius - 0.06f) +
                                   Vector3.forward * rib * 0.85f;
                    b.Box(lie * Along(previous, next), new Vector3(RibSection, RibSection,
                        Vector3.Distance(previous, next) + RibSection), PaletteSwatch.Metal);
                    previous = next;
                }
            }

            Matrix4x4 face = lie * At(Polar(62f, radius + 0.025f), new Vector3(0f, 0f, -28f));
            SiteKit.RustStreak(b, face * At(Vector3.zero, new Vector3(-90f, 0f, 0f)), -0.3f, 0.2f, 0.35f);
            return b;
        }

        /// <summary>A flat hull plate ripped off along a jagged edge, stringers on its back, bent up from the
        /// dust.</summary>
        private static LowPolyMeshBuilder TornPlate()
        {
            var b = new LowPolyMeshBuilder(500);
            var outline = new[]
            {
                new Vector2(-1f, -0.7f), new Vector2(1f, -0.7f), new Vector2(1f, 0.25f), new Vector2(0.78f, 0.42f),
                new Vector2(0.62f, 0.3f), new Vector2(0.45f, 0.58f), new Vector2(0.18f, 0.44f),
                new Vector2(0.02f, 0.66f),
                new Vector2(-0.24f, 0.5f), new Vector2(-0.46f, 0.7f), new Vector2(-0.7f, 0.48f), new Vector2(-1f, 0.55f),
            };
            Matrix4x4 plate = At(new Vector3(0f, 0.12f, 0f), new Vector3(-48f, 18f, 0f));
            MeshRange skin = b.Extrude(plate, outline, SkinThickness, PaletteSwatch.WornPanel);
            SiteKit.DustCap(b, skin);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 0.38f;
                b.Box(plate * At(x, -0.12f, -0.05f), new Vector3(0.05f, 1.1f, 0.06f), PaletteSwatch.Metal);
            }

            b.Box(plate * At(-0.3f, -0.35f, SkinThickness * 0.5f + 0.003f), new Vector3(1.2f, 0.12f, 0.006f),
                PaletteSwatch.FadedAccent);
            return b;
        }

        /// <summary>A hull panel with its crew hatch still in its frame, the wheel on it, leaning half out of the
        /// dust.</summary>
        private static LowPolyMeshBuilder HatchPanel()
        {
            var b = new LowPolyMeshBuilder(700);
            Matrix4x4 panel = At(new Vector3(0f, 0.2f, 0f), new Vector3(-55f, -10f, 6f));
            MeshRange skin = b.Box(panel, new Vector3(1.7f, 1.4f, SkinThickness), PaletteSwatch.FadedPaint, 0.01f);
            SiteKit.DustCap(b, skin);
            Matrix4x4 hatch = panel * At(0.15f, 0.1f, SkinThickness * 0.5f);
            b.Prism(hatch * At(Vector3.zero, new Vector3(90f, 0f, 0f)), 0.42f, 0.05f, 12, PaletteSwatch.Metal);
            b.Prism(hatch * At(new Vector3(0f, 0f, 0.03f), new Vector3(90f, 0f, 0f)), 0.36f, 0.03f, 12,
                PaletteSwatch.WornPanel);
            b.Torus(hatch * At(new Vector3(0f, 0f, 0.07f), new Vector3(90f, 0f, 0f)), 0.14f, 0.018f, 10, 4,
                PaletteSwatch.Charcoal);
            for (int i = 0; i < 2; i++)
            {
                b.Box(hatch * At(new Vector3(0f, 0f, 0.07f), new Vector3(0f, 0f, i * 90f)), new Vector3(0.26f, 0.025f,
                    0.025f), PaletteSwatch.Charcoal);
            }

            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                b.Icosphere(hatch * At(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.46f), 0.02f, 0,
                    PaletteSwatch.Metal);
            }

            b.Box(panel * At(-0.5f, -0.45f, SkinThickness * 0.5f + 0.003f), new Vector3(0.5f, 0.1f, 0.006f),
                PaletteSwatch.FadedAccent);
            return b;
        }

        /// <summary>One side of the hull's frame: bare ribs curving up out of the dust, stringers across them and
        /// two scraps of skin still riveted on.</summary>
        private static LowPolyMeshBuilder RibCage()
        {
            var b = new LowPolyMeshBuilder(800);
            const float radius = 1.1f;
            const int steps = 5;
            for (int rib = 0; rib < 3; rib++)
            {
                float z = -0.7f + rib * 0.7f;
                float tilt = (rib - 1) * 6f;
                Matrix4x4 frame = At(new Vector3(0f, -0.25f, z), new Vector3(tilt, 0f, 8f));
                Vector3 previous = Polar(-12f, radius);
                for (int i = 1; i <= steps; i++)
                {
                    Vector3 next = Polar(Mathf.Lerp(-12f, 100f - rib * 12f, (float)i / steps), radius);
                    b.Box(frame * Along(previous, next), new Vector3(RibSection, RibSection,
                        Vector3.Distance(previous, next) + RibSection), PaletteSwatch.Metal);
                    previous = next;
                }
            }

            foreach (float a in new[] { 20f, 55f, 90f })
            {
                Vector3 p = Polar(a, radius) + new Vector3(0f, -0.25f, 0f);
                b.Box(At(new Vector3(p.x, p.y, 0f), new Vector3(0f, 0f, a - 90f)), new Vector3(0.06f, 0.06f, 1.55f),
                    PaletteSwatch.Charcoal);
            }

            foreach ((float a, float z, float length) in new[] { (32f, -0.35f, 0.75f), (70f, 0.3f, 0.55f) })
            {
                Vector3 scrap = Polar(a, radius + 0.035f) + new Vector3(0f, -0.25f, z);
                MeshRange skin = b.Box(At(scrap, new Vector3(0f, 0f, a - 90f)), new Vector3(0.6f, SkinThickness, length),
                    PaletteSwatch.FadedPaint);
                SiteKit.DustCap(b, skin);
            }

            return b;
        }

        private static Vector3 Polar(float degrees, float radius)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }
    }
}
