using System;
using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The 18 scannable fragments of 0.5.5's blueprints (owner feedback §1.3), each a broken machine that fits where it
    /// lies: Kenji's half-built rack frames, Kestrel-3's burst lamp and torn reflector, the crashed jumper's coils and
    /// leg, the drill rig's cracked capacitor bank, the relay hut's spare cells, the cockpit's broken lens stack and the
    /// lander's ripped panniers. Built upright here (fragment space: its footprint's centre on the ground, +Z its
    /// front); <see cref="FragmentBuilder"/> half-tilts and grounds each one.
    /// </summary>
    internal static class FragmentMeshes
    {
        /// <summary>The blueprints, named as RoverAbility's members, and how many fragments each has.</summary>
        public static readonly (string Blueprint, int Count)[] Sets =
        {
            ("CargoCradle", 2), ("WarmHeadlamp", 2), ("BoostCoils", 3), ("CellPack", 3), ("FocusLens", 3),
            ("SideBins", 2),
        };

        /// <summary>How each fragment lies on its surface: a tilt about X then Z, in degrees.</summary>
        public static Vector2 Tilt(string blueprint, int index)
        {
            return new Vector2(12f + 9f * ((index + blueprint.Length) % 3), -8f + 11f * (index % 2));
        }

        public static LowPolyMeshBuilder Fragment(string blueprint, int index)
        {
            switch (blueprint + "_" + index)
            {
                case "CargoCradle_0":
                    return RackFrame(0.8f, 0.42f, true);
                case "CargoCradle_1":
                    return RackCorner();
                case "WarmHeadlamp_0":
                    return LampHousing();
                case "WarmHeadlamp_1":
                    return Reflector();
                case "BoostCoils_0":
                    return Capacitor(0.18f, 0.7f, true);
                case "BoostCoils_1":
                    return CapacitorPair();
                case "BoostCoils_2":
                    return CapacitorBank();
                case "CellPack_0":
                    return CellModule(0.34f);
                case "CellPack_1":
                    return CellModule(0.3f);
                case "CellPack_2":
                    return CellRail();
                case "FocusLens_0":
                    return LensBarrel();
                case "FocusLens_1":
                    return LensRing();
                case "FocusLens_2":
                    return CameraBody();
                case "SideBins_0":
                    return Pannier(0.75f);
                case "SideBins_1":
                    return PannierPair();
                default:
                    throw new ArgumentOutOfRangeException(nameof(blueprint), $"{blueprint} has no fragment {index}.");
            }
        }

        private static LowPolyMeshBuilder RackFrame(float length, float width, bool slats)
        {
            var b = new LowPolyMeshBuilder(400);
            for (int side = -1; side <= 1; side += 2)
            {
                RecipeKit.Rod(b, new Vector3(side * width * 0.5f, 0.03f, -length * 0.5f),
                    new Vector3(side * width * 0.5f, 0.03f, length * 0.5f), 0.022f, 6, PaletteSwatch.Metal);
                RecipeKit.Rod(b, new Vector3(-width * 0.5f, 0.03f, side * length * 0.5f),
                    new Vector3(width * 0.5f, 0.03f, side * length * 0.5f), 0.022f, 6, PaletteSwatch.Metal);
                RecipeKit.Rod(b, new Vector3(side * width * 0.5f, 0.03f, length * 0.5f),
                    new Vector3(side * width * 0.5f, 0.28f, length * 0.5f), 0.02f, 6, PaletteSwatch.Metal);
            }

            if (slats)
            {
                for (int i = 0; i < 3; i++)
                {
                    b.Box(At(0f, 0.05f, -length * 0.3f + i * length * 0.25f), new Vector3(width, 0.02f, 0.08f),
                        PaletteSwatch.Wood);
                }
            }

            return b;
        }

        private static LowPolyMeshBuilder RackCorner()
        {
            LowPolyMeshBuilder b = RackFrame(0.5f, 0.5f, false);
            b.Box(At(0.25f, 0.16f, 0f), new Vector3(0.02f, 0.18f, 0.48f), PaletteSwatch.Wood);
            b.Box(At(0f, 0.2f, 0.25f), new Vector3(0.48f, 0.03f, 0.02f), PaletteSwatch.Rust);
            SiteKit.Bar(b, new Vector3(-0.25f, 0.28f, 0.25f), new Vector3(0.18f, 0.05f, -0.2f), 0.02f,
                PaletteSwatch.Charcoal);
            return b;
        }

        private static LowPolyMeshBuilder LampHousing()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Frustum(At(new Vector3(0f, 0.13f, 0f), new Vector3(90f, 0f, 0f)), 0.11f, 0.14f, 0.2f, 10,
                PaletteSwatch.Charcoal);
            b.Torus(At(new Vector3(0f, 0.13f, 0.1f), new Vector3(90f, 0f, 0f)), 0.135f, 0.014f, 10, 3,
                PaletteSwatch.Metal);
            b.Icosphere(At(new Vector3(0.03f, 0.15f, 0.1f), Quaternion.identity, new Vector3(1f, 1f, 0.25f)), 0.1f, 1,
                PaletteSwatch.SkyHorizon, new Displacement(4, 0.02f, 20f));
            b.Box(At(0f, 0.02f, -0.06f), new Vector3(0.14f, 0.04f, 0.12f), PaletteSwatch.Metal);
            return b;
        }

        private static LowPolyMeshBuilder Reflector()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Frustum(At(new Vector3(0f, 0.12f, 0f), new Vector3(70f, 0f, 0f)), 0.06f, 0.34f, 0.16f, 12,
                PaletteSwatch.Metal, false, new Displacement(9, 0.025f, 4f));
            b.Torus(At(new Vector3(0f, 0.18f, 0.06f), new Vector3(70f, 0f, 0f)), 0.34f, 0.015f, 12, 3,
                PaletteSwatch.Charcoal);
            SiteKit.Bar(b, new Vector3(0f, 0.04f, -0.1f), new Vector3(0.22f, 0.02f, -0.3f), 0.025f, PaletteSwatch.Metal);
            return b;
        }

        private static LowPolyMeshBuilder Capacitor(float radius, float length, bool cracked)
        {
            var b = new LowPolyMeshBuilder(300);
            Matrix4x4 axis = At(new Vector3(0f, radius, 0f), new Vector3(0f, 0f, 90f));
            b.Prism(axis, radius, length, 10, PaletteSwatch.Cream);
            for (int i = -1; i <= 1; i++)
            {
                b.Prism(axis * At(0f, i * length * 0.35f, 0f), radius + 0.008f, 0.03f, 10, PaletteSwatch.Honey);
            }

            if (cracked)
            {
                b.Box(At(new Vector3(length * 0.15f, radius * 1.9f, 0f), new Vector3(0f, 25f, 10f)),
                    new Vector3(0.18f, 0.01f, 0.02f), PaletteSwatch.Charcoal);
            }

            return b;
        }

        private static LowPolyMeshBuilder CapacitorPair()
        {
            var b = new LowPolyMeshBuilder(600);
            b.Box(At(0f, 0.015f, 0f), new Vector3(0.7f, 0.03f, 0.42f), PaletteSwatch.Charcoal, 0.006f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Append(Capacitor(0.09f, 0.6f, side > 0), Matrix4x4.Translate(new Vector3(0f, 0.03f, side * 0.1f)));
            }

            return b;
        }

        private static LowPolyMeshBuilder CapacitorBank()
        {
            var b = new LowPolyMeshBuilder(1200);
            b.Box(At(0f, 0.02f, 0f), new Vector3(1.24f, 0.04f, 0.68f), PaletteSwatch.WornPanel);
            for (int x = -1; x <= 1; x += 2)
            {
                b.Box(At(x * 0.6f, 0.22f, -0.3f), new Vector3(0.05f, 0.4f, 0.08f), PaletteSwatch.Metal);
                b.Box(At(x * 0.6f, 0.12f, 0.05f), new Vector3(0.04f, 0.2f, 0.5f), PaletteSwatch.Charcoal);
            }

            var bend = new Vector3(0.25f, 0.5f, -0.28f);
            SiteKit.Bar(b, new Vector3(-0.6f, 0.42f, -0.3f), bend, 0.035f, PaletteSwatch.Metal);
            SiteKit.Bar(b, bend, new Vector3(0.48f, 0.3f, -0.1f), 0.035f, PaletteSwatch.Rust);
            for (int i = 0; i < 3; i++)
            {
                b.Append(Capacitor(0.11f, 1f, i == 1), Matrix4x4.Translate(new Vector3(0f, 0.04f + (i == 2 ? 0.24f : 0f),
                    -0.16f + i * 0.16f)));
            }

            return b;
        }

        private static LowPolyMeshBuilder CellModule(float length)
        {
            var b = new LowPolyMeshBuilder(160);
            b.Box(At(0f, 0.08f, 0f), new Vector3(length, 0.16f, 0.16f), PaletteSwatch.Cream, 0.015f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * length * 0.3f, 0.08f, 0f), new Vector3(0.025f, 0.17f, 0.17f), PaletteSwatch.Honey);
            }

            b.Prism(At(new Vector3(length * 0.5f + 0.015f, 0.1f, 0f), new Vector3(0f, 0f, 90f)), 0.025f, 0.03f, 8,
                PaletteSwatch.Metal);
            return b;
        }

        private static LowPolyMeshBuilder CellRail()
        {
            var b = new LowPolyMeshBuilder(500);
            b.Box(At(0f, 0.015f, 0f), new Vector3(0.72f, 0.03f, 0.2f), PaletteSwatch.Metal);
            b.Append(CellModule(0.32f), Matrix4x4.Translate(new Vector3(-0.18f, 0.03f, 0f)));
            b.Append(CellModule(0.32f), Matrix4x4.TRS(new Vector3(0.2f, 0.03f, 0.02f), Quaternion.Euler(0f, 8f, 0f),
                Vector3.one));
            return b;
        }

        private static LowPolyMeshBuilder LensBarrel()
        {
            var b = new LowPolyMeshBuilder(300);
            Matrix4x4 axis = At(new Vector3(0f, 0.09f, 0f), new Vector3(90f, 0f, 0f));
            b.Prism(axis, 0.085f, 0.24f, 10, PaletteSwatch.Charcoal);
            for (int i = 0; i < 3; i++)
            {
                b.Torus(axis * At(0f, -0.08f + i * 0.08f, 0f), 0.09f, 0.01f, 10, 3, PaletteSwatch.Metal);
            }

            b.Prism(axis * At(0f, 0.125f, 0f), 0.07f, 0.012f, 10, PaletteSwatch.SkyHorizon);
            return b;
        }

        private static LowPolyMeshBuilder LensRing()
        {
            var b = new LowPolyMeshBuilder(260);
            b.Torus(At(new Vector3(0f, 0.03f, 0f)), 0.13f, 0.022f, 12, 4, PaletteSwatch.Honey);
            b.Prism(At(0f, 0.03f, 0f), 0.11f, 0.012f, 12, PaletteSwatch.SkyHorizon,
                displacement: new Displacement(11, 0.004f, 30f));
            b.Box(At(new Vector3(0.16f, 0.02f, 0.04f), new Vector3(0f, 30f, 0f)), new Vector3(0.08f, 0.02f, 0.03f),
                PaletteSwatch.Metal);
            return b;
        }

        private static LowPolyMeshBuilder CameraBody()
        {
            var b = new LowPolyMeshBuilder(260);
            b.Box(At(0f, 0.1f, 0f), new Vector3(0.3f, 0.2f, 0.22f), PaletteSwatch.WornPanel, 0.015f);
            b.Prism(At(new Vector3(0f, 0.11f, 0.13f), new Vector3(90f, 0f, 0f)), 0.06f, 0.06f, 10,
                PaletteSwatch.Charcoal);
            b.Prism(At(new Vector3(0f, 0.11f, 0.165f), new Vector3(90f, 0f, 0f)), 0.045f, 0.01f, 10,
                PaletteSwatch.SkyHorizon);
            b.Box(At(0.1f, 0.21f, -0.04f), new Vector3(0.06f, 0.03f, 0.06f), PaletteSwatch.Metal);
            return b;
        }

        private static LowPolyMeshBuilder Pannier(float length)
        {
            var b = new LowPolyMeshBuilder(300);
            const float wall = 0.02f;
            float height = 0.32f;
            float depth = 0.36f;
            b.Box(At(0f, wall * 0.5f, 0f), new Vector3(length, wall, depth), PaletteSwatch.WornPanel);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (length - wall) * 0.5f, height * 0.5f, 0f), new Vector3(wall, height, depth),
                    PaletteSwatch.WornPanel);
                b.Box(At(0f, height * 0.5f, side * (depth - wall) * 0.5f), new Vector3(length, height, wall),
                    side > 0 ? PaletteSwatch.FadedAccent : PaletteSwatch.WornPanel);
            }

            var strap = new Vector3(-length * 0.4f, height, -depth * 0.5f);
            SiteKit.Bar(b, strap, strap + new Vector3(-length * 0.15f, 0.18f, -depth * 0.2f), 0.03f,
                PaletteSwatch.Charcoal);
            return b;
        }

        private static LowPolyMeshBuilder PannierPair()
        {
            var b = new LowPolyMeshBuilder(800);
            b.Append(Pannier(0.7f), Matrix4x4.Translate(new Vector3(-0.38f, 0f, 0f)));
            b.Append(Pannier(0.6f), Matrix4x4.TRS(new Vector3(0.42f, 0f, 0.1f), Quaternion.Euler(0f, -14f, 0f),
                Vector3.one));
            RecipeKit.Rod(b, new Vector3(-0.75f, 0.36f, -0.2f), new Vector3(0.7f, 0.36f, -0.1f), 0.025f, 6,
                PaletteSwatch.Metal);
            return b;
        }
    }
}
