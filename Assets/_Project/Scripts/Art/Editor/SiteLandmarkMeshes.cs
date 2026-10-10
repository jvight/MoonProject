using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Each salvage site's tall landmark (M3-18 L1), at least 8 m so the site reads from far across the basin, in
    /// landmark space (origin on the ground at its foot, +Y up): the garage's crane, the drill rig's spare derrick
    /// still standing, the Kestrel's tail fin rising out of the dust and the depot's radio mast. Each carries a pilot
    /// lamp at its top (<see cref="PilotTop"/>), its own renderer, dark until the site's heart is found.
    /// </summary>
    public static class SiteLandmarkMeshes
    {
        /// <summary>The lowest any landmark may stand, so it reads from across the basin (L1).</summary>
        public const float MinHeight = 8f;

        private const float PilotRadius = 0.11f;
        private const float LegBar = 0.09f;
        private const float BraceBar = 0.045f;
        private const float CableRadius = 0.016f;

        private const float CraneMast = 8.6f;
        private const float CraneHalf = 0.32f;
        private const float JibLength = 5.4f;
        private const float JibSag = 4f;
        private const float CounterJib = 1.9f;

        private const float DerrickHeight = 9.4f;
        private const float DerrickBaseHalf = 1.05f;
        private const float DerrickTopHalf = 0.28f;
        private const float DerrickLean = 5f;

        private const float FinRootChord = 4.6f;
        private const float FinTipChord = 1.5f;
        private const float FinHeight = 8.6f;
        private const float FinSweep = 2.6f;
        private const float FinThickness = 0.26f;
        private const float FinLean = 7f;

        private const float MastHeight = 9.2f;
        private const float MastRadius = 0.12f;
        private const float GuyRadius = 3.6f;

        /// <summary>The sites that have a landmark (the lander site is home's own wreck, seen from the base).</summary>
        public static bool HasLandmark(string id)
        {
            return id == "garage" || id == "drill" || id == "kestrel" || id == "depot";
        }

        /// <summary>Where the pilot lamp sits on <paramref name="id"/>'s landmark (landmark space).</summary>
        public static Vector3 PilotTop(string id)
        {
            switch (id)
            {
                case "garage":
                    return new Vector3(0f, CraneMast + 0.62f, 0f);
                case "drill":
                    return Rotation(new Vector3(0f, 0f, -DerrickLean)) * new Vector3(0f, DerrickHeight + 0.55f, 0f);
                case "kestrel":
                    return Rotation(new Vector3(0f, 0f, -FinLean))
                        * new Vector3(FinSweep + FinTipChord - 0.1f, FinHeight + 0.05f, 0f);
                case "depot":
                    return new Vector3(0f, MastHeight + 0.18f, 0f);
                default:
                    throw new System.ArgumentException($"No landmark for site '{id}'.", nameof(id));
            }
        }

        /// <summary><paramref name="id"/>'s landmark (landmark space).</summary>
        public static LowPolyMeshBuilder Landmark(string id)
        {
            switch (id)
            {
                case "garage":
                    return Crane();
                case "drill":
                    return Derrick();
                case "kestrel":
                    return TailFin();
                case "depot":
                    return Mast();
                default:
                    throw new System.ArgumentException($"No landmark for site '{id}'.", nameof(id));
            }
        }

        /// <summary>The pilot lamp: dark glass in a little metal cage (centred on its node).</summary>
        public static LowPolyMeshBuilder Pilot()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Icosphere(Matrix4x4.identity, PilotRadius, 1, PaletteSwatch.LampGlass);
            b.Prism(At(0f, -PilotRadius - 0.03f, 0f), PilotRadius * 0.9f, 0.06f, 6, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// The garage's tower crane: a square lattice mast, a slewing cab, the jib reaching out over the yard and
        /// sagging a little, its counterweight, and a slack hook cable.
        /// </summary>
        private static LowPolyMeshBuilder Crane()
        {
            var b = new LowPolyMeshBuilder(1400);
            Lattice(b, Vector3.zero, CraneMast, CraneHalf, CraneHalf, 1.1f);
            b.Box(At(0f, 0.12f, 0f), new Vector3(1.1f, 0.24f, 1.1f), PaletteSwatch.WornPanel, 0.03f);
            var cab = new Vector3(0f, CraneMast + 0.3f, 0f);
            b.Box(At(cab), new Vector3(0.9f, 0.55f, 0.8f), PaletteSwatch.FadedAccent, 0.03f);
            Quaternion sag = Rotation(new Vector3(0f, 0f, -JibSag));
            Vector3 root = cab + new Vector3(0.45f, 0f, 0f);
            Vector3 tip = root + sag * new Vector3(JibLength, 0f, 0f);
            for (int side = -1; side <= 1; side += 2)
            {
                var offset = new Vector3(0f, -0.2f, side * 0.25f);
                SiteKit.Bar(b, root + offset, tip + offset, LegBar * 0.8f, PaletteSwatch.WornPanel);
            }

            Vector3 apexRoot = root + new Vector3(0f, 0.3f, 0f);
            Vector3 apexTip = tip + sag * new Vector3(0f, 0.3f, 0f);
            SiteKit.Bar(b, apexRoot, apexTip, LegBar * 0.8f, PaletteSwatch.WornPanel);
            for (int i = 0; i < 6; i++)
            {
                Vector3 low = Vector3.Lerp(root, tip, i / 6f) + new Vector3(0f, -0.2f, i % 2 == 0 ? 0.25f : -0.25f);
                Vector3 high = Vector3.Lerp(apexRoot, apexTip, (i + 0.5f) / 6f);
                SiteKit.Bar(b, low, high, BraceBar, PaletteSwatch.WornPanel);
            }

            Vector3 counterEnd = cab + new Vector3(-0.45f - CounterJib, 0f, 0f);
            SiteKit.Bar(b, cab + new Vector3(-0.45f, -0.1f, 0f), counterEnd, LegBar, PaletteSwatch.WornPanel);
            b.Box(At(counterEnd + new Vector3(0.3f, -0.45f, 0f)), new Vector3(0.7f, 0.7f, 0.6f),
                PaletteSwatch.WornPanel, 0.03f);
            Vector3 hookTop = tip + new Vector3(-0.3f, -0.25f, 0f);
            var hook = new Vector3(hookTop.x - 0.15f, hookTop.y - 3.1f, 0.2f);
            SiteKit.Cable(b, SiteKit.Sag(hookTop, hook, 0.08f, 3), CableRadius, PaletteSwatch.Charcoal);
            b.Torus(At(hook + Vector3.down * 0.1f, new Vector3(90f, 0f, 0f)), 0.12f, 0.03f, 8, 3,
                PaletteSwatch.Rust);
            Matrix4x4 face = SiteKit.Face(cab + new Vector3(0f, 0f, 0.41f), Vector3.forward, Vector3.up);
            SiteKit.RustStreak(b, face, 0.25f, 0.2f, 0.4f);
            return b;
        }

        /// <summary>The rig's spare derrick, still pinned upright and leaning, tapering to its crown block.</summary>
        private static LowPolyMeshBuilder Derrick()
        {
            var b = new LowPolyMeshBuilder(1400);
            Matrix4x4 lean = Matrix4x4.Rotate(Rotation(new Vector3(0f, 0f, -DerrickLean)));
            var tower = new LowPolyMeshBuilder(1200);
            Lattice(tower, Vector3.zero, DerrickHeight, DerrickBaseHalf, DerrickTopHalf, 1.3f);
            tower.Box(At(0f, DerrickHeight + 0.2f, 0f), new Vector3(0.75f, 0.4f, 0.75f), PaletteSwatch.FadedAccent,
                0.03f);
            b.Append(tower, lean);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    b.Box(At(x * DerrickBaseHalf, 0.1f, z * DerrickBaseHalf), new Vector3(0.36f, 0.2f, 0.36f),
                        PaletteSwatch.WornPanel, 0.02f);
                }
            }

            Vector3 crown = lean.MultiplyPoint3x4(new Vector3(0f, DerrickHeight, 0f));
            SiteKit.Cable(b, SiteKit.Sag(crown, new Vector3(4.2f, 0.02f, 1.6f), 0.5f, 5), CableRadius,
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// The Kestrel's tail fin, torn off with a stub of the fuselage and rising out of the dust at a lean: faded
        /// panels, the squadron stripe near the tip, a rusted hinge line where the rudder hung.
        /// </summary>
        private static LowPolyMeshBuilder TailFin()
        {
            var b = new LowPolyMeshBuilder(800);
            var fin = new LowPolyMeshBuilder(400);
            var outline = new[]
            {
                new Vector2(0f, 0f), new Vector2(FinRootChord, 0f), new Vector2(FinSweep + FinTipChord, FinHeight),
                new Vector2(FinSweep, FinHeight),
            };
            fin.Extrude(At(0f, 0f, -FinThickness * 0.5f), outline, FinThickness, PaletteSwatch.WornPanel);
            const float stripe = 0.82f;
            const float stripeTop = stripe + 0.07f;
            const float tipTrail = FinSweep + FinTipChord;
            var band = new[]
            {
                new Vector2(Mathf.Lerp(0f, FinSweep, stripe), FinHeight * stripe),
                new Vector2(Mathf.Lerp(FinRootChord, tipTrail, stripe), FinHeight * stripe),
                new Vector2(Mathf.Lerp(FinRootChord, tipTrail, stripeTop), FinHeight * stripeTop),
                new Vector2(Mathf.Lerp(0f, FinSweep, stripeTop), FinHeight * stripeTop),
            };
            fin.Extrude(At(0f, 0f, -FinThickness * 0.5f - 0.01f), band, FinThickness + 0.02f,
                PaletteSwatch.FadedAccent);
            Vector3 hingeLow = new Vector3(FinRootChord - 0.9f, 0.3f, 0f);
            Vector3 hingeHigh = new Vector3(FinSweep + FinTipChord - 0.35f, FinHeight - 0.4f, 0f);
            SiteKit.Bar(fin, hingeLow, hingeHigh, FinThickness + 0.03f, PaletteSwatch.Rust);
            for (int side = -1; side <= 1; side += 2)
            {
                Matrix4x4 skin = SiteKit.Face(new Vector3(0f, 0f, side * (FinThickness * 0.5f + 0.004f)),
                    Vector3.forward * side, Vector3.up);
                SiteKit.RustPatch(fin, skin, side * 1.4f, 1.1f, 0.5f);
                SiteKit.RustPatch(fin, skin, side * 3.1f, 4.6f, 0.35f);
                SiteKit.RustStreak(fin, skin, side * 2.6f, 6.8f, 1.6f);
            }

            b.Append(fin, Matrix4x4.Rotate(Rotation(new Vector3(0f, 0f, -FinLean))));
            b.Prism(At(new Vector3(FinRootChord * 0.45f, -0.3f, 0f), AlongX), 0.95f, FinRootChord * 1.1f, 10,
                PaletteSwatch.WornPanel);
            return b;
        }

        /// <summary>The depot's radio mast: a tube in three collared sections, guyed three ways, a dish.</summary>
        private static LowPolyMeshBuilder Mast()
        {
            var b = new LowPolyMeshBuilder(900);
            b.Box(At(0f, 0.1f, 0f), new Vector3(0.7f, 0.2f, 0.7f), PaletteSwatch.WornPanel, 0.02f);
            for (int section = 0; section < 3; section++)
            {
                float from = section * MastHeight / 3f;
                float radius = MastRadius * (1f - section * 0.18f);
                b.Prism(At(0f, from + MastHeight / 6f, 0f), radius, MastHeight / 3f, 8,
                    section == 1 ? PaletteSwatch.FadedAccent : PaletteSwatch.Metal);
                b.Prism(At(0f, from + 0.05f, 0f), radius + 0.04f, 0.1f, 8, PaletteSwatch.Metal);
            }

            for (int guy = 0; guy < 3; guy++)
            {
                float angle = guy * 120f * Mathf.Deg2Rad;
                var anchor = new Vector3(Mathf.Cos(angle) * GuyRadius, 0.05f, Mathf.Sin(angle) * GuyRadius);
                Vector3 top = new Vector3(0f, MastHeight * 0.7f, 0f);
                SiteKit.Cable(b, SiteKit.Sag(top, anchor, guy == 2 ? 0.6f : 0.12f, 4), CableRadius,
                    PaletteSwatch.Charcoal);
                b.Box(At(anchor + Vector3.down * 0.02f), new Vector3(0.22f, 0.12f, 0.22f), PaletteSwatch.Metal, 0.02f);
            }

            SiteKit.Dish(b, At(new Vector3(0.25f, MastHeight * 0.78f, 0f), new Vector3(0f, 0f, -70f)), 0.55f,
                PaletteSwatch.WornPanel);
            SiteKit.Bar(b, new Vector3(-0.45f, MastHeight - 0.15f, 0f), new Vector3(0.45f, MastHeight - 0.15f, 0f),
                0.05f, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// A four-legged lattice tower from <paramref name="foot"/>, tapering from <paramref name="baseHalf"/> to
        /// <paramref name="topHalf"/>, braced in a zigzag every <paramref name="bay"/> metres.
        /// </summary>
        private static void Lattice(LowPolyMeshBuilder b, Vector3 foot, float height, float baseHalf, float topHalf,
            float bay)
        {
            var corners = new[]
            {
                new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f),
            };
            for (int c = 0; c < 4; c++)
            {
                Vector3 low = foot + new Vector3(corners[c].x * baseHalf, 0f, corners[c].y * baseHalf);
                Vector3 high = foot + new Vector3(corners[c].x * topHalf, height, corners[c].y * topHalf);
                SiteKit.Bar(b, low, high, LegBar, PaletteSwatch.WornPanel);
            }

            int bays = Mathf.Max(1, Mathf.RoundToInt(height / bay));
            for (int i = 0; i < bays; i++)
            {
                float t0 = (float)i / bays;
                float t1 = (float)(i + 1) / bays;
                for (int c = 0; c < 4; c++)
                {
                    Vector2 a = corners[c];
                    Vector2 n = corners[(c + 1) % 4];
                    float h0 = Mathf.Lerp(baseHalf, topHalf, t0);
                    float h1 = Mathf.Lerp(baseHalf, topHalf, t1);
                    var a0 = foot + new Vector3(a.x * h0, t0 * height, a.y * h0);
                    var n0 = foot + new Vector3(n.x * h0, t0 * height, n.y * h0);
                    var n1 = foot + new Vector3(n.x * h1, t1 * height, n.y * h1);
                    var a1 = foot + new Vector3(a.x * h1, t1 * height, a.y * h1);
                    SiteKit.Bar(b, a0, n0, BraceBar, PaletteSwatch.WornPanel);
                    SiteKit.Bar(b, i % 2 == 0 ? a0 : n0, i % 2 == 0 ? n1 : a1, BraceBar, PaletteSwatch.WornPanel);
                }
            }
        }
    }
}
