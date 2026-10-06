using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The abandoned lander that 07 calls home: a gold-foil descent stage on four splayed legs under a cream
    /// enamel cabin with 07's worn orange stripe, a hatch onto the deck, a ladder down to a welcome mat, round warm
    /// windows and little porch lamps. Built in base space: origin on the ground under the lander's centre,
    /// +Z = the hatch side.
    /// </summary>
    internal static class LanderMeshes
    {
        public const float StageBottom = 1.45f;
        public const float StageTop = 2.75f;
        public const float CabinTop = 4.64f;

        public const int LampCount = 4;

        /// <summary>
        /// Top of the cushion on Tilly's perch (the lander's FriendSocket_tilly): a little landing pad on a post at
        /// the left edge of the porch, beside the window and under the left wall lamp.
        /// </summary>
        public static readonly Vector3 TillyPerch = new Vector3(-1.62f, StageTop + 0.52f, 0.95f);

        private const float StageRadius = 2.1f;
        private const float CabinRadius = 1.45f;
        private const float CabinBottom = StageTop + 0.04f;
        private const float HipRadius = 1.75f;
        private const float FootRadius = 3.4f;
        private const float WindowHeight = 3.55f;
        private const float HatchCentreY = 3.5f;
        private const float DeckEdge = 1.92f;

        private static readonly float CabinApothem = CabinRadius * Mathf.Cos(Mathf.PI / 8f);

        private static readonly Vector2[] FootpadProfile =
        {
            new Vector2(0f, 0f), new Vector2(0.46f, 0.02f), new Vector2(0.48f, 0.1f), new Vector2(0.3f, 0.17f),
            new Vector2(0f, 0.19f),
        };

        private static readonly Vector2[] DomeProfile =
        {
            new Vector2(1.33f, 0f), new Vector2(1.18f, 0.3f), new Vector2(0.78f, 0.55f), new Vector2(0f, 0.66f),
        };

        private static readonly Vector2[] DishProfile =
        {
            new Vector2(0f, -0.1f), new Vector2(0.22f, -0.07f), new Vector2(0.42f, 0.05f), new Vector2(0.38f, 0.06f),
            new Vector2(0.2f, -0.03f), new Vector2(0f, -0.05f),
        };

        public static LowPolyMeshBuilder Hull()
        {
            var b = new LowPolyMeshBuilder(4000);
            DescentStage(b);
            Legs(b);
            Cabin(b);
            Deck(b);
            Ladder(b);
            Roof(b);
            Clutter(b);
            for (int i = 0; i < LampCount; i++)
            {
                LampFixture(b, LampPosition(i));
            }

            HomeSign(b);
            MissionPatch(b);
            Perch(b);
            return b;
        }

        /// <summary>Everything that glows warm: window panes, the hatch porthole and the lamp bulbs.</summary>
        public static LowPolyMeshBuilder Windows()
        {
            var b = new LowPolyMeshBuilder(400);
            for (int side = -1; side <= 1; side += 2)
            {
                float yaw = side * 45f;
                Vector3 normal = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward;
                Vector3 centre = normal * (CabinApothem + 0.03f) + Vector3.up * WindowHeight;
                b.Prism(At(centre, new Vector3(90f, yaw, 0f)), 0.27f, 0.03f, 12, PaletteSwatch.WarmLamp);
            }

            b.Prism(At(new Vector3(0f, WindowHeight, -CabinApothem - 0.03f), new Vector3(90f, 180f, 0f)), 0.22f,
                0.03f, 12, PaletteSwatch.WarmLamp);
            b.Prism(At(new Vector3(0f, HatchCentreY + 0.3f, CabinApothem + 0.1f), AlongZ), 0.13f, 0.03f, 10,
                PaletteSwatch.WarmLamp);
            for (int i = 0; i < LampCount; i++)
            {
                b.Icosphere(At(LampPosition(i)), 0.075f, 1, PaletteSwatch.WarmLamp);
            }

            return b;
        }

        /// <summary>
        /// Lamp socket <paramref name="index"/> (0..<see cref="LampCount"/> - 1): over the hatch, on the lamp post by
        /// the ladder, and on the walls facing the shelf and the tower. Each gets a fixture so the light has a source.
        /// </summary>
        public static Vector3 LampPosition(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(0f, 4.08f, 1.62f);
                case 1:
                    return new Vector3(-0.95f, 1.05f, 3.05f);
                case 2:
                    return new Vector3(1.78f, 3.4f, 0f);
                case 3:
                    return new Vector3(-1.78f, 3.4f, 0f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), index, "Lander lamps are 0..3.");
            }
        }

        private static void DescentStage(LowPolyMeshBuilder b)
        {
            float height = StageTop - StageBottom;
            b.Prism(At(0f, StageBottom + height * 0.5f, 0f), StageRadius, height, 8, PaletteSwatch.Honey);
            b.Prism(At(0f, StageBottom + 0.12f, 0f), StageRadius + 0.03f, 0.09f, 8, PaletteSwatch.Charcoal);
            b.Prism(At(0f, StageBottom + 0.82f, 0f), StageRadius + 0.03f, 0.06f, 8, PaletteSwatch.Charcoal);
            b.Frustum(At(0f, StageBottom - 0.32f, 0f), 0.78f, 0.46f, 0.66f, 12, PaletteSwatch.Charcoal, false);
            b.Prism(At(0f, StageBottom - 0.02f, 0f), 0.5f, 0.06f, 12, PaletteSwatch.Metal);
            for (int i = 1; i < 4; i++)
            {
                float yaw = i * 90f;
                Vector3 normal = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward;
                Vector3 centre = normal * (StageRadius * Mathf.Cos(Mathf.PI / 8f) + 0.02f) + Vector3.up * 2.05f;
                b.Box(At(centre, new Vector3(0f, yaw, 0f)), new Vector3(0.6f, 0.36f, 0.05f), PaletteSwatch.Metal,
                    0.015f);
            }
        }

        private static void Legs(LowPolyMeshBuilder b)
        {
            for (int i = 0; i < 4; i++)
            {
                float yaw = 45f + i * 90f;
                Vector3 direction = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward;
                Vector3 hip = direction * HipRadius + Vector3.up * 2.35f;
                Vector3 foot = direction * FootRadius + Vector3.up * 0.18f;
                Vector3 knee = Vector3.Lerp(hip, foot, 0.45f);
                RecipeKit.Strut(b, hip, foot, new Vector2(0.15f, 0.15f), PaletteSwatch.Metal);
                RecipeKit.Strut(b, direction * 1.55f + Vector3.up * 1.5f, knee, new Vector2(0.09f, 0.09f),
                    PaletteSwatch.Metal);
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                RecipeKit.Strut(b, direction * 1.5f + side * 0.75f + Vector3.up * 1.55f, knee,
                    new Vector2(0.07f, 0.07f), PaletteSwatch.Metal);
                RecipeKit.Strut(b, direction * 1.5f - side * 0.75f + Vector3.up * 1.55f, knee,
                    new Vector2(0.07f, 0.07f), PaletteSwatch.Metal);
                b.Lathe(At(direction * FootRadius), FootpadProfile, 10, PaletteSwatch.Metal);
                b.Prism(At(foot + Vector3.up * 0.1f), 0.1f, 0.2f, 8, PaletteSwatch.Charcoal);
            }
        }

        private static void Cabin(LowPolyMeshBuilder b)
        {
            float height = CabinTop - CabinBottom;
            b.Prism(At(0f, CabinBottom + height * 0.5f, 0f), CabinRadius, height, 8, PaletteSwatch.Enamel);
            b.Prism(At(0f, 4.36f, 0f), CabinRadius + 0.012f, 0.16f, 8, PaletteSwatch.WarmAccent);
            b.Prism(At(0f, CabinBottom + 0.05f, 0f), CabinRadius + 0.03f, 0.1f, 8, PaletteSwatch.Charcoal);

            for (int side = -1; side <= 1; side += 2)
            {
                float yaw = side * 45f;
                Vector3 normal = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward;
                Vector3 centre = normal * (CabinApothem + 0.03f) + Vector3.up * WindowHeight;
                b.Torus(At(centre, new Vector3(90f, yaw, 0f)), 0.3f, 0.05f, 12, 4, PaletteSwatch.Charcoal);
                Vector3 thruster = Rotation(new Vector3(0f, side * 90f, 0f)) * Vector3.forward * (CabinApothem + 0.1f);
                RcsQuad(b, thruster + Vector3.up * 4.0f, side * 90f);
            }

            b.Torus(At(new Vector3(0f, WindowHeight, -CabinApothem - 0.03f), new Vector3(90f, 180f, 0f)), 0.25f,
                0.045f, 12, 4, PaletteSwatch.Charcoal);

            float face = CabinApothem;
            b.Box(At(0f, HatchCentreY, face + 0.03f), new Vector3(0.98f, 1.45f, 0.06f), PaletteSwatch.Charcoal,
                0.02f);
            b.Box(At(0f, HatchCentreY, face + 0.07f), new Vector3(0.78f, 1.25f, 0.05f), PaletteSwatch.Metal, 0.02f);
            b.Torus(At(new Vector3(0f, HatchCentreY + 0.3f, face + 0.1f), AlongZ), 0.15f, 0.03f, 10, 4,
                PaletteSwatch.Charcoal);
            b.Box(At(0.28f, HatchCentreY - 0.12f, face + 0.11f), new Vector3(0.05f, 0.2f, 0.04f),
                PaletteSwatch.Charcoal, 0.01f);
        }

        /// <summary>Four small reaction-control nozzles on a block, facing out of the cabin along a yaw.</summary>
        private static void RcsQuad(LowPolyMeshBuilder b, Vector3 centre, float yaw)
        {
            Matrix4x4 frame = At(centre, new Vector3(0f, yaw, 0f));
            b.Box(frame, new Vector3(0.24f, 0.24f, 0.2f), PaletteSwatch.Metal, 0.02f);
            b.Cone(frame * At(new Vector3(0f, 0.2f, 0f)), 0.06f, 0.16f, 6, PaletteSwatch.Charcoal);
            b.Cone(frame * At(new Vector3(0f, -0.2f, 0f), new Vector3(180f, 0f, 0f)), 0.06f, 0.16f, 6,
                PaletteSwatch.Charcoal);
            b.Cone(frame * At(new Vector3(0.2f, 0f, 0f), new Vector3(0f, 0f, -90f)), 0.06f, 0.16f, 6,
                PaletteSwatch.Charcoal);
            b.Cone(frame * At(new Vector3(-0.2f, 0f, 0f), new Vector3(0f, 0f, 90f)), 0.06f, 0.16f, 6,
                PaletteSwatch.Charcoal);
        }

        /// <summary>The porch: the stage's top deck in front of the hatch, with a little railing.</summary>
        private static void Deck(LowPolyMeshBuilder b)
        {
            b.Prism(At(0f, StageTop + 0.02f, 0f), StageRadius + 0.05f, 0.06f, 8, PaletteSwatch.Metal);
            Vector3[] posts =
            {
                new Vector3(-1.35f, StageTop, 1.4f), new Vector3(-0.75f, StageTop, DeckEdge - 0.05f),
                new Vector3(0.75f, StageTop, DeckEdge - 0.05f), new Vector3(1.35f, StageTop, 1.4f),
            };
            for (int i = 0; i < posts.Length; i++)
            {
                RecipeKit.Rod(b, posts[i], posts[i] + Vector3.up * 0.75f, 0.03f, 6, PaletteSwatch.Metal);
            }

            RecipeKit.Rod(b, posts[0] + Vector3.up * 0.75f, posts[1] + Vector3.up * 0.75f, 0.025f, 6,
                PaletteSwatch.WarmAccent);
            RecipeKit.Rod(b, posts[2] + Vector3.up * 0.75f, posts[3] + Vector3.up * 0.75f, 0.025f, 6,
                PaletteSwatch.WarmAccent);
        }

        /// <summary>A ladder from the deck edge down to a warm welcome mat on the dust.</summary>
        private static void Ladder(LowPolyMeshBuilder b)
        {
            var topLeft = new Vector3(-0.32f, StageTop + 0.02f, DeckEdge);
            var bottomLeft = new Vector3(-0.32f, 0.035f, DeckEdge + 0.95f);
            var across = new Vector3(0.64f, 0f, 0f);
            RecipeKit.Rod(b, topLeft, bottomLeft, 0.035f, 6, PaletteSwatch.Metal);
            RecipeKit.Rod(b, topLeft + across, bottomLeft + across, 0.035f, 6, PaletteSwatch.Metal);
            for (int i = 1; i < 8; i++)
            {
                Vector3 rung = Vector3.Lerp(bottomLeft, topLeft, i / 8f);
                RecipeKit.Rod(b, rung, rung + across, 0.022f, 5, PaletteSwatch.Metal);
            }

            b.Box(At(0f, 0.012f, DeckEdge + 1.55f), new Vector3(1.1f, 0.024f, 0.7f), PaletteSwatch.WarmAccent);
            b.Box(At(0f, 0.026f, DeckEdge + 1.55f), new Vector3(0.9f, 0.006f, 0.5f), PaletteSwatch.Honey);
        }

        private static void Roof(LowPolyMeshBuilder b)
        {
            b.Lathe(At(0f, CabinTop, 0f), DomeProfile, 12, PaletteSwatch.Enamel);
            b.Prism(At(0f, CabinTop + 0.68f, 0f), 0.22f, 0.08f, 8, PaletteSwatch.Charcoal);

            var mastBase = new Vector3(-0.6f, CabinTop + 0.3f, -0.55f);
            Vector3 mastTop = mastBase + Vector3.up * 0.9f;
            RecipeKit.Rod(b, mastBase, mastTop, 0.04f, 6, PaletteSwatch.Metal);
            b.Lathe(At(mastTop + new Vector3(0f, 0.05f, 0.05f), new Vector3(70f, 25f, 0f)), DishProfile, 10,
                PaletteSwatch.Enamel);

            var whipBase = new Vector3(0.7f, CabinTop + 0.25f, -0.4f);
            RecipeKit.Rod(b, whipBase, whipBase + new Vector3(0.08f, 1.6f, -0.05f), 0.015f, 5, PaletteSwatch.Metal);
        }

        /// <summary>Signs of a long, quiet life: crates by a leg, a spare wheel leaning on a footpad.</summary>
        private static void Clutter(LowPolyMeshBuilder b)
        {
            b.Box(At(new Vector3(-2.75f, 0.25f, 1.45f), new Vector3(0f, 12f, 0f)), new Vector3(0.6f, 0.5f, 0.5f),
                PaletteSwatch.Enamel, 0.03f);
            b.Box(At(new Vector3(-2.7f, 0.68f, 1.42f), new Vector3(0f, -8f, 0f)), new Vector3(0.45f, 0.36f, 0.42f),
                PaletteSwatch.Metal, 0.03f);
            b.Box(At(new Vector3(-2.2f, 0.18f, 1.95f), new Vector3(0f, 30f, 0f)), new Vector3(0.36f, 0.36f, 0.36f),
                PaletteSwatch.Honey, 0.03f);
            Matrix4x4 wheel = At(new Vector3(2.6f, 0.345f, -2.05f), new Vector3(0f, 40f, -14f));
            b.Prism(wheel * Matrix4x4.Rotate(Rotation(AlongX)), 0.32f, 0.2f, 10, PaletteSwatch.Charcoal);
            b.Prism(wheel * At(new Vector3(0.11f, 0f, 0f), AlongX), 0.2f, 0.03f, 10, PaletteSwatch.Enamel);
        }

        /// <summary>
        /// A shade over a lamp position (the bulb itself is in the window mesh) on a post from the ground when the
        /// lamp is low, otherwise on a bracket from the nearest cabin wall.
        /// </summary>
        private static void LampFixture(LowPolyMeshBuilder b, Vector3 lamp)
        {
            b.Frustum(At(lamp + Vector3.up * 0.1f), 0.13f, 0.045f, 0.08f, 8, PaletteSwatch.Charcoal);
            if (lamp.y < StageBottom)
            {
                RecipeKit.Rod(b, new Vector3(lamp.x, 0f, lamp.z), lamp + Vector3.up * 0.06f, 0.03f, 6,
                    PaletteSwatch.Metal);
                b.Prism(At(lamp.x, 0.03f, lamp.z), 0.12f, 0.06f, 8, PaletteSwatch.Charcoal);
                return;
            }

            var flat = new Vector3(lamp.x, 0f, lamp.z);
            Vector3 wall = flat.normalized * (CabinApothem - 0.05f) + Vector3.up * (lamp.y + 0.14f);
            RecipeKit.Rod(b, wall, lamp + Vector3.up * 0.14f, 0.025f, 5, PaletteSwatch.Charcoal);
        }

        /// <summary>Tilly's perch: a padded landing disc on a post, its rim painted in 07's orange.</summary>
        private static void Perch(LowPolyMeshBuilder b)
        {
            var foot = new Vector3(TillyPerch.x, StageTop + 0.02f, TillyPerch.z);
            RecipeKit.Rod(b, foot, foot + Vector3.up * 0.44f, 0.035f, 8, PaletteSwatch.Metal);
            b.Prism(At(foot + Vector3.up * 0.02f), 0.08f, 0.04f, 8, PaletteSwatch.Charcoal);
            Vector3 pad = TillyPerch - Vector3.up * 0.06f;
            b.Prism(At(pad - Vector3.up * 0.02f), 0.3f, 0.04f, 12, PaletteSwatch.Metal);
            b.Torus(At(pad), 0.3f, 0.02f, 12, 4, PaletteSwatch.WarmAccent);
            Vector2[] cushion =
            {
                new Vector2(0f, 0f), new Vector2(0.21f, 0.004f), new Vector2(0.235f, 0.03f), new Vector2(0.2f, 0.056f),
                new Vector2(0f, 0.06f),
            };
            b.Lathe(At(pad), cushion, 12, PaletteSwatch.Honey);
            RecipeKit.Rod(b, foot + Vector3.up * 0.12f, foot + new Vector3(0.18f, 0.02f, -0.1f), 0.018f, 6,
                PaletteSwatch.Metal);
        }

        /// <summary>A little hand-painted "HOME" sign planted in the dust beside the ladder, a touch crooked.</summary>
        private static void HomeSign(LowPolyMeshBuilder b)
        {
            Matrix4x4 sign = At(new Vector3(1.25f, 0f, 3.15f), new Vector3(0f, -12f, 4f));
            b.Box(sign * At(0f, 0.46f, 0f), new Vector3(0.06f, 0.9f, 0.05f), PaletteSwatch.Metal);
            b.Box(sign * At(0f, 0.95f, 0.035f), new Vector3(0.92f, 0.36f, 0.04f), PaletteSwatch.Enamel, 0.012f);
            Glyphs.Write(b, sign * At(0f, 0.95f, 0.055f), "HOME", 0.2f, PaletteSwatch.WarmAccent, PaletteSwatch.Enamel);
        }

        /// <summary>A faded round mission patch on the right wall: a sage field, a cream moon, a star.</summary>
        private static void MissionPatch(LowPolyMeshBuilder b)
        {
            Matrix4x4 patch = At(new Vector3(CabinApothem + 0.02f, 3.55f, 0f), new Vector3(0f, 90f, 0f));
            b.Prism(patch * Matrix4x4.Rotate(Rotation(AlongZ)), 0.34f, 0.025f, 14, PaletteSwatch.Sage);
            b.Torus(patch * At(new Vector3(0f, 0f, 0.012f), AlongZ), 0.34f, 0.025f, 14, 3, PaletteSwatch.Honey);
            b.Prism(patch * At(new Vector3(-0.09f, 0.08f, 0.02f), AlongZ), 0.12f, 0.02f, 10, PaletteSwatch.Cream);
            b.Extrude(patch * At(0.12f, -0.1f, 0.022f), Glyphs.Star(0.08f, 0.035f), 0.01f, PaletteSwatch.Honey);
        }
    }
}
