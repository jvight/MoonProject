using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The crew's radio relay mast (docs/features/M3-06-relay-network.md), a sibling of the base's radio tower but
    /// built to read as a silhouette at 100-280 m: a dark tapering core inside four metal corner legs, a few chunky
    /// collars and cross braces, a faded orange band, a railed platform, a lantern on top and a small dish aimed home.
    /// At its foot a concrete footing carries a sage junction box facing the pad; three guy wires run back to stakes.
    /// Built in the mast's space: origin on the ground at the pad centre, +Z = toward home, +Y up. The mast stands at
    /// the back edge of the 3 m pad so the front of the pad stays open for 07 and for radio-hop arrivals.
    /// </summary>
    internal static class RelayMeshes
    {
        /// <summary>Radius of the flat pad World keeps clear at every relay anchor.</summary>
        public const float PadRadius = 3f;

        /// <summary>Height of the lamp's centre above the mast foot.</summary>
        public const float LampHeight = 7.95f;

        /// <summary>Foot of the mast on top of its footing (Mast pivot).</summary>
        public static readonly Vector3 MastFoot = new Vector3(0f, FootingTop, -2.2f);

        /// <summary>The dish's mount on the front of the mast, in the mast's space (Dish pivot).</summary>
        public static readonly Vector3 DishMount = new Vector3(0f, 6.1f, 0.4f);

        /// <summary>The lamp's centre in the mast's space (Lamp pivot).</summary>
        public static readonly Vector3 LampCentre = new Vector3(0f, LampHeight, 0f);

        /// <summary>The relay part's slot on the junction box front, +Z out (PartSocket).</summary>
        public static readonly Vector3 PartSocket = new Vector3(0f, 0.78f, BoxFront + 0.01f);

        /// <summary>The top of the junction box front, where 07's repair beam lands (BeamPoint).</summary>
        public static readonly Vector3 BeamPoint = new Vector3(0f, 1.12f, BoxFront + 0.01f);

        private const float FootingTop = 0.3f;
        private const float FootingDepth = 0.6f;
        private const float FootingHalf = 0.95f;
        private const float BoxFront = -2.2f + FootingHalf;
        private const float CoreHeight = 7.4f;
        private const float CoreBottomHalf = 0.44f;
        private const float CoreTopHalf = 0.18f;
        private const float LegOffset = 0.07f;
        private const float GuyCollar = 4.6f;

        private static readonly Vector2[] DishProfile =
        {
            new Vector2(0f, -0.18f), new Vector2(0.4f, -0.12f), new Vector2(0.75f, 0.1f), new Vector2(0.69f, 0.12f),
            new Vector2(0.36f, -0.04f), new Vector2(0f, -0.08f),
        };

        /// <summary>Guy wire stake <paramref name="index"/> (0..2): back-left, back-right and straight back.</summary>
        public static Vector3 GuyStake(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(-2.6f, 0f, MastFoot.z - 1.6f);
                case 1:
                    return new Vector3(2.6f, 0f, MastFoot.z - 1.6f);
                case 2:
                    return new Vector3(0f, 0f, MastFoot.z - 3.2f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), index, "A mast has guy stakes 0..2.");
            }
        }

        /// <summary>
        /// Footing, junction box and guy wires (root space). Restored: wires taut to the mast's collar and the box
        /// door shut on the slot. Broken: wires slack and one snapped in the dust, the door hanging open on an empty
        /// slot.
        /// </summary>
        public static LowPolyMeshBuilder Base(bool broken)
        {
            var b = new LowPolyMeshBuilder(900);
            b.Box(At(0f, (FootingTop - FootingDepth) * 0.5f, MastFoot.z),
                new Vector3(FootingHalf * 2f, FootingTop + FootingDepth, FootingHalf * 2f), PaletteSwatch.Metal, 0.05f);
            b.Box(At(0f, FootingTop + 0.02f, MastFoot.z), new Vector3(1.3f, 0.04f, 1.3f), PaletteSwatch.Charcoal);
            JunctionBox(b, broken);
            for (int i = 0; i < 3; i++)
            {
                Vector3 stake = GuyStake(i);
                b.Box(At(stake + Vector3.up * -0.15f), new Vector3(0.28f, 0.6f, 0.28f), PaletteSwatch.Charcoal, 0.03f);
                b.Torus(At(stake + Vector3.up * 0.18f, AlongX), 0.07f, 0.02f, 8, 3, PaletteSwatch.Metal);
                Vector3 top = stake + Vector3.up * 0.2f;
                Vector3 collar = MastFoot + Collar(i, GuyCollar);
                if (!broken)
                {
                    RecipeKit.Rod(b, top, collar, 0.022f, 5, PaletteSwatch.Metal);
                }
                else if (i < 2)
                {
                    Slack(b, top, new Vector3(Mathf.Sign(stake.x) * 1.1f, 0f, MastFoot.z - 0.3f));
                }
                else
                {
                    Coil(b, top + new Vector3(0.6f, -0.18f, 0.5f));
                }
            }

            return b;
        }

        /// <summary>The mast from its foot (origin) to the lantern's cap; the lamp glass is the Lamp node.</summary>
        public static LowPolyMeshBuilder Mast(bool broken)
        {
            var b = new LowPolyMeshBuilder(1600);
            Matrix4x4 square = Matrix4x4.Rotate(Rotation(new Vector3(0f, 45f, 0f)));
            b.Frustum(At(0f, CoreHeight * 0.5f, 0f) * square, CoreBottomHalf * Mathf.Sqrt(2f),
                CoreTopHalf * Mathf.Sqrt(2f), CoreHeight, 4, PaletteSwatch.Charcoal);
            for (int corner = 0; corner < 4; corner++)
            {
                RecipeKit.Strut(b, Leg(corner, 0f), Leg(corner, CoreHeight), new Vector2(0.13f, 0.13f),
                    PaletteSwatch.Metal);
            }

            float[] collars = { 1.9f, 3.8f, GuyCollar, 6.1f };
            foreach (float y in collars)
            {
                float half = Half(y) + LegOffset + 0.05f;
                b.Box(At(0f, y, 0f), new Vector3(half * 2f, 0.16f, half * 2f), PaletteSwatch.Metal, 0.02f);
            }

            for (int face = 0; face < 4; face++)
            {
                Braces(b, face, 0.15f, collars[0], broken && face == 1);
                Braces(b, face, collars[0], collars[1], false);
            }

            b.Prism(At(0f, 5.45f, 0f) * square, (Half(5.45f) + 0.012f) * Mathf.Sqrt(2f), 0.32f, 4,
                PaletteSwatch.WarmAccent, false);
            RecipeKit.Rod(b, new Vector3(0f, 0.25f, Half(0.25f) + 0.04f), new Vector3(0f, DishMount.y - 0.1f,
                Half(DishMount.y) + 0.04f), 0.035f, 5, PaletteSwatch.Charcoal);
            Platform(b, broken);
            Lantern(b);
            return b;
        }

        /// <summary>The relay dish (origin on its mount on the mast front), facing +Z, home.</summary>
        public static LowPolyMeshBuilder Dish()
        {
            var b = new LowPolyMeshBuilder(400);
            b.Box(At(0f, 0f, 0.06f), new Vector3(0.16f, 0.22f, 0.12f), PaletteSwatch.Charcoal, 0.02f);
            Matrix4x4 bowl = At(new Vector3(0f, 0f, 0.3f), AlongZ);
            b.Lathe(bowl, DishProfile, 12, PaletteSwatch.Enamel);
            b.Torus(bowl * At(0f, 0.11f, 0f), 0.72f, 0.025f, 12, 3, PaletteSwatch.Metal);
            RecipeKit.Rod(b, new Vector3(0f, 0f, 0.15f), new Vector3(0f, 0f, 0.85f), 0.025f, 5, PaletteSwatch.Metal);
            b.Prism(At(new Vector3(0f, 0f, 0.88f), AlongZ), 0.06f, 0.1f, 6, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The lamp's glass (origin at its centre): cold and dark until gameplay lights it warm.</summary>
        public static LowPolyMeshBuilder Lamp()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Prism(Matrix4x4.identity, 0.27f, 0.6f, 8, PaletteSwatch.SignalGlass);
            return b;
        }

        /// <summary>
        /// The relay part as a pickup (built upright on y = 0; the builder pivots it at the centre of mass): a sage
        /// relay cassette with a carry handle, three amber valves behind a window, an amber status strip, contact pins
        /// at the back and a honey tag.
        /// </summary>
        public static LowPolyMeshBuilder Module()
        {
            var b = new LowPolyMeshBuilder(500);
            b.Box(At(0f, 0.09f, 0f), new Vector3(0.3f, 0.18f, 0.14f), PaletteSwatch.Sage, 0.015f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.16f, 0.09f, 0f), new Vector3(0.025f, 0.2f, 0.16f), PaletteSwatch.Charcoal, 0.006f);
            }

            b.Box(At(0f, 0.11f, 0.072f), new Vector3(0.22f, 0.09f, 0.008f), PaletteSwatch.Charcoal);
            Vector2[] valve =
            {
                new Vector2(0f, 0f), new Vector2(0.022f, 0.002f), new Vector2(0.024f, 0.05f),
                new Vector2(0.016f, 0.066f), new Vector2(0f, 0.07f),
            };
            for (int i = 0; i < 3; i++)
            {
                b.Lathe(At((i - 1) * 0.065f, 0.18f, 0.01f), valve, 8, PaletteSwatch.LampGlass);
                b.Prism(At((i - 1) * 0.065f, 0.185f, 0.01f), 0.03f, 0.012f, 8, PaletteSwatch.Metal);
            }

            b.Box(At(0f, 0.04f, 0.074f), new Vector3(0.16f, 0.018f, 0.008f), PaletteSwatch.WarmLamp);
            RecipeKit.Rod(b, new Vector3(-0.13f, 0.18f, -0.03f), new Vector3(-0.13f, 0.27f, -0.03f), 0.01f, 5,
                PaletteSwatch.Metal);
            RecipeKit.Rod(b, new Vector3(0.13f, 0.18f, -0.03f), new Vector3(0.13f, 0.27f, -0.03f), 0.01f, 5,
                PaletteSwatch.Metal);
            RecipeKit.Rod(b, new Vector3(-0.13f, 0.27f, -0.03f), new Vector3(0.13f, 0.27f, -0.03f), 0.012f, 5,
                PaletteSwatch.Charcoal);
            for (int pin = 0; pin < 4; pin++)
            {
                b.Prism(At(new Vector3((pin - 1.5f) * 0.05f, 0.06f, -0.08f), AlongZ), 0.008f, 0.03f, 5,
                    PaletteSwatch.Metal);
            }

            b.Box(At(0.09f, 0.21f, -0.06f), new Vector3(0.012f, 0.03f, 0.01f), PaletteSwatch.Charcoal);
            b.Box(At(new Vector3(0.09f, 0.245f, -0.06f), new Vector3(0f, 0f, -12f)), new Vector3(0.05f, 0.035f, 0.008f),
                PaletteSwatch.Honey, 0.003f);
            return b;
        }

        /// <summary>A collar corner the guy wire of stake <paramref name="index"/> ties to (mast space).</summary>
        private static Vector3 Collar(int index, float y)
        {
            float half = Half(y) + LegOffset;
            switch (index)
            {
                case 0:
                    return new Vector3(-half, y, -half);
                case 1:
                    return new Vector3(half, y, -half);
                default:
                    return new Vector3(0f, y, -half);
            }
        }

        private static float Half(float y)
        {
            return Mathf.Lerp(CoreBottomHalf, CoreTopHalf, y / CoreHeight);
        }

        private static Vector3 Leg(int corner, float y)
        {
            float half = Half(y) + LegOffset;
            float x = corner == 0 || corner == 3 ? half : -half;
            float z = corner < 2 ? half : -half;
            return new Vector3(x, y, z);
        }

        /// <summary>An X brace across face <paramref name="face"/> (0 front, then round) between two heights.</summary>
        private static void Braces(LowPolyMeshBuilder b, int face, float bottom, float top, bool sprung)
        {
            int next = (face + 1) % 4;
            RecipeKit.Strut(b, Leg(face, bottom), Leg(next, top), new Vector2(0.08f, 0.07f), PaletteSwatch.Metal);
            if (sprung)
            {
                Vector3 from = Leg(next, bottom);
                Vector3 to = Vector3.Lerp(from, Leg(face, top), 0.45f) + new Vector3(0.25f, -0.35f, 0.3f);
                RecipeKit.Strut(b, from, to, new Vector2(0.08f, 0.07f), PaletteSwatch.Metal);
                return;
            }

            RecipeKit.Strut(b, Leg(next, bottom), Leg(face, top), new Vector2(0.08f, 0.07f), PaletteSwatch.Metal);
        }

        /// <summary>The railed platform under the lantern, its orange rail missing a side once the mast fell.</summary>
        private static void Platform(LowPolyMeshBuilder b, bool broken)
        {
            const float y = 7.05f;
            const float half = 0.6f;
            b.Box(At(0f, y, 0f), new Vector3(half * 2f, 0.08f, half * 2f), PaletteSwatch.Charcoal, 0.01f);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 post = PlatformCorner(corner, y, half);
                Vector3 nextPost = PlatformCorner((corner + 1) % 4, y, half);
                RecipeKit.Rod(b, post, post + Vector3.up * 0.42f, 0.025f, 5, PaletteSwatch.Metal);
                if (!broken || corner != 2)
                {
                    RecipeKit.Rod(b, post + Vector3.up * 0.4f, nextPost + Vector3.up * 0.4f, 0.03f, 5,
                        PaletteSwatch.WarmAccent);
                }
            }
        }

        private static Vector3 PlatformCorner(int corner, float y, float half)
        {
            float x = corner == 0 || corner == 3 ? half : -half;
            float z = corner < 2 ? half : -half;
            return new Vector3(x, y, z);
        }

        /// <summary>The lantern around the lamp glass: charcoal base, four cage bars, a cap, a short whip.</summary>
        private static void Lantern(LowPolyMeshBuilder b)
        {
            b.Prism(At(0f, LampHeight - 0.38f, 0f), 0.34f, 0.16f, 8, PaletteSwatch.Charcoal);
            b.Prism(At(0f, LampHeight - 0.62f, 0f), 0.26f, 0.36f, 8, PaletteSwatch.Metal);
            for (int bar = 0; bar < 4; bar++)
            {
                float angle = bar * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var offset = new Vector3(0.3f * Mathf.Sin(angle), 0f, 0.3f * Mathf.Cos(angle));
                RecipeKit.Rod(b, LampCentre + offset + Vector3.down * 0.32f, LampCentre + offset + Vector3.up * 0.34f,
                    0.025f, 5, PaletteSwatch.Metal);
            }

            b.Frustum(At(0f, LampHeight + 0.4f, 0f), 0.38f, 0.12f, 0.16f, 8, PaletteSwatch.Charcoal);
            RecipeKit.Rod(b, LampCentre + Vector3.up * 0.46f, LampCentre + new Vector3(0.05f, 1.1f, -0.04f), 0.018f,
                5, PaletteSwatch.Metal);
        }

        /// <summary>The sage junction box on the footing's front: a door over the part's slot and a dial.</summary>
        private static void JunctionBox(LowPolyMeshBuilder b, bool broken)
        {
            const float depth = 0.45f;
            var centre = new Vector3(0f, FootingTop + 0.5f, BoxFront - depth * 0.5f);
            b.Box(At(centre), new Vector3(0.8f, 1f, depth), PaletteSwatch.Sage, 0.03f);
            b.Box(At(0f, FootingTop + 1.02f, centre.z), new Vector3(0.86f, 0.05f, depth + 0.06f),
                PaletteSwatch.Charcoal, 0.01f);
            b.Box(At(PartSocket + Vector3.back * 0.008f), new Vector3(0.36f, 0.24f, 0.012f), PaletteSwatch.Charcoal);
            if (broken)
            {
                Matrix4x4 door = At(new Vector3(0.38f, PartSocket.y, BoxFront + 0.02f), new Vector3(0f, -110f, 0f));
                b.Box(door * At(-0.2f, 0f, 0f), new Vector3(0.4f, 0.3f, 0.02f), PaletteSwatch.Sage, 0.005f);
            }
            else
            {
                b.Box(At(PartSocket + Vector3.forward * 0.004f), new Vector3(0.4f, 0.3f, 0.012f), PaletteSwatch.Sage,
                    0.004f);
                b.Box(At(PartSocket + new Vector3(0.14f, 0f, 0.012f)), new Vector3(0.03f, 0.08f, 0.012f),
                    PaletteSwatch.Metal);
            }

            b.Prism(At(new Vector3(-0.22f, FootingTop + 0.82f, BoxFront + 0.01f), AlongZ), 0.06f, 0.02f, 10,
                PaletteSwatch.Cream);
            b.Box(At(BeamPoint + Vector3.back * 0.005f), new Vector3(0.5f, 0.05f, 0.01f), PaletteSwatch.WarmAccent);
        }

        /// <summary>
        /// A slack guy wire: off its stake <paramref name="from"/> and lying loose in the dust towards the footing,
        /// ending at <paramref name="end"/> on the ground.
        /// </summary>
        private static void Slack(LowPolyMeshBuilder b, Vector3 from, Vector3 end)
        {
            const float lying = 0.03f;
            Vector3 drop = Vector3.Lerp(from, end, 0.2f);
            drop.y = lying;
            Vector3 kink = Vector3.Lerp(from, end, 0.6f) + Vector3.Cross(Vector3.up, end - from).normalized * 0.35f;
            kink.y = lying;
            end.y = lying;
            RecipeKit.Rod(b, from, drop, 0.02f, 5, PaletteSwatch.Metal);
            RecipeKit.Rod(b, drop, kink, 0.02f, 5, PaletteSwatch.Metal);
            RecipeKit.Rod(b, kink, end, 0.02f, 5, PaletteSwatch.Metal);
        }

        /// <summary>A snapped guy wire coiled in the dust beside its stake.</summary>
        private static void Coil(LowPolyMeshBuilder b, Vector3 centre)
        {
            b.Torus(At(centre + Vector3.up * 0.02f), 0.32f, 0.02f, 12, 3, PaletteSwatch.Metal);
            b.Torus(At(centre + new Vector3(0.08f, 0.05f, 0.04f), new Vector3(8f, 0f, 6f)), 0.26f, 0.02f, 12, 3,
                PaletteSwatch.Metal);
        }
    }
}
