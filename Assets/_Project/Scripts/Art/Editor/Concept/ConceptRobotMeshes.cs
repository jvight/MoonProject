using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor.Concept
{
    /// <summary>
    /// Concept blockouts for 0.6+ review (docs/design/0.6-living-moon.md, 0.6-world-regions.md): the moon's machine
    /// ecology, 07's elders and the friends still to meet, in 07's family (cream enamel, faded orange bands, one eye
    /// glass, honey and sage accents), medium detail. Each is built in its own space: root on the ground, +Z its
    /// front. Preview only: nothing here is written to a prefab or reaches the game.
    /// </summary>
    internal static class ConceptRobotMeshes
    {
        /// <summary>
        /// A panel mite, about 14 cm: a honey shell on six needle legs, a cream brush skirt, one cyan eye.
        /// </summary>
        public static LowPolyMeshBuilder PanelMite()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Prism(At(0f, 0.014f, 0f), 0.048f, 0.012f, 8, PaletteSwatch.Cream);
            b.Icosphere(At(new Vector3(0f, 0.045f, 0f), Vector3.zero, new Vector3(1f, 0.55f, 1.3f)), 0.05f, 1,
                PaletteSwatch.Honey);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = -1; i <= 1; i++)
                {
                    var hip = new Vector3(side * 0.03f, 0.04f, i * 0.025f);
                    var foot = new Vector3(side * 0.078f, 0.002f, i * 0.045f);
                    Vector3 knee = hip + (foot - hip) * 0.5f + Vector3.up * 0.025f;
                    SiteKit.Bar(b, hip, knee, 0.006f, PaletteSwatch.Charcoal);
                    SiteKit.Bar(b, knee, foot, 0.006f, PaletteSwatch.Charcoal);
                }
            }

            b.Icosphere(At(0f, 0.06f, 0.055f), 0.013f, 0, PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>
        /// The sweeper, about 1 m across: a round cream body on three wheels, a faded orange band, one round lamp-eye
        /// in its hood and two brush arms out front. <paramref name="repaired"/> false: one arm hangs bent, its disc
        /// in the dust, the lamp dark and dust caked on top; true: both arms level, the lamp warm, the shell wiped.
        /// </summary>
        public static LowPolyMeshBuilder Sweeper(bool repaired)
        {
            var b = new LowPolyMeshBuilder(1600);
            for (int i = 0; i < 3; i++)
            {
                float angle = (i * 120f + 60f) * Mathf.Deg2Rad;
                b.Prism(At(new Vector3(Mathf.Sin(angle) * 0.32f, 0.09f, Mathf.Cos(angle) * 0.32f), new Vector3(0f,
                    i * 120f + 60f, 90f)), 0.09f, 0.07f, 10, PaletteSwatch.Charcoal);
            }

            MeshRange body = b.Frustum(At(0f, 0.12f, 0f), 0.5f, 0.42f, 0.3f, 16, PaletteSwatch.Enamel);
            b.Frustum(At(0f, 0.26f, 0f), 0.47f, 0.46f, 0.06f, 16, PaletteSwatch.FadedAccent);
            MeshRange top = b.Icosphere(At(new Vector3(0f, 0.42f, -0.04f), Vector3.zero, new Vector3(1f, 0.38f, 1f)),
                0.4f, 1, PaletteSwatch.Enamel);
            b.Box(At(new Vector3(0f, 0.5f, 0.24f), new Vector3(-12f, 0f, 0f)), new Vector3(0.3f, 0.16f, 0.16f),
                PaletteSwatch.WornPanel, 0.03f);
            b.Torus(At(new Vector3(0f, 0.5f, 0.33f), new Vector3(90f, 0f, 0f)), 0.06f, 0.015f, 12, 4,
                PaletteSwatch.Charcoal);
            b.Icosphere(At(0f, 0.5f, 0.33f), 0.05f, 1, repaired ? PaletteSwatch.WarmLamp : PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                bool hangs = !repaired && side > 0;
                var shoulder = new Vector3(side * 0.3f, 0.24f, 0.36f);
                Vector3 hand = hangs ? new Vector3(side * 0.55f, 0.05f, 0.62f)
                    : new Vector3(side * 0.62f, 0.12f, 0.72f);
                SiteKit.Bar(b, shoulder, hand, 0.05f, PaletteSwatch.Metal);
                Vector3 tilt = hangs ? new Vector3(0f, 0f, side * 28f) : Vector3.zero;
                b.Prism(At(hand + Vector3.down * 0.06f, tilt), 0.2f, 0.035f, 12, PaletteSwatch.Charcoal);
                b.Prism(At(hand + Vector3.down * 0.09f, tilt), 0.21f, 0.03f, 12, PaletteSwatch.Honey);
            }

            if (!repaired)
            {
                SiteKit.DustCap(b, top);
                SiteKit.DustCap(b, body);
            }

            return b;
        }

        /// <summary>
        /// A survey drone, Tilly's kin, about 45 cm: a squat cream pod with a cyan eye, a faded orange ring frame,
        /// four ducted rotors and a whip with a warm tip. Hovering: its root is its own middle, not the ground.
        /// </summary>
        public static LowPolyMeshBuilder SurveyDrone()
        {
            var b = new LowPolyMeshBuilder(900);
            b.Icosphere(At(Vector3.zero, Vector3.zero, new Vector3(1f, 0.7f, 1.15f)), 0.11f, 1, PaletteSwatch.Cream);
            b.Torus(At(Vector3.zero), 0.2f, 0.014f, 16, 4, PaletteSwatch.FadedAccent);
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                var hub = new Vector3(Mathf.Sin(angle) * 0.2f, 0.02f, Mathf.Cos(angle) * 0.2f);
                b.Torus(At(hub), 0.075f, 0.012f, 12, 4, PaletteSwatch.Metal);
                b.Prism(At(hub), 0.065f, 0.006f, 10, PaletteSwatch.Charcoal);
            }

            b.Prism(At(new Vector3(0f, -0.06f, 0f)), 0.04f, 0.04f, 8, PaletteSwatch.Honey);
            b.Icosphere(At(0f, 0.01f, 0.115f), 0.03f, 1, PaletteSwatch.EyeGlass);
            SiteKit.Bar(b, new Vector3(0f, 0.06f, -0.06f), new Vector3(0f, 0.24f, -0.12f), 0.008f, PaletteSwatch.Metal);
            b.Icosphere(At(0f, 0.245f, -0.122f), 0.014f, 0, PaletteSwatch.WarmLamp);
            return b;
        }

        /// <summary>
        /// The trencher, about 3 m: a low sage hull on two long tracks, a small cab with one eye window, a big
        /// toothed cutting wheel at its nose and a spoil chute throwing dust to its left.
        /// </summary>
        public static LowPolyMeshBuilder Trencher()
        {
            var b = new LowPolyMeshBuilder(3000);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.55f, 0.22f, 0f), new Vector3(0.3f, 0.42f, 2.6f), PaletteSwatch.Charcoal, 0.12f);
                for (int i = -2; i <= 2; i++)
                {
                    b.Prism(At(new Vector3(side * 0.72f, 0.2f, i * 0.5f), new Vector3(0f, 0f, 90f)), 0.13f, 0.04f, 8,
                        PaletteSwatch.Metal);
                }
            }

            MeshRange hull = b.Box(At(0f, 0.6f, -0.1f), new Vector3(1.05f, 0.42f, 2.2f), PaletteSwatch.Sage, 0.06f);
            SiteKit.DustCap(b, hull);
            b.Box(At(0f, 0.66f, -0.1f), new Vector3(1.08f, 0.08f, 2.22f), PaletteSwatch.FadedAccent);
            b.Box(At(0.15f, 1.02f, -0.55f), new Vector3(0.55f, 0.42f, 0.6f), PaletteSwatch.Enamel, 0.05f);
            b.Box(At(0.15f, 1.08f, -0.24f), new Vector3(0.36f, 0.16f, 0.02f), PaletteSwatch.EyeGlass);
            b.Box(At(0f, 0.55f, 1.2f), new Vector3(0.3f, 0.3f, 0.5f), PaletteSwatch.Metal, 0.03f);
            Matrix4x4 wheel = At(new Vector3(0f, 0.55f, 1.55f), new Vector3(0f, 0f, 90f));
            b.Prism(wheel, 0.55f, 0.22f, 14, PaletteSwatch.Rust);
            for (int t = 0; t < 10; t++)
            {
                float angle = t * 36f * Mathf.Deg2Rad;
                var tooth = new Vector3(0f, 0.55f + Mathf.Cos(angle) * 0.6f, 1.55f + Mathf.Sin(angle) * 0.6f);
                b.Box(At(tooth, new Vector3(-t * 36f, 0f, 0f)), new Vector3(0.24f, 0.12f, 0.06f), PaletteSwatch.Metal);
            }

            SiteKit.Bar(b, new Vector3(-0.3f, 0.8f, 1.1f), new Vector3(-1.25f, 1.0f, 0.6f), 0.18f, PaletteSwatch.Honey);
            b.Icosphere(At(new Vector3(-1.4f, 0.1f, 0.55f), Vector3.zero, new Vector3(1.4f, 0.5f, 1f)), 0.35f, 1,
                PaletteSwatch.DustMid);
            return b;
        }

        /// <summary>
        /// A cable climber, about 45 cm, on a vertical cable (the cable's foot at the root, along +Y): a cream body
        /// clamped to it by two grip wheels, four splayed legs braced on the wall behind (-Z) and a curious eye stalk.
        /// </summary>
        public static LowPolyMeshBuilder CableClimber()
        {
            var b = new LowPolyMeshBuilder(900);
            var body = new Vector3(0f, 1.6f, 0.1f);
            b.Box(At(body), new Vector3(0.3f, 0.36f, 0.18f), PaletteSwatch.Cream, 0.05f);
            b.Box(At(body + new Vector3(0f, 0.04f, 0f)), new Vector3(0.31f, 0.06f, 0.19f), PaletteSwatch.FadedAccent);
            for (int end = -1; end <= 1; end += 2)
            {
                b.Torus(At(new Vector3(0f, body.y + end * 0.2f, 0f), new Vector3(0f, 0f, 90f)), 0.05f, 0.018f, 10, 4,
                    PaletteSwatch.Charcoal);
            }

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    var hip = body + new Vector3(x * 0.13f, y * 0.13f, -0.02f);
                    var knee = body + new Vector3(x * 0.3f, y * 0.26f, 0.02f);
                    var foot = body + new Vector3(x * 0.36f, y * 0.34f, -0.12f);
                    SiteKit.Bar(b, hip, knee, 0.02f, PaletteSwatch.Metal);
                    SiteKit.Bar(b, knee, foot, 0.018f, PaletteSwatch.Metal);
                }
            }

            SiteKit.Bar(b, body + new Vector3(0.08f, 0.17f, 0.06f), body + new Vector3(0.14f, 0.3f, 0.12f), 0.02f,
                PaletteSwatch.Metal);
            b.Icosphere(At(body + new Vector3(0.15f, 0.32f, 0.14f)), 0.04f, 1, PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>
        /// An ice tapper, about 70 cm: a low sage shell on four bent legs, a hammer arm raised mid-tap and its eye
        /// dimmed to a faint point (shy: dim when lit).
        /// </summary>
        public static LowPolyMeshBuilder IceTapper()
        {
            var b = new LowPolyMeshBuilder(1200);
            b.Icosphere(At(new Vector3(0f, 0.36f, 0f), Vector3.zero, new Vector3(1.1f, 0.55f, 1.3f)), 0.24f, 1,
                PaletteSwatch.Sage);
            b.Torus(At(0f, 0.36f, 0f), 0.26f, 0.018f, 14, 4, PaletteSwatch.FadedAccent);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var hip = new Vector3(x * 0.16f, 0.34f, z * 0.16f);
                    var knee = new Vector3(x * 0.36f, 0.42f, z * 0.32f);
                    var foot = new Vector3(x * 0.42f, 0f, z * 0.4f);
                    SiteKit.Bar(b, hip, knee, 0.035f, PaletteSwatch.Metal);
                    SiteKit.Bar(b, knee, foot, 0.03f, PaletteSwatch.Metal);
                }
            }

            var shoulder = new Vector3(0f, 0.46f, 0.2f);
            var elbow = new Vector3(0f, 0.82f, 0.36f);
            var hammer = new Vector3(0f, 0.72f, 0.62f);
            SiteKit.Bar(b, shoulder, elbow, 0.04f, PaletteSwatch.Metal);
            SiteKit.Bar(b, elbow, hammer, 0.035f, PaletteSwatch.Metal);
            b.Box(At(hammer, new Vector3(30f, 0f, 0f)), new Vector3(0.16f, 0.09f, 0.09f), PaletteSwatch.Honey, 0.02f);
            b.Icosphere(At(0f, 0.42f, 0.29f), 0.035f, 1, PaletteSwatch.Charcoal);
            b.Icosphere(At(0f, 0.42f, 0.315f), 0.01f, 0, PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>
        /// An echo beacon, about 12 cm: a metal puck on a short spike driven into the rock (+Z out of the wall), a
        /// ring and a teal core that blinks back when 07 listens.
        /// </summary>
        public static LowPolyMeshBuilder EchoBeacon()
        {
            var b = new LowPolyMeshBuilder(200);
            Matrix4x4 face = At(Vector3.zero, new Vector3(90f, 0f, 0f));
            SiteKit.Bar(b, new Vector3(0f, 0f, -0.06f), new Vector3(0f, 0f, 0.02f), 0.015f, PaletteSwatch.Metal);
            b.Prism(face * At(0f, 0.035f, 0f), 0.06f, 0.035f, 10, PaletteSwatch.Metal);
            b.Torus(face * At(0f, 0.055f, 0f), 0.05f, 0.008f, 12, 3, PaletteSwatch.FadedAccent);
            b.Icosphere(face * At(0f, 0.06f, 0f), 0.022f, 1, PaletteSwatch.BiolumTeal);
            return b;
        }

        /// <summary>
        /// 01, the crew's first rover, about 70 cm: a small cream box on four little wheels, a faded orange band, a
        /// fixed camera post with one eye glass and a whip; "01" on its back. Nothing tilts or folds yet.
        /// </summary>
        public static LowPolyMeshBuilder Elder01()
        {
            var b = new LowPolyMeshBuilder(2000);
            MeshRange body = b.Box(At(0f, 0.27f, 0f), new Vector3(0.44f, 0.22f, 0.66f), PaletteSwatch.Enamel, 0.05f);
            SiteKit.DustCap(b, body);
            b.Box(At(0f, 0.29f, 0f), new Vector3(0.452f, 0.05f, 0.672f), PaletteSwatch.FadedAccent);
            LowPolyMeshBuilder wheel = RoverMeshes.Wheel();
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Matrix4x4 at = Matrix4x4.TRS(new Vector3(x * 0.27f, 0.15f, z * 0.22f), Quaternion.identity,
                        Vector3.one * 0.43f);
                    b.Append(wheel, x > 0 ? at : at * MirrorX);
                }
            }

            SiteKit.Bar(b, new Vector3(0f, 0.38f, 0.22f), new Vector3(0f, 0.72f, 0.22f), 0.035f, PaletteSwatch.Metal);
            b.Box(At(0f, 0.78f, 0.24f), new Vector3(0.16f, 0.12f, 0.14f), PaletteSwatch.Enamel, 0.02f);
            b.Prism(At(new Vector3(0f, 0.78f, 0.315f), new Vector3(90f, 0f, 0f)), 0.035f, 0.02f, 10,
                PaletteSwatch.EyeGlass);
            SiteKit.Bar(b, new Vector3(0.16f, 0.38f, -0.26f), new Vector3(0.18f, 0.95f, -0.3f), 0.008f,
                PaletteSwatch.Metal);
            Glyphs.Write(b, At(new Vector3(-0.08f, 0.24f, -0.334f), new Vector3(0f, 180f, 0f)), "01", 0.09f,
                PaletteSwatch.FadedPaint, PaletteSwatch.Enamel);
            return b;
        }

        /// <summary>
        /// Atlas, the big gentle hauler, about 4 m to its boom's lamp: wide tracks, a honey turret, a cab with one big
        /// round warm eye, a cargo fork low at its front and a lattice boom with a hook and its work lamp on top (the
        /// lamp the terraces are lured by).
        /// </summary>
        public static LowPolyMeshBuilder Atlas()
        {
            var b = new LowPolyMeshBuilder(5000);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.95f, 0.32f, 0f), new Vector3(0.5f, 0.64f, 2.6f), PaletteSwatch.Charcoal, 0.2f);
            }

            MeshRange deck = b.Box(At(0f, 0.75f, 0f), new Vector3(1.9f, 0.3f, 2.3f), PaletteSwatch.Metal, 0.05f);
            SiteKit.DustCap(b, deck);
            MeshRange turret = b.Box(At(0f, 1.45f, -0.2f), new Vector3(1.5f, 1.1f, 1.5f), PaletteSwatch.Honey, 0.12f);
            SiteKit.DustCap(b, turret);
            b.Box(At(0f, 1.25f, -0.2f), new Vector3(1.52f, 0.14f, 1.52f), PaletteSwatch.FadedAccent);
            b.Box(At(0.25f, 2.2f, 0.25f), new Vector3(0.8f, 0.55f, 0.7f), PaletteSwatch.Enamel, 0.1f);
            b.Torus(At(new Vector3(0.25f, 2.22f, 0.61f), new Vector3(90f, 0f, 0f)), 0.17f, 0.04f, 16, 4,
                PaletteSwatch.Charcoal);
            b.Prism(At(new Vector3(0.25f, 2.22f, 0.6f), new Vector3(90f, 0f, 0f)), 0.15f, 0.03f, 16,
                PaletteSwatch.WarmLamp);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.45f, 0.62f, 1.55f), new Vector3(0.14f, 0.08f, 0.9f), PaletteSwatch.Metal);
                SiteKit.Bar(b, new Vector3(side * 0.45f, 0.62f, 1.1f), new Vector3(side * 0.45f, 1.3f, 1.05f), 0.1f,
                    PaletteSwatch.Metal);
            }

            var boomFoot = new Vector3(-0.35f, 2.0f, -0.4f);
            var boomTip = new Vector3(-0.35f, 3.9f, 1.9f);
            for (int side = -1; side <= 1; side += 2)
            {
                var offset = new Vector3(side * 0.16f, 0f, 0f);
                SiteKit.Bar(b, boomFoot + offset, boomTip + offset, 0.08f, PaletteSwatch.FadedAccent);
            }

            for (int i = 1; i < 6; i++)
            {
                Vector3 at = Vector3.Lerp(boomFoot, boomTip, i / 6f);
                SiteKit.Bar(b, at + new Vector3(-0.16f, 0f, 0f), at + new Vector3(0.16f, 0f, 0f), 0.04f,
                    PaletteSwatch.Metal);
            }

            SiteKit.Bar(b, boomTip, boomTip + Vector3.down * 1.6f, 0.02f, PaletteSwatch.Charcoal);
            b.Box(At(boomTip + Vector3.down * 1.7f), new Vector3(0.22f, 0.2f, 0.16f), PaletteSwatch.Honey, 0.03f);
            b.Box(At(boomTip + new Vector3(0f, 0.14f, 0f)), new Vector3(0.24f, 0.16f, 0.2f), PaletteSwatch.Charcoal);
            b.Icosphere(At(boomTip + new Vector3(0f, 0.15f, 0.1f)), 0.09f, 1, PaletteSwatch.WarmLamp);
            return b;
        }

        /// <summary>
        /// Moss, the slow garden bot, about 80 cm: a round sage body on three soft wheels, a ribbed greenhouse cage
        /// over small plants glowing faint teal, a watering arm with a honey nozzle, one soft warm eye and a speaker
        /// grille it hums through.
        /// </summary>
        public static LowPolyMeshBuilder Moss()
        {
            var b = new LowPolyMeshBuilder(2400);
            for (int i = 0; i < 3; i++)
            {
                float yaw = i * 120f;
                float angle = yaw * Mathf.Deg2Rad;
                b.Prism(At(new Vector3(Mathf.Sin(angle) * 0.24f, 0.08f, Mathf.Cos(angle) * 0.24f),
                    new Vector3(0f, yaw, 90f)), 0.08f, 0.09f, 10, PaletteSwatch.Charcoal);
            }

            MeshRange body = b.Lathe(At(0f, 0.1f, 0f), new[]
            {
                new Vector2(0.001f, 0f), new Vector2(0.3f, 0.02f), new Vector2(0.36f, 0.14f), new Vector2(0.33f, 0.3f),
                new Vector2(0.001f, 0.32f),
            }, 14, PaletteSwatch.Sage);
            SiteKit.DustCap(b, body);
            b.Frustum(At(0f, 0.3f, 0f), 0.35f, 0.34f, 0.05f, 14, PaletteSwatch.FadedAccent);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? 0.1f : 0.16f;
                b.Icosphere(At(new Vector3(Mathf.Sin(angle) * r, 0.46f, Mathf.Cos(angle) * r), Vector3.zero,
                    new Vector3(1f, 1.4f, 1f)), 0.05f, 1, PaletteSwatch.Sage);
                b.Icosphere(At(Mathf.Sin(angle) * r, 0.53f, Mathf.Cos(angle) * r), 0.014f, 0,
                    PaletteSwatch.BiolumTeal);
            }

            b.Torus(At(0f, 0.36f, 0f), 0.27f, 0.014f, 16, 4, PaletteSwatch.Metal);
            for (int i = 0; i < 4; i++)
            {
                float yaw = i * 45f;
                b.Torus(At(new Vector3(0f, 0.36f, 0f), new Vector3(90f, yaw, 0f), new Vector3(1f, 1f, 0.8f)), 0.27f,
                    0.01f, 16, 3, PaletteSwatch.Metal);
            }
            b.Icosphere(At(0f, 0.25f, 0.34f), 0.035f, 1, PaletteSwatch.WarmLamp);
            for (int i = 0; i < 3; i++)
            {
                b.Box(At(new Vector3(0.17f, 0.17f + i * 0.03f, 0.3f), new Vector3(0f, 25f, 0f)),
                    new Vector3(0.08f, 0.01f, 0.01f), PaletteSwatch.Charcoal);
            }

            var elbow = new Vector3(-0.42f, 0.5f, 0.3f);
            SiteKit.Bar(b, new Vector3(-0.28f, 0.3f, 0.05f), elbow, 0.025f, PaletteSwatch.Metal);
            SiteKit.Bar(b, elbow, new Vector3(-0.38f, 0.42f, 0.48f), 0.022f, PaletteSwatch.Metal);
            b.Frustum(At(new Vector3(-0.37f, 0.38f, 0.5f), new Vector3(150f, 0f, 0f)), 0.025f, 0.045f, 0.06f, 8,
                PaletteSwatch.Honey);
            return b;
        }
    }
}
