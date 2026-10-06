using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Tilly, Ines's tiny survey drone (docs/STORY.md): a round cream bun of a body on four little legs, four ducted
    /// rotors on short arms, one big curious lens in 07's visual language but younger and brighter (cyan glass with a
    /// catchlight), a whip antenna, a scuffed survey-orange stripe, a tiny "INES" sticker and three part lamps on her
    /// crown. Built in the model's space: origin on the ground between the feet, +Z = gaze, +Y up.
    /// </summary>
    internal static class TillyMeshes
    {
        public const int RotorCount = 4;
        public const int PartLampCount = 3;

        /// <summary>The lens centre (Eye pivot); the eye looks along +Z.</summary>
        public static readonly Vector3 EyeCentre = new Vector3(0f, 0.27f, 0.19f);

        /// <summary>Base of the whip antenna on the top-back of the body.</summary>
        public static readonly Vector3 AntennaBase = new Vector3(0.06f, 0.4f, -0.075f);

        /// <summary>Hook under the belly where a tether or 07's repair beam attaches.</summary>
        public static readonly Vector3 TetherPoint = new Vector3(0f, 0.07f, 0f);

        private const float LensFront = 0.028f;
        private const float RotorHeight = 0.3f;
        private const float RotorRadius = 0.27f;
        private const float DuctInner = 0.078f;
        private const float DuctOuter = 0.093f;
        private const float BladeLength = 0.072f;

        private static readonly Vector2[] BodyProfile =
        {
            new Vector2(0f, 0.1f), new Vector2(0.09f, 0.105f), new Vector2(0.15f, 0.135f), new Vector2(0.182f, 0.19f),
            new Vector2(0.188f, 0.25f), new Vector2(0.178f, 0.31f), new Vector2(0.145f, 0.365f),
            new Vector2(0.085f, 0.405f), new Vector2(0f, 0.42f),
        };

        private static readonly Vector2[] DuctProfile =
        {
            new Vector2(DuctInner, -0.03f), new Vector2(DuctOuter, -0.03f), new Vector2(DuctOuter + 0.004f, 0.02f),
            new Vector2(DuctInner, 0.025f), new Vector2(DuctInner, -0.03f),
        };

        private static readonly Vector2[] LensProfile =
        {
            new Vector2(0.085f, -0.006f), new Vector2(0.072f, 0.012f), new Vector2(0.042f, 0.024f),
            new Vector2(0f, LensFront),
        };

        /// <summary>Rotor pivot <paramref name="index"/>: 0 FL, 1 FR, 2 RL, 3 RR (they spin about +Y).</summary>
        public static Vector3 RotorCentre(int index)
        {
            return ArmDirection(index) * RotorRadius + Vector3.up * RotorHeight;
        }

        /// <summary>Part lamp <paramref name="index"/> (0..2), a row across the crown.</summary>
        public static Vector3 PartLampPosition(int index)
        {
            if (index < 0 || index >= PartLampCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), index, "Tilly has part lamps 0..2.");
            }

            return new Vector3((index - 1) * 0.052f, 0.418f, -0.012f);
        }

        public static LowPolyMeshBuilder Body()
        {
            var b = new LowPolyMeshBuilder(1800);
            MeshRange shell = b.Lathe(Matrix4x4.identity, BodyProfile, 12, PaletteSwatch.Enamel);
            b.RepaintFacing(shell, Vector3.down, 0.55f, PaletteSwatch.Charcoal);
            MeshRange stripe = b.Prism(At(0f, 0.2f, 0f), 0.191f, 0.045f, 12, PaletteSwatch.WarmAccent, false);
            b.RepaintFacing(stripe, new Vector3(-0.8f, 0f, -0.6f), 0.95f, PaletteSwatch.Metal);
            b.RepaintFacing(stripe, new Vector3(0.6f, 0f, -0.8f), 0.97f, PaletteSwatch.Enamel);

            b.Torus(At(0f, 0.355f, 0f), 0.158f, 0.007f, 12, 3, PaletteSwatch.Charcoal);
            b.Torus(At(TetherPoint + new Vector3(0f, 0.018f, 0f), AlongX), 0.022f, 0.007f, 8, 3, PaletteSwatch.Metal);

            for (int i = 0; i < RotorCount; i++)
            {
                Vector3 direction = ArmDirection(i);
                Vector3 centre = RotorCentre(i);
                RecipeKit.Strut(b, direction * 0.13f + Vector3.up * 0.27f, centre - direction * DuctOuter,
                    new Vector2(0.034f, 0.03f), PaletteSwatch.Metal);
                b.Lathe(At(centre), DuctProfile, 12, PaletteSwatch.Enamel);
                b.Prism(At(centre - Vector3.up * 0.022f), 0.022f, 0.034f, 8, PaletteSwatch.Charcoal);
                for (int spoke = 0; spoke < 3; spoke++)
                {
                    float angle = spoke * 120f * Mathf.Deg2Rad;
                    var outward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    Vector3 from = centre + outward * 0.02f - Vector3.up * 0.02f;
                    Vector3 to = centre + outward * (DuctInner + 0.004f) - Vector3.up * 0.02f;
                    b.Box(Along(from, to), new Vector3(0.008f, 0.006f, Vector3.Distance(from, to)),
                        PaletteSwatch.Charcoal);
                }

                Vector3 hip = direction * 0.075f + Vector3.up * 0.115f;
                Vector3 foot = direction * 0.115f + Vector3.up * 0.016f;
                RecipeKit.Rod(b, hip, foot, 0.011f, 5, PaletteSwatch.Metal);
                b.Prism(At(foot - Vector3.up * 0.008f), 0.024f, 0.016f, 8, PaletteSwatch.Charcoal);
            }

            for (int i = 0; i < PartLampCount; i++)
            {
                b.Prism(At(PartLampPosition(i) - Vector3.up * 0.008f), 0.026f, 0.012f, 8, PaletteSwatch.Charcoal);
            }

            InesSticker(b);
            return b;
        }

        /// <summary>The big curious lens: barrel, bezel, glass dome, catchlight (origin: lens centre).</summary>
        public static LowPolyMeshBuilder Eye()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Prism(At(new Vector3(0f, 0f, -0.04f), AlongZ), 0.1f, 0.07f, 16, PaletteSwatch.Charcoal);
            b.Torus(At(new Vector3(0f, 0f, 0.002f), AlongZ), 0.094f, 0.012f, 16, 4, PaletteSwatch.Metal);
            b.Lathe(At(Vector3.zero, AlongZ), LensProfile, 16, PaletteSwatch.EyeGlass);
            b.Icosphere(At(-0.028f, 0.03f, LensFront - 0.006f), 0.013f, 1, PaletteSwatch.PilotLight);
            return b;
        }

        /// <summary>Two-blade rotor around its hub (origin), spinning about +Y.</summary>
        public static LowPolyMeshBuilder Rotor()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Prism(Matrix4x4.identity, 0.016f, 0.02f, 8, PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(new Vector3(side * BladeLength * 0.5f, 0.004f, 0f), new Vector3(side * 10f, 0f, 0f)),
                    new Vector3(BladeLength, 0.004f, 0.022f), PaletteSwatch.Metal);
            }

            return b;
        }

        /// <summary>The rotor that took the fall: one blade folded down, the other snapped short.</summary>
        public static LowPolyMeshBuilder BentRotor()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Prism(Matrix4x4.identity, 0.016f, 0.02f, 8, PaletteSwatch.Metal);
            b.Box(At(new Vector3(0.03f, -0.018f, 0f), new Vector3(0f, 0f, -38f)),
                new Vector3(BladeLength, 0.004f, 0.022f), PaletteSwatch.Metal);
            Vector2[] stub =
            {
                new Vector2(0f, -0.011f), new Vector2(0.032f, -0.011f), new Vector2(0.026f, 0f),
                new Vector2(0.036f, 0.011f), new Vector2(0f, 0.011f),
            };
            b.Extrude(At(new Vector3(-0.008f, 0.004f, 0f), new Vector3(90f, 180f, 0f)), stub, 0.004f,
                PaletteSwatch.Metal);
            return b;
        }

        /// <summary>Whip antenna from its base (origin) with a little orange ball tip.</summary>
        public static LowPolyMeshBuilder Antenna()
        {
            return Whip(new Vector3(0.012f, 0.11f, -0.02f), new Vector3(0.028f, 0.2f, -0.065f));
        }

        /// <summary>The antenna after the fall: kinked over and drooping.</summary>
        public static LowPolyMeshBuilder BentAntenna()
        {
            return Whip(new Vector3(0.005f, 0.075f, -0.01f), new Vector3(0.09f, 0.095f, -0.07f));
        }

        public static LowPolyMeshBuilder PartLamp()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Icosphere(Matrix4x4.identity, 0.02f, 1, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// Dust settled on whatever faces up once she lies in <paramref name="pose"/> (her resting pose): those faces
        /// turn dusty and a few soft drifts sit on the body. Applied to a body built by <see cref="Body"/>.
        /// </summary>
        public static void Dust(LowPolyMeshBuilder body, Quaternion pose)
        {
            Vector3 up = Quaternion.Inverse(pose) * Vector3.up;
            body.RepaintFacing(body.RangeFrom(0), up, 0.72f, PaletteSwatch.DustLight);
            Vector3 side = Vector3.Cross(up, Vector3.forward).normalized;
            Vector3 centre = Vector3.up * 0.25f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 spot = centre + up * 0.17f + side * ((i - 1) * 0.08f) + Vector3.forward * ((i - 1) * -0.04f);
                Matrix4x4 frame = Along(spot - up * 0.01f, spot + up * 0.01f) * Matrix4x4.Rotate(Rotation(AlongZ));
                body.Icosphere(frame * Matrix4x4.Scale(new Vector3(1f, 0.3f, 1f)), 0.04f - i * 0.006f, 1,
                    PaletteSwatch.DustLight);
            }
        }

        private static LowPolyMeshBuilder Whip(Vector3 kink, Vector3 tip)
        {
            var b = new LowPolyMeshBuilder(120);
            b.Prism(At(0f, 0.01f, 0f), 0.016f, 0.02f, 8, PaletteSwatch.Charcoal);
            RecipeKit.Rod(b, new Vector3(0f, 0.015f, 0f), kink, 0.0055f, 5, PaletteSwatch.Metal);
            RecipeKit.Rod(b, kink, tip, 0.005f, 5, PaletteSwatch.Metal);
            b.Icosphere(At(kink), 0.007f, 0, PaletteSwatch.Metal);
            b.Icosphere(At(tip), 0.017f, 1, PaletteSwatch.WarmAccent);
            return b;
        }

        /// <summary>A tiny cream sticker on her right side with "INES" in charcoal block letters.</summary>
        private static void InesSticker(LowPolyMeshBuilder b)
        {
            Matrix4x4 sticker = At(new Vector3(0.186f, 0.29f, -0.02f), new Vector3(-8f, 90f, 0f));
            b.Box(sticker, new Vector3(0.1f, 0.034f, 0.004f), PaletteSwatch.Cream);
            Glyphs.Write(b, sticker * At(0f, 0f, 0.002f), "INES", 0.022f, PaletteSwatch.Charcoal, PaletteSwatch.Cream);
        }

        private static Vector3 ArmDirection(int index)
        {
            if (index < 0 || index >= RotorCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), index, "Tilly has rotors 0..3.");
            }

            float x = index % 2 == 0 ? -1f : 1f;
            float z = index < 2 ? 1f : -1f;
            return new Vector3(x, 0f, z).normalized;
        }
    }
}
